using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Projects;

public class Project
{
    private static readonly string AppName = "AgentApp";

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ProjectFolderPath { get; private set; } = string.Empty;
    public string ControlFolderPath { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private Project() { }

    // Used by Infrastructure layer to reconstitute from persisted state
    public static Project Reconstitute(Guid id, string name, string projectFolderPath, string controlFolderPath, DateTimeOffset createdAt)
    {
        return new Project
        {
            Id = id,
            Name = name,
            ProjectFolderPath = projectFolderPath,
            ControlFolderPath = controlFolderPath,
            CreatedAt = createdAt
        };
    }

    public static Project Create(string name, string projectFolderPath)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Project name cannot be empty.");

        if (string.IsNullOrWhiteSpace(projectFolderPath))
            throw new DomainValidationException("Project folder path cannot be empty.");

        var id = Guid.NewGuid();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var controlFolder = Path.Combine(localAppData, AppName, "projects", id.ToString());

        return new Project
        {
            Id = id,
            Name = name.Trim(),
            ProjectFolderPath = projectFolderPath,
            ControlFolderPath = controlFolder,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
