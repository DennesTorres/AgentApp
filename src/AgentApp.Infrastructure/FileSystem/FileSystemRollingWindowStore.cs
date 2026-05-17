using AgentApp.Domain.Interfaces;

namespace AgentApp.Infrastructure.FileSystem;

public class FileSystemRollingWindowStore : IRollingWindowStore
{
    private readonly string _storageFolder;

    public FileSystemRollingWindowStore(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<string> ReadActiveAsync(string fileType)
    {
        var path = ActiveFilePath(fileType);
        return File.Exists(path) ? await File.ReadAllTextAsync(path) : string.Empty;
    }

    public async Task WriteActiveAsync(string fileType, string content) =>
        await File.WriteAllTextAsync(ActiveFilePath(fileType), content);

    public async Task ArchiveAsync(string fileType, string content)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var unique = Guid.NewGuid().ToString("N")[..8];
        var path = Path.Combine(_storageFolder, $"rolling-archive-{fileType}-{timestamp}-{unique}.md");
        await File.WriteAllTextAsync(path, content);
    }

    public async Task<IReadOnlyList<string>> SearchArchivesAsync(string query, string fileType)
    {
        var pattern = $"rolling-archive-{fileType}-*.md";
        var files = Directory.GetFiles(_storageFolder, pattern);
        var matches = new List<string>();

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
                matches.Add(content);
        }

        return matches;
    }

    private string ActiveFilePath(string fileType) =>
        Path.Combine(_storageFolder, $"rolling-{fileType}.md");
}
