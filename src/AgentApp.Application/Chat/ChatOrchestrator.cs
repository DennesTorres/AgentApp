using System.Text.Json;
using AgentApp.Application.Context;
using AgentApp.Application.Filters;
using AgentApp.Application.Gates;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Orchestration;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Context;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;
using AgentApp.Domain.Providers;
using AgentApp.Domain.Reasoning;
using AgentApp.Domain.Rules;
using AgentApp.Domain.Settings;
using Microsoft.Extensions.AI;

namespace AgentApp.Application.Chat;

public record InitializeResult(string? ActiveProjectName, string? InitialMessage);

public class ChatOrchestrator
{
    private readonly CapabilityDispatcher _dispatcher;
    private readonly IResponsePreparationService _responsePrep;
    private readonly IActionProviderRegistry _actionRegistry;
    private readonly IFilePermissionGate _fileGate;
    private readonly IScaffoldService _scaffoldService;
    private readonly ProjectService _projectService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IOnboardingService _onboardingService;
    private readonly IAgentContextService _contextService;
    private readonly ISystemMessageProvider[] _systemMessageProviders;
    private readonly IProjectSettingsRepository _projectSettingsRepo;
    private readonly ISessionRepository _sessionRepository;
    private readonly ContextAssembler _contextAssembler;
    private readonly IReasoningTraceRepository _reasoningTraceRepo;
    private readonly FilterPipeline _filterPipeline;
    private readonly GateValidator _gateValidator;
    private readonly IGateRuleRepository _gateRuleRepo;
    private readonly ContextWindowManager _contextWindowManager;
    private readonly RollingWindowManager _rollingWindowManager;
    private readonly BoardService _boardService;
    private readonly List<ChatTurn> _history = [];
    private readonly ConversationContext _conversationContext = new();
    private Guid? _currentSessionId;
    private Guid? _activeStoryId;

    public ChatOrchestrator(
        CapabilityDispatcher dispatcher,
        IResponsePreparationService responsePrep,
        IActionProviderRegistry actionRegistry,
        IFilePermissionGate fileGate,
        IScaffoldService scaffoldService,
        ProjectService projectService,
        ISettingsRepository settingsRepository,
        IOnboardingService onboardingService,
        IAgentContextService contextService,
        ISystemMessageProvider[] systemMessageProviders,
        IProjectSettingsRepository projectSettingsRepo,
        ISessionRepository sessionRepository,
        ContextAssembler contextAssembler,
        IReasoningTraceRepository reasoningTraceRepo,
        FilterPipeline filterPipeline,
        GateValidator gateValidator,
        IGateRuleRepository gateRuleRepo,
        ContextWindowManager contextWindowManager,
        RollingWindowManager rollingWindowManager,
        BoardService boardService)
    {
        _dispatcher = dispatcher;
        _responsePrep = responsePrep;
        _actionRegistry = actionRegistry;
        _fileGate = fileGate;
        _scaffoldService = scaffoldService;
        _projectService = projectService;
        _settingsRepository = settingsRepository;
        _onboardingService = onboardingService;
        _contextService = contextService;
        _systemMessageProviders = systemMessageProviders;
        _projectSettingsRepo = projectSettingsRepo;
        _sessionRepository = sessionRepository;
        _contextAssembler = contextAssembler;
        _reasoningTraceRepo = reasoningTraceRepo;
        _filterPipeline = filterPipeline;
        _gateValidator = gateValidator;
        _gateRuleRepo = gateRuleRepo;
        _contextWindowManager = contextWindowManager;
        _rollingWindowManager = rollingWindowManager;
        _boardService = boardService;
    }

    // ── Session tracking ──────────────────────────────────────────────────────

    public void SetCurrentSession(Guid? sessionId) => _currentSessionId = sessionId;

    public void SetActiveStory(Guid? storyId) => _activeStoryId = storyId;

    // ── Initialization ────────────────────────────────────────────────────────

    public async Task<InitializeResult> InitializeAsync(Guid? sessionId = null)
    {
        _currentSessionId = sessionId;
        _conversationContext.Reset();

        // US-184/US-185: load the project linked to this specific session, not most-recent globally
        Project? project = null;
        if (sessionId.HasValue)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId.Value);
            if (session?.ProjectId.HasValue == true)
                project = await _projectService.GetProjectByIdAsync(session.ProjectId.Value);
        }

        if (project is not null)
        {
            var agentFolder = _scaffoldService.GetAgentFolderPath(project.FolderName);
            _contextService.SetProject(project, agentFolder, project.ProjectFolderPath);
            if (!string.IsNullOrEmpty(project.ProjectFolderPath))
                _fileGate.SetProjectRoots(agentFolder, project.ProjectFolderPath);
            // US-189: pre-populate always-allowed paths from persisted project settings
            var projectSettings = await _projectSettingsRepo.GetByProjectIdAsync(project.Id);
            if (projectSettings is not null)
                foreach (var path in projectSettings.AlwaysAllowedPaths)
                    _fileGate.GrantReadAccess(path);
            return new InitializeResult(project.Name, null);
        }

        // No project linked to this session: trigger greeting from InitializationPromptProvider
        var greeting = await GetGreetingAsync();
        return new InitializeResult(null, greeting);
    }

    private async Task<string?> GetGreetingAsync()
    {
        var context = _contextService.GetCurrent();
        var systemSections = _systemMessageProviders
            .Where(p => p.IsApplicable(context))
            .Select(p => p.GetSection(context))
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var systemMessage = string.Join("\n\n", systemSections);
        if (string.IsNullOrWhiteSpace(systemMessage)) return null;

        var payload = new Dictionary<string, object>
        {
            ["history"] = new List<ChatTurn> { new(ChatTurnRole.User, "[SESSION_STARTED]", DateTimeOffset.UtcNow) },
            ["tools"] = new List<AITool>(),
            ["systemMessage"] = systemMessage
        };

        var request = ProviderRequest.Create(ProviderCapability.ModelCall, payload);
        var response = await _dispatcher.SendAsync(request);
        if (!response.Success) return null;

        var rawText = (string)response.Result["text"];
        var (displayText, _) = _responsePrep.Prepare(rawText);
        return string.IsNullOrWhiteSpace(displayText) ? null : displayText;
    }

    // ── Core chat ─────────────────────────────────────────────────────────────

    public async Task<ChatServiceResult> SendAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatTurn(ChatTurnRole.User, userMessage, DateTimeOffset.UtcNow));

        // Assemble system message (US-135b + Epic 2 ContextAssembler)
        var context = _contextService.GetCurrent();
        var projectId = context.CurrentProject?.Id;

        // Epic 2: load MD files from ContextAssembler (core file + trigger enrichment)
        var orchestratorContext = await _contextAssembler.AssembleBaseContextAsync(projectId);
        var mdFileSections = orchestratorContext.LoadedFiles
            .Select(f => f.Content)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        if (orchestratorContext.TriggersIndex is not null &&
            await _contextAssembler.NeedsEnrichmentCallAsync(userMessage, projectId))
        {
            var triggeredNames = userMessage
                .Split([' ', '\t', '\n', '\r', ',', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
                .Select(w => orchestratorContext.TriggersIndex.GetFileNameForTrigger(w))
                .Where(n => n is not null)
                .Distinct()
                .Cast<string>();
            var enrichedFiles = await _contextAssembler.LoadFilesForNamesAsync(triggeredNames, projectId);
            mdFileSections.AddRange(enrichedFiles
                .Select(f => f.Content)
                .Where(c => !string.IsNullOrWhiteSpace(c)));
        }

        var providerSections = _systemMessageProviders
            .Where(p => p.IsApplicable(context))
            .Select(p => p.GetSection(context))
            .Where(s => !string.IsNullOrWhiteSpace(s));

        var systemMessage = string.Join("\n\n", mdFileSections.Concat(providerSections));

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(
                (string path) => ReadFileToolAsync(path),
                "read_file",
                "Read the contents of a file at the given path. Returns the file content or an access-denied error."),
            AIFunctionFactory.Create(
                (string path, string content) => WriteFileToolAsync(path, content),
                "write_file",
                "Write content to a file at the given path. Returns success or an error."),
            AIFunctionFactory.Create(
                (string path) => ListDirectoryToolAsync(path),
                "list_directory",
                "List files and subdirectories in a directory. Returns a JSON array of entry names.")
        };

        // Epic 3: load gate rules once before the retry loop
        var gateRules = await _gateRuleRepo.GetAllAsync();
        var activeGateRule = gateRules.FirstOrDefault();
        const int MaxGateRetries = 1;
        int gateRetryCount = 0;

        string rawText;
        while (true)
        {
            var payload = new Dictionary<string, object>
            {
                ["history"] = _history.ToList(),
                ["tools"] = tools
            };
            if (!string.IsNullOrWhiteSpace(systemMessage))
                payload["systemMessage"] = systemMessage;

            var request = ProviderRequest.Create(ProviderCapability.ModelCall, payload);
            var modelResponse = await _dispatcher.SendAsync(request, cancellationToken);

            if (!modelResponse.Success)
                return new ChatServiceResult($"Error: {modelResponse.ErrorMessage}", []);

            rawText = (string)modelResponse.Result["text"];

            // Epic 3: gate validation with one retry
            if (activeGateRule is not null && gateRetryCount < MaxGateRetries)
            {
                var gateResult = _gateValidator.Validate(rawText, activeGateRule);
                if (!gateResult.IsValid)
                {
                    gateRetryCount++;
                    _history.Add(new ChatTurn(ChatTurnRole.Assistant, rawText, DateTimeOffset.UtcNow));
                    var missing = string.Join(", ", gateResult.MissingKeys);
                    _history.Add(new ChatTurn(ChatTurnRole.User,
                        $"[GATE_RETRY] Your response is missing required gate-output keys: {missing}. Please include a ```gate-output``` block with all required keys.",
                        DateTimeOffset.UtcNow));
                    continue;
                }
            }
            break;
        }

        // Epic 8: capture reasoning trace
        if (_currentSessionId.HasValue)
        {
            var trace = ReasoningTrace.Capture(_currentSessionId.Value, Guid.NewGuid(), rawText);
            await _reasoningTraceRepo.SaveAsync(trace);
        }

        var (displayText, commands) = _responsePrep.Prepare(rawText);

        // Epic 4: Filter2 on assistant display text
        displayText = await _filterPipeline.ApplyFilter2Async(displayText, "assistant-response", projectId);

        // Handle STATE_TRANSITION internally before dispatching to action providers (US-135b)
        var remainingCommands = new List<ChatCommand>();
        foreach (var cmd in commands)
        {
            if (cmd is StateTransitionCommand stateTransition)
            {
                _contextService.UpdateConversationState(ConversationState.FromMode(stateTransition.Mode));

                // Epic 10: board state update when active story is set
                if (_activeStoryId.HasValue)
                {
                    var newStatus = stateTransition.Mode switch
                    {
                        "implementing" => (KnowledgeRecordStatus?)KnowledgeRecordStatus.InImplementation,
                        "testing" => (KnowledgeRecordStatus?)KnowledgeRecordStatus.ReviewedUser,
                        _ => null
                    };
                    if (newStatus.HasValue)
                        try { await _boardService.TransitionStatusAsync(_activeStoryId.Value, newStatus.Value); }
                        catch { /* board update is best-effort */ }
                }
            }
            else
                remainingCommands.Add(cmd);
        }

        var contextBefore = _contextService.GetCurrent();
        var unhandledCommands = await _actionRegistry.DispatchAsync(remainingCommands, cancellationToken);

        // US-185: link session to project when STARTPROJECT is processed
        var contextAfter = _contextService.GetCurrent();
        var projectJustConfigured = contextAfter.HasProject && !contextBefore.HasProject;
        if (projectJustConfigured && _currentSessionId.HasValue)
        {
            var session = await _sessionRepository.GetByIdAsync(_currentSessionId.Value);
            if (session is not null && !session.IsLinkedToProject)
            {
                session.LinkToProject(contextAfter.CurrentProject!.Id);
                await _sessionRepository.SaveAsync(session);
            }
        }

        _history.Add(new ChatTurn(ChatTurnRole.Assistant, rawText, DateTimeOffset.UtcNow));

        // Epic 7: token monitoring — track usage and archive when near limit
        _conversationContext.AddMessage(ConversationMessage.Create(MessageRole.User, userMessage));
        _conversationContext.AddMessage(ConversationMessage.Create(MessageRole.Assistant, rawText));
        if (_currentSessionId.HasValue && await _contextWindowManager.NeedsResetAsync(_conversationContext))
            await _contextWindowManager.ArchiveAndResetAsync(
                _conversationContext, _currentSessionId.Value,
                "Conversation archived for context window management.");

        // Epic 9: append to rolling window
        var rwScope = projectId.HasValue ? MdFileScope.Project : MdFileScope.Global;
        await _rollingWindowManager.AppendAsync(
            "conversation", $"User: {userMessage}\nAssistant: {displayText}", rwScope, projectId);

        // C-063: continuation turn — project was just configured; send PROJECT_CONFIGURED so model
        // can complete the original task in a follow-up turn where file access is already granted.
        if (projectJustConfigured)
        {
            var startCmd = remainingCommands.OfType<StartProjectCommand>().FirstOrDefault();
            var continuationMsg = !string.IsNullOrEmpty(startCmd?.AdditionalPath)
                ? $"[PROJECT_CONFIGURED] Project setup is complete and read access to \"{startCmd.AdditionalPath}\" has been granted — please proceed with the original request now."
                : "[PROJECT_CONFIGURED] Project setup is complete — please proceed with the original request.";
            var continuation = await SendAsync(continuationMsg, cancellationToken);
            var combinedText = string.IsNullOrWhiteSpace(displayText)
                ? continuation.DisplayText
                : displayText.TrimEnd() + "\n\n" + continuation.DisplayText;
            return new ChatServiceResult(combinedText, continuation.Commands);
        }

        return new ChatServiceResult(displayText, unhandledCommands);
    }

    // ── Native file tools (US-173) ────────────────────────────────────────────

    private async Task<string> ReadFileToolAsync(string path)
    {
        if (!_fileGate.CanRead(path))
            return JsonSerializer.Serialize(new { error = "Access denied", path });

        var response = await _dispatcher.SendAsync(
            ProviderRequest.Create(ProviderCapability.FileRead,
                new Dictionary<string, object> { ["path"] = path }));

        if (!response.Success)
            return JsonSerializer.Serialize(new { error = response.ErrorMessage, path });

        var content = (string)response.Result["content"];
        return content;
    }

    private async Task<string> WriteFileToolAsync(string path, string content)
    {
        if (!_fileGate.CanWrite(path))
            return JsonSerializer.Serialize(new { error = "Access denied — path outside project roots", path });

        var response = await _dispatcher.SendAsync(
            ProviderRequest.Create(ProviderCapability.FileWrite,
                new Dictionary<string, object> { ["path"] = path, ["content"] = content }));

        return response.Success
            ? JsonSerializer.Serialize(new { success = true, path })
            : JsonSerializer.Serialize(new { error = response.ErrorMessage, path });
    }

    private async Task<string> ListDirectoryToolAsync(string path)
    {
        if (!_fileGate.CanRead(path))
            return JsonSerializer.Serialize(new { error = "Access denied", path });

        var response = await _dispatcher.SendAsync(
            ProviderRequest.Create(ProviderCapability.DirectoryList,
                new Dictionary<string, object> { ["path"] = path }));

        if (!response.Success)
            return JsonSerializer.Serialize(new { error = response.ErrorMessage, path });

        var entries = (IReadOnlyList<string>)response.Result["entries"];
        return JsonSerializer.Serialize(entries);
    }

    // ── Project confirmation (US-165) ─────────────────────────────────────────

    public async Task<(string ProjectName, string Message)> ConfirmProjectAsync(ProjectConfirmCommand cmd)
    {
        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        var folderName = Project.ToFolderName(cmd.ProjectName);
        var codeFolder = _scaffoldService.GetCodeFolderPath(folderName, settings.SourceControlRoot);
        var project = await _projectService.CreateProjectAsync(cmd.ProjectName, folderName, codeFolder, cmd.ProjectIntent);

        await _scaffoldService.CreateScaffoldAsync(folderName, settings.SourceControlRoot);
        var agentFolder = _scaffoldService.GetAgentFolderPath(folderName);
        _fileGate.SetProjectRoots(agentFolder, codeFolder);
        _contextService.SetProject(project, agentFolder, codeFolder);

        return (project.Name, $"Project \"{project.Name}\" created! Your agent folder and code folder are ready.");
    }

    // ── Gate management (US-166, US-189, US-190) ──────────────────────────────

    public async Task<ChatServiceResult> GrantPermissionAsync(string path)
    {
        _fileGate.GrantReadAccess(path);
        return await SendAsync($"[PATH_ACCESS_GRANTED:{{\"path\":\"{path}\"}}]");
    }

    // US-189: grant access and persist so it survives session restarts
    public async Task<ChatServiceResult> GrantPermissionAlwaysAsync(string path)
    {
        _fileGate.GrantReadAccess(path);
        var context = _contextService.GetCurrent();
        if (context.CurrentProject is not null)
        {
            var settings = await _projectSettingsRepo.GetByProjectIdAsync(context.CurrentProject.Id)
                ?? ProjectSettings.Create(context.CurrentProject.Id);
            settings.AddAlwaysAllowedPath(path);
            await _projectSettingsRepo.SaveAsync(settings);
        }
        return await SendAsync($"[PATH_ACCESS_GRANTED:{{\"path\":\"{path}\"}}]");
    }

    // US-190: toggle session bypass mode
    public void SetBypassMode(bool bypass) => _fileGate.SetBypassMode(bypass);
    public bool IsBypassMode => _fileGate.IsBypassMode;

    // ── Folder selection (US-155 / US-165) ────────────────────────────────────

    public async Task<ChatServiceResult> HandleFolderSelectedAsync(string path)
    {
        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        if (string.IsNullOrEmpty(settings.SourceControlRoot))
        {
            settings.SourceControlRoot = path;
            await _settingsRepository.SaveGlobalSettingsAsync(settings);
        }

        // Complete partial initialization: project created but SourceControlRoot not yet set
        var context = _contextService.GetCurrent();
        if (context.HasProject && string.IsNullOrEmpty(context.CodeFolderPath))
        {
            var project = context.CurrentProject!;
            var codeFolder = _scaffoldService.GetCodeFolderPath(project.FolderName, path);
            await _scaffoldService.CreateScaffoldAsync(project.FolderName, path);
            var agentFolder = _scaffoldService.GetAgentFolderPath(project.FolderName);
            _fileGate.SetProjectRoots(agentFolder, codeFolder);
            _contextService.SetProject(project, agentFolder, codeFolder);
        }

        return await SendAsync($"[FOLDER_SELECT_RESULT:{{\"path\":\"{path}\"}}]");
    }

    public Task<ChatServiceResult> HandleFolderCancelledAsync()
        => SendAsync("[FOLDER_SELECT_RESULT:{\"cancelled\":true}]");

    // ── History ───────────────────────────────────────────────────────────────

    public IReadOnlyList<ChatTurn> History => _history;

    public void ClearHistory() => _history.Clear();
}
