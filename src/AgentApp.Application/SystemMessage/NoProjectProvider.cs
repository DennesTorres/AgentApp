using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class NoProjectProvider : ISystemMessageProvider
{
    public bool IsApplicable(AgentContext context) => !context.HasProject;

    public string GetSection(AgentContext context)
    {
        // C-066-R14: disk file checked first (user override), falls back to embedded resource
        var overridePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower", "prompts", "no-project.md");
        return PromptLoader.Load(
            "AgentApp.Application.SystemMessage.Prompts.no-project.md",
            overridePath);
    }
}
