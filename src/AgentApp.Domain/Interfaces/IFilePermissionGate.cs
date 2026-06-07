namespace AgentApp.Domain.Interfaces;

public interface IFilePermissionGate
{
    void SetProjectRoots(string agentRoot, string codeRoot);
    void GrantReadAccess(string path);
    bool CanRead(string path);
    bool CanWrite(string path);
    // US-190: bypass mode — all path checks return true for the session
    void SetBypassMode(bool bypass);
    bool IsBypassMode { get; }
}
