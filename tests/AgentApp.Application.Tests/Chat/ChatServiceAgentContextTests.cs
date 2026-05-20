using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Providers;
using AgentApp.Application.SystemMessage;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Tests.Chat;

public class ChatServiceAgentContextTests
{
    private static (ChatService service, FakeModelProvider fakeModel, AgentContextService contextService)
        BuildWithFake(string modelResponse, ISystemMessageProvider[]? providers = null)
    {
        var fakeModel = new FakeModelProvider(modelResponse);
        var registry = new ProviderRegistry();
        registry.Register(fakeModel);
        var pipeline = new OrchestratorPipeline(registry);
        var contextService = new AgentContextService();
        var service = new ChatService(pipeline, new ChatCommandParser(),
            providers ?? [], contextService);
        return (service, fakeModel, contextService);
    }

    [Fact]
    public async Task StateTransition_UpdatesAgentContext()
    {
        var (service, _, ctx) = BuildWithFake(
            "Switching mode. [STATE_TRANSITION:{\"mode\":\"implementing\"}]");

        await service.SendAsync("Start implementing");

        Assert.Equal("implementing", ctx.GetCurrent().ConversationState.Mode);
    }

    [Fact]
    public async Task StateTransitionCommand_NotReturnedToViewModel()
    {
        var (service, _, _) = BuildWithFake(
            "Done. [STATE_TRANSITION:{\"mode\":\"testing\"}]");

        var result = await service.SendAsync("Switch state");

        Assert.DoesNotContain(result.Commands, c => c is StateTransitionCommand);
    }

    [Fact]
    public async Task SystemMessageProviders_PassedToModel()
    {
        var (service, fakeModel, _) = BuildWithFake("OK",
            [new NoProjectProvider()]);

        await service.SendAsync("Hello");

        Assert.NotEmpty(fakeModel.LastSystemMessage);
        Assert.Contains("Tower", fakeModel.LastSystemMessage);
    }

    [Fact]
    public async Task NoProviders_EmptySystemMessage()
    {
        var (service, fakeModel, _) = BuildWithFake("OK");

        await service.SendAsync("Hello");

        Assert.Empty(fakeModel.LastSystemMessage);
    }

    [Fact]
    public async Task MultipleTransitions_UpdatesToLatest()
    {
        var (service, _, ctx) = BuildWithFake(
            "OK [STATE_TRANSITION:{\"mode\":\"testing\"}]");

        await service.SendAsync("First");
        await service.SendAsync("Second");

        Assert.Equal("testing", ctx.GetCurrent().ConversationState.Mode);
    }
}
