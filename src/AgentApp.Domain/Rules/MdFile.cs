using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Rules;

public class MdFile
{
    private const string CoreFileName = "core";

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public MdFileScope Scope { get; private set; }
    public Guid? ProjectId { get; private set; }
    public bool IsTechnology { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsCore => Scope == MdFileScope.Global && Name == CoreFileName;

    private MdFile() { }

    public static MdFile CreateGlobal(string name, string content)
    {
        ValidateName(name);
        return new MdFile
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Content = content,
            Scope = MdFileScope.Global,
            ProjectId = null,
            IsTechnology = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static MdFile CreateForProject(string name, string content, Guid projectId)
    {
        ValidateName(name);
        return new MdFile
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Content = content,
            Scope = MdFileScope.Project,
            ProjectId = projectId,
            IsTechnology = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static MdFile CreateTechnology(string name, string content)
    {
        ValidateName(name);
        return new MdFile
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Content = content,
            Scope = MdFileScope.Global,
            ProjectId = null,
            IsTechnology = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static MdFile Reconstitute(Guid id, string name, string content, MdFileScope scope,
        Guid? projectId, bool isTechnology, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        return new MdFile
        {
            Id = id,
            Name = name,
            Content = content,
            Scope = scope,
            ProjectId = projectId,
            IsTechnology = isTechnology,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void UpdateContent(string newContent)
    {
        Content = newContent;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("MD file name cannot be empty.");
    }
}
