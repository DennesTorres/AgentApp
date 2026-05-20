namespace AgentApp.Domain.Interfaces;

public interface IScaffoldService
{
    Task CreateScaffoldAsync(string projectName, string sourceControlRoot);
    string GetAgentFolderPath(string projectName);
    string GetCodeFolderPath(string projectName, string sourceControlRoot);
}
