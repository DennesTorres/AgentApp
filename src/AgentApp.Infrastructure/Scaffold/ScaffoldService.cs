using AgentApp.Domain.Interfaces;

namespace AgentApp.Infrastructure.Scaffold;

public class ScaffoldService : IScaffoldService
{
    public Task CreateScaffoldAsync(string folderName, string sourceControlRoot)
    {
        Directory.CreateDirectory(GetAgentFolderPath(folderName));
        Directory.CreateDirectory(GetCodeFolderPath(folderName, sourceControlRoot));
        return Task.CompletedTask;
    }

    public string GetAgentFolderPath(string folderName) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower",
            folderName);

    public string GetCodeFolderPath(string folderName, string sourceControlRoot) =>
        Path.Combine(sourceControlRoot, folderName);
}
