using AgentApp.Domain.Projects;

namespace AgentApp.Domain.Agent;

public class AgentContext
{
    public Project? CurrentProject { get; private set; }
    public ConversationState ConversationState { get; private set; } = ConversationState.Initial;
    public string AgentFolderPath { get; private set; } = string.Empty;
    public string CodeFolderPath { get; private set; } = string.Empty;

    public bool HasProject => CurrentProject is not null;

    // True when a project exists AND the code folder path is set (partial init = project created but no source control root yet)
    public bool IsFullyInitialized => HasProject && !string.IsNullOrEmpty(CodeFolderPath);

    // True when the user has confirmed the inferred project name (Stage 2 → Stage 3 transition)
    public bool NameConfirmed { get; private set; }

    public void SetProject(Project? project, string agentFolderPath, string codeFolderPath)
    {
        CurrentProject = project;
        AgentFolderPath = agentFolderPath;
        CodeFolderPath = codeFolderPath;
        NameConfirmed = false;
    }

    public void ConfirmProjectName() => NameConfirmed = true;

    public void UpdateConversationState(ConversationState state)
    {
        ConversationState = state;
    }
}
