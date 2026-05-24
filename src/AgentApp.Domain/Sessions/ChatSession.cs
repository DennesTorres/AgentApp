namespace AgentApp.Domain.Sessions;

public class ChatSession
{
    public Guid Id { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsArchived { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsLinkedToProject => ProjectId.HasValue;

    private ChatSession() { }

    public static ChatSession CreateStandalone()
    {
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = $"Session {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}",
            IsArchived = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ChatSession CreateForProject(Guid projectId)
    {
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = $"Session {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}",
            IsArchived = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ChatSession Reconstitute(Guid id, Guid? projectId, string name, bool isArchived, DateTimeOffset createdAt) =>
        new() { Id = id, ProjectId = projectId, Name = name, IsArchived = isArchived, CreatedAt = createdAt };

    public void LinkToProject(Guid projectId)
    {
        if (IsLinkedToProject)
            throw new InvalidOperationException("Session is already linked to a project.");
        ProjectId = projectId;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Session name must not be empty.", nameof(newName));
        Name = newName.Trim();
    }

    public void Archive()
    {
        if (IsArchived)
            throw new InvalidOperationException("Session is already archived.");
        IsArchived = true;
    }
}
