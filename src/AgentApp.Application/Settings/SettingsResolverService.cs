using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Settings;

public class SettingsResolverService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IProjectSettingsRepository _projectSettingsRepository;

    public SettingsResolverService(
        ISettingsRepository settingsRepository,
        IProjectSettingsRepository projectSettingsRepository)
    {
        _settingsRepository = settingsRepository;
        _projectSettingsRepository = projectSettingsRepository;
    }

    public async Task<EffectiveSettings> ResolveAsync(Guid? projectId)
    {
        var global = await _settingsRepository.GetGlobalSettingsAsync();
        var project = projectId.HasValue
            ? await _projectSettingsRepository.GetByProjectIdAsync(projectId.Value)
            : null;

        return new EffectiveSettings(
            maxGateRetries: project?.MaxGateRetries ?? global.MaxGateRetries,
            requireConfirmationInternal: project?.RequireUserConfirmationForInternalLearning ?? global.RequireUserConfirmationForInternalLearning,
            requireConfirmationFindings: project?.RequireUserConfirmationForFindingsExtraction ?? global.RequireUserConfirmationForFindingsExtraction,
            tokenThreshold: project?.TokenThresholdForContextReset ?? global.TokenThresholdForContextReset);
    }
}
