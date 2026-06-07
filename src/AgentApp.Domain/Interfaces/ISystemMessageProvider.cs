using AgentApp.Domain.Agent;

namespace AgentApp.Domain.Interfaces;

public interface ISystemMessageProvider
{
    bool IsApplicable(AgentContext context);
    string GetSection(AgentContext context);
}
