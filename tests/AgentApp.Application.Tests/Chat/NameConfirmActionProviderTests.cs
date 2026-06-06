using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Domain.Chat;

namespace AgentApp.Application.Tests.Chat;

public class NameConfirmActionProviderTests
{
    private readonly AgentContextService _contextService = new();

    private NameConfirmActionProvider Build() => new(_contextService);

    [Fact]
    public void CanHandle_NameConfirmedCommand_ReturnsTrue()
    {
        var provider = Build();
        Assert.True(provider.CanHandle(new NameConfirmedCommand()));
    }

    [Fact]
    public void CanHandle_OtherCommand_ReturnsFalse()
    {
        var provider = Build();
        Assert.False(provider.CanHandle(new FolderSelectCommand("reason")));
    }

    [Fact]
    public async Task HandleAsync_SetsNameConfirmedOnContext()
    {
        var provider = Build();
        await provider.HandleAsync(new NameConfirmedCommand());
        Assert.True(_contextService.GetCurrent().NameConfirmed);
    }
}
