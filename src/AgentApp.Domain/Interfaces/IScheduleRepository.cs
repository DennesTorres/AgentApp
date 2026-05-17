using AgentApp.Domain.Scheduling;

namespace AgentApp.Domain.Interfaces;

public interface IScheduleRepository
{
    Task<ScheduleDefinition?> GetByJobTypeAsync(ScheduledJobType jobType);
    Task<IReadOnlyList<ScheduleDefinition>> GetAllAsync();
    Task SaveAsync(ScheduleDefinition definition);
}
