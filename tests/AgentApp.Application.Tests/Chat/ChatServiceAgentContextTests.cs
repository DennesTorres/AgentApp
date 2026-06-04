using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.FileSystem;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Application.SystemMessage;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.Scaffold;

namespace AgentApp.Application.Tests.Chat;

public class ChatServiceAgentContextTests
{
    private static (ChatOrchestrator orchestrator, FakeModelProvider fakeModel, AgentContextService contextService)
        BuildWithFake(string modelResponse, ISystemMessageProvider[]? providers = null)
    {
        var fakeModel = new FakeModelProvider(modelResponse);
        var registry = new ProviderRegistry();
        registry.Register(fakeModel);
        var dispatcher = new CapabilityDispatcher(registry);

        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var projectRepo = new JsonProjectRepository(tempDir);
        var settingsRepo = new JsonSettingsRepository(tempDir);

        var commandParser = new ChatCommandParser();
        var responsePrep = new ResponsePreparationService(commandParser);
        var actionRegistry = new ActionProviderRegistry();
        var contextService = new AgentContextService();

        var orchestrator = new ChatOrchestrator(
            dispatcher,
            responsePrep,
            actionRegistry,
            new FileSessionGate(),
            new ScaffoldService(),
            new ProjectService(projectRepo, settingsRepo),
            settingsRepo,
            new OnboardingService(projectRepo, settingsRepo),
            contextService,
            providers ?? [],
            new JsonProjectSettingsRepository(tempDir),
            new JsonSessionRepository(tempDir));
        return (orchestrator, fakeModel, contextService);
    }

    [Fact]
    public async Task StateTransition_UpdatesAgentContext()
    {
        var (orchestrator, _, ctx) = BuildWithFake(
            "Switching mode. [STATE_TRANSITION:{\"mode\":\"implementing\"}]");

        await orchestrator.SendAsync("Start implementing");

        Assert.Equal("implementing", ctx.GetCurrent().ConversationState.Mode);
    }

    [Fact]
    public async Task StateTransitionCommand_NotReturnedToViewModel()
    {
        var (orchestrator, _, _) = BuildWithFake(
            "Done. [STATE_TRANSITION:{\"mode\":\"testing\"}]");

        var result = await orchestrator.SendAsync("Switch state");

        Assert.DoesNotContain(result.Commands, c => c is StateTransitionCommand);
    }

    [Fact]
    public async Task SystemMessageProviders_PassedToModel()
    {
        var (orchestrator, fakeModel, _) = BuildWithFake("OK",
            [new NoProjectProvider()]);

        await orchestrator.SendAsync("Hello");

        Assert.NotEmpty(fakeModel.LastSystemMessage);
        Assert.Contains("Tower", fakeModel.LastSystemMessage);
    }

    [Fact]
    public async Task NoProviders_EmptySystemMessage()
    {
        var (orchestrator, fakeModel, _) = BuildWithFake("OK");

        await orchestrator.SendAsync("Hello");

        Assert.Empty(fakeModel.LastSystemMessage);
    }

    [Fact]
    public async Task MultipleTransitions_UpdatesToLatest()
    {
        var (orchestrator, _, ctx) = BuildWithFake(
            "OK [STATE_TRANSITION:{\"mode\":\"testing\"}]");

        await orchestrator.SendAsync("First");
        await orchestrator.SendAsync("Second");

        Assert.Equal("testing", ctx.GetCurrent().ConversationState.Mode);
    }
}
