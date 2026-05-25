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
        ProjectSettingsViewModel projectSettingsViewModel)
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
    }

    [RelayCommand]
    private void ToggleSessionPanel() => IsSessionPanelVisible = !IsSessionPanelVisible;
}
