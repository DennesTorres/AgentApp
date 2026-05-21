using AgentApp.UI.ViewModels.Chat;
using AgentApp.UI.ViewModels.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AgentApp.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "AgentApp";

    public ChatViewModel ChatViewModel { get; }
    public ApiKeySettingsViewModel ApiKeySettingsViewModel { get; }

    public MainWindowViewModel(ChatViewModel chatViewModel,
        ApiKeySettingsViewModel apiKeySettingsViewModel)
    {
        ChatViewModel = chatViewModel;
        ApiKeySettingsViewModel = apiKeySettingsViewModel;
    }
}
