using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.FileSystem;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.Scaffold;

namespace AgentApp.Application.Tests.Chat;

public class ChatOrchestratorTests
{
    private static ChatOrchestrator BuildOrchestrator()
    {
        var chatClient = AzureClientFactory.BuildFromCredentials(new WindowsCredentialManager())!;
        var provider = new AzureChatClientProvider(chatClient);
        var providerRegistry = new ProviderRegistry();
        providerRegistry.Register(provider);
        var dispatcher = new CapabilityDispatcher(providerRegistry);

        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var projectRepo = new JsonProjectRepository(tempDir);
        var settingsRepo = new JsonSettingsRepository(tempDir);

        var commandParser = new ChatCommandParser();
        var responsePrep = new ResponsePreparationService(commandParser);
        var actionRegistry = new ActionProviderRegistry();

        return new ChatOrchestrator(
            dispatcher,
            responsePrep,
            actionRegistry,
            new FileSessionGate(),
            new ScaffoldService(),
            new ProjectService(projectRepo, settingsRepo),
            settingsRepo,
            new OnboardingService(projectRepo, settingsRepo),
            new AgentContextService(),
            Array.Empty<ISystemMessageProvider>(),
            new JsonProjectSettingsRepository(tempDir));
    }

    [Fact]
    public async Task SendAsync_ReturnsAssistantResponseText()
    {
        var orchestrator = BuildOrchestrator();

        var result = await orchestrator.SendAsync("Say hello in one word.");

        Assert.False(string.IsNullOrWhiteSpace(result.DisplayText));
    }

    [Fact]
    public async Task SendAsync_AddsUserAndAssistantTurnsToHistory()
    {
        var orchestrator = BuildOrchestrator();

        await orchestrator.SendAsync("Say hello in one word.");

        Assert.Equal(2, orchestrator.History.Count);
        Assert.Equal(ChatTurnRole.User, orchestrator.History[0].Role);
        Assert.Equal(ChatTurnRole.Assistant, orchestrator.History[1].Role);
    }

    [Fact]
    public void ClearHistory_EmptiesHistory()
    {
        var orchestrator = BuildOrchestrator();
        orchestrator.ClearHistory();
        Assert.Empty(orchestrator.History);
    }
}
