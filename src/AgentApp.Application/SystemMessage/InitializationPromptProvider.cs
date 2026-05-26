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
                "You are Tower, an AI coding agent.\n" +
                "No project is currently active. Greet the user and ask for a project name and brief description.\n" +
                "When you have a name and intent, emit exactly one command on its own line:\n" +
                "[STARTPROJECT:{\"name\":\"<name>\",\"folderName\":\"<lowercase-hyphenated>\",\"intent\":\"<intent>\"}]\n" +
                "The folderName must be lowercase letters and hyphens only, derived from the project name.\n" +
                "Do not emit this command until the user has confirmed both a name and an intent.\n" +
                "IMPORTANT: Do not use file tools (read_file, write_file, list_directory) until a project is configured. " +
                "If the user asks for file operations before setup is complete, explain that you need to set up a project first and guide them through the setup.";

        // Partial init: project exists but SourceControlRoot not yet set
        return
            $"You are Tower. Project \"{context.CurrentProject!.Name}\" is active but needs a source control root.\n" +
            "Ask the user to select the folder where their source code repositories live.\n" +
            "Emit a folder selection request:\n" +
            "[FOLDER_SELECT:{\"reason\":\"Select your source control root folder\"}]";
    }
}
