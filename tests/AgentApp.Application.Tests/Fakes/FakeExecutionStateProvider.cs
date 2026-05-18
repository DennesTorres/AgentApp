using AgentApp.Domain.ExecutionStates;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class FakeExecutionStateProvider : IExecutionStateProvider
{
    private readonly List<string> _fileNames;

    public ExecutionStateName StateName { get; }

    public FakeExecutionStateProvider(ExecutionStateName stateName, IEnumerable<string>? fileNames = null)
    {
        StateName = stateName;
        _fileNames = fileNames?.ToList() ?? [];
    }

    public IReadOnlyList<string> GetSystemMdFileNames() => _fileNames;
}
