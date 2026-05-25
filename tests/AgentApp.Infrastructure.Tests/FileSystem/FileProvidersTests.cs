using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class FileProvidersTests : IDisposable
{
    private readonly string _tempFolder;

    public FileProvidersTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), $"FileProviderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempFolder);
    }

    // ── FileReadProvider ──────────────────────────────────────────────────────

    [Fact]
    public async Task FileReadProvider_ExistingFile_ReturnsContent()
    {
        var filePath = Path.Combine(_tempFolder, "test.txt");
        await File.WriteAllTextAsync(filePath, "hello world");

        var provider = new FileReadProvider();
        var request = ProviderRequest.Create(ProviderCapability.FileRead,
            new Dictionary<string, object> { ["path"] = filePath });

        var response = await provider.HandleAsync(request, CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal("hello world", response.Result["content"]);
    }

    [Fact]
    public async Task FileReadProvider_NonExistentFile_ReturnsFailure()
    {
        var provider = new FileReadProvider();
        var request = ProviderRequest.Create(ProviderCapability.FileRead,
            new Dictionary<string, object> { ["path"] = @"C:\nonexistent\file.txt" });

        var response = await provider.HandleAsync(request, CancellationToken.None);

        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
    }

    [Fact]
    public void FileReadProvider_HasCorrectCapability()
    {
        var provider = new FileReadProvider();
        Assert.Equal(ProviderCapability.FileRead, provider.Capability);
    }

    // ── FileWriteProvider ─────────────────────────────────────────────────────

    [Fact]
    public async Task FileWriteProvider_WritesContentToFile()
    {
        var filePath = Path.Combine(_tempFolder, "output.txt");
        var provider = new FileWriteProvider();
        var request = ProviderRequest.Create(ProviderCapability.FileWrite,
            new Dictionary<string, object> { ["path"] = filePath, ["content"] = "written content" });

        var response = await provider.HandleAsync(request, CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal("written content", await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task FileWriteProvider_CreatesIntermediateDirectories()
    {
        var filePath = Path.Combine(_tempFolder, "sub", "dir", "file.txt");
        var provider = new FileWriteProvider();
        var request = ProviderRequest.Create(ProviderCapability.FileWrite,
            new Dictionary<string, object> { ["path"] = filePath, ["content"] = "deep" });

        var response = await provider.HandleAsync(request, CancellationToken.None);

        Assert.True(response.Success);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void FileWriteProvider_HasCorrectCapability()
    {
        var provider = new FileWriteProvider();
        Assert.Equal(ProviderCapability.FileWrite, provider.Capability);
    }

    // ── DirectoryListProvider ─────────────────────────────────────────────────

    [Fact]
    public async Task DirectoryListProvider_ReturnsFilesAndFolders()
    {
        var sub = Path.Combine(_tempFolder, "mydir");
        Directory.CreateDirectory(sub);
        await File.WriteAllTextAsync(Path.Combine(sub, "a.txt"), "");
        await File.WriteAllTextAsync(Path.Combine(sub, "b.cs"), "");
        Directory.CreateDirectory(Path.Combine(sub, "child"));

        var provider = new DirectoryListProvider();
        var request = ProviderRequest.Create(ProviderCapability.DirectoryList,
            new Dictionary<string, object> { ["path"] = sub });

        var response = await provider.HandleAsync(request, CancellationToken.None);

        Assert.True(response.Success);
        var entries = (IReadOnlyList<string>)response.Result["entries"];
        Assert.Contains("a.txt", entries);
        Assert.Contains("b.cs", entries);
        Assert.Contains("child", entries);
    }

    [Fact]
    public async Task DirectoryListProvider_NonExistentDirectory_ReturnsFailure()
    {
        var provider = new DirectoryListProvider();
        var request = ProviderRequest.Create(ProviderCapability.DirectoryList,
            new Dictionary<string, object> { ["path"] = @"C:\nonexistent\dir" });

        var response = await provider.HandleAsync(request, CancellationToken.None);

        Assert.False(response.Success);
    }

    [Fact]
    public void DirectoryListProvider_HasCorrectCapability()
    {
        var provider = new DirectoryListProvider();
        Assert.Equal(ProviderCapability.DirectoryList, provider.Capability);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }
}
