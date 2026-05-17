using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class FileSystemRollingWindowStoreTests : IDisposable
{
    private readonly string _testFolder;
    private readonly FileSystemRollingWindowStore _sut;

    public FileSystemRollingWindowStoreTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new FileSystemRollingWindowStore(_testFolder);
    }

    [Fact]
    public async Task ReadActiveAsync_NoFile_ReturnsEmptyString()
    {
        var result = await _sut.ReadActiveAsync("session-memory");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task WriteActiveAsync_ThenRead_ReturnsContent()
    {
        await _sut.WriteActiveAsync("session-memory", "Session notes content.");

        var result = await _sut.ReadActiveAsync("session-memory");

        Assert.Equal("Session notes content.", result);
    }

    [Fact]
    public async Task ArchiveAsync_CreatesArchiveFile()
    {
        await _sut.ArchiveAsync("session-memory", "Archived session content.");

        var archives = Directory.GetFiles(_testFolder, "rolling-archive-session-memory-*.md");
        Assert.Single(archives);
    }

    [Fact]
    public async Task SearchArchivesAsync_MatchingContent_ReturnsMatches()
    {
        await _sut.ArchiveAsync("impl-notes", "Implementation note about async patterns.");
        await _sut.ArchiveAsync("impl-notes", "Another note about error handling.");

        var results = await _sut.SearchArchivesAsync("async", "impl-notes");

        Assert.Single(results);
        Assert.Contains("async", results[0]);
    }

    [Fact]
    public async Task SearchArchivesAsync_NoMatch_ReturnsEmpty()
    {
        await _sut.ArchiveAsync("impl-notes", "Implementation note about async patterns.");

        var results = await _sut.SearchArchivesAsync("nonexistent-query", "impl-notes");

        Assert.Empty(results);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
