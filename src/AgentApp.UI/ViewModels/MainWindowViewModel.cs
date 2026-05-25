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

        // C-029: when user selects a session in sidebar, load it into chat
        sessionListViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SessionListViewModel.SelectedSession)
                && sessionListViewModel.SelectedSession is { } sel)
            {
                _ = chatViewModel.LoadSessionAsync(sel.Id, sel.Name);
            }
        };
    }

    [RelayCommand]
    private void ToggleSessionPanel() => IsSessionPanelVisible = !IsSessionPanelVisible;
}
