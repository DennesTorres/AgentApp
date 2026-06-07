using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class FileToolsPromptProvider : ISystemMessageProvider
{
    // C-066-R16: active only when project is set — Stage 1 file tool signals handled by StagePromptProvider.
    public bool IsApplicable(AgentContext context) => context.HasProject;

    public string GetSection(AgentContext context)
    {
        // C-093: partial-init note — project exists but source control root not yet set
        var partialInitNote = context.HasProject && string.IsNullOrEmpty(context.CodeFolderPath)
            ? "\nNote: The source control root folder has not been set yet. " +
              "Do NOT call file tools or emit PATH_PERMISSION_REQUEST. " +
              "Only emit FOLDER_SELECT to let the user set the source control root first:\n" +
              "[FOLDER_SELECT:{\"reason\":\"Select your source control root folder\"}]\n"
            : string.Empty;

        // C-058: disk file checked first (user override), falls back to embedded resource (C-066-R14)
        var overridePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower", "prompts", "file-tools.md");
        var staticContent = PromptLoader.Load(
            "AgentApp.Application.SystemMessage.Prompts.file-tools.md",
            overridePath);

        return string.IsNullOrEmpty(partialInitNote)
            ? staticContent
            : staticContent + partialInitNote;
    }
}
