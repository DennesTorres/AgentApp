namespace AgentApp.Domain.Interfaces;

public interface IOnboardingService
{
    Task<bool> IsOnboardingRequiredAsync();
    Task<bool> IsSourceControlRootSetAsync();
}
