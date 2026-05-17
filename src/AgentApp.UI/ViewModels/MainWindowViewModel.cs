using AgentApp.UI.ViewModels.Chat;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AgentApp.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "AgentApp";

    public ChatViewModel ChatViewModel { get; }

    public MainWindowViewModel(ChatViewModel chatViewModel)
    {
        ChatViewModel = chatViewModel;
    }
}
