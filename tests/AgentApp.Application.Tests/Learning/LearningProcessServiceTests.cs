using AgentApp.Application.Learning;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Learning;
using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Learning;

public class LearningProcessServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly ILearningSessionRepository _sessionRepository;
    private readonly FakeLearningLogger _logger;
    private readonly IMdFileRepository _mdFileRepository;
    private readonly LearningProcessService _sut;

    private static readonly ProposedRuleChange SampleChange =
        ProposedRuleChange.Create("coding-standards", "## New Rule\nAlways verify.",
            "Add verification rule", "add");

    public LearningProcessServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
        _sessionRepository = new JsonLearningSessionRepository(_tempFolder);
        _logger = new FakeLearningLogger();
        _mdFileRepository = new JsonMdFileRepository(_tempFolder);
        _sut = new LearningProcessService(_sessionRepository, _logger, _mdFileRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task InitiateAsync_SavesAndReturnsSession()
    {
        var session = await _sut.InitiateAsync(LearningTrigger.InternalGateFailure, "gate failed");

        Assert.Equal(LearningOutcome.Pending, session.Outcome);
        var saved = await _sessionRepository.GetByIdAsync(session.Id);
        Assert.NotNull(saved);
        Assert.Equal(LearningTrigger.InternalGateFailure, saved.Trigger);
        Assert.Single(_logger.Entries);
    }

    [Fact]
    public async Task ProposeChangeAsync_UpdatesAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        await _sessionRepository.SaveAsync(session);

        var updated = await _sut.ProposeChangeAsync(session.Id, SampleChange);

        Assert.Equal(1, updated.IterationCount);
        var saved = await _sessionRepository.GetByIdAsync(session.Id);
        Assert.NotNull(saved);
        Assert.Equal(1, saved.IterationCount);
    }

    [Fact]
    public async Task ApproveAsync_WritesRuleToMdFileAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        session.ProposeChange(SampleChange);
        await _sessionRepository.SaveAsync(session);

        var updated = await _sut.ApproveAsync(session.Id);

        Assert.Equal(LearningOutcome.Approved, updated.Outcome);
        var mdFile = await _mdFileRepository.GetByNameAsync("coding-standards", MdFileScope.Global, null);
        Assert.NotNull(mdFile);
        var savedSession = await _sessionRepository.GetByIdAsync(session.Id);
        Assert.NotNull(savedSession);
        Assert.Equal(LearningOutcome.Approved, savedSession.Outcome);
    }

    [Fact]
    public async Task ApproveAsync_ExistingMdFile_UpdatesContent()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        session.ProposeChange(SampleChange);
        await _sessionRepository.SaveAsync(session);
        var existingFile = MdFile.CreateGlobal("coding-standards", "## Existing rule");
        await _mdFileRepository.SaveAsync(existingFile);

        await _sut.ApproveAsync(session.Id);

        var saved = await _mdFileRepository.GetByNameAsync("coding-standards", MdFileScope.Global, null);
        Assert.NotNull(saved);
        Assert.Contains("## New Rule", saved.Content);
    }

    [Fact]
    public async Task RejectAsync_UpdatesOutcomeAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.ExternalUserError, "error", null);
        session.ProposeChange(SampleChange);
        await _sessionRepository.SaveAsync(session);

        var updated = await _sut.RejectAsync(session.Id, "Too vague");

        Assert.Equal(LearningOutcome.Rejected, updated.Outcome);
        Assert.Equal("Too vague", updated.RejectionReason);
        var saved = await _sessionRepository.GetByIdAsync(session.Id);
        Assert.NotNull(saved);
        Assert.Equal(LearningOutcome.Rejected, saved.Outcome);
        Assert.True(_logger.Entries.Count >= 1);
    }

    [Fact]
    public async Task CancelAsync_SetsCancelledAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation", null);
        await _sessionRepository.SaveAsync(session);

        var updated = await _sut.CancelAsync(session.Id);

        Assert.Equal(LearningOutcome.Cancelled, updated.Outcome);
        var saved = await _sessionRepository.GetByIdAsync(session.Id);
        Assert.NotNull(saved);
        Assert.Equal(LearningOutcome.Cancelled, saved.Outcome);
    }
}
