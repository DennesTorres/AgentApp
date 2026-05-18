using AgentApp.Application.Chat;
using AgentApp.Application.Providers;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Providers;

namespace AgentApp.Application.Tests.Chat;

public class ChatServiceTests
{
    private static (ChatService service, FakeProvider modelProvider) BuildService()
    {
        var registry = new ProviderRegistry();
        var modelProvider = new FakeProvider(ProviderCapability.ModelCall);
        registry.Register(modelProvider);
        var pipeline = new OrchestratorPipeline(registry);
        return (new ChatService(pipeline), modelProvider);
    }

    [Fact]
    public async Task SendAsync_ReturnsAssistantResponseText()
    {
        var (service, provider) = BuildService();
        provider.SetResponse(r => ProviderResponse.Ok(r.RequestId,
            new Dictionary<string, object> { ["text"] = "Hello back!" }));

        var result = await service.SendAsync("Hello");

        Assert.Equal("Hello back!", result);
    }

    [Fact]
    public async Task SendAsync_AddsUserAndAssistantTurnsToHistory()
    {
        var (service, provider) = BuildService();
        provider.SetResponse(r => ProviderResponse.Ok(r.RequestId,
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
        provider.SetResponse(r => ProviderResponse.Fail(r.RequestId, "API error"));

        var result = await service.SendAsync("Hello");

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public async Task SendAsync_FailedResponse_DoesNotAddAssistantTurnToHistory()
    {
        var (service, provider) = BuildService();
        provider.SetResponse(r => ProviderResponse.Fail(r.RequestId, "API error"));

        await service.SendAsync("Hello");

        Assert.Equal(1, service.History.Count);
    }

    [Fact]
    public void ClearHistory_EmptiesHistory()
    {
        var (service, _) = BuildService();
        service.ClearHistory();
        Assert.Empty(service.History);
    }
}
