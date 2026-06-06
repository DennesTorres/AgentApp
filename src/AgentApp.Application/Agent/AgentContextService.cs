using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Agent;

public class AgentContextService : IAgentContextService
{
    private readonly AgentContext _context = new();

    public AgentContext GetCurrent() => _context;

    public void SetProject(Project? project, string agentFolderPath, string codeFolderPath)
        => _context.SetProject(project, agentFolderPath, codeFolderPath);

    public void UpdateConversationState(ConversationState state)
        => _context.UpdateConversationState(state);

    public void ConfirmProjectName() => _context.ConfirmProjectName();
}
