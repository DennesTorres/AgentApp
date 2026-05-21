using AgentApp.Application.Chat;
using AgentApp.Domain.Chat;

namespace AgentApp.Application.Tests.Chat;

public class ActionProviderRegistryTests
{
    [Fact]
    public async Task DispatchAsync_NoProviders_ReturnsAllCommandsAsUnhandled()
    {
        var registry = new ActionProviderRegistry();
        var commands = new List<ChatCommand> { new FolderSelectCommand("reason") };

        var unhandled = await registry.DispatchAsync(commands);

        Assert.Single(unhandled);
        Assert.Same(commands[0], unhandled[0]);
    }

    [Fact]
    public async Task DispatchAsync_MatchingProvider_HandlesCommandAndReturnsEmpty()
    {
        var registry = new ActionProviderRegistry();
        var provider = new StubProvider(cmd => cmd is FolderSelectCommand);
        registry.Register(provider);

        var unhandled = await registry.DispatchAsync(
            new List<ChatCommand> { new FolderSelectCommand("reason") });

        Assert.Empty(unhandled);
        Assert.True(provider.Handled);
    }

    [Fact]
    public async Task DispatchAsync_MixedCommands_HandlesMatchedReturnsUnmatched()
    {
        var registry = new ActionProviderRegistry();
        registry.Register(new StubProvider(cmd => cmd is FolderSelectCommand));

        var folder = new FolderSelectCommand("r");
        var confirm = new ProjectConfirmCommand("App", "desc");
        var unhandled = await registry.DispatchAsync(
            new List<ChatCommand> { folder, confirm });

        Assert.Single(unhandled);
        Assert.IsType<ProjectConfirmCommand>(unhandled[0]);
    }

    private sealed class StubProvider(Func<ChatCommand, bool> matcher) : IActionProvider
    {
        public bool Handled { get; private set; }
        public bool CanHandle(ChatCommand command) => matcher(command);
        public Task HandleAsync(ChatCommand command, CancellationToken cancellationToken = default)
        {
            Handled = true;
            return Task.CompletedTask;
        }
    }
}
