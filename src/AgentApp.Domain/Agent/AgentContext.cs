using AgentApp.Domain.Projects;

namespace AgentApp.Domain.Agent;

public class AgentContext
{
    public Project? CurrentProject { get; private set; }
    public ConversationState ConversationState { get; private set; } = ConversationState.Initial;
    public string AgentFolderPath { get; private set; } = string.Empty;
    public string CodeFolderPath { get; private set; } = string.Empty;

    public bool HasProject => CurrentProject is not null;

    public void SetProject(Project? project, string agentFolderPath, string codeFolderPath)
    {
        CurrentProject = project;
        AgentFolderPath = agentFolderPath;
        CodeFolderPath = codeFolderPath;
    }

    public void UpdateConversationState(ConversationState state)
    {
        ConversationState = state;
    }
}
