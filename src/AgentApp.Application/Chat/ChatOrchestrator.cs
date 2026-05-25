using System.Text.Json;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;
using AgentApp.Domain.Providers;
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
    private readonly List<ChatTurn> _history = [];

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
        ISystemMessageProvider[] systemMessageProviders)
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
    }

    // ── Initialization ────────────────────────────────────────────────────────

    public async Task<InitializeResult> InitializeAsync()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        var recent = projects.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (recent is not null)
        {
            var agentFolder = _scaffoldService.GetAgentFolderPath(recent.FolderName);
            _contextService.SetProject(recent, agentFolder, recent.ProjectFolderPath);
            if (!string.IsNullOrEmpty(recent.ProjectFolderPath))
                _fileGate.SetProjectRoots(agentFolder, recent.ProjectFolderPath);
            return new InitializeResult(recent.Name, null);
        }

        return new InitializeResult(null, null);
    }

    // ── Core chat ─────────────────────────────────────────────────────────────

    public async Task<ChatServiceResult> SendAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatTurn(ChatTurnRole.User, userMessage, DateTimeOffset.UtcNow));

        // Assemble system message from registered providers (US-135b)
        var context = _contextService.GetCurrent();
        var systemSections = _systemMessageProviders
            .Where(p => p.IsApplicable(context))
            .Select(p => p.GetSection(context))
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var systemMessage = string.Join("\n\n", systemSections);

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

        var payload = new Dictionary<string, object>
        {
            ["history"] = _history.ToList(),
            ["tools"] = tools
        };
        if (!string.IsNullOrWhiteSpace(systemMessage))
            payload["systemMessage"] = systemMessage;

        var request = ProviderRequest.Create(ProviderCapability.ModelCall, payload);
        var response = await _dispatcher.SendAsync(request, cancellationToken);

        if (!response.Success)
            return new ChatServiceResult($"Error: {response.ErrorMessage}", []);

        var rawText = (string)response.Result["text"];
        var (displayText, commands) = _responsePrep.Prepare(rawText);

        // Handle STATE_TRANSITION internally before dispatching to action providers (US-135b)
        var remainingCommands = new List<ChatCommand>();
        foreach (var cmd in commands)
        {
            if (cmd is StateTransitionCommand stateTransition)
                _contextService.UpdateConversationState(ConversationState.FromMode(stateTransition.Mode));
            else
                remainingCommands.Add(cmd);
        }

        var unhandledCommands = await _actionRegistry.DispatchAsync(remainingCommands, cancellationToken);

        _history.Add(new ChatTurn(ChatTurnRole.Assistant, rawText, DateTimeOffset.UtcNow));
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

    // ── Gate management (US-166) ──────────────────────────────────────────────

    public async Task<ChatServiceResult> GrantPermissionAsync(string path)
    {
        _fileGate.GrantReadAccess(path);
        return await SendAsync($"[PATH_ACCESS_GRANTED:{{\"path\":\"{path}\"}}]");
    }

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
