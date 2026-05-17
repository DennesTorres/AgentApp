using AgentApp.Application.Reasoning;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Reasoning;
using NSubstitute;

namespace AgentApp.Application.Tests.Reasoning;

public class ReasoningTraceServiceTests
{
    private readonly IReasoningTraceRepository _repository;
    private readonly ReasoningTraceService _sut;

    public ReasoningTraceServiceTests()
    {
        _repository = Substitute.For<IReasoningTraceRepository>();
        _sut = new ReasoningTraceService(_repository);
    }

    [Fact]
    public async Task CaptureAsync_ValidInputs_SavesTraceAndReturnsReferenceId()
    {
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var referenceId = await _sut.CaptureAsync(sessionId, messageId, "Model evaluated approach X over Y.");

        Assert.NotEqual(Guid.Empty, referenceId);
        await _repository.Received(1).SaveAsync(Arg.Is<ReasoningTrace>(t =>
            t.SessionId == sessionId &&
            t.MessageId == messageId &&
            t.Id == referenceId));
    }

    [Fact]
    public async Task GetByReferenceIdAsync_ExistingId_ReturnsTrace()
    {
        var referenceId = Guid.NewGuid();
        var expected = ReasoningTrace.Reconstitute(
            referenceId, Guid.NewGuid(), Guid.NewGuid(), "Some reasoning", DateTimeOffset.UtcNow);
        _repository.GetByIdAsync(referenceId).Returns(expected);

        var result = await _sut.GetByReferenceIdAsync(referenceId);

        Assert.NotNull(result);
        Assert.Equal(referenceId, result.Id);
    }

    [Fact]
    public async Task GetByReferenceIdAsync_NonExistingId_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((ReasoningTrace?)null);

        var result = await _sut.GetByReferenceIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
