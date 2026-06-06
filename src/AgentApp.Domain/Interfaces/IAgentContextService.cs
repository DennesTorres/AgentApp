using AgentApp.Domain.Agent;
using AgentApp.Domain.Projects;

namespace AgentApp.Domain.Interfaces;

public interface IAgentContextService
{
    AgentContext GetCurrent();
    void SetProject(Project? project, string agentFolderPath, string codeFolderPath);
    void UpdateConversationState(ConversationState state);
    void ConfirmProjectName();
}
