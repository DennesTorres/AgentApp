using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class FileToolsPromptProvider : ISystemMessageProvider
{
    public bool IsApplicable(AgentContext context) => context.IsFullyInitialized;

    public string GetSection(AgentContext context) =>
        $"Project: {context.CurrentProject!.Name}\n" +
        $"Code folder: {context.CodeFolderPath}\n" +
        $"Agent folder: {context.AgentFolderPath}\n\n" +
        "You have access to file tools: read_file, write_file, list_directory.\n" +
        "These tools only work within the project's code folder. " +
        "If you need to read a path outside the project, emit a permission request first:\n" +
        "[PATH_PERMISSION_REQUEST:{\"path\":\"<path>\",\"reason\":\"<why you need access>\"}]\n" +
        "Wait for the user to grant access before attempting to read that path.";
}
