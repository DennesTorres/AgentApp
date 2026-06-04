using System.IO;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class InitializationPromptProvider : ISystemMessageProvider
{
    public bool IsApplicable(AgentContext context) => !context.IsFullyInitialized;

    public string GetSection(AgentContext context)
    {
        // C-058: load from %USERPROFILE%\.tower\prompts\initialization.md at call time
        var promptPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower", "prompts", "initialization.md");
        if (File.Exists(promptPath))
            return File.ReadAllText(promptPath);

        if (!context.HasProject)
            return
                // C-058: engage warmly
                "You are Tower, an AI development agent. Engage warmly and helpfully with the user.\n" +
                "RULE: Do not use file tools (read_file, write_file, list_directory) until a project is configured.\n" +
                "RULE: Emit STARTPROJECT as soon as you have a name and intent — do not wait for explicit confirmation.\n" +
                "If the user's message already contains a project name and what they want to build, extract it and emit the command immediately.\n" +
                "If the user requested access to a specific path or file (anywhere in conversation history), " +
                "capture that path in additionalPath so access is granted after setup.\n" +
                "If the project name was not explicitly provided by the user (you are deriving it), use a reasonable name based on context — confirm the name AFTER completing the user's main task, not before.\n" +
                "If name or intent is missing, ask in one short friendly sentence.\n" +
                "When ready, emit exactly one command on its own line:\n" +
                "[STARTPROJECT:{\"name\":\"<name>\",\"folderName\":\"<lowercase-hyphenated>\",\"intent\":\"<intent>\",\"additionalPath\":\"<requested-path-or-empty>\"}]\n" +
                "The folderName must be lowercase letters and hyphens only, derived from the project name.\n" +
                "After emitting STARTPROJECT, continue with the user's original request immediately — complete the task, then confirm the project name at the end.";

        // Partial init: project exists but SourceControlRoot not yet set
        return
            $"You are Tower. Project \"{context.CurrentProject!.Name}\" is active but needs a source control root.\n" +
            "Ask the user to select the folder where their source code repositories live.\n" +
            "Emit a folder selection request:\n" +
            "[FOLDER_SELECT:{\"reason\":\"Select your source control root folder\"}]";
    }
}
