using System.Collections.ObjectModel;
using AgentApp.Application.Sessions;
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
    private bool _shouldRenameOnFirstMessage;
    // C-045: guard against stale concurrent loads
    private int _loadGeneration;

    // C-025/C-031: Avatar configuration
    [ObservableProperty]
    private string _agentAvatarImagePath = string.Empty;

    [ObservableProperty]
    private string _userAvatarImagePath = string.Empty;

    [ObservableProperty]
    private string _agentAvatarColor = "#5B8AF5";   // C-031: preset color

    [ObservableProperty]
    private string _userAvatarColor = "#4A7A4A";

    // C-044: avatar shape preset
    [ObservableProperty]
    private string _agentAvatarShape = "person";

    [ObservableProperty]
    private string _userAvatarShape = "person";

    // Path permission request (US-163)
    private string? _pendingPermissionPath;

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
        AgentAvatarShape = string.IsNullOrWhiteSpace(settings.AgentAvatarShape) ? "person" : settings.AgentAvatarShape;
        UserAvatarShape = string.IsNullOrWhiteSpace(settings.UserAvatarShape) ? "person" : settings.UserAvatarShape;
        // C-040: greeting is shown via LoadSessionAsync for empty sessions — not here
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

        // C-023/C-024/C-035: Create or rename session on first message
        if (_isFirstMessage)
        {
            _isFirstMessage = false;
            var session = await _sessionService.StartStandaloneSessionAsync();
            _currentSessionId = session.Id;
            _presenter.SetCurrentSession(session.Id);
            await RenameFromTextAsync(session.Id, text);
        }
        else if (_shouldRenameOnFirstMessage && _currentSessionId.HasValue)
        {
            _shouldRenameOnFirstMessage = false;
            await RenameFromTextAsync(_currentSessionId.Value, text);
        }

        // C-029: Save user message
        if (_currentSessionId.HasValue)
            await _sessionService.SaveMessageAsync(_currentSessionId.Value, "User", text);

        var result = await _presenter.SendAsync(text);
        await HandlePresenterResultAsync(result);

        IsBusy = false;
    }

    // C-033: Reset session state (called when sidebar has no selection)
    public void ClearSession()
    {
        _currentSessionId = null;
        _presenter.SetCurrentSession(null);
        _isFirstMessage = true;
        _shouldRenameOnFirstMessage = false;
        CurrentSessionName = string.Empty;
        Messages.Clear();
    }

    // C-029/C-034/C-035: Load a session's messages (called when user selects session in sidebar)
    public async Task LoadSessionAsync(Guid sessionId, string sessionName)
    {
        // C-045: track generation so a stale concurrent load doesn't overwrite a newer one
        var generation = ++_loadGeneration;
        _currentSessionId = sessionId;
        _presenter.SetCurrentSession(sessionId);
        _isFirstMessage = false;
        CurrentSessionName = sessionName;
        Messages.Clear();

        var messages = await _sessionService.GetMessagesAsync(sessionId);
        if (generation != _loadGeneration) return; // superseded by a newer load

        foreach (var msg in messages)
        {
            Messages.Add(new ChatTurnViewModel
            {
                Role = msg.Role,
                Content = msg.Content,
                Timestamp = msg.Timestamp.LocalDateTime.ToString("HH:mm")
            });
        }

        // C-034: trigger greeting for empty sessions
        // C-035: flag unnamed sessions for rename on first message
        _shouldRenameOnFirstMessage = messages.Count == 0 && IsDefaultSessionName(sessionName);
        if (messages.Count == 0)
        {
            var result = await _presenter.InitializeAsync(_currentSessionId);
            if (generation != _loadGeneration) return;
            if (result.InitialMessage is not null)
            {
                AddAgentMessage(result.InitialMessage);
                // C-067: persist the intro message so it survives session switching
                if (_currentSessionId.HasValue)
                    await _sessionService.SaveMessageAsync(_currentSessionId.Value, "Tower", result.InitialMessage);
            }
        }
    }

    // C-051: update the chat title when the current session is renamed externally
    public void UpdateSessionName(Guid sessionId, string newName)
    {
        if (_currentSessionId == sessionId)
            CurrentSessionName = newName;
    }

    // C-047: re-read avatar settings after settings saved
    public async Task ReloadAvatarSettingsAsync()
    {
        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        AgentAvatarImagePath = settings.AgentAvatarImagePath;
        UserAvatarImagePath = settings.UserAvatarImagePath;
        AgentAvatarColor = PresetColor(settings.AgentAvatarPreset, "#5B8AF5");
        UserAvatarColor = PresetColor(settings.UserAvatarPreset, "#4A7A4A");
        AgentAvatarShape = string.IsNullOrWhiteSpace(settings.AgentAvatarShape) ? "person" : settings.AgentAvatarShape;
        UserAvatarShape = string.IsNullOrWhiteSpace(settings.UserAvatarShape) ? "person" : settings.UserAvatarShape;
    }

    // US-190: session bypass mode
    [ObservableProperty]
    private bool _bypassPermissions;

    partial void OnBypassPermissionsChanged(bool value) =>
        _presenter.SetBypassMode(value);

    // ── Path permission request (US-163, US-189) ──────────────────────────────

    [RelayCommand]
    private async Task ApprovePermissionAsync()
    {
        if (_pendingPermissionPath is null) return;
        var path = _pendingPermissionPath;
        HasPermissionRequestPending = false;
        _pendingPermissionPath = null;
        var result = await _presenter.GrantPermissionAsync(path);
        if (!string.IsNullOrWhiteSpace(result.DisplayText))
            AddAgentMessage(result.DisplayText);
    }

    // US-189: persist permission across sessions
    [RelayCommand]
    private async Task ApprovePermissionAlwaysAsync()
    {
        if (_pendingPermissionPath is null) return;
        var path = _pendingPermissionPath;
        HasPermissionRequestPending = false;
        _pendingPermissionPath = null;
        var result = await _presenter.GrantPermissionAlwaysAsync(path);
        if (!string.IsNullOrWhiteSpace(result.DisplayText))
            AddAgentMessage(result.DisplayText);
    }

    [RelayCommand]
    private void DenyPermission()
    {
        _pendingPermissionPath = null;
        HasPermissionRequestPending = false;
        AddAgentMessage("Access denied. I'll work within the current permitted paths.");
    }

    // ── Presenter result dispatch ─────────────────────────────────────────────

    private async Task HandlePresenterResultAsync(PresenterResult result)
    {
        if (result.PendingFolderSelect is { } folderSelect)
            await HandleFolderSelectAsync(folderSelect.Reason);

        if (result.PendingPermissionRequest is { } permRequest)
        {
            _pendingPermissionPath = permRequest.Path;
            PendingPermissionSummary = $"Tower is requesting read access to:\n{permRequest.Path}\n\nReason: {permRequest.Reason}";
            HasPermissionRequestPending = true;
        }

        if (!string.IsNullOrWhiteSpace(result.DisplayText))
        {
            AddAgentMessage(result.DisplayText);
            if (_currentSessionId.HasValue)
                await _sessionService.SaveMessageAsync(_currentSessionId.Value, "Tower", result.DisplayText);
        }
    }

    // ── Folder picker (US-155) ───────────────────────────────────────────────

    private async Task HandleFolderSelectAsync(string reason)
    {
        var dialog = new OpenFolderDialog { Title = reason };
        if (dialog.ShowDialog() != true)
        {
            var cancelResult = await _presenter.HandleFolderCancelledAsync();
            await HandlePresenterResultAsync(cancelResult);
            return;
        }
        var continueResult = await _presenter.HandleFolderSelectedAsync(dialog.FolderName);
        await HandlePresenterResultAsync(continueResult);
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

    // C-035: Rename session from first message text
    private async Task RenameFromTextAsync(Guid sessionId, string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var draftName = string.Join(" ", words.Take(5));
        if (draftName.Length > 40) draftName = draftName[..40];
        if (!string.IsNullOrWhiteSpace(draftName))
        {
            await _sessionService.RenameAsync(sessionId, draftName);
            CurrentSessionName = draftName;
        }
    }

    // C-035: Detect default date/time session name (e.g. "Session 2026-05-25 04:22")
    private static bool IsDefaultSessionName(string name)
        => name.StartsWith("Session ", StringComparison.Ordinal);

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
