using AgentApp.Domain.Learning;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonLearningSessionRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonLearningSessionRepository _sut;

    public JsonLearningSessionRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonLearningSessionRepository(_testFolder);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyStore_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_NewSession_CanBeRetrievedById()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);

        await _sut.SaveAsync(session);
        var loaded = await _sut.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);
        Assert.Equal(session.Id, loaded.Id);
        Assert.Equal(LearningTrigger.InternalGateFailure, loaded.Trigger);
        Assert.Equal("violation", loaded.ViolationDescription);
        Assert.Equal(LearningOutcome.Pending, loaded.Outcome);
    }

    [Fact]
    public async Task GetAllPendingAsync_ReturnsOnlyPendingSessions()
    {
        var pending = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation1", null);
        var cancelled = LearningSession.Initiate(LearningTrigger.ExternalUserError, "violation2", null);
        cancelled.Cancel();

        await _sut.SaveAsync(pending);
        await _sut.SaveAsync(cancelled);

        var result = await _sut.GetAllPendingAsync();

        Assert.Single(result);
        Assert.Equal(pending.Id, result[0].Id);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        await _sut.SaveAsync(session);

        session.Cancel();
        await _sut.SaveAsync(session);

        var all = await _sut.GetAllPendingAsync();
        Assert.Empty(all);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
