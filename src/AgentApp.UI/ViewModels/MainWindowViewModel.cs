using AgentApp.Application.Sessions;
using AgentApp.UI.ViewModels.Board;
using AgentApp.UI.ViewModels.Chat;
using AgentApp.UI.ViewModels.Projects;
using AgentApp.UI.ViewModels.Sessions;
using AgentApp.UI.ViewModels.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "AgentApp";

    // C-021: Session panel toggle
    [ObservableProperty]
    private bool _isSessionPanelVisible = true;

    // C-057: tab index (0=Chat, 1=Projects, 2=Board, 3=Settings)
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public ChatViewModel ChatViewModel { get; }
    public ProjectListViewModel ProjectListViewModel { get; }
    public SessionListViewModel SessionListViewModel { get; }
    public BoardViewModel BoardViewModel { get; }
    public GlobalSettingsViewModel GlobalSettingsViewModel { get; }
    public ProjectSettingsViewModel ProjectSettingsViewModel { get; }

    public MainWindowViewModel(
        ChatViewModel chatViewModel,
        ProjectListViewModel projectListViewModel,
        SessionListViewModel sessionListViewModel,
        BoardViewModel boardViewModel,
        GlobalSettingsViewModel globalSettingsViewModel,
        ProjectSettingsViewModel projectSettingsViewModel,
        SessionService sessionService)
    {
        ChatViewModel = chatViewModel;
        ProjectListViewModel = projectListViewModel;
        SessionListViewModel = sessionListViewModel;
        BoardViewModel = boardViewModel;
        GlobalSettingsViewModel = globalSettingsViewModel;
        ProjectSettingsViewModel = projectSettingsViewModel;

        // C-029/C-033: when user selects (or deselects) a session in sidebar
        // C-042: suppress ClearSession/LoadSession during RefreshAsync (collection rebuild)
        sessionListViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(SessionListViewModel.SelectedSession)) return;
            if (sessionListViewModel.IsRefreshing) return;
            if (sessionListViewModel.SelectedSession is { } sel)
                _ = chatViewModel.LoadSessionAsync(sel.Id, sel.Name);
            else
                chatViewModel.ClearSession();
        };

        // C-051: keep chat title in sync when a session is renamed from the sidebar
        sessionService.SessionRenamed += (_, e) => chatViewModel.UpdateSessionName(e.sessionId, e.newName);

        // C-047: reload avatar settings in chat whenever global settings are saved
        globalSettingsViewModel.SettingsSaved += (_, _) => _ = chatViewModel.ReloadAvatarSettingsAsync();

        // C-057: navigate to Chat tab when user creates a new session from the Sessions panel
        sessionListViewModel.NavigateToChatRequested += () => SelectedTabIndex = 0;

        // C-086: OPEN on Projects tab — find/create session for that project, navigate to Chat
        projectListViewModel.OpenProjectRequested += projectId =>
        {
            _ = sessionListViewModel.OpenProjectSessionAsync(projectId);
            SelectedTabIndex = 0;
        };
    }

    [RelayCommand]
    private void ToggleSessionPanel() => IsSessionPanelVisible = !IsSessionPanelVisible;
}
