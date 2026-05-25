using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Sessions;

namespace AgentApp.Infrastructure.Persistence;

public class JsonSessionRepository : ISessionRepository
{
    private readonly string _storageFolder;
    private const string FileName = "sessions.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonSessionRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<ChatSession?> GetByIdAsync(Guid sessionId)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(s => s.Id == sessionId);
    }

    public async Task SaveAsync(ChatSession session)
    {
        var all = await LoadAllAsync();
        var list = all.ToList();
        var index = list.FindIndex(s => s.Id == session.Id);
        if (index >= 0)
            list[index] = session;
        else
            list.Add(session);
        await PersistAsync(list);
    }

    public async Task<IReadOnlyList<ChatSession>> GetAllAsync() => await LoadAllAsync();

    public async Task DeleteAsync(Guid sessionId)
    {
        var all = await LoadAllAsync();
        await PersistAsync(all.Where(s => s.Id != sessionId));
    }

    private async Task<IReadOnlyList<ChatSession>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<ChatSessionDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToSession()).ToList();
    }

    private async Task PersistAsync(IEnumerable<ChatSession> sessions)
    {
        var dtos = sessions.Select(ChatSessionDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class ChatSessionDto
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static ChatSessionDto From(ChatSession s) => new()
    {
        Id = s.Id,
        ProjectId = s.ProjectId,
        Name = s.Name,
        IsArchived = s.IsArchived,
        CreatedAt = s.CreatedAt
    };

    public ChatSession ToSession() =>
        ChatSession.Reconstitute(Id, ProjectId, Name, IsArchived, CreatedAt);
}
