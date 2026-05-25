using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Projects;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string FolderName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Purpose { get; private set; } = string.Empty;
    public string ProjectFolderPath { get; private set; } = string.Empty;
    public string ControlFolderPath { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private Project() { }

    public static Project Reconstitute(
        Guid id, string name, string folderName, string description, string purpose,
        string projectFolderPath, string controlFolderPath, DateTimeOffset createdAt)
    {
        return new Project
        {
            Id = id,
            Name = name,
            FolderName = folderName,
            Description = description,
            Purpose = purpose,
            ProjectFolderPath = projectFolderPath,
            ControlFolderPath = controlFolderPath,
            CreatedAt = createdAt
        };
    }

    public static Project Create(string name, string folderName, string projectFolderPath,
        string description = "", string purpose = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Project name cannot be empty.");
        if (string.IsNullOrWhiteSpace(folderName))
            throw new DomainValidationException("Project folder name cannot be empty.");

        var towerRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tower");
        var controlFolder = Path.Combine(towerRoot, folderName);

        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            FolderName = folderName.Trim(),
            Description = description,
            Purpose = purpose,
            ProjectFolderPath = projectFolderPath,
            ControlFolderPath = controlFolder,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // Derives a safe folder name from a display name (lowercase, hyphens)
    public static string ToFolderName(string name) =>
        name.Trim().ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("_", "-");
}
