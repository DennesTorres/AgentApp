using System.IO;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AgentApp.UI.ViewModels.Settings;

public partial class GlobalSettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICredentialService _credentialService;
    private const string CredentialName = "AgentApp:AzureModelKey";
    private const string DefaultModelUrl = "https://msdnfoundry.services.ai.azure.com/models";
    private const string DefaultModelName = "Kimi-K2.5";

    [ObservableProperty] private string _rootProjectFolderPath = string.Empty;
    [ObservableProperty] private string _modelUrl = string.Empty;
    [ObservableProperty] private string _modelName = string.Empty;
    [ObservableProperty] private int _maxGateRetries = 3;
    [ObservableProperty] private bool _requireUserConfirmationForInternalLearning;
    [ObservableProperty] private bool _requireUserConfirmationForFindingsExtraction;
    [ObservableProperty] private int _tokenThresholdForContextReset = 80000;
    [ObservableProperty] private string _sourceControlRoot = string.Empty;
    [ObservableProperty] private string _agentAvatarLetter = "T";
    [ObservableProperty] private string _userAvatarLetter = "U";
    [ObservableProperty] private string _agentAvatarImagePath = string.Empty;
    [ObservableProperty] private string _userAvatarImagePath = string.Empty;
    [ObservableProperty] private string _agentAvatarPreset = "blue";
    [ObservableProperty] private string _userAvatarPreset = "teal";
    // C-044: shape preset ("person"|"robot"|"star"|"cat")
    [ObservableProperty] private string _agentAvatarShape = "person";
    [ObservableProperty] private string _userAvatarShape = "person";

    // C-026: API key status indicator
    [ObservableProperty] private bool _isApiKeySet;
    [ObservableProperty] private string _newApiKey = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // C-047/C-049: unsaved changes indicator + event for chat to reload avatars after save
    [ObservableProperty] private bool _hasUnsavedChanges;
    public event EventHandler? SettingsSaved;

    // C-055: suppress HasUnsavedChanges during initial load
    private bool _isLoading;

    public GlobalSettingsViewModel(ISettingsRepository settingsRepository, ICredentialService credentialService)
    {
        _settingsRepository = settingsRepository;
        _credentialService = credentialService;
        var existing = _credentialService.ReadCredential(CredentialName);
        IsApiKeySet = !string.IsNullOrWhiteSpace(existing);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _isLoading = true;
        var settings = await _settingsRepository.GetGlobalSettingsAsync();

        // C-056: default to Tower canonical root; clear old "C:\GitHub" written by C-028
        var storedRoot = settings.RootProjectFolderPath;
        var towerDefault = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tower");
        RootProjectFolderPath = string.IsNullOrEmpty(storedRoot) || storedRoot is @"C:\GitHub"
            ? towerDefault
            : storedRoot;

        // C-027: model URL with default
        ModelUrl = string.IsNullOrWhiteSpace(settings.ModelUrl)
            ? DefaultModelUrl
            : settings.ModelUrl;

        // C-037: model name with default
        ModelName = string.IsNullOrWhiteSpace(settings.ModelName)
            ? DefaultModelName
            : settings.ModelName;

        MaxGateRetries = settings.MaxGateRetries;
        RequireUserConfirmationForInternalLearning = settings.RequireUserConfirmationForInternalLearning;
        RequireUserConfirmationForFindingsExtraction = settings.RequireUserConfirmationForFindingsExtraction;
        TokenThresholdForContextReset = settings.TokenThresholdForContextReset;
        SourceControlRoot = settings.SourceControlRoot;
        AgentAvatarLetter = settings.AgentAvatarLetter;
        UserAvatarLetter = settings.UserAvatarLetter;
        AgentAvatarImagePath = settings.AgentAvatarImagePath;
        UserAvatarImagePath = settings.UserAvatarImagePath;
        AgentAvatarPreset = string.IsNullOrWhiteSpace(settings.AgentAvatarPreset) ? "blue" : settings.AgentAvatarPreset;
        UserAvatarPreset = string.IsNullOrWhiteSpace(settings.UserAvatarPreset) ? "teal" : settings.UserAvatarPreset;
        AgentAvatarShape = string.IsNullOrWhiteSpace(settings.AgentAvatarShape) ? "person" : settings.AgentAvatarShape;
        UserAvatarShape = string.IsNullOrWhiteSpace(settings.UserAvatarShape) ? "person" : settings.UserAvatarShape;
        _isLoading = false;
    }

    // C-049: mark unsaved on any user-editable property change (not status/key-state fields)
    // C-055: skip during LoadAsync to prevent startup false positive
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (!_isLoading && e.PropertyName is not (nameof(HasUnsavedChanges) or nameof(StatusMessage) or nameof(IsApiKeySet) or nameof(NewApiKey)))
            HasUnsavedChanges = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Save API key if a new one was entered
        if (!string.IsNullOrWhiteSpace(NewApiKey))
        {
            _credentialService.WriteCredential(CredentialName, NewApiKey.Trim());
            IsApiKeySet = true;
            NewApiKey = string.Empty;
        }

        var settings = new GlobalSettings
        {
            RootProjectFolderPath = RootProjectFolderPath.Trim(),
            ModelUrl = ModelUrl.Trim(),
            ModelName = ModelName.Trim(),
            MaxGateRetries = MaxGateRetries,
            RequireUserConfirmationForInternalLearning = RequireUserConfirmationForInternalLearning,
            RequireUserConfirmationForFindingsExtraction = RequireUserConfirmationForFindingsExtraction,
            TokenThresholdForContextReset = TokenThresholdForContextReset,
            SourceControlRoot = SourceControlRoot.Trim(),
            AgentAvatarLetter = string.IsNullOrWhiteSpace(AgentAvatarLetter) ? "T" : AgentAvatarLetter.Trim()[..1],
            UserAvatarLetter = string.IsNullOrWhiteSpace(UserAvatarLetter) ? "U" : UserAvatarLetter.Trim()[..1],
            AgentAvatarImagePath = AgentAvatarImagePath.Trim(),
            UserAvatarImagePath = UserAvatarImagePath.Trim(),
            AgentAvatarPreset = AgentAvatarPreset,
            UserAvatarPreset = UserAvatarPreset,
            AgentAvatarShape = AgentAvatarShape,
            UserAvatarShape = UserAvatarShape,
        };
        await _settingsRepository.SaveGlobalSettingsAsync(settings);
        HasUnsavedChanges = false;
        StatusMessage = "Settings saved.";
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    // C-056: Browse for root project folder
    [RelayCommand]
    private void BrowseRootProjectFolder()
    {
        var dlg = new OpenFolderDialog { Title = "Select root project folder (%USERPROFILE%\\.tower)" };
        if (dlg.ShowDialog() == true)
            RootProjectFolderPath = dlg.FolderName;
    }

    // C-025: Browse for avatar images
    [RelayCommand]
    private void BrowseAgentAvatar()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select agent avatar image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };
        if (dlg.ShowDialog() == true)
            AgentAvatarImagePath = dlg.FileName;
    }

    [RelayCommand]
    private void BrowseUserAvatar()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select user avatar image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };
        if (dlg.ShowDialog() == true)
            UserAvatarImagePath = dlg.FileName;
    }

    // C-039: Color preset selection commands
    [RelayCommand]
    private void SelectAgentPreset(string preset) => AgentAvatarPreset = preset;

    [RelayCommand]
    private void SelectUserPreset(string preset) => UserAvatarPreset = preset;

    // C-044: Shape preset selection commands
    [RelayCommand]
    private void SelectAgentShape(string shape) => AgentAvatarShape = shape;

    [RelayCommand]
    private void SelectUserShape(string shape) => UserAvatarShape = shape;
}
