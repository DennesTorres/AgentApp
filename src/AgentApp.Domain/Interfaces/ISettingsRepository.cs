using AgentApp.Domain.Settings;

namespace AgentApp.Domain.Interfaces;

public interface ISettingsRepository
{
    Task<GlobalSettings> GetGlobalSettingsAsync();
    Task SaveGlobalSettingsAsync(GlobalSettings settings);
}
