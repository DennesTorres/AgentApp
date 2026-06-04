using System.IO;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class FileToolsPromptProvider : ISystemMessageProvider
{
    // C-059: active whenever a project exists, not just when fully initialized —
    // so the model always knows about PATH_PERMISSION_REQUEST even before sourceControlRoot is set
    public bool IsApplicable(AgentContext context) => context.HasProject;

    public string GetSection(AgentContext context)
    {
        // C-058: load from %USERPROFILE%\.tower\prompts\file-tools.md at call time
        var promptPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower", "prompts", "file-tools.md");
        if (File.Exists(promptPath))
            return File.ReadAllText(promptPath);

        // Fallback when file is missing
        var codeFolder = string.IsNullOrEmpty(context.CodeFolderPath)
            ? "(not set yet — source control root not configured)"
            : context.CodeFolderPath;

        return
            $"Project: {context.CurrentProject!.Name}\n" +
            $"Code folder: {codeFolder}\n" +
            $"Agent folder: {context.AgentFolderPath}\n\n" +
            "You have access to file tools: read_file, write_file, list_directory.\n" +
            "These tools only work within the project's code folder. " +
            "If you need to read a path outside the project, emit a permission request first:\n" +
            "[PATH_PERMISSION_REQUEST:{\"path\":\"<path>\",\"reason\":\"<why you need access>\"}]\n" +
            "Wait for the user to grant access before attempting to read that path.\n" +
            // C-060: if permission dialog fails internally, do not blame the OS
            "If no permission dialog appears after emitting the request, do NOT tell the user it is an OS or " +
            "filesystem limitation. Instead say naturally: \"I've requested access to [path] — a permission " +
            "prompt should appear above. If it doesn't, let me know and I can guide you through granting it manually.\"";
    }
}
