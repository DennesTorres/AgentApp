using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Learning;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonLearningSessionRepository : ILearningSessionRepository
{
    private readonly string _storageFolder;
    private const string FileName = "learning-sessions.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonLearningSessionRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<LearningSession?> GetByIdAsync(Guid id)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(s => s.Id == id);
    }

    public async Task<IReadOnlyList<LearningSession>> GetAllPendingAsync()
    {
        var all = await LoadAllAsync();
        return all.Where(s => s.Outcome == LearningOutcome.Pending).ToList();
    }

    public async Task SaveAsync(LearningSession session)
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

    private async Task<IReadOnlyList<LearningSession>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path))
            return [];

        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<LearningSessionDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToLearningSession()).ToList();
    }

    private async Task PersistAsync(IEnumerable<LearningSession> sessions)
    {
        var dtos = sessions.Select(LearningSessionDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class ProposedRuleChangeDto
{
    public string FileName { get; set; } = string.Empty;
    public string RuleText { get; set; } = string.Empty;
    public string HumanSummary { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    public static ProposedRuleChangeDto From(ProposedRuleChange c) => new()
    { FileName = c.FileName, RuleText = c.RuleText, HumanSummary = c.HumanSummary, Action = c.Action };

    public ProposedRuleChange ToProposedRuleChange() =>
        ProposedRuleChange.Create(FileName, RuleText, HumanSummary, Action);
}

internal class LearningSessionDto
{
    public Guid Id { get; set; }
    public LearningTrigger Trigger { get; set; }
    public string ViolationDescription { get; set; } = string.Empty;
    public Guid? ReasoningTraceId { get; set; }
    public LearningOutcome Outcome { get; set; }
    public ProposedRuleChangeDto? ProposedChange { get; set; }
    public int IterationCount { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static LearningSessionDto From(LearningSession s) => new()
    {
        Id = s.Id, Trigger = s.Trigger, ViolationDescription = s.ViolationDescription,
        ReasoningTraceId = s.ReasoningTraceId, Outcome = s.Outcome,
        ProposedChange = s.ProposedChange != null ? ProposedRuleChangeDto.From(s.ProposedChange) : null,
        IterationCount = s.IterationCount, RejectionReason = s.RejectionReason,
        CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt
    };

    public LearningSession ToLearningSession() =>
        LearningSession.Reconstitute(Id, Trigger, ViolationDescription, ReasoningTraceId, Outcome,
            ProposedChange?.ToProposedRuleChange(), IterationCount, RejectionReason, CreatedAt, UpdatedAt);
}
