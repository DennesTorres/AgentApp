using System.Text;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class AgentContextProvider : ISystemMessageProvider
{
    // Always active — canonical state source. Serializes AgentContext so every other provider
    // emits behavioral instructions only, never restates state facts.
    public bool IsApplicable(AgentContext context) => true;

    public string GetSection(AgentContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Agent Context");
        sb.AppendLine();

        if (!context.HasProject)
        {
            sb.AppendLine("project: none");
        }
        else
        {
            var project = context.CurrentProject!;
            sb.AppendLine($"project: {project.Name}");
            sb.AppendLine($"agent_folder: {context.AgentFolderPath}");
            sb.AppendLine(string.IsNullOrEmpty(context.CodeFolderPath)
                ? "code_folder: not set"
                : $"code_folder: {context.CodeFolderPath}");
            sb.AppendLine($"name_confirmed: {context.NameConfirmed.ToString().ToLower()}");
        }

        sb.Append($"conversation_mode: {context.ConversationState.Mode}");
        return sb.ToString();
    }
}
