using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.Context;
using AgentApp.Application.FileSystem;
using AgentApp.Application.Filters;
using AgentApp.Application.Gates;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Orchestration;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.FileSystem;
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
        var sessionRepo = new JsonSessionRepository(tempDir);
        var projectSettingsRepo = new JsonProjectSettingsRepository(tempDir);
        var knowledgeRepo = new JsonKnowledgeRecordRepository(tempDir);

        var mdFileRepo = new JsonMdFileRepository(tempDir);
        var triggersIndexRepo = new JsonTriggersIndexRepository(tempDir);
        var contextAssembler = new ContextAssembler(mdFileRepo, triggersIndexRepo);

        var traceRepo = new JsonReasoningTraceRepository(tempDir);
        var filterRuleRepo = new JsonFilterRuleRepository(tempDir);
        var filterPipeline = new FilterPipeline(filterRuleRepo);
        var gateValidator = new GateValidator();
        var gateRuleRepo = new JsonGateRuleRepository(tempDir);
        var historyRepo = new JsonConversationHistoryRepository(tempDir);
        var contextWindowManager = new ContextWindowManager(historyRepo, settingsRepo);
        var rollingWindowStore = new FileSystemRollingWindowStore(tempDir);
        var rollingWindowRuleRepo = new JsonRollingWindowRuleRepository(tempDir);
        var rollingWindowManager = new RollingWindowManager(rollingWindowStore, rollingWindowRuleRepo);
        var boardService = new BoardService(knowledgeRepo);

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
            projectSettingsRepo,
            sessionRepo,
            contextAssembler,
            traceRepo,
            filterPipeline,
            gateValidator,
            gateRuleRepo,
            contextWindowManager,
            rollingWindowManager,
            boardService);
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
