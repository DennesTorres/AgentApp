using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Tests.Fakes;

internal sealed class FakeLearningLogger : ILearningLogger
{
    public List<string> Entries { get; } = [];

    public Task LogAsync(string entry)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}
