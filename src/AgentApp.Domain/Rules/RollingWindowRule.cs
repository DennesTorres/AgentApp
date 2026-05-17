using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Rules;

public class RollingWindowRule
{
    public Guid Id { get; private set; }
    public string FileType { get; private set; }
    public int MaxSizeChars { get; private set; }
    public int RetentionDays { get; private set; }
    public MdFileScope Scope { get; private set; }
    public Guid? ProjectId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RollingWindowRule() { FileType = string.Empty; }

    public static RollingWindowRule Create(
        string fileType, int maxSizeChars, int retentionDays, MdFileScope scope, Guid? projectId)
    {
        if (string.IsNullOrWhiteSpace(fileType))
            throw new DomainValidationException("FileType must not be empty.");
        if (maxSizeChars <= 0)
            throw new DomainValidationException("MaxSizeChars must be positive.");
        if (retentionDays <= 0)
            throw new DomainValidationException("RetentionDays must be positive.");

        return new RollingWindowRule
        {
            Id = Guid.NewGuid(),
            FileType = fileType,
            MaxSizeChars = maxSizeChars,
            RetentionDays = retentionDays,
            Scope = scope,
            ProjectId = projectId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static RollingWindowRule Reconstitute(
        Guid id, string fileType, int maxSizeChars, int retentionDays,
        MdFileScope scope, Guid? projectId, DateTimeOffset createdAt) =>
        new()
        {
            Id = id, FileType = fileType, MaxSizeChars = maxSizeChars,
            RetentionDays = retentionDays, Scope = scope, ProjectId = projectId, CreatedAt = createdAt
        };
}
