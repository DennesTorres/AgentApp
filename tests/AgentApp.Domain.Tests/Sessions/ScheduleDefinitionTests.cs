using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Scheduling;

namespace AgentApp.Domain.Tests.Sessions;

public class ScheduleDefinitionTests
{
    [Fact]
    public void Create_ValidInputs_SetsDefaults()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);

        Assert.NotEqual(Guid.Empty, schedule.Id);
        Assert.Equal(ScheduledJobType.ReviewAgent, schedule.JobType);
        Assert.Equal(TimeSpan.FromHours(24), schedule.Interval);
        Assert.True(schedule.IsEnabled);
        Assert.Null(schedule.LastRunAt);
    }

    [Fact]
    public void Create_ZeroInterval_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.Zero, true));
    }

    [Fact]
    public void NextRunAt_WhenNeverRun_ReturnsNull()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);

        Assert.Null(schedule.NextRunAt);
    }

    [Fact]
    public void NextRunAt_WhenLastRunSet_ReturnsLastRunPlusInterval()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        var lastRun = DateTimeOffset.UtcNow.AddHours(-6);
        schedule.UpdateLastRun(lastRun);

        var expected = lastRun + TimeSpan.FromHours(24);
        Assert.Equal(expected, schedule.NextRunAt);
    }

    [Fact]
    public void NextRunAt_WhenDisabled_ReturnsNull()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), false);
        schedule.UpdateLastRun(DateTimeOffset.UtcNow.AddHours(-30));

        Assert.Null(schedule.NextRunAt);
    }

    [Fact]
    public void SetEnabled_TogglesEnabledState()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.RollingWindowArchive, TimeSpan.FromHours(1), true);

        schedule.SetEnabled(false);

        Assert.False(schedule.IsEnabled);
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var lastRun = DateTimeOffset.UtcNow.AddHours(-2);
        var createdAt = DateTimeOffset.UtcNow.AddDays(-10);

        var schedule = ScheduleDefinition.Reconstitute(
            id, ScheduledJobType.ReviewAgent, TimeSpan.FromHours(12), true, lastRun, createdAt);

        Assert.Equal(id, schedule.Id);
        Assert.Equal(ScheduledJobType.ReviewAgent, schedule.JobType);
        Assert.Equal(TimeSpan.FromHours(12), schedule.Interval);
        Assert.Equal(lastRun, schedule.LastRunAt);
    }
}
