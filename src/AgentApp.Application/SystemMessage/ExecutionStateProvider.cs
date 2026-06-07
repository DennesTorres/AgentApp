using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class ExecutionStateProvider : ISystemMessageProvider
{
    public bool IsApplicable(AgentContext context) => context.HasProject;

    public string GetSection(AgentContext context) =>
        $"Current conversation mode: {context.ConversationState.Mode}";
}
