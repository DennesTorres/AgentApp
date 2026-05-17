using System.Text.Json;
using AgentApp.Domain.Context;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonConversationHistoryRepository : IConversationHistoryRepository
{
    private readonly string _storageFolder;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonConversationHistoryRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetBySessionIdAsync(Guid sessionId)
    {
        var path = ActiveFilePath(sessionId);
        if (!File.Exists(path))
            return [];

        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<ConversationMessageDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToMessage()).ToList();
    }

    public async Task SaveAsync(Guid sessionId, IReadOnlyList<ConversationMessage> messages)
    {
        var dtos = messages.Select(ConversationMessageDto.From).ToList();
        await File.WriteAllTextAsync(ActiveFilePath(sessionId), JsonSerializer.Serialize(dtos, Options));
    }

    public async Task ArchiveAsync(Guid sessionId, IReadOnlyList<ConversationMessage> messages)
    {
        var dtos = messages.Select(ConversationMessageDto.From).ToList();
        await File.WriteAllTextAsync(ArchiveFilePath(sessionId), JsonSerializer.Serialize(dtos, Options));
    }

    private string ActiveFilePath(Guid sessionId) =>
        Path.Combine(_storageFolder, $"history-{sessionId}.json");

    private string ArchiveFilePath(Guid sessionId) =>
        Path.Combine(_storageFolder, $"history-archive-{sessionId}.json");
}

internal class ConversationMessageDto
{
    public Guid Id { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenEstimate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static ConversationMessageDto From(ConversationMessage m) => new()
    { Id = m.Id, Role = m.Role, Content = m.Content, TokenEstimate = m.TokenEstimate, CreatedAt = m.CreatedAt };

    public ConversationMessage ToMessage() =>
        ConversationMessage.Reconstitute(Id, Role, Content, TokenEstimate, CreatedAt);
}
