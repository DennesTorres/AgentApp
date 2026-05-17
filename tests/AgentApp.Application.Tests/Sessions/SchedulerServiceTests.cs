using AgentApp.Application.Scheduling;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Scheduling;
using NSubstitute;

namespace AgentApp.Application.Tests.Sessions;

public class SchedulerServiceTests
{
    private readonly IScheduleRepository _repository;
    private readonly SchedulerService _sut;

    public SchedulerServiceTests()
    {
        _repository = Substitute.For<IScheduleRepository>();
        _sut = new SchedulerService(_repository);
    }

    [Fact]
    public async Task IsDueAsync_EnabledAndOverdue_ReturnsTrue()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        schedule.UpdateLastRun(DateTimeOffset.UtcNow.AddHours(-25));
        _repository.GetByJobTypeAsync(ScheduledJobType.ReviewAgent).Returns(schedule);

        var result = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.True(result);
    }

    [Fact]
    public async Task IsDueAsync_EnabledAndNotDue_ReturnsFalse()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        schedule.UpdateLastRun(DateTimeOffset.UtcNow.AddHours(-6));
        _repository.GetByJobTypeAsync(ScheduledJobType.ReviewAgent).Returns(schedule);

        var result = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.False(result);
    }

    [Fact]
    public async Task IsDueAsync_Disabled_ReturnsFalse()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), false);
        schedule.UpdateLastRun(DateTimeOffset.UtcNow.AddHours(-48));
        _repository.GetByJobTypeAsync(ScheduledJobType.ReviewAgent).Returns(schedule);

        var result = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.False(result);
    }

    [Fact]
    public async Task IsDueAsync_NeverRun_ReturnsTrueWhenEnabled()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        _repository.GetByJobTypeAsync(ScheduledJobType.ReviewAgent).Returns(schedule);

        var result = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.True(result);
    }

    [Fact]
    public async Task RecordRunAsync_UpdatesLastRunAndSaves()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        _repository.GetByJobTypeAsync(ScheduledJobType.ReviewAgent).Returns(schedule);

        await _sut.RecordRunAsync(ScheduledJobType.ReviewAgent);

        await _repository.Received(1).SaveAsync(Arg.Is<ScheduleDefinition>(s =>
            s.JobType == ScheduledJobType.ReviewAgent && s.LastRunAt.HasValue));
    }

    [Fact]
    public async Task GetAllStatusesAsync_ReturnsStatusForAllDefinitions()
    {
        var schedules = new List<ScheduleDefinition>
        {
            ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true),
            ScheduleDefinition.Create(ScheduledJobType.RollingWindowArchive, TimeSpan.FromHours(6), false)
        };
        _repository.GetAllAsync().Returns(schedules);

        var result = await _sut.GetAllStatusesAsync();

        Assert.Equal(2, result.Count);
    }
}
