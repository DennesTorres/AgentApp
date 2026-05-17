using AgentApp.Application.Learning;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Learning;
using AgentApp.Domain.Rules;
using NSubstitute;

namespace AgentApp.Application.Tests.Learning;

public class LearningProcessServiceTests
{
    private readonly ILearningSessionRepository _sessionRepository;
    private readonly ILearningLogger _logger;
    private readonly IMdFileRepository _mdFileRepository;
    private readonly LearningProcessService _sut;

    private static readonly ProposedRuleChange SampleChange =
        ProposedRuleChange.Create("coding-standards", "## New Rule\nAlways verify.",
            "Add verification rule", "add");

    public LearningProcessServiceTests()
    {
        _sessionRepository = Substitute.For<ILearningSessionRepository>();
        _logger = Substitute.For<ILearningLogger>();
        _mdFileRepository = Substitute.For<IMdFileRepository>();
        _sut = new LearningProcessService(_sessionRepository, _logger, _mdFileRepository);
    }

    [Fact]
    public async Task InitiateAsync_SavesAndReturnsSession()
    {
        var session = await _sut.InitiateAsync(LearningTrigger.InternalGateFailure, "gate failed");

        Assert.Equal(LearningOutcome.Pending, session.Outcome);
        await _sessionRepository.Received(1).SaveAsync(Arg.Is<LearningSession>(s =>
            s.Trigger == LearningTrigger.InternalGateFailure));
        await _logger.Received(1).LogAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ProposeChangeAsync_UpdatesAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        _sessionRepository.GetByIdAsync(session.Id).Returns(session);

        var updated = await _sut.ProposeChangeAsync(session.Id, SampleChange);

        Assert.Equal(1, updated.IterationCount);
        await _sessionRepository.Received(1).SaveAsync(Arg.Any<LearningSession>());
    }

    [Fact]
    public async Task ApproveAsync_WritesRuleToMdFileAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        session.ProposeChange(SampleChange);
        _sessionRepository.GetByIdAsync(session.Id).Returns(session);
        _mdFileRepository.GetByNameAsync("coding-standards", MdFileScope.Global, null).Returns((MdFile?)null);

        var updated = await _sut.ApproveAsync(session.Id);

        Assert.Equal(LearningOutcome.Approved, updated.Outcome);
        await _mdFileRepository.Received(1).SaveAsync(Arg.Any<MdFile>());
        await _sessionRepository.Received(1).SaveAsync(Arg.Any<LearningSession>());
    }

    [Fact]
    public async Task ApproveAsync_ExistingMdFile_UpdatesContent()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        session.ProposeChange(SampleChange);
        _sessionRepository.GetByIdAsync(session.Id).Returns(session);
        var existingFile = MdFile.CreateGlobal("coding-standards", "## Existing rule");
        _mdFileRepository.GetByNameAsync("coding-standards", MdFileScope.Global, null).Returns(existingFile);

        await _sut.ApproveAsync(session.Id);

        await _mdFileRepository.Received(1).SaveAsync(Arg.Is<MdFile>(f =>
            f.Content.Contains("## New Rule")));
    }

    [Fact]
    public async Task RejectAsync_UpdatesOutcomeAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.ExternalUserError, "error", null);
        session.ProposeChange(SampleChange);
        _sessionRepository.GetByIdAsync(session.Id).Returns(session);

        var updated = await _sut.RejectAsync(session.Id, "Too vague");

        Assert.Equal(LearningOutcome.Rejected, updated.Outcome);
        Assert.Equal("Too vague", updated.RejectionReason);
        await _sessionRepository.Received(1).SaveAsync(Arg.Any<LearningSession>());
        await _logger.Received(1).LogAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task CancelAsync_SetsCancelledAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        _sessionRepository.GetByIdAsync(session.Id).Returns(session);

        var updated = await _sut.CancelAsync(session.Id);

        Assert.Equal(LearningOutcome.Cancelled, updated.Outcome);
        await _sessionRepository.Received(1).SaveAsync(Arg.Any<LearningSession>());
    }
}
