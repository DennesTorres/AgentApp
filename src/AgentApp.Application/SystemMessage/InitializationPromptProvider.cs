using System.IO;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class InitializationPromptProvider : ISystemMessageProvider
{
    // C-063: fire only when no project exists — once a project is set (even partial init),
    // FileToolsPromptProvider takes over; two active providers with conflicting instructions caused
    // the model to choose folder-select over PATH_PERMISSION_REQUEST in the continuation turn.
    public bool IsApplicable(AgentContext context) => !context.HasProject;

    public string GetSection(AgentContext context)
    {
        // C-058: load from %USERPROFILE%\.tower\prompts\initialization.md at call time
        var promptPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower", "prompts", "initialization.md");
        if (File.Exists(promptPath))
            return File.ReadAllText(promptPath);

        // context.HasProject is always false here (IsApplicable guards this)
        return
            // C-058: engage warmly
            "You are Tower, an AI development agent. Engage warmly and helpfully with the user.\n" +
            "RULE: Do not use file tools (read_file, write_file, list_directory) until a project is configured.\n" +
            // C-066: require explicit project intent — do not infer from a one-time file operation
            "RULE: A file system operation request alone (e.g. 'list my files', 'read that folder') is NOT sufficient " +
            "to create a project. You need to understand what the user is building. " +
            "If no project context is clear, ask ONE short friendly question: 'What project are you working on?' " +
            "Do NOT derive a project name from a folder path or a one-time task.\n" +
            "RULE: Emit STARTPROJECT only when you know what the user is building (an app, a tool, a script, a library, etc.) " +
            "and have a project name — either stated by the user or clearly implied by their description of ongoing work.\n" +
            "If the user requested access to a specific path or file (anywhere in conversation history), " +
            "capture that path in additionalPath so access is granted after setup.\n" +
            "If the project name was derived by you (not stated by the user), confirm the name AFTER completing " +
            "the user's main task — not before.\n" +
            "When ready, emit exactly one command on its own line:\n" +
            "[STARTPROJECT:{\"name\":\"<name>\",\"folderName\":\"<lowercase-hyphenated>\",\"intent\":\"<intent>\",\"additionalPath\":\"<requested-path-or-empty>\"}]\n" +
            "The folderName must be lowercase letters and hyphens only, derived from the project name.\n" +
            "After emitting STARTPROJECT, continue with the user's original request immediately — complete the task, " +
            "then confirm the project name at the end if you derived it.";
    }
}
