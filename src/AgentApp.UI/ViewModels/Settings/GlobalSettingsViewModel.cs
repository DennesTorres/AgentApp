using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Settings;

public partial class GlobalSettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICredentialService _credentialService;
    private const string CredentialName = "AgentApp:AzureModelKey";

    // C-012: Global settings fields
    [ObservableProperty] private string _rootProjectFolderPath = string.Empty;
    [ObservableProperty] private int _maxGateRetries = 3;
    [ObservableProperty] private bool _requireUserConfirmationForInternalLearning;
    [ObservableProperty] private bool _requireUserConfirmationForFindingsExtraction;
    [ObservableProperty] private int _tokenThresholdForContextReset = 80000;
    [ObservableProperty] private string _sourceControlRoot = string.Empty;

    // C-014: Avatar letters
    [ObservableProperty] private string _agentAvatarLetter = "T";
    [ObservableProperty] private string _userAvatarLetter = "U";

    // API key (from existing ApiKeySettingsViewModel, unified here)
    [ObservableProperty] private string _apiKey = string.Empty;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public GlobalSettingsViewModel(ISettingsRepository settingsRepository, ICredentialService credentialService)
    {
        _settingsRepository = settingsRepository;
        _credentialService = credentialService;
        _apiKey = _credentialService.ReadCredential(CredentialName) ?? string.Empty;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        RootProjectFolderPath = settings.RootProjectFolderPath;
        MaxGateRetries = settings.MaxGateRetries;
        RequireUserConfirmationForInternalLearning = settings.RequireUserConfirmationForInternalLearning;
        RequireUserConfirmationForFindingsExtraction = settings.RequireUserConfirmationForFindingsExtraction;
        TokenThresholdForContextReset = settings.TokenThresholdForContextReset;
        SourceControlRoot = settings.SourceControlRoot;
        AgentAvatarLetter = settings.AgentAvatarLetter;
        UserAvatarLetter = settings.UserAvatarLetter;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = new GlobalSettings
        {
            RootProjectFolderPath = RootProjectFolderPath.Trim(),
            MaxGateRetries = MaxGateRetries,
            RequireUserConfirmationForInternalLearning = RequireUserConfirmationForInternalLearning,
            RequireUserConfirmationForFindingsExtraction = RequireUserConfirmationForFindingsExtraction,
            TokenThresholdForContextReset = TokenThresholdForContextReset,
            SourceControlRoot = SourceControlRoot.Trim(),
            AgentAvatarLetter = string.IsNullOrWhiteSpace(AgentAvatarLetter) ? "T" : AgentAvatarLetter.Trim()[..1],
            UserAvatarLetter = string.IsNullOrWhiteSpace(UserAvatarLetter) ? "U" : UserAvatarLetter.Trim()[..1]
        };
        await _settingsRepository.SaveGlobalSettingsAsync(settings);

        if (!string.IsNullOrWhiteSpace(ApiKey))
            _credentialService.WriteCredential(CredentialName, ApiKey.Trim());

        StatusMessage = "Settings saved. Restart the app for some changes to take effect.";
    }
}
