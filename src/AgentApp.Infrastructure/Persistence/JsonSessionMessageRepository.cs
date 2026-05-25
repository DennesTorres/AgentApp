using System.IO;
using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Sessions;

namespace AgentApp.Infrastructure.Persistence;

public class JsonSessionMessageRepository : ISessionMessageRepository
{
    private readonly string _folder;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public JsonSessionMessageRepository(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(folder);
    }

    public async Task<IReadOnlyList<SessionMessage>> GetBySessionIdAsync(Guid sessionId)
    {
        var path = FilePath(sessionId);
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<MessageDto>>(json) ?? [];
        return dtos
            .Select(d => SessionMessage.Reconstitute(d.Id, d.SessionId, d.Role, d.Content, d.Timestamp))
            .OrderBy(m => m.Timestamp)
            .ToList();
    }

    public async Task SaveAsync(SessionMessage message)
    {
        var path = FilePath(message.SessionId);
        var existing = new List<MessageDto>();
        if (File.Exists(path))
        {
            var json = await File.ReadAllTextAsync(path);
            existing = JsonSerializer.Deserialize<List<MessageDto>>(json) ?? [];
        }
        existing.Add(MessageDto.From(message));
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(existing, _options));
    }

    private string FilePath(Guid sessionId) => Path.Combine(_folder, $"messages_{sessionId}.json");

    private class MessageDto
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }

        public static MessageDto From(SessionMessage m) => new()
        {
            Id = m.Id,
            SessionId = m.SessionId,
            Role = m.Role,
            Content = m.Content,
            Timestamp = m.Timestamp
        };
    }
}
