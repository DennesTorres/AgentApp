using System.Text.Json;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Scheduling;

namespace AgentApp.Infrastructure.FileSystem;

public class JsonScheduleRepository : IScheduleRepository
{
    private readonly string _storageFolder;
    private const string FileName = "schedules.json";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonScheduleRepository(string storageFolder)
    {
        _storageFolder = storageFolder;
        Directory.CreateDirectory(storageFolder);
    }

    public async Task<ScheduleDefinition?> GetByJobTypeAsync(ScheduledJobType jobType)
    {
        var all = await LoadAllAsync();
        return all.FirstOrDefault(s => s.JobType == jobType);
    }

    public async Task<IReadOnlyList<ScheduleDefinition>> GetAllAsync() => await LoadAllAsync();

    public async Task SaveAsync(ScheduleDefinition definition)
    {
        var all = await LoadAllAsync();
        var list = all.ToList();
        var index = list.FindIndex(s => s.Id == definition.Id);
        if (index >= 0)
            list[index] = definition;
        else
            list.Add(definition);
        await PersistAsync(list);
    }

    private async Task<IReadOnlyList<ScheduleDefinition>> LoadAllAsync()
    {
        var path = FilePath();
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path);
        var dtos = JsonSerializer.Deserialize<List<ScheduleDefinitionDto>>(json, Options) ?? [];
        return dtos.Select(d => d.ToDefinition()).ToList();
    }

    private async Task PersistAsync(IEnumerable<ScheduleDefinition> definitions)
    {
        var dtos = definitions.Select(ScheduleDefinitionDto.From).ToList();
        await File.WriteAllTextAsync(FilePath(), JsonSerializer.Serialize(dtos, Options));
    }

    private string FilePath() => Path.Combine(_storageFolder, FileName);
}

internal class ScheduleDefinitionDto
{
    public Guid Id { get; set; }
    public ScheduledJobType JobType { get; set; }
    public long IntervalTicks { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static ScheduleDefinitionDto From(ScheduleDefinition s) => new()
    {
        Id = s.Id, JobType = s.JobType, IntervalTicks = s.Interval.Ticks,
        IsEnabled = s.IsEnabled, LastRunAt = s.LastRunAt, CreatedAt = s.CreatedAt
    };

    public ScheduleDefinition ToDefinition() =>
        ScheduleDefinition.Reconstitute(Id, JobType, TimeSpan.FromTicks(IntervalTicks), IsEnabled, LastRunAt, CreatedAt);
}
