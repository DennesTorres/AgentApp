using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class NoProjectProvider : ISystemMessageProvider
{
    public bool IsApplicable(AgentContext context) => !context.HasProject;

    public string GetSection(AgentContext context) =>
        "You are Tower, an AI coding agent. No project is currently active. " +
        "Ask the user to create or select a project to begin.";
}
