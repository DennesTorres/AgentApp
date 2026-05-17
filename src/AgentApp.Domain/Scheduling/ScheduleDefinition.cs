using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Scheduling;

public class ScheduleDefinition
{
    public Guid Id { get; private set; }
    public ScheduledJobType JobType { get; private set; }
    public TimeSpan Interval { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? NextRunAt =>
        IsEnabled && LastRunAt.HasValue ? LastRunAt.Value + Interval : null;

    private ScheduleDefinition() { }

    public static ScheduleDefinition Create(ScheduledJobType jobType, TimeSpan interval, bool isEnabled)
    {
        if (interval <= TimeSpan.Zero)
            throw new DomainValidationException("Interval must be positive.");

        return new ScheduleDefinition
        {
            Id = Guid.NewGuid(),
            JobType = jobType,
            Interval = interval,
            IsEnabled = isEnabled,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ScheduleDefinition Reconstitute(
        Guid id, ScheduledJobType jobType, TimeSpan interval, bool isEnabled,
        DateTimeOffset? lastRunAt, DateTimeOffset createdAt) =>
        new()
        {
            Id = id, JobType = jobType, Interval = interval,
            IsEnabled = isEnabled, LastRunAt = lastRunAt, CreatedAt = createdAt
        };

    public void UpdateLastRun(DateTimeOffset runAt) => LastRunAt = runAt;

    public void SetEnabled(bool enabled) => IsEnabled = enabled;
}
