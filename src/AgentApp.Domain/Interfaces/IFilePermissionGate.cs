namespace AgentApp.Domain.Interfaces;

public interface IFilePermissionGate
{
    void SetProjectRoots(string agentRoot, string codeRoot);
    void GrantReadAccess(string path);
    bool CanRead(string path);
    bool CanWrite(string path);
}
