using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class ActiveProjectProvider : ISystemMessageProvider
{
    public bool IsApplicable(AgentContext context) => context.HasProject;

    public string GetSection(AgentContext context)
    {
        var project = context.CurrentProject!;
        return $"Active project: {project.Name}\n" +
               $"Agent folder: {context.AgentFolderPath}\n" +
               $"Code folder: {context.CodeFolderPath}";
    }
}
