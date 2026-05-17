using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Reasoning;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonReasoningTraceRepository : IReasoningTraceRepository
{
    private readonly string _storageFolder;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonReasoningTraceRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task SaveAsync(ReasoningTrace trace)
    {
        var all = await LoadBySessionAsync(trace.SessionId);
        var list = all.ToList();
        var index = list.FindIndex(t => t.Id == trace.Id);
        if (index >= 0)
            list[index] = trace;
        else
            list.Add(trace);
        await PersistAsync(trace.SessionId, list);
    }

    public async Task<ReasoningTrace?> GetByIdAsync(Guid id)
    {
        foreach (var file in Directory.GetFiles(_storageFolder, "reasoning-traces-*.json"))
        {
            var traces = await LoadFromFileAsync(file);
            var match = traces.FirstOrDefault(t => t.Id == id);
            if (match != null) return match;
        }
        return null;
    }

    public async Task<IReadOnlyList<ReasoningTrace>> GetBySessionIdAsync(Guid sessionId) =>
        await LoadBySessionAsync(sessionId);

    private async Task<IReadOnlyList<ReasoningTrace>> LoadBySessionAsync(Guid sessionId)
    {
        var path = FilePath(sessionId);
        return File.Exists(path) ? await LoadFromFileAsync(path) : [];
    }

    private async Task<IReadOnlyList<ReasoningTrace>> LoadFromFileAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<ReasoningTraceDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToReasoningTrace()).ToList();
    }

    private async Task PersistAsync(Guid sessionId, IEnumerable<ReasoningTrace> traces)
    {
        var dtos = traces.Select(ReasoningTraceDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(sessionId), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath(Guid sessionId) =>
        Path.Combine(_storageFolder, $"reasoning-traces-{sessionId}.json");
}

internal class ReasoningTraceDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public static ReasoningTraceDto From(ReasoningTrace t) => new()
    { Id = t.Id, SessionId = t.SessionId, MessageId = t.MessageId, Content = t.Content, CreatedAt = t.CreatedAt };

    public ReasoningTrace ToReasoningTrace() =>
        ReasoningTrace.Reconstitute(Id, SessionId, MessageId, Content, CreatedAt);
}
