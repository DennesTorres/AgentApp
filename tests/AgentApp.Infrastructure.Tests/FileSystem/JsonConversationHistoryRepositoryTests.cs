using AgentApp.Domain.Context;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonConversationHistoryRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonConversationHistoryRepository _sut;

    public JsonConversationHistoryRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonConversationHistoryRepository(_testFolder);
    }

    [Fact]
    public async Task GetBySessionIdAsync_EmptyStore_ReturnsEmptyList()
    {
        var result = await _sut.GetBySessionIdAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_Messages_CanBeReloadedBySessionId()
    {
        var sessionId = Guid.NewGuid();
        var messages = new List<ConversationMessage>
        {
            ConversationMessage.Create(MessageRole.User, "Hello"),
            ConversationMessage.Create(MessageRole.Assistant, "Hi there!")
        };

        await _sut.SaveAsync(sessionId, messages);
        var loaded = await _sut.GetBySessionIdAsync(sessionId);

        Assert.Equal(2, loaded.Count);
        Assert.Equal(MessageRole.User, loaded[0].Role);
        Assert.Equal("Hello", loaded[0].Content);
    }

    [Fact]
    public async Task SaveAsync_DifferentSessions_StoreIndependently()
    {
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();

        await _sut.SaveAsync(session1, [ConversationMessage.Create(MessageRole.User, "Session 1 message")]);
        await _sut.SaveAsync(session2, [ConversationMessage.Create(MessageRole.User, "Session 2 message")]);

        var s1 = await _sut.GetBySessionIdAsync(session1);
        var s2 = await _sut.GetBySessionIdAsync(session2);

        Assert.Single(s1);
        Assert.Single(s2);
        Assert.Equal("Session 1 message", s1[0].Content);
        Assert.Equal("Session 2 message", s2[0].Content);
    }

    [Fact]
    public async Task ArchiveAsync_MovesMessagesToArchiveFile()
    {
        var sessionId = Guid.NewGuid();
        var messages = new List<ConversationMessage>
        {
            ConversationMessage.Create(MessageRole.User, "archived message")
        };

        await _sut.ArchiveAsync(sessionId, messages);

        var archivePath = Path.Combine(_testFolder, $"history-archive-{sessionId}.json");
        Assert.True(File.Exists(archivePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
