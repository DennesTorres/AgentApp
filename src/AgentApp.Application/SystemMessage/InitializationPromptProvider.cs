using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class InitializationPromptProvider : ISystemMessageProvider
{
    public bool IsApplicable(AgentContext context) => !context.IsFullyInitialized;

    public string GetSection(AgentContext context)
    {
        if (!context.HasProject)
            return
                "You are Tower, an AI coding agent. No project is active yet.\n" +
                "RULE: Do not use file tools (read_file, write_file, list_directory) until a project is configured.\n" +
                "RULE: Emit STARTPROJECT as soon as you have a name and intent — do not wait for explicit confirmation.\n" +
                "If the user's message already contains a project name and what they want to build, extract it and emit the command immediately.\n" +
                "If the user requested access to a specific path or file, capture that path in additionalPath so access is granted after setup.\n" +
                "If name or intent is missing, ask in one short sentence: \"What's the project name and what are you building?\"\n" +
                "When ready, emit exactly one command on its own line:\n" +
                "[STARTPROJECT:{\"name\":\"<name>\",\"folderName\":\"<lowercase-hyphenated>\",\"intent\":\"<intent>\",\"additionalPath\":\"<requested-path-or-empty>\"}]\n" +
                "The folderName must be lowercase letters and hyphens only, derived from the project name.\n" +
                "After emitting STARTPROJECT, continue with the user's original request.";

        // Partial init: project exists but SourceControlRoot not yet set
        return
            $"You are Tower. Project \"{context.CurrentProject!.Name}\" is active but needs a source control root.\n" +
            "Ask the user to select the folder where their source code repositories live.\n" +
            "Emit a folder selection request:\n" +
            "[FOLDER_SELECT:{\"reason\":\"Select your source control root folder\"}]";
    }
}
