using System.Collections.ObjectModel;
using AgentApp.Domain.Chat;
using AgentApp.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AgentApp.UI.ViewModels.Chat;

public partial class ChatViewModel : ObservableObject
{
    private readonly ChatPresenter _presenter;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _userInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _activeProjectName = string.Empty;

    // US-134: Current execution state (wired to state machine when Epic 17 merges)
    [ObservableProperty]
    private string _currentState = "Chat";

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

    public ChatViewModel(ChatPresenter presenter)
    {
        _presenter = presenter;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
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

        // C-004: Show user message immediately, before processing
        Messages.Add(new ChatTurnViewModel
        {
            Role = "User",
            Content = text,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });

        IsBusy = true;

        var result = await _presenter.SendAsync(text);

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
}
