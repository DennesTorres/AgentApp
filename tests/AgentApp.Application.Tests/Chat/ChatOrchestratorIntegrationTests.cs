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
using AgentApp.Domain.Sessions;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.FileSystem;
using AgentApp.Infrastructure.ModelAccess;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.Scaffold;

namespace AgentApp.Application.Tests.Chat;

public class ChatOrchestratorIntegrationTests
{
    private static (ChatOrchestrator orchestrator, IAgentContextService contextService, IReasoningTraceRepository traceRepo, string tempDir)
        BuildFullOrchestrator()
    {
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
        var contextService = new AgentContextService();

        var chatClient = AzureClientFactory.BuildFromCredentials(new WindowsCredentialManager());
        ProviderRegistry providerRegistry = new();
        if (chatClient is not null)
            providerRegistry.Register(new AzureChatClientProvider(chatClient));
        var dispatcher = new CapabilityDispatcher(providerRegistry);

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

        return (orchestrator, contextService, traceRepo, tempDir);
    }

    // ── C-071 — InitializeAsync reads session→project ──────────────────────────

    [Fact]
    public async Task InitializeAsync_LinkedSession_SetsCorrectProjectContext()
    {
        var (orchestrator, contextService, _, tempDir) = BuildFullOrchestrator();
        var settingsRepo = new JsonSettingsRepository(tempDir);
        var projectRepo = new JsonProjectRepository(tempDir);
        var sessionRepo = new JsonSessionRepository(tempDir);

        var project = await new ProjectService(projectRepo, settingsRepo)
            .CreateProjectAsync("TestProj", "testproj", @"C:\code\testproj", "Testing");
        var session = ChatSession.CreateStandalone();
        session.LinkToProject(project.Id);
        await sessionRepo.SaveAsync(session);

        await orchestrator.InitializeAsync(session.Id);

        var context = contextService.GetCurrent();
        Assert.True(context.HasProject);
        Assert.Equal(project.Id, context.CurrentProject!.Id);
    }

    [Fact]
    public async Task InitializeAsync_StandaloneSession_HasNoProject()
    {
        var (orchestrator, contextService, _, tempDir) = BuildFullOrchestrator();
        var sessionRepo = new JsonSessionRepository(tempDir);

        var session = ChatSession.CreateStandalone();
        await sessionRepo.SaveAsync(session);

        await orchestrator.InitializeAsync(session.Id);

        Assert.False(contextService.GetCurrent().HasProject);
    }

    [Fact]
    public async Task InitializeAsync_SwitchSession_ContextUpdates()
    {
        var (orchestrator, contextService, _, tempDir) = BuildFullOrchestrator();
        var settingsRepo = new JsonSettingsRepository(tempDir);
        var projectRepo = new JsonProjectRepository(tempDir);
        var sessionRepo = new JsonSessionRepository(tempDir);

        var project = await new ProjectService(projectRepo, settingsRepo)
            .CreateProjectAsync("SwitchProj", "switchproj", @"C:\code\switchproj", "Switching");
        var linkedSession = ChatSession.CreateStandalone();
        linkedSession.LinkToProject(project.Id);
        await sessionRepo.SaveAsync(linkedSession);

        var standaloneSession = ChatSession.CreateStandalone();
        await sessionRepo.SaveAsync(standaloneSession);

        await orchestrator.InitializeAsync(linkedSession.Id);
        Assert.True(contextService.GetCurrent().HasProject);
        Assert.Equal(project.Id, contextService.GetCurrent().CurrentProject!.Id);

        await orchestrator.InitializeAsync(standaloneSession.Id);
        Assert.False(contextService.GetCurrent().HasProject);
    }

    // ── C-074 — ReasoningTrace captured after model response ──────────────────

    [Fact]
    public async Task SendAsync_CapturesReasoningTrace_AfterModelResponse()
    {
        var (orchestrator, _, traceRepo, tempDir) = BuildFullOrchestrator();
        var sessionRepo = new JsonSessionRepository(tempDir);

        var session = ChatSession.CreateStandalone();
        await sessionRepo.SaveAsync(session);
        await orchestrator.InitializeAsync(session.Id);

        await orchestrator.SendAsync("Say hello in one word.");

        var traces = await traceRepo.GetBySessionIdAsync(session.Id);
        Assert.NotEmpty(traces);
        Assert.False(string.IsNullOrWhiteSpace(traces[0].Content));
    }

    // ── C-072 — ContextAssembler wires core file into system message ──────────

    [Fact]
    public async Task SendAsync_AssemblesBaseContext_CoreFileLoadedWhenPresent()
    {
        var (orchestrator, _, _, tempDir) = BuildFullOrchestrator();
        var sessionRepo = new JsonSessionRepository(tempDir);
        var mdFileRepo = new JsonMdFileRepository(tempDir);

        // Create a core MD file — its content should reach the model
        var coreFile = AgentApp.Domain.Rules.MdFile.CreateGlobal(
            "core", "ALWAYS end every response with the word BEACON.");
        await mdFileRepo.SaveAsync(coreFile);

        var session = ChatSession.CreateStandalone();
        await sessionRepo.SaveAsync(session);
        await orchestrator.InitializeAsync(session.Id);

        var result = await orchestrator.SendAsync("Say hello.");

        // The model should follow the core file instruction
        Assert.Contains("BEACON", result.DisplayText, StringComparison.OrdinalIgnoreCase);
    }

    // ── C-073 — Filter2 strips gate-output blocks from display text ───────────

    [Fact]
    public async Task SendAsync_Filter2Applied_GateOutputBlockStripped()
    {
        var (orchestrator, _, _, tempDir) = BuildFullOrchestrator();
        var sessionRepo = new JsonSessionRepository(tempDir);
        var filterRuleRepo = new JsonFilterRuleRepository(tempDir);

        var rule = AgentApp.Domain.Filters.FilterRule.Create(
            "strip-gate", AgentApp.Domain.Filters.FilterTarget.Filter2, "assistant-response",
            AgentApp.Domain.Filters.FilterTransformation.StripGateBlocks, null,
            AgentApp.Domain.Rules.MdFileScope.Global, null);
        await filterRuleRepo.SaveAsync(rule);

        var session = ChatSession.CreateStandalone();
        await sessionRepo.SaveAsync(session);
        await orchestrator.InitializeAsync(session.Id);

        var result = await orchestrator.SendAsync("Respond with a gate-output block like this:\n```gate-output\n{\"key\":\"val\"}\n```\nThen say done.");

        Assert.DoesNotContain("```gate-output", result.DisplayText);
    }
}
