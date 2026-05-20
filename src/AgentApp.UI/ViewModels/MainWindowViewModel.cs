using AgentApp.UI.ViewModels.Chat;
using AgentApp.UI.ViewModels.Projects;
using AgentApp.UI.ViewModels.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AgentApp.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "AgentApp";

    public ChatViewModel ChatViewModel { get; }
    public ProjectListViewModel ProjectListViewModel { get; }
    public ApiKeySettingsViewModel ApiKeySettingsViewModel { get; }

    public MainWindowViewModel(ChatViewModel chatViewModel,
        ProjectListViewModel projectListViewModel,
        ApiKeySettingsViewModel apiKeySettingsViewModel)
    {
        ChatViewModel = chatViewModel;
        ProjectListViewModel = projectListViewModel;
        ApiKeySettingsViewModel = apiKeySettingsViewModel;
    }
}
