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
        var codeFolder = string.IsNullOrEmpty(context.CodeFolderPath)
            ? "(not set yet)"
            : context.CodeFolderPath;

        var partialInitNote = string.IsNullOrEmpty(context.CodeFolderPath)
            ? "\nNote: The source control root folder has not been set yet, so project code files are not accessible. " +
              "If the user needs project code access, ask them to select the source control root by emitting:\n" +
              "[FOLDER_SELECT:{\"reason\":\"Select your source control root folder\"}]\n" +
              "However, you CAN still access any paths where read access has already been granted.\n"
            : string.Empty;

        var header =
            $"Project: {context.CurrentProject!.Name}\n" +
            $"Code folder: {codeFolder}\n" +
            $"Agent folder: {context.AgentFolderPath}" +
            partialInitNote;

        // C-058: disk file checked first (user override), falls back to embedded resource (C-066-R14)
        var overridePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower", "prompts", "file-tools.md");
        var staticContent = PromptLoader.Load(
            "AgentApp.Application.SystemMessage.Prompts.file-tools.md",
            overridePath);

        return header + "\n\n" + staticContent;
    }
}
