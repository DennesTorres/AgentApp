using AgentApp.Application.Chat;
using AgentApp.Application.Providers;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Providers;
using NSubstitute;

namespace AgentApp.Application.Tests.Chat;

public class ChatServiceTests
{
    private static (ChatService service, IProvider modelProvider) BuildService()
    {
        var registry = Substitute.For<IProviderRegistry>();
        var modelProvider = Substitute.For<IProvider>();
        modelProvider.Capability.Returns(ProviderCapability.ModelCall);
        registry.Resolve(ProviderCapability.ModelCall).Returns(modelProvider);
        var pipeline = new OrchestratorPipeline(registry);
        return (new ChatService(pipeline), modelProvider);
    }

    [Fact]
    public async Task SendAsync_ReturnsAssistantResponseText()
    {
        var (service, provider) = BuildService();
        provider.HandleAsync(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => ProviderResponse.Ok(
                ci.Arg<ProviderRequest>().RequestId,
                new Dictionary<string, object> { ["text"] = "Hello back!" }));

        var result = await service.SendAsync("Hello");

        Assert.Equal("Hello back!", result);
    }

    [Fact]
    public async Task SendAsync_AddsUserAndAssistantTurnsToHistory()
    {
        var (service, provider) = BuildService();
        provider.HandleAsync(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => ProviderResponse.Ok(
                ci.Arg<ProviderRequest>().RequestId,
                new Dictionary<string, object> { ["text"] = "Response" }));

        await service.SendAsync("Message");

        Assert.Equal(2, service.History.Count);
        Assert.Equal(ChatTurnRole.User, service.History[0].Role);
        Assert.Equal("Message", service.History[0].Content);
        Assert.Equal(ChatTurnRole.Assistant, service.History[1].Role);
        Assert.Equal("Response", service.History[1].Content);
    }

    [Fact]
    public async Task SendAsync_FailedResponse_ReturnsErrorPrefix()
    {
        var (service, provider) = BuildService();
        provider.HandleAsync(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => ProviderResponse.Fail(ci.Arg<ProviderRequest>().RequestId, "API error"));

        var result = await service.SendAsync("Hello");

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public async Task SendAsync_FailedResponse_DoesNotAddAssistantTurnToHistory()
    {
        var (service, provider) = BuildService();
        provider.HandleAsync(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => ProviderResponse.Fail(ci.Arg<ProviderRequest>().RequestId, "API error"));

        await service.SendAsync("Hello");

        Assert.Equal(1, service.History.Count); // only user turn added
    }

    [Fact]
    public void ClearHistory_EmptiesHistory()
    {
        var (service, _) = BuildService();
        service.ClearHistory();
        Assert.Empty(service.History);
    }
}
