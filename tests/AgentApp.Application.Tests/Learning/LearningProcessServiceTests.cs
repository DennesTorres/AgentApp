using AgentApp.Application.Learning;
using AgentApp.Domain.Learning;
using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Learning;

public class LearningProcessServiceTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonLearningSessionRepository _sessionRepository;
    private readonly FileSystemLearningLogger _logger;
    private readonly JsonMdFileRepository _mdFileRepository;
    private readonly LearningProcessService _sut;

    private static readonly ProposedRuleChange SampleChange =
        ProposedRuleChange.Create("coding-standards", "## New Rule\nAlways verify.",
            "Add verification rule", "add");

    public LearningProcessServiceTests()
    {
        _sessionRepository = new JsonLearningSessionRepository(_tempFolder);
        _logger = new FileSystemLearningLogger(_tempFolder);
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
        var logFile = Path.Combine(_tempFolder, "learning-log.txt");
        Assert.True(File.Exists(logFile));
    }

    [Fact]
    public async Task ProposeChangeAsync_UpdatesAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");
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
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");
        session.ProposeChange(SampleChange);
        await _sessionRepository.SaveAsync(session);

        var updated = await _sut.ApproveAsync(session.Id);

        Assert.Equal(LearningOutcome.Approved, updated.Outcome);
        var mdFile = await _mdFileRepository.GetByNameAsync("coding-standards", MdFileScope.Global, null);
        Assert.NotNull(mdFile);
    }

    [Fact]
    public async Task ApproveAsync_ExistingMdFile_UpdatesContent()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");
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
        var session = LearningSession.Initiate(LearningTrigger.ExternalUserError, "error");
        session.ProposeChange(SampleChange);
        await _sessionRepository.SaveAsync(session);

        var updated = await _sut.RejectAsync(session.Id, "Too vague");

        Assert.Equal(LearningOutcome.Rejected, updated.Outcome);
        Assert.Equal("Too vague", updated.RejectionReason);
        var logFile = Path.Combine(_tempFolder, "learning-log.txt");
        Assert.True(File.Exists(logFile));
    }

    [Fact]
    public async Task CancelAsync_SetsCancelledAndSavesSession()
    {
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");
        await _sessionRepository.SaveAsync(session);

        var updated = await _sut.CancelAsync(session.Id);

        Assert.Equal(LearningOutcome.Cancelled, updated.Outcome);
    }
}
