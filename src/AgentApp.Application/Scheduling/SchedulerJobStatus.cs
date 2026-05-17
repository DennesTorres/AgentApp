using AgentApp.Domain.Scheduling;

namespace AgentApp.Application.Scheduling;

public class SchedulerJobStatus
{
    public ScheduledJobType JobType { get; }
    public bool IsEnabled { get; }
    public DateTimeOffset? LastRunAt { get; }
    public DateTimeOffset? NextRunAt { get; }

    public SchedulerJobStatus(ScheduleDefinition definition)
    {
        JobType = definition.JobType;
        IsEnabled = definition.IsEnabled;
        LastRunAt = definition.LastRunAt;
        NextRunAt = definition.NextRunAt;
    }
}
