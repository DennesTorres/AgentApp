using AgentApp.Application.Context;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Context;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Application.Tests.Context;

public class ContextWindowManagerTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly FakeConversationHistoryRepository _historyRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ContextWindowManager _sut;

    public ContextWindowManagerTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
        _historyRepository = new FakeConversationHistoryRepository();
        _settingsRepository = new JsonSettingsRepository(_tempFolder);
        _sut = new ContextWindowManager(_historyRepository, _settingsRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task NeedsResetAsync_TokensBelowThreshold_ReturnsFalse()
    {
        await _settingsRepository.SaveGlobalSettingsAsync(new GlobalSettings { TokenThresholdForContextReset = 80000 });
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "short message"));

        Assert.False(await _sut.NeedsResetAsync(ctx));
    }

    [Fact]
    public async Task NeedsResetAsync_TokensAtOrAboveThreshold_ReturnsTrue()
    {
        await _settingsRepository.SaveGlobalSettingsAsync(new GlobalSettings { TokenThresholdForContextReset = 5 });
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "A".PadRight(80, 'A')));

        Assert.True(await _sut.NeedsResetAsync(ctx));
    }

    [Fact]
    public async Task ArchiveAndResetAsync_ArchivesOldHistory()
    {
        var sessionId = Guid.NewGuid();
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "old message"));

        await _sut.ArchiveAndResetAsync(ctx, sessionId, "Summary of the previous conversation.");

        Assert.True(_historyRepository.WasArchived(sessionId));
    }

    [Fact]
    public async Task ArchiveAndResetAsync_ResetsContextWithSummarySeed()
    {
        var sessionId = Guid.NewGuid();
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "old message"));

        await _sut.ArchiveAndResetAsync(ctx, sessionId, "Summary of the previous conversation.");

        Assert.Single(ctx.Messages);
        Assert.Equal(MessageRole.System, ctx.Messages[0].Role);
        Assert.Contains("Summary", ctx.Messages[0].Content);
    }

    [Fact]
    public async Task ArchiveAndResetAsync_SavesNewContext()
    {
        var sessionId = Guid.NewGuid();
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "old message"));

        await _sut.ArchiveAndResetAsync(ctx, sessionId, "Summary");

        var saved = _historyRepository.GetSaved(sessionId);
        Assert.NotEmpty(saved);
    }
}
