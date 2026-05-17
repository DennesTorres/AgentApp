namespace AgentApp.Domain.Interfaces;

public interface IRollingWindowStore
{
    Task<string> ReadActiveAsync(string fileType);
    Task WriteActiveAsync(string fileType, string content);
    Task ArchiveAsync(string fileType, string content);
    Task<IReadOnlyList<string>> SearchArchivesAsync(string query, string fileType);
}
