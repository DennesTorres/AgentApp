using AgentApp.Application.Reasoning;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Reasoning;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Reasoning;

public class ReasoningTraceServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly IReasoningTraceRepository _repository;
    private readonly ReasoningTraceService _sut;

    public ReasoningTraceServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
        _repository = new JsonReasoningTraceRepository(_tempFolder);
        _sut = new ReasoningTraceService(_repository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task CaptureAsync_ValidInputs_SavesTraceAndReturnsReferenceId()
    {
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var referenceId = await _sut.CaptureAsync(sessionId, messageId, "Model evaluated approach X over Y.");

        Assert.NotEqual(Guid.Empty, referenceId);
        var saved = await _repository.GetByIdAsync(referenceId);
        Assert.NotNull(saved);
        Assert.Equal(sessionId, saved.SessionId);
        Assert.Equal(messageId, saved.MessageId);
    }

    [Fact]
    public async Task GetByReferenceIdAsync_ExistingId_ReturnsTrace()
    {
        var trace = ReasoningTrace.Reconstitute(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Some reasoning", DateTimeOffset.UtcNow);
        await _repository.SaveAsync(trace);

        var result = await _sut.GetByReferenceIdAsync(trace.Id);

        Assert.NotNull(result);
        Assert.Equal(trace.Id, result.Id);
    }

    [Fact]
    public async Task GetByReferenceIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sut.GetByReferenceIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
