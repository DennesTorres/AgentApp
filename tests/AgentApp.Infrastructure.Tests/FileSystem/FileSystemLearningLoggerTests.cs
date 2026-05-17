using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class FileSystemLearningLoggerTests : IDisposable
{
    private readonly string _testFolder;
    private readonly FileSystemLearningLogger _sut;

    public FileSystemLearningLoggerTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new FileSystemLearningLogger(_testFolder);
    }

    [Fact]
    public async Task LogAsync_WritesEntryToFile()
    {
        await _sut.LogAsync("Learning process initiated");

        var logPath = Path.Combine(_testFolder, "learning-log.txt");
        Assert.True(File.Exists(logPath));
        var content = await File.ReadAllTextAsync(logPath);
        Assert.Contains("Learning process initiated", content);
    }

    [Fact]
    public async Task LogAsync_AppendsToExistingFile()
    {
        await _sut.LogAsync("Entry 1");
        await _sut.LogAsync("Entry 2");

        var logPath = Path.Combine(_testFolder, "learning-log.txt");
        var content = await File.ReadAllTextAsync(logPath);
        Assert.Contains("Entry 1", content);
        Assert.Contains("Entry 2", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
