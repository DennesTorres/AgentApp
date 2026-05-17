using System.Collections.ObjectModel;
using AgentApp.Application.Chat;
using AgentApp.Domain.ExecutionStates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Chat;

public partial class ChatViewModel : ObservableObject
{
    private readonly ChatService _chatService;
    private readonly IExecutionStateMachine _stateMachine;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _userInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _currentStateLabel = "Chat";

    public ObservableCollection<ChatTurnViewModel> Messages { get; } = [];
    public ObservableCollection<string> AvailableTransitions { get; } = [];

    public ChatViewModel(ChatService chatService, IExecutionStateMachine stateMachine)
    {
        _chatService = chatService;
        _stateMachine = stateMachine;
        _stateMachine.StateChanged += OnStateChanged;
        RefreshStateUi();
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

    [RelayCommand]
    private void SwitchState(string targetStateName)
    {
        if (Enum.TryParse<ExecutionStateName>(targetStateName, out var target))
            _stateMachine.TransitionTo(target);
    }

    private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(UserInput);

    private void OnStateChanged(object? sender, ExecutionStateName newState) => RefreshStateUi();

    private void RefreshStateUi()
    {
        CurrentStateLabel = _stateMachine.CurrentState.ToString();
        AvailableTransitions.Clear();
        foreach (ExecutionStateName s in Enum.GetValues<ExecutionStateName>())
            if (_stateMachine.CanTransitionTo(s))
                AvailableTransitions.Add(s.ToString());
    }
}
