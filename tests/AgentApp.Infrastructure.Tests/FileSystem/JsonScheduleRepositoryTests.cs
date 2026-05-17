using AgentApp.Domain.Scheduling;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonScheduleRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonScheduleRepository _sut;

    public JsonScheduleRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonScheduleRepository(_testFolder);
    }

    [Fact]
    public async Task GetByJobTypeAsync_EmptyStore_ReturnsNull()
    {
        var result = await _sut.GetByJobTypeAsync(ScheduledJobType.ReviewAgent);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_Schedule_CanBeRetrievedByJobType()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);

        await _sut.SaveAsync(schedule);
        var result = await _sut.GetByJobTypeAsync(ScheduledJobType.ReviewAgent);

        Assert.NotNull(result);
        Assert.Equal(schedule.Id, result.Id);
        Assert.Equal(TimeSpan.FromHours(24), result.Interval);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSchedules()
    {
        await _sut.SaveAsync(ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true));
        await _sut.SaveAsync(ScheduleDefinition.Create(ScheduledJobType.RollingWindowArchive, TimeSpan.FromHours(6), false));

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SaveAsync_UpdatedSchedule_OverwritesExisting()
    {
        var schedule = ScheduleDefinition.Create(ScheduledJobType.ReviewAgent, TimeSpan.FromHours(24), true);
        await _sut.SaveAsync(schedule);

        schedule.UpdateLastRun(DateTimeOffset.UtcNow);
        await _sut.SaveAsync(schedule);

        var result = await _sut.GetByJobTypeAsync(ScheduledJobType.ReviewAgent);
        Assert.NotNull(result!.LastRunAt);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
