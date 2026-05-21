using System.Text.Json;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;

namespace AgentApp.Application.Chat;

public record InitializeResult(string? ActiveProjectName, string? InitialMessage);

public class ChatOrchestrator
{
    private readonly CapabilityDispatcher _dispatcher;
    private readonly IChatCommandParser _commandParser;
    private readonly IFilePermissionGate _fileGate;
    private readonly IScaffoldService _scaffoldService;
    private readonly ProjectService _projectService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IOnboardingService _onboardingService;
    private readonly List<ChatTurn> _history = [];

    public ChatOrchestrator(
        CapabilityDispatcher dispatcher,
        IChatCommandParser commandParser,
        IFilePermissionGate fileGate,
        IScaffoldService scaffoldService,
        ProjectService projectService,
        ISettingsRepository settingsRepository,
        IOnboardingService onboardingService)
    {
        _dispatcher = dispatcher;
        _commandParser = commandParser;
        _fileGate = fileGate;
        _scaffoldService = scaffoldService;
        _projectService = projectService;
        _settingsRepository = settingsRepository;
        _onboardingService = onboardingService;
    }

    // ── Initialization ────────────────────────────────────────────────────────

    public async Task<InitializeResult> InitializeAsync()
    {
        if (await _onboardingService.IsOnboardingRequiredAsync())
            return new InitializeResult(null, "Hello! I'm Tower, your AI development agent. What would you like to build today?");

        var projects = await _projectService.GetAllProjectsAsync();
        var recent = projects.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (recent is not null)
        {
            _fileGate.SetProjectRoots(
                _scaffoldService.GetAgentFolderPath(recent.Name),
                recent.ProjectFolderPath);
            return new InitializeResult(recent.Name, null);
        }

        return new InitializeResult(null, null);
    }

    // ── Core chat ─────────────────────────────────────────────────────────────

    public async Task<ChatServiceResult> SendAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatTurn(ChatTurnRole.User, userMessage, DateTimeOffset.UtcNow));

        var request = ProviderRequest.Create(ProviderCapability.ModelCall,
            new Dictionary<string, object> { ["history"] = _history.ToList() });

        var response = await _dispatcher.SendAsync(request, cancellationToken);

        if (!response.Success)
            return new ChatServiceResult($"Error: {response.ErrorMessage}", []);

        var rawText = (string)response.Result["text"];
        var (displayText, commands) = _commandParser.Parse(rawText);

        _history.Add(new ChatTurn(ChatTurnRole.Assistant, rawText, DateTimeOffset.UtcNow));
        return new ChatServiceResult(displayText, commands);
    }

    // ── File command execution (US-164) ───────────────────────────────────────

    public Task<ChatServiceResult?> ExecuteFileCommandAsync(ChatCommand command)
    {
        return command switch
        {
            ReadFileCommand read => ExecuteReadFileAsync(read).ContinueWith(t => (ChatServiceResult?)t.Result),
            WriteFileCommand write => ExecuteWriteFileAsync(write).ContinueWith(t => (ChatServiceResult?)t.Result),
            ListDirectoryCommand list => ExecuteListDirectoryAsync(list).ContinueWith(t => (ChatServiceResult?)t.Result),
            _ => Task.FromResult<ChatServiceResult?>(null)
        };
    }

    private async Task<ChatServiceResult> ExecuteReadFileAsync(ReadFileCommand command)
    {
        if (!_fileGate.CanRead(command.Path))
            return await SendAsync($"[READ_FILE_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"Access denied\"}}]");

        var response = await _dispatcher.SendAsync(
            ProviderRequest.Create(ProviderCapability.FileRead,
                new Dictionary<string, object> { ["path"] = command.Path }));

        var resultMsg = response.Success
            ? $"[READ_FILE_RESULT:{{\"path\":\"{command.Path}\",\"content\":{JsonSerializer.Serialize((string)response.Result["content"])}}}]"
            : $"[READ_FILE_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"{response.ErrorMessage}\"}}]";

        return await SendAsync(resultMsg);
    }

    private async Task<ChatServiceResult> ExecuteWriteFileAsync(WriteFileCommand command)
    {
        if (!_fileGate.CanWrite(command.Path))
            return await SendAsync($"[WRITE_FILE_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"Access denied — path outside project roots\"}}]");

        var response = await _dispatcher.SendAsync(
            ProviderRequest.Create(ProviderCapability.FileWrite,
                new Dictionary<string, object> { ["path"] = command.Path, ["content"] = command.Content }));

        var resultMsg = response.Success
            ? $"[WRITE_FILE_RESULT:{{\"path\":\"{command.Path}\",\"success\":true}}]"
            : $"[WRITE_FILE_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"{response.ErrorMessage}\"}}]";

        return await SendAsync(resultMsg);
    }

    private async Task<ChatServiceResult> ExecuteListDirectoryAsync(ListDirectoryCommand command)
    {
        if (!_fileGate.CanRead(command.Path))
            return await SendAsync($"[LIST_DIR_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"Access denied\"}}]");

        var response = await _dispatcher.SendAsync(
            ProviderRequest.Create(ProviderCapability.DirectoryList,
                new Dictionary<string, object> { ["path"] = command.Path }));

        string resultMsg;
        if (response.Success)
        {
            var entries = (IReadOnlyList<string>)response.Result["entries"];
            resultMsg = $"[LIST_DIR_RESULT:{{\"path\":\"{command.Path}\",\"entries\":{JsonSerializer.Serialize(entries)}}}]";
        }
        else
        {
            resultMsg = $"[LIST_DIR_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"{response.ErrorMessage}\"}}]";
        }

        return await SendAsync(resultMsg);
    }

    // ── Project confirmation (US-165) ─────────────────────────────────────────

    public async Task<(string ProjectName, string Message)> ConfirmProjectAsync(ProjectConfirmCommand cmd)
    {
        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        var codeFolder = _scaffoldService.GetCodeFolderPath(cmd.ProjectName, settings.SourceControlRoot);
        var project = await _projectService.CreateProjectAsync(cmd.ProjectName, codeFolder);

        await _scaffoldService.CreateScaffoldAsync(cmd.ProjectName, settings.SourceControlRoot);
        _fileGate.SetProjectRoots(_scaffoldService.GetAgentFolderPath(project.Name), codeFolder);

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
        if (!await _onboardingService.IsSourceControlRootSetAsync())
        {
            var settings = await _settingsRepository.GetGlobalSettingsAsync();
            settings.SourceControlRoot = path;
            await _settingsRepository.SaveGlobalSettingsAsync(settings);
        }

        return await SendAsync($"[FOLDER_SELECT_RESULT:{{\"path\":\"{path}\"}}]");
    }

    public Task<ChatServiceResult> HandleFolderCancelledAsync()
        => SendAsync("[FOLDER_SELECT_RESULT:{\"cancelled\":true}]");

    // ── History ───────────────────────────────────────────────────────────────

    public IReadOnlyList<ChatTurn> History => _history;

    public void ClearHistory() => _history.Clear();
}
