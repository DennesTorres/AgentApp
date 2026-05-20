using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Onboarding;

public class OnboardingService : IOnboardingService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISettingsRepository _settingsRepository;

    public OnboardingService(IProjectRepository projectRepository, ISettingsRepository settingsRepository)
    {
        _projectRepository = projectRepository;
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> IsOnboardingRequiredAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        return projects.Count == 0;
    }

    public async Task<bool> IsSourceControlRootSetAsync()
    {
        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        return !string.IsNullOrEmpty(settings.SourceControlRoot);
    }
}
