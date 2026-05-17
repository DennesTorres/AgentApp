using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Orchestration;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonOrchestratorSessionRepository : IOrchestratorSessionRepository
{
    private readonly string _storageFolder;
    private const string FileName = "orchestrator-sessions.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonOrchestratorSessionRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task SaveAsync(OrchestratorSession session)
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

    public async Task<OrchestratorSession?> GetByIdAsync(Guid id)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(s => s.Id == id);
    }

    public async Task<IReadOnlyList<OrchestratorSession>> GetByProjectIdAsync(Guid projectId)
    {
        var all = await LoadAllAsync();
        return all.Where(s => s.ProjectId == projectId).ToList();
    }

    private async Task<IReadOnlyList<OrchestratorSession>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<OrchestratorSessionDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToSession()).ToList();
    }

    private async Task PersistAsync(IEnumerable<OrchestratorSession> sessions)
    {
        var dtos = sessions.Select(OrchestratorSessionDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class OrchestratorSessionDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public AgentType AgentType { get; set; }
    public OrchestratorStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public static OrchestratorSessionDto From(OrchestratorSession s) => new()
    {
        Id = s.Id, ProjectId = s.ProjectId, AgentType = s.AgentType,
        Status = s.Status, StartedAt = s.StartedAt, CompletedAt = s.CompletedAt
    };

    public OrchestratorSession ToSession() =>
        OrchestratorSession.Reconstitute(Id, ProjectId, AgentType, Status, StartedAt, CompletedAt);
}
