using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class NameConfirmationProvider : ISystemMessageProvider
{
    public bool IsApplicable(AgentContext context) => context.HasProject && !context.NameConfirmed;

    public string GetSection(AgentContext context)
    {
        var name = context.CurrentProject!.Name;
        return $"You recently inferred the project name \"{name}\" from the user's intent.\n" +
               $"After completing your current task, warmly ask the user whether the name is correct:\n" +
               $"\"I've named this project '{name}' — does that work for you?\"\n" +
               $"When the user confirms the name is acceptable, emit on its own line:\n" +
               $"[NAME_CONFIRMED:{{}}]";
    }
}
