using AgentApp.Application.Scheduling;
using AgentApp.Domain.Scheduling;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Sessions;

public class SchedulerServiceTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonScheduleRepository _repository;
    private readonly SchedulerService _sut;

    public SchedulerServiceTests()
    {
        _repository = new JsonScheduleRepository(_tempFolder);
        _sut = new SchedulerService(_repository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task IsDueAsync_EnabledAndOverdue_ReturnsTrue()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        schedule.UpdateLastRun(DateTimeOffset.UtcNow.AddHours(-25));
        await _repository.SaveAsync(schedule);

        var result = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.True(result);
    }

    [Fact]
    public async Task IsDueAsync_EnabledAndNotDue_ReturnsFalse()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        schedule.UpdateLastRun(DateTimeOffset.UtcNow.AddHours(-6));
        await _repository.SaveAsync(schedule);

        var result = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.False(result);
    }

    [Fact]
    public async Task IsDueAsync_Disabled_ReturnsFalse()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), false);
        schedule.UpdateLastRun(DateTimeOffset.UtcNow.AddHours(-48));
        await _repository.SaveAsync(schedule);

        var result = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.False(result);
    }

    [Fact]
    public async Task IsDueAsync_NeverRun_ReturnsTrueWhenEnabled()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        await _repository.SaveAsync(schedule);

        var result = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.True(result);
    }

    [Fact]
    public async Task RecordRunAsync_UpdatesLastRunAndSaves()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        await _repository.SaveAsync(schedule);

        await _sut.RecordRunAsync(ScheduledJobType.ReviewAgent);

        var saved = await _repository.GetByJobTypeAsync(ScheduledJobType.ReviewAgent);
        Assert.NotNull(saved);
        Assert.True(saved.LastRunAt.HasValue);
    }

    [Fact]
    public async Task GetAllStatusesAsync_ReturnsStatusForAllDefinitions()
    {
        await _repository.SaveAsync(ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true));
        await _repository.SaveAsync(ScheduleDefinition.Create(ScheduledJobType.RollingWindowArchive, TimeSpan.FromHours(6), false));

        var result = await _sut.GetAllStatusesAsync();

        Assert.Equal(2, result.Count);
    }
}
