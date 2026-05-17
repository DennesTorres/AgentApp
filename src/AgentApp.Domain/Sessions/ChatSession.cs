namespace AgentApp.Domain.Sessions;

public class ChatSession
{
    public Guid Id { get; private set; }
    public Guid? ProjectId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsLinkedToProject => ProjectId.HasValue;

    private ChatSession() { }

    public static ChatSession CreateStandalone()
    {
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ChatSession CreateForProject(Guid projectId)
    {
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void LinkToProject(Guid projectId)
    {
        if (IsLinkedToProject)
            throw new InvalidOperationException("Session is already linked to a project.");

        ProjectId = projectId;
    }
}
