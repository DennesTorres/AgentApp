using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Settings;

public partial class ProjectSettingsViewModel : ObservableObject
{
    private readonly IProjectSettingsRepository _projectSettingsRepository;

    // C-013: Project-level overrides (null = use global)
    [ObservableProperty] private string _projectId = string.Empty;
    [ObservableProperty] private string _maxGateRetries = string.Empty;
    [ObservableProperty] private bool? _requireUserConfirmationForInternalLearning;
    [ObservableProperty] private bool? _requireUserConfirmationForFindingsExtraction;
    [ObservableProperty] private string _tokenThresholdForContextReset = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ProjectSettingsViewModel(IProjectSettingsRepository projectSettingsRepository)
    {
        _projectSettingsRepository = projectSettingsRepository;
    }

    [RelayCommand]
    private async Task LoadProjectSettingsAsync()
    {
        if (!Guid.TryParse(ProjectId, out var id))
        {
            StatusMessage = "Enter a valid project ID.";
            return;
        }

        var settings = await _projectSettingsRepository.GetByProjectIdAsync(id);
        if (settings is null)
        {
            MaxGateRetries = string.Empty;
            RequireUserConfirmationForInternalLearning = null;
            RequireUserConfirmationForFindingsExtraction = null;
            TokenThresholdForContextReset = string.Empty;
            StatusMessage = "No project-specific settings found — using global defaults.";
            return;
        }

        MaxGateRetries = settings.MaxGateRetries?.ToString() ?? string.Empty;
        RequireUserConfirmationForInternalLearning = settings.RequireUserConfirmationForInternalLearning;
        RequireUserConfirmationForFindingsExtraction = settings.RequireUserConfirmationForFindingsExtraction;
        TokenThresholdForContextReset = settings.TokenThresholdForContextReset?.ToString() ?? string.Empty;
        StatusMessage = "Project settings loaded.";
    }

    [RelayCommand]
    private async Task SaveProjectSettingsAsync()
    {
        if (!Guid.TryParse(ProjectId, out var id))
        {
            StatusMessage = "Enter a valid project ID.";
            return;
        }

        int? retries = int.TryParse(MaxGateRetries, out var r) ? r : null;
        int? threshold = int.TryParse(TokenThresholdForContextReset, out var t) ? t : null;

        var settings = ProjectSettings.Reconstitute(
            id, retries,
            RequireUserConfirmationForInternalLearning,
            RequireUserConfirmationForFindingsExtraction,
            threshold,
            DateTimeOffset.UtcNow);

        await _projectSettingsRepository.SaveAsync(settings);
        StatusMessage = "Project settings saved.";
    }

    [RelayCommand]
    private async Task ClearProjectSettingsAsync()
    {
        if (!Guid.TryParse(ProjectId, out var id))
        {
            StatusMessage = "Enter a valid project ID.";
            return;
        }

        // Save empty overrides to remove project-specific configuration
        var settings = ProjectSettings.Create(id);
        await _projectSettingsRepository.SaveAsync(settings);
        StatusMessage = "Project settings cleared — global defaults will be used.";
        await LoadProjectSettingsAsync();
    }
}
