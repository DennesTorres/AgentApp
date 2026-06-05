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
        // C-058: disk file checked first (user override), falls back to embedded resource (C-066-R14)
        var overridePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower", "prompts", "initialization.md");
        return PromptLoader.Load(
            "AgentApp.Application.SystemMessage.Prompts.initialization.md",
            overridePath);
    }
}
