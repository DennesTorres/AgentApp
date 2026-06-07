using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class AgentFoundationProvider : ISystemMessageProvider
{
    // C-066-R16: always active — carries persona, tone, command format, and STARTPROJECT instruction only.
    // Stage-specific behavioral guidance has moved to StagePromptProvider.
    public bool IsApplicable(AgentContext context) => true;

    public string GetSection(AgentContext context)
    {
        // C-058: disk file checked first (user override), falls back to embedded resource
        var overridePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower", "prompts", "initialization.md");
        return PromptLoader.Load(
            "AgentApp.Application.SystemMessage.Prompts.initialization.md",
            overridePath);
    }
}
