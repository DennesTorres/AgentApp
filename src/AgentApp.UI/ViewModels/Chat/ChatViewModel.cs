using System.Collections.ObjectModel;
using AgentApp.Application.Chat;
using AgentApp.Application.Projects;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AgentApp.UI.ViewModels.Chat;

public partial class ChatViewModel : ObservableObject
{
    private readonly ChatService _chatService;
    private readonly IOnboardingService _onboardingService;
    private readonly ProjectService _projectService;
    private readonly IScaffoldService _scaffoldService;
    private readonly ISettingsRepository _settingsRepository;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _userInput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _activeProjectName = string.Empty;

    private ProjectConfirmCommand? _pendingProjectConfirm;

    [ObservableProperty]
    private bool _hasProjectConfirmPending;

    [ObservableProperty]
    private string _pendingProjectSummary = string.Empty;

    public ObservableCollection<ChatTurnViewModel> Messages { get; } = [];

    public ChatViewModel(
        ChatService chatService,
        IOnboardingService onboardingService,
        ProjectService projectService,
        IScaffoldService scaffoldService,
        ISettingsRepository settingsRepository)
    {
        _chatService = chatService;
        _onboardingService = onboardingService;
        _projectService = projectService;
        _scaffoldService = scaffoldService;
        _settingsRepository = settingsRepository;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (await _onboardingService.IsOnboardingRequiredAsync())
        {
            AddAgentMessage("Hello! I'm Tower, your AI development agent. What would you like to build today?");
        }
        else
        {
            var projects = await _projectService.GetAllProjectsAsync();
            var recent = projects.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
            if (recent is not null)
                ActiveProjectName = recent.Name;
        }
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

        var result = await _chatService.SendAsync(text);

        foreach (var command in result.Commands)
            await HandleCommandAsync(command);

        if (!string.IsNullOrWhiteSpace(result.DisplayText))
            AddAgentMessage(result.DisplayText);

        IsBusy = false;
    }

    [RelayCommand]
    private async Task ConfirmProjectAsync()
    {
        if (_pendingProjectConfirm is null) return;

        IsBusy = true;
        HasProjectConfirmPending = false;

        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        var project = await _projectService.CreateProjectAsync(
            _pendingProjectConfirm.ProjectName,
            _scaffoldService.GetCodeFolderPath(_pendingProjectConfirm.ProjectName, settings.SourceControlRoot));

        await _scaffoldService.CreateScaffoldAsync(_pendingProjectConfirm.ProjectName, settings.SourceControlRoot);
        ActiveProjectName = project.Name;
        _pendingProjectConfirm = null;

        AddAgentMessage($"Project \"{project.Name}\" created! Your agent folder and code folder are ready.");
        IsBusy = false;
    }

    [RelayCommand]
    private void CancelProject()
    {
        _pendingProjectConfirm = null;
        HasProjectConfirmPending = false;
        AddAgentMessage("No problem — let me know what you'd like to build.");
    }

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
        }
    }

    private async Task HandleFolderSelectAsync(FolderSelectCommand command)
    {
        var dialog = new OpenFolderDialog { Title = command.Reason };

        if (dialog.ShowDialog() != true)
        {
            var cancelResult = await _chatService.SendAsync("[FOLDER_SELECT_RESULT:{\"cancelled\":true}]");
            if (!string.IsNullOrWhiteSpace(cancelResult.DisplayText))
                AddAgentMessage(cancelResult.DisplayText);
            return;
        }

        var selectedPath = dialog.FolderName;

        if (!await _onboardingService.IsSourceControlRootSetAsync())
        {
            var settings = await _settingsRepository.GetGlobalSettingsAsync();
            settings.SourceControlRoot = selectedPath;
            await _settingsRepository.SaveGlobalSettingsAsync(settings);
        }

        var continueResult = await _chatService.SendAsync($"[FOLDER_SELECT_RESULT:{{\"path\":\"{selectedPath}\"}}]");

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
