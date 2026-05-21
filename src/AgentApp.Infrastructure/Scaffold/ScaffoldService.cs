using AgentApp.Domain.Interfaces;

namespace AgentApp.Infrastructure.Scaffold;

public class ScaffoldService : IScaffoldService
{
    public Task CreateScaffoldAsync(string projectName, string sourceControlRoot)
    {
        Directory.CreateDirectory(GetAgentFolderPath(projectName));
        Directory.CreateDirectory(GetCodeFolderPath(projectName, sourceControlRoot));
        return Task.CompletedTask;
    }

    public string GetAgentFolderPath(string projectName) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".TowerAgent",
            projectName);

    public string GetCodeFolderPath(string projectName, string sourceControlRoot) =>
        Path.Combine(sourceControlRoot, projectName);
}
