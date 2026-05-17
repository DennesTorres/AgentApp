namespace AgentApp.Domain.Rules;

public class TriggersIndex
{
    // trigger keyword → file name
    private readonly Dictionary<string, string> _entries = new(StringComparer.OrdinalIgnoreCase);

    public Guid Id { get; private set; }
    public MdFileScope Scope { get; private set; }
    public Guid? ProjectId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TriggersIndex() { }

    public static TriggersIndex CreateGlobal()
    {
        return new TriggersIndex
        {
            Id = Guid.NewGuid(),
            Scope = MdFileScope.Global,
            ProjectId = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static TriggersIndex CreateForProject(Guid projectId)
    {
        return new TriggersIndex
        {
            Id = Guid.NewGuid(),
            Scope = MdFileScope.Project,
            ProjectId = projectId,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddEntry(string fileName, IEnumerable<string> triggers)
    {
        foreach (var trigger in triggers)
            _entries[trigger.Trim()] = fileName;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveEntry(string fileName)
    {
        var toRemove = _entries.Where(kv => kv.Value == fileName).Select(kv => kv.Key).ToList();
        foreach (var key in toRemove)
            _entries.Remove(key);

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool HasTrigger(string trigger) => _entries.ContainsKey(trigger);

    public string? GetFileNameForTrigger(string trigger) =>
        _entries.TryGetValue(trigger, out var name) ? name : null;

    public IReadOnlyList<string> GetAllFileNames() =>
        _entries.Values.Distinct().ToList();

    // For persistence — expose raw entries
    public IReadOnlyDictionary<string, string> Entries => _entries;
}
