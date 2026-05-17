using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Projects;

public class KnowledgeRecord
{
    public const int CurrentFormatVersion = 1;

    private static readonly IReadOnlyDictionary<KnowledgeRecordStatus, IReadOnlyList<KnowledgeRecordStatus>> ValidTransitions =
        new Dictionary<KnowledgeRecordStatus, IReadOnlyList<KnowledgeRecordStatus>>
        {
            [KnowledgeRecordStatus.Backlog] = [KnowledgeRecordStatus.InImplementation],
            [KnowledgeRecordStatus.InImplementation] = [KnowledgeRecordStatus.Implemented],
            [KnowledgeRecordStatus.Implemented] = [KnowledgeRecordStatus.ReviewedUser, KnowledgeRecordStatus.ReviewedAgent],
            [KnowledgeRecordStatus.ReviewedUser] = [KnowledgeRecordStatus.Done, KnowledgeRecordStatus.InFix],
            [KnowledgeRecordStatus.ReviewedAgent] = [KnowledgeRecordStatus.Done, KnowledgeRecordStatus.InFix],
            [KnowledgeRecordStatus.InFix] = [KnowledgeRecordStatus.Implemented],
            [KnowledgeRecordStatus.Done] = []
        };

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public KnowledgeRecordStatus Status { get; private set; }
    public KnowledgeRecordType RecordType { get; private set; }
    public Guid? ParentId { get; private set; }
    public int FormatVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private KnowledgeRecord() { Title = string.Empty; Description = string.Empty; }

    public static KnowledgeRecord Create(
        Guid projectId, string title, string description, KnowledgeRecordType recordType, Guid? parentId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException("Title must not be empty.");

        var now = DateTimeOffset.UtcNow;
        return new KnowledgeRecord
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Description = description,
            Status = KnowledgeRecordStatus.Backlog,
            RecordType = recordType,
            ParentId = parentId,
            FormatVersion = CurrentFormatVersion,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static KnowledgeRecord Reconstitute(
        Guid id, Guid projectId, string title, string description, KnowledgeRecordStatus status,
        KnowledgeRecordType recordType, Guid? parentId, int formatVersion,
        DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        new()
        {
            Id = id, ProjectId = projectId, Title = title, Description = description,
            Status = status, RecordType = recordType, ParentId = parentId,
            FormatVersion = formatVersion, CreatedAt = createdAt, UpdatedAt = updatedAt
        };

    public void TransitionTo(KnowledgeRecordStatus newStatus)
    {
        if (!ValidTransitions[Status].Contains(newStatus))
            throw new DomainValidationException(
                $"Cannot transition from {Status} to {newStatus}.");

        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
