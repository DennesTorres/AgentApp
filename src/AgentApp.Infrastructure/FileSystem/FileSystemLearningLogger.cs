using AgentApp.Domain.Interfaces;

namespace AgentApp.Infrastructure.FileSystem;

public class FileSystemLearningLogger : ILearningLogger
{
    private readonly string _storageFolder;
    private const string LogFileName = "learning-log.txt";

    public FileSystemLearningLogger(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task LogAsync(string entry)
    {
        var path = Path.Combine(_storageFolder, LogFileName);
        await File.AppendAllTextAsync(path, entry + Environment.NewLine);
    }
}
