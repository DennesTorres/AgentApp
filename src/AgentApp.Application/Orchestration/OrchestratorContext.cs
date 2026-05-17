using AgentApp.Domain.Rules;

namespace AgentApp.Application.Orchestration;

public class OrchestratorContext
{
    public IReadOnlyList<MdFile> LoadedFiles { get; init; } = [];
    public TriggersIndex? TriggersIndex { get; init; }
    public Guid? ProjectId { get; init; }
}
