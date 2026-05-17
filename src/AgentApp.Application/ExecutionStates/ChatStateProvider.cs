using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.ExecutionStates;

public class ChatStateProvider : IExecutionStateProvider
{
    public ExecutionStateName StateName => ExecutionStateName.Chat;
    public IReadOnlyList<string> GetSystemMdFileNames() => [];
}
