using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Providers;
using AgentApp.Domain.Chat;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;

namespace AgentApp.Application.Tests.Chat;

public class ChatServiceTests
{
    private static ChatService BuildService()
    {
        var chatClient = AzureClientFactory.BuildFromCredentials(new WindowsCredentialManager())!;
        var provider = new AzureChatClientProvider(chatClient);
        var registry = new ProviderRegistry();
        registry.Register(provider);
        var pipeline = new OrchestratorPipeline(registry);
        return new ChatService(pipeline, new ChatCommandParser(), [], new AgentContextService());
    }

    [Fact]
    public async Task SendAsync_ReturnsAssistantResponseText()
    {
        var service = BuildService();

        var result = await service.SendAsync("Say hello in one word.");

        Assert.False(string.IsNullOrWhiteSpace(result.DisplayText));
    }

    [Fact]
    public async Task SendAsync_AddsUserAndAssistantTurnsToHistory()
    {
        var service = BuildService();

        await service.SendAsync("Say hello in one word.");

        Assert.Equal(2, service.History.Count);
        Assert.Equal(ChatTurnRole.User, service.History[0].Role);
        Assert.Equal(ChatTurnRole.Assistant, service.History[1].Role);
    }

    [Fact]
    public void ClearHistory_EmptiesHistory()
    {
        var service = BuildService();
        service.ClearHistory();
        Assert.Empty(service.History);
    }
}
