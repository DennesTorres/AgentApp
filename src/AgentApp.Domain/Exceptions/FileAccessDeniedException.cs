namespace AgentApp.Domain.Exceptions;

public class FileAccessDeniedException : Exception
{
    public string RequestedPath { get; }

    public FileAccessDeniedException(string path)
        : base($"Access denied to path: {path}")
    {
        RequestedPath = path;
    }
}
