using AgentApp.Application.Context;
using AgentApp.Domain.Context;
using AgentApp.Domain.Settings;
using AgentApp.Infrastructure.FileSystem;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Application.Tests.Context;

public class ContextWindowManagerTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonConversationHistoryRepository _historyRepository;
    private readonly JsonSettingsRepository _settingsRepository;
    private readonly ContextWindowManager _sut;

    public ContextWindowManagerTests()
    {
        _historyRepository = new JsonConversationHistoryRepository(_tempFolder);
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

        var archivePath = Path.Combine(_tempFolder, $"history-archive-{sessionId}.json");
        Assert.True(File.Exists(archivePath));
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

        var saved = await _historyRepository.GetBySessionIdAsync(sessionId);
        Assert.NotEmpty(saved);
    }
}
