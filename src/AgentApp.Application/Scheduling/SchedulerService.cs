using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Scheduling;

namespace AgentApp.Application.Scheduling;

public class SchedulerService
{
    private readonly IScheduleRepository _repository;

    public SchedulerService(IScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> IsDueAsync(ScheduledJobType jobType)
    {
        var schedule = await _repository.GetByJobTypeAsync(jobType);
        if (schedule == null || !schedule.IsEnabled) return false;
        if (!schedule.LastRunAt.HasValue) return true;
        return DateTimeOffset.UtcNow >= schedule.NextRunAt;
    }

    public async Task RecordRunAsync(ScheduledJobType jobType)
    {
        var schedule = await _repository.GetByJobTypeAsync(jobType);
        if (schedule == null) return;
        schedule.UpdateLastRun(DateTimeOffset.UtcNow);
        await _repository.SaveAsync(schedule);
    }

    public async Task<IReadOnlyList<SchedulerJobStatus>> GetAllStatusesAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.Select(s => new SchedulerJobStatus(s)).ToList();
    }
}
