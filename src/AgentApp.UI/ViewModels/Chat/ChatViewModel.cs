using System.Collections.ObjectModel;
using AgentApp.Application.Chat;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Chat;

public partial class ChatViewModel : ObservableObject
{
    private readonly ChatService _chatService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _userInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isBusy;

    public ObservableCollection<ChatTurnViewModel> Messages { get; } = [];

    public ChatViewModel(ChatService chatService)
    {
        _chatService = chatService;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        var text = UserInput.Trim();
        UserInput = string.Empty;
        IsBusy = true;

        Messages.Add(new ChatTurnViewModel
        {
            Role = "User",
            Content = text,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });

        var response = await _chatService.SendAsync(text);

        Messages.Add(new ChatTurnViewModel
        {
            Role = "Assistant",
            Content = response,
            Timestamp = DateTime.Now.ToString("HH:mm")
        });

        IsBusy = false;
    }

    private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(UserInput);
}
