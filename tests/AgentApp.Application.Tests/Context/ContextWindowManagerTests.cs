using AgentApp.Application.Context;
using AgentApp.Domain.Context;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Settings;
using NSubstitute;

namespace AgentApp.Application.Tests.Context;

public class ContextWindowManagerTests
{
    private readonly IConversationHistoryRepository _historyRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ContextWindowManager _sut;

    public ContextWindowManagerTests()
    {
        _historyRepository = Substitute.For<IConversationHistoryRepository>();
        _settingsRepository = Substitute.For<ISettingsRepository>();
        _sut = new ContextWindowManager(_historyRepository, _settingsRepository);
    }

    [Fact]
    public async Task NeedsResetAsync_TokensBelowThreshold_ReturnsFalse()
    {
        _settingsRepository.GetGlobalSettingsAsync().Returns(new GlobalSettings { TokenThresholdForContextReset = 80000 });
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "short message"));

        Assert.False(await _sut.NeedsResetAsync(ctx));
    }

    [Fact]
    public async Task NeedsResetAsync_TokensAtOrAboveThreshold_ReturnsTrue()
    {
        _settingsRepository.GetGlobalSettingsAsync().Returns(new GlobalSettings { TokenThresholdForContextReset = 5 });
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "A".PadRight(80, 'A'))); // 20 tokens

        Assert.True(await _sut.NeedsResetAsync(ctx));
    }

    [Fact]
    public async Task ArchiveAndResetAsync_ArchivesOldHistory()
    {
        var sessionId = Guid.NewGuid();
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "old message"));
        var summaryContent = "Summary of the previous conversation.";

        await _sut.ArchiveAndResetAsync(ctx, sessionId, summaryContent);

        await _historyRepository.Received(1).ArchiveAsync(sessionId, Arg.Any<IReadOnlyList<ConversationMessage>>());
    }

    [Fact]
    public async Task ArchiveAndResetAsync_ResetsContextWithSummarySeed()
    {
        var sessionId = Guid.NewGuid();
        var ctx = new ConversationContext();
        ctx.AddMessage(ConversationMessage.Create(MessageRole.User, "old message"));
        var summaryContent = "Summary of the previous conversation.";

        await _sut.ArchiveAndResetAsync(ctx, sessionId, summaryContent);

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

        await _historyRepository.Received(1).SaveAsync(sessionId, Arg.Any<IReadOnlyList<ConversationMessage>>());
    }
}
