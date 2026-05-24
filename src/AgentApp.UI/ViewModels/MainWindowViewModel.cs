using AgentApp.UI.ViewModels.Board;
using AgentApp.UI.ViewModels.Chat;
using AgentApp.UI.ViewModels.Projects;
using AgentApp.UI.ViewModels.Sessions;
using AgentApp.UI.ViewModels.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AgentApp.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "AgentApp";

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
    }
}
