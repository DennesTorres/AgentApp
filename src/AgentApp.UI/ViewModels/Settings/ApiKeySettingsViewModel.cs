using AgentApp.Domain.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Settings;

public partial class ApiKeySettingsViewModel : ObservableObject
{
    private const string CredentialName = "AgentApp:AzureModelKey";
    private readonly ICredentialService _credentialService;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ApiKeySettingsViewModel(ICredentialService credentialService)
    {
        _credentialService = credentialService;
        _apiKey = _credentialService.ReadCredential(CredentialName) ?? string.Empty;
    }

    [RelayCommand]
    private void SaveKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "API key must not be empty.";
            return;
        }

        _credentialService.WriteCredential(CredentialName, ApiKey.Trim());
        StatusMessage = "Key saved. Restart the app for changes to take effect.";
    }
}
