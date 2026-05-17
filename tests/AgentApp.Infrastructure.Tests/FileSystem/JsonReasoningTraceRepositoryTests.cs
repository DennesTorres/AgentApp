using AgentApp.Domain.Reasoning;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonReasoningTraceRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonReasoningTraceRepository _sut;

    public JsonReasoningTraceRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonReasoningTraceRepository(_testFolder);
    }

    [Fact]
    public async Task GetBySessionIdAsync_EmptyStore_ReturnsEmptyList()
    {
        var result = await _sut.GetBySessionIdAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_Trace_CanBeRetrievedBySessionId()
    {
        var sessionId = Guid.NewGuid();
        var trace = ReasoningTrace.Capture(sessionId, Guid.NewGuid(), "Reasoning: chose approach A over B.");

        await _sut.SaveAsync(trace);
        var result = await _sut.GetBySessionIdAsync(sessionId);

        Assert.Single(result);
        Assert.Equal(trace.Id, result[0].Id);
        Assert.Equal(trace.Content, result[0].Content);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTrace_ReturnsTrace()
    {
        var sessionId = Guid.NewGuid();
        var trace = ReasoningTrace.Capture(sessionId, Guid.NewGuid(), "Some detailed reasoning content here.");

        await _sut.SaveAsync(trace);
        var result = await _sut.GetByIdAsync(trace.Id);

        Assert.NotNull(result);
        Assert.Equal(trace.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_DifferentSessions_StoredIndependently()
    {
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();
        var trace1 = ReasoningTrace.Capture(session1, Guid.NewGuid(), "Session 1 reasoning content here.");
        var trace2 = ReasoningTrace.Capture(session2, Guid.NewGuid(), "Session 2 reasoning content here.");

        await _sut.SaveAsync(trace1);
        await _sut.SaveAsync(trace2);

        var s1 = await _sut.GetBySessionIdAsync(session1);
        var s2 = await _sut.GetBySessionIdAsync(session2);
        Assert.Single(s1);
        Assert.Single(s2);
        Assert.Equal(trace1.Id, s1[0].Id);
        Assert.Equal(trace2.Id, s2[0].Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
