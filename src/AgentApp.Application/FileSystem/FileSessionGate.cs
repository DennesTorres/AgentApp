using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.FileSystem;

public class FileSessionGate : IFilePermissionGate
{
    private string? _agentRoot;
    private string? _codeRoot;
    private readonly List<string> _grantedReadPaths = [];
    private bool _bypassMode;

    public bool IsBypassMode => _bypassMode;

    public void SetProjectRoots(string agentRoot, string codeRoot)
    {
        _agentRoot = Normalize(agentRoot);
        _codeRoot = Normalize(codeRoot);
    }

    public void GrantReadAccess(string path) =>
        _grantedReadPaths.Add(Normalize(path));

    // US-190: bypass all path checks for the session
    public void SetBypassMode(bool bypass) => _bypassMode = bypass;

    public bool CanRead(string path)
    {
        if (_bypassMode) return true;
        var normalized = Normalize(path);
        return IsUnderRoot(normalized, _agentRoot)
            || IsUnderRoot(normalized, _codeRoot)
            || _grantedReadPaths.Any(granted => IsUnderRoot(normalized, granted));
    }

    public bool CanWrite(string path)
    {
        if (_bypassMode) return true;
        var normalized = Normalize(path);
        return IsUnderRoot(normalized, _agentRoot)
            || IsUnderRoot(normalized, _codeRoot);
    }

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToLowerInvariant();

    private static bool IsUnderRoot(string normalizedPath, string? normalizedRoot)
    {
        if (normalizedRoot is null) return false;
        return normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
