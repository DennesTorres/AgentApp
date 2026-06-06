using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class InitializationPromptProvider : ISystemMessageProvider
{
    // C-066-R15: always active — carries the Tower persona and Stage 1 initialization instructions.
    // The initialization.md content is scoped to "when no active project" so the model ignores
    // the Stage 1 section when AgentContext already shows a project.
    public bool IsApplicable(AgentContext context) => true;

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
