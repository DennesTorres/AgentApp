using System.Collections.ObjectModel;
using System.Text.Json;
using AgentApp.Application.Chat;
using AgentApp.Application.Providers;
using AgentApp.Application.Projects;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AgentApp.UI.ViewModels.Chat;

public partial class ChatViewModel : ObservableObject
{
    private readonly ChatService _chatService;
    private readonly IOnboardingService _onboardingService;
    private readonly ProjectService _projectService;
    private readonly IScaffoldService _scaffoldService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly OrchestratorPipeline _pipeline;
    private readonly IFilePermissionGate _fileGate;
    private readonly IAgentContextService _contextService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _userInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _activeProjectName = string.Empty;

    // Project confirmation (US-153)
    private ProjectConfirmCommand? _pendingProjectConfirm;

    [ObservableProperty]
    private bool _hasProjectConfirmPending;

    [ObservableProperty]
    private string _pendingProjectSummary = string.Empty;

    // Path permission request (US-163)
    private PathPermissionRequestCommand? _pendingPermissionRequest;

    [ObservableProperty]
    private bool _hasPermissionRequestPending;

    [ObservableProperty]
    private string _pendingPermissionSummary = string.Empty;

    public ObservableCollection<ChatTurnViewModel> Messages { get; } = [];

    public ChatViewModel(
        ChatService chatService,
        IOnboardingService onboardingService,
        ProjectService projectService,
        IScaffoldService scaffoldService,
        ISettingsRepository settingsRepository,
        OrchestratorPipeline pipeline,
        IFilePermissionGate fileGate,
        IAgentContextService contextService)
    {
        _chatService = chatService;
        _onboardingService = onboardingService;
        _projectService = projectService;
        _scaffoldService = scaffoldService;
        _settingsRepository = settingsRepository;
        _pipeline = pipeline;
        _fileGate = fileGate;
        _contextService = contextService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (await _onboardingService.IsOnboardingRequiredAsync())
        {
            AddAgentMessage("Hello! I'm Tower, your AI development agent. What would you like to build today?");
        }
        else
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var recent = projects.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
            if (recent is not null)
            {
                var agentFolder = _scaffoldService.GetAgentFolderPath(recent.Name);
                ActiveProjectName = recent.Name;
                _fileGate.SetProjectRoots(agentFolder, recent.ProjectFolderPath);
                _contextService.SetProject(recent, agentFolder, recent.ProjectFolderPath);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        var text = UserInput.Trim();
        UserInput = string.Empty;
        IsBusy = true;

        Messages.Add(new ChatTurnViewModel
        {
            Role = "User",
            Content = text,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });

        var result = await _chatService.SendAsync(text);

        foreach (var command in result.Commands)
            await HandleCommandAsync(command);

        if (!string.IsNullOrWhiteSpace(result.DisplayText))
            AddAgentMessage(result.DisplayText);

        IsBusy = false;
    }

    // ── Project confirmation (US-153) ────────────────────────────────────────

    [RelayCommand]
    private async Task ConfirmProjectAsync()
    {
        if (_pendingProjectConfirm is null) return;

        IsBusy = true;
        HasProjectConfirmPending = false;

        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        var codeFolder = _scaffoldService.GetCodeFolderPath(_pendingProjectConfirm.ProjectName, settings.SourceControlRoot);
        var project = await _projectService.CreateProjectAsync(_pendingProjectConfirm.ProjectName, codeFolder);

        await _scaffoldService.CreateScaffoldAsync(_pendingProjectConfirm.ProjectName, settings.SourceControlRoot);

        var agentFolder = _scaffoldService.GetAgentFolderPath(project.Name);
        ActiveProjectName = project.Name;
        _fileGate.SetProjectRoots(agentFolder, codeFolder);
        _contextService.SetProject(project, agentFolder, codeFolder);

        _pendingProjectConfirm = null;
        AddAgentMessage($"Project \"{project.Name}\" created! Your agent folder and code folder are ready.");
        IsBusy = false;
    }

    [RelayCommand]
    private void CancelProject()
    {
        _pendingProjectConfirm = null;
        HasProjectConfirmPending = false;
        AddAgentMessage("No problem — let me know what you'd like to build.");
    }

    // ── Path permission request (US-163) ─────────────────────────────────────

    [RelayCommand]
    private async Task ApprovePermissionAsync()
    {
        if (_pendingPermissionRequest is null) return;

        _fileGate.GrantReadAccess(_pendingPermissionRequest.Path);
        HasPermissionRequestPending = false;
        var path = _pendingPermissionRequest.Path;
        _pendingPermissionRequest = null;

        var result = await _chatService.SendAsync($"[PATH_ACCESS_GRANTED:{{\"path\":\"{path}\"}}]");
        foreach (var cmd in result.Commands) await HandleCommandAsync(cmd);
        if (!string.IsNullOrWhiteSpace(result.DisplayText))
            AddAgentMessage(result.DisplayText);
    }

    [RelayCommand]
    private void DenyPermission()
    {
        _pendingPermissionRequest = null;
        HasPermissionRequestPending = false;
        AddAgentMessage("Access denied. I'll work within the current permitted paths.");
    }

    // ── Command dispatch ─────────────────────────────────────────────────────

    private async Task HandleCommandAsync(ChatCommand command)
    {
        switch (command)
        {
            case FolderSelectCommand folderSelect:
                await HandleFolderSelectAsync(folderSelect);
                break;

            case ProjectConfirmCommand projectConfirm:
                _pendingProjectConfirm = projectConfirm;
                PendingProjectSummary = $"Create project \"{projectConfirm.ProjectName}\" — {projectConfirm.ProjectIntent}";
                HasProjectConfirmPending = true;
                break;

            case ReadFileCommand readFile:
                await HandleReadFileAsync(readFile);
                break;

            case WriteFileCommand writeFile:
                await HandleWriteFileAsync(writeFile);
                break;

            case ListDirectoryCommand listDir:
                await HandleListDirectoryAsync(listDir);
                break;

            case PathPermissionRequestCommand permissionRequest:
                _pendingPermissionRequest = permissionRequest;
                PendingPermissionSummary = $"Tower is requesting read access to:\n{permissionRequest.Path}\n\nReason: {permissionRequest.Reason}";
                HasPermissionRequestPending = true;
                break;
        }
    }

    // ── File operations (US-159–162) ─────────────────────────────────────────

    private async Task HandleReadFileAsync(ReadFileCommand command)
    {
        if (!_fileGate.CanRead(command.Path))
        {
            var denied = await _chatService.SendAsync(
                $"[READ_FILE_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"Access denied\"}}]");
            if (!string.IsNullOrWhiteSpace(denied.DisplayText))
                AddAgentMessage(denied.DisplayText);
            return;
        }

        var response = await _pipeline.SendAsync(
            ProviderRequest.Create(ProviderCapability.FileRead,
                new Dictionary<string, object> { ["path"] = command.Path }));

        var resultMsg = response.Success
            ? $"[READ_FILE_RESULT:{{\"path\":\"{command.Path}\",\"content\":{JsonSerializer.Serialize((string)response.Result["content"])}}}]"
            : $"[READ_FILE_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"{response.ErrorMessage}\"}}]";

        var followUp = await _chatService.SendAsync(resultMsg);
        foreach (var cmd in followUp.Commands) await HandleCommandAsync(cmd);
        if (!string.IsNullOrWhiteSpace(followUp.DisplayText))
            AddAgentMessage(followUp.DisplayText);
    }

    private async Task HandleWriteFileAsync(WriteFileCommand command)
    {
        if (!_fileGate.CanWrite(command.Path))
        {
            var denied = await _chatService.SendAsync(
                $"[WRITE_FILE_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"Access denied — path outside project roots\"}}]");
            if (!string.IsNullOrWhiteSpace(denied.DisplayText))
                AddAgentMessage(denied.DisplayText);
            return;
        }

        var response = await _pipeline.SendAsync(
            ProviderRequest.Create(ProviderCapability.FileWrite,
                new Dictionary<string, object> { ["path"] = command.Path, ["content"] = command.Content }));

        var resultMsg = response.Success
            ? $"[WRITE_FILE_RESULT:{{\"path\":\"{command.Path}\",\"success\":true}}]"
            : $"[WRITE_FILE_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"{response.ErrorMessage}\"}}]";

        var followUp = await _chatService.SendAsync(resultMsg);
        foreach (var cmd in followUp.Commands) await HandleCommandAsync(cmd);
        if (!string.IsNullOrWhiteSpace(followUp.DisplayText))
            AddAgentMessage(followUp.DisplayText);
    }

    private async Task HandleListDirectoryAsync(ListDirectoryCommand command)
    {
        if (!_fileGate.CanRead(command.Path))
        {
            var denied = await _chatService.SendAsync(
                $"[LIST_DIR_RESULT:{{\"path\":\"{command.Path}\",\"error\":\"Access denied\"}}]");
            if (!string.IsNullOrWhiteSpace(denied.DisplayText))
                AddAgentMessage(denied.DisplayText);
            return;
        }

        var response = await _pipeline.SendAsync(
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

        var followUp = await _chatService.SendAsync(resultMsg);
        foreach (var cmd in followUp.Commands) await HandleCommandAsync(cmd);
        if (!string.IsNullOrWhiteSpace(followUp.DisplayText))
            AddAgentMessage(followUp.DisplayText);
    }

    // ── Folder picker (US-155) ───────────────────────────────────────────────

    private async Task HandleFolderSelectAsync(FolderSelectCommand command)
    {
        var dialog = new OpenFolderDialog { Title = command.Reason };

        if (dialog.ShowDialog() != true)
        {
            var cancelResult = await _chatService.SendAsync("[FOLDER_SELECT_RESULT:{\"cancelled\":true}]");
            if (!string.IsNullOrWhiteSpace(cancelResult.DisplayText))
                AddAgentMessage(cancelResult.DisplayText);
            return;
        }

        var selectedPath = dialog.FolderName;

        if (!await _onboardingService.IsSourceControlRootSetAsync())
        {
            var settings = await _settingsRepository.GetGlobalSettingsAsync();
            settings.SourceControlRoot = selectedPath;
            await _settingsRepository.SaveGlobalSettingsAsync(settings);
        }

        var continueResult = await _chatService.SendAsync($"[FOLDER_SELECT_RESULT:{{\"path\":\"{selectedPath}\"}}]");
        foreach (var nestedCmd in continueResult.Commands)
            await HandleCommandAsync(nestedCmd);
        if (!string.IsNullOrWhiteSpace(continueResult.DisplayText))
            AddAgentMessage(continueResult.DisplayText);
    }

    private void AddAgentMessage(string content)
    {
        Messages.Add(new ChatTurnViewModel
        {
            Role = "Tower",
            Content = content,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });
    }

    private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(UserInput);
}
