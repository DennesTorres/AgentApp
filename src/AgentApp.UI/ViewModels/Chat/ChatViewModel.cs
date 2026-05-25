using System.Collections.ObjectModel;
using AgentApp.Application.Sessions;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AgentApp.UI.ViewModels.Chat;

public partial class ChatViewModel : ObservableObject
{
    private readonly ChatPresenter _presenter;
    private readonly SessionService _sessionService;
    private readonly ISettingsRepository _settingsRepository;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _userInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _activeProjectName = string.Empty;

    [ObservableProperty]
    private string _currentState = "Chat";

    // C-023/C-024: Session tracking
    [ObservableProperty]
    private string _currentSessionName = "New Session";

    private Guid? _currentSessionId;
    private bool _isFirstMessage = true;

    // C-025/C-031: Avatar configuration
    [ObservableProperty]
    private string _agentAvatarImagePath = string.Empty;

    [ObservableProperty]
    private string _userAvatarImagePath = string.Empty;

    [ObservableProperty]
    private string _agentAvatarColor = "#5B8AF5";   // C-031: preset color

    [ObservableProperty]
    private string _userAvatarColor = "#4A7A4A";

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

    public ChatViewModel(ChatPresenter presenter, SessionService sessionService, ISettingsRepository settingsRepository)
    {
        _presenter = presenter;
        _sessionService = sessionService;
        _settingsRepository = settingsRepository;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        AgentAvatarImagePath = settings.AgentAvatarImagePath;
        UserAvatarImagePath = settings.UserAvatarImagePath;
        AgentAvatarColor = PresetColor(settings.AgentAvatarPreset, "#5B8AF5");
        UserAvatarColor = PresetColor(settings.UserAvatarPreset, "#4A7A4A");

        var result = await _presenter.InitializeAsync();
        if (result.InitialMessage is not null)
            AddAgentMessage(result.InitialMessage);
        if (result.ActiveProjectName is not null)
            ActiveProjectName = result.ActiveProjectName;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        var text = UserInput.Trim();
        UserInput = string.Empty;

        Messages.Add(new ChatTurnViewModel
        {
            Role = "User",
            Content = text,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });

        IsBusy = true;

        // C-023/C-024: On first message, create session and name it
        if (_isFirstMessage)
        {
            _isFirstMessage = false;
            var session = await _sessionService.StartStandaloneSessionAsync();
            _currentSessionId = session.Id;
            var nameWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var draftName = string.Join(" ", nameWords.Take(5));
            if (draftName.Length > 40) draftName = draftName[..40];
            if (!string.IsNullOrWhiteSpace(draftName))
            {
                await _sessionService.RenameAsync(session.Id, draftName);
                CurrentSessionName = draftName;
            }
        }

        // C-029: Save user message
        if (_currentSessionId.HasValue)
            await _sessionService.SaveMessageAsync(_currentSessionId.Value, "User", text);

        var result = await _presenter.SendAsync(text);

        foreach (var command in result.Commands)
            await HandleCommandAsync(command);

        if (!string.IsNullOrWhiteSpace(result.DisplayText))
        {
            AddAgentMessage(result.DisplayText);
            // C-029: Save agent message
            if (_currentSessionId.HasValue)
                await _sessionService.SaveMessageAsync(_currentSessionId.Value, "Tower", result.DisplayText);
        }

        IsBusy = false;
    }

    // C-029: Load a session's messages (called when user selects session in sidebar)
    public async Task LoadSessionAsync(Guid sessionId, string sessionName)
    {
        _currentSessionId = sessionId;
        _isFirstMessage = false;
        CurrentSessionName = sessionName;
        Messages.Clear();

        var messages = await _sessionService.GetMessagesAsync(sessionId);
        foreach (var msg in messages)
        {
            Messages.Add(new ChatTurnViewModel
            {
                Role = msg.Role,
                Content = msg.Content,
                Timestamp = msg.Timestamp.LocalDateTime.ToString("HH:mm")
            });
        }
    }

    // ── Project confirmation (US-153) ────────────────────────────────────────

    [RelayCommand]
    private async Task ConfirmProjectAsync()
    {
        if (_pendingProjectConfirm is null) return;
        IsBusy = true;
        HasProjectConfirmPending = false;
        var (projectName, message) = await _presenter.ConfirmProjectAsync(_pendingProjectConfirm);
        ActiveProjectName = projectName;
        _pendingProjectConfirm = null;
        AddAgentMessage(message);
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
        var path = _pendingPermissionRequest.Path;
        HasPermissionRequestPending = false;
        _pendingPermissionRequest = null;
        var result = await _presenter.GrantPermissionAsync(path);
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
            case PathPermissionRequestCommand permissionRequest:
                _pendingPermissionRequest = permissionRequest;
                PendingPermissionSummary = $"Tower is requesting read access to:\n{permissionRequest.Path}\n\nReason: {permissionRequest.Reason}";
                HasPermissionRequestPending = true;
                break;
        }
    }

    // ── Folder picker (US-155) ───────────────────────────────────────────────

    private async Task HandleFolderSelectAsync(FolderSelectCommand command)
    {
        var dialog = new OpenFolderDialog { Title = command.Reason };
        if (dialog.ShowDialog() != true)
        {
            var cancelResult = await _presenter.HandleFolderCancelledAsync();
            if (!string.IsNullOrWhiteSpace(cancelResult.DisplayText))
                AddAgentMessage(cancelResult.DisplayText);
            return;
        }
        var selectedPath = dialog.FolderName;
        var continueResult = await _presenter.HandleFolderSelectedAsync(selectedPath);
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

    // C-031: Map preset name to hex color
    private static string PresetColor(string preset, string defaultColor) => preset switch
    {
        "blue"   => "#5B8AF5",
        "purple" => "#A855F7",
        "teal"   => "#10B981",
        "amber"  => "#F59E0B",
        _        => defaultColor
    };
}
