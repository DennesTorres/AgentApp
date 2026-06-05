using AgentApp.Application.Scheduling;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Scheduling;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Sessions;

public class SchedulerServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly IScheduleRepository _repository;
    private readonly SchedulerService _sut;

    public SchedulerServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
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

        var updated = await _repository.GetByJobTypeAsync(ScheduledJobType.ReviewAgent);
        Assert.NotNull(updated);
        Assert.True(updated.LastRunAt.HasValue);
    }

    [Fact]
    public async Task GetAllStatusesAsync_ReturnsStatusForAllDefinitions()
    {
        await _repository.SaveAsync(ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true));
        await _repository.SaveAsync(ScheduleDefinition.Create(ScheduledJobType.RollingWindowArchive, TimeSpan.FromHours(6), false));

        var result = await _sut.GetAllStatusesAsync();

        Assert.Equal(2, result.Count);
    }

    // C-080: SchedulerService responds to startup check — IsDueAsync returns true for
    // a configured, never-run enabled schedule (simulates what the startup DispatcherTimer does)
    [Fact]
    public async Task SchedulerService_OnStartup_StartsChecking()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        await _repository.SaveAsync(schedule);

        // Simulate startup timer check: IsDueAsync should return true for a never-run schedule
        var isDue = await _sut.IsDueAsync(ScheduledJobType.ReviewAgent);

        Assert.True(isDue, "Scheduler should report ReviewAgent as due on startup when never run.");
    }
}
