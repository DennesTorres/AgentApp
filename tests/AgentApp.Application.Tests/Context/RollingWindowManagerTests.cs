using AgentApp.Application.Context;
using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Context;

public class RollingWindowManagerTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly FileSystemRollingWindowStore _store;
    private readonly JsonRollingWindowRuleRepository _ruleRepository;
    private readonly RollingWindowManager _sut;

    public RollingWindowManagerTests()
    {
        _store = new FileSystemRollingWindowStore(_tempFolder);
        _ruleRepository = new JsonRollingWindowRuleRepository(_tempFolder);
        _sut = new RollingWindowManager(_store, _ruleRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task AppendAsync_EmptyFile_WritesContentDirectly()
    {
        await _sut.AppendAsync("session-memory", "New session content.", MdFileScope.Global, null);

        var content = await _store.ReadActiveAsync("session-memory");
        Assert.Equal("New session content.", content);
    }

    [Fact]
    public async Task AppendAsync_ExistingContent_AppendsWithSeparator()
    {
        await _store.WriteActiveAsync("session-memory", "Existing content.");

        await _sut.AppendAsync("session-memory", "Appended content.", MdFileScope.Global, null);

        var content = await _store.ReadActiveAsync("session-memory");
        Assert.Equal("Existing content.\n\nAppended content.", content);
    }

    [Fact]
    public async Task ArchiveIfNeededAsync_ContentBelowLimit_DoesNotArchive()
    {
        var rule = RollingWindowRule.Create("session-memory", 1000, 30, MdFileScope.Global, null);
        await _ruleRepository.SaveAsync(rule);
        await _store.WriteActiveAsync("session-memory", "Short content.");

        await _sut.ArchiveIfNeededAsync("session-memory", MdFileScope.Global, null);

        var content = await _store.ReadActiveAsync("session-memory");
        Assert.Equal("Short content.", content);
    }

    [Fact]
    public async Task ArchiveIfNeededAsync_ContentOverLimit_ArchivesAndClears()
    {
        var rule = RollingWindowRule.Create("session-memory", 10, 30, MdFileScope.Global, null);
        await _ruleRepository.SaveAsync(rule);
        await _store.WriteActiveAsync("session-memory", "This content is over the limit.");

        await _sut.ArchiveIfNeededAsync("session-memory", MdFileScope.Global, null);

        var content = await _store.ReadActiveAsync("session-memory");
        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public async Task ArchiveIfNeededAsync_NoRule_DoesNothing()
    {
        await _store.WriteActiveAsync("session-memory", "Some content.");

        await _sut.ArchiveIfNeededAsync("session-memory", MdFileScope.Global, null);

        var content = await _store.ReadActiveAsync("session-memory");
        Assert.Equal("Some content.", content);
    }

    [Fact]
    public async Task SearchArchivesAsync_DelegatesToStore()
    {
        var results = await _sut.SearchArchivesAsync("query", "session-memory", null);

        Assert.NotNull(results);
    }
}
