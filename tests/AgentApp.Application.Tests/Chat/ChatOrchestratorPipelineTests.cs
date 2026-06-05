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
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Filters;
using AgentApp.Domain.Gates;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;
using AgentApp.Domain.Sessions;
using AgentApp.Domain.Settings;
using AgentApp.Infrastructure.FileSystem;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.Scaffold;

namespace AgentApp.Application.Tests.Chat;

public class ChatOrchestratorPipelineTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonSettingsRepository _settingsRepo;
    private readonly JsonMdFileRepository _mdFileRepo;
    private readonly JsonTriggersIndexRepository _triggersIndexRepo;
    private readonly JsonFilterRuleRepository _filterRuleRepo;
    private readonly JsonGateRuleRepository _gateRuleRepo;
    private readonly JsonReasoningTraceRepository _traceRepo;
    private readonly JsonConversationHistoryRepository _historyRepo;
    private readonly JsonSessionRepository _sessionRepo;
    private readonly JsonRollingWindowRuleRepository _rollingWindowRuleRepo;
    private readonly FileSystemRollingWindowStore _rollingWindowStore;

    public ChatOrchestratorPipelineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _settingsRepo = new JsonSettingsRepository(_tempDir);
        _mdFileRepo = new JsonMdFileRepository(_tempDir);
        _triggersIndexRepo = new JsonTriggersIndexRepository(_tempDir);
        _filterRuleRepo = new JsonFilterRuleRepository(_tempDir);
        _gateRuleRepo = new JsonGateRuleRepository(_tempDir);
        _traceRepo = new JsonReasoningTraceRepository(_tempDir);
        _historyRepo = new JsonConversationHistoryRepository(_tempDir);
        _sessionRepo = new JsonSessionRepository(_tempDir);
        _rollingWindowRuleRepo = new JsonRollingWindowRuleRepository(_tempDir);
        _rollingWindowStore = new FileSystemRollingWindowStore(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private (ChatOrchestrator orchestrator, FakeModelProvider fakeModel) Build(string modelResponse)
    {
        var fakeModel = new FakeModelProvider(modelResponse);
        var registry = new ProviderRegistry();
        registry.Register(fakeModel);
        var dispatcher = new CapabilityDispatcher(registry);

        var projectRepo = new JsonProjectRepository(_tempDir);
        var projectSettingsRepo = new JsonProjectSettingsRepository(_tempDir);
        var knowledgeRepo = new JsonKnowledgeRecordRepository(_tempDir);
        var contextAssembler = new ContextAssembler(_mdFileRepo, _triggersIndexRepo);
        var filterPipeline = new FilterPipeline(_filterRuleRepo);
        var gateValidator = new GateValidator();
        var contextWindowManager = new ContextWindowManager(_historyRepo, _settingsRepo);
        var rollingWindowManager = new RollingWindowManager(_rollingWindowStore, _rollingWindowRuleRepo);
        var boardService = new BoardService(knowledgeRepo);
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
            new ProjectService(projectRepo, _settingsRepo),
            _settingsRepo,
            new OnboardingService(projectRepo, _settingsRepo),
            contextService,
            Array.Empty<ISystemMessageProvider>(),
            projectSettingsRepo,
            _sessionRepo,
            contextAssembler,
            _traceRepo,
            filterPipeline,
            gateValidator,
            _gateRuleRepo,
            contextWindowManager,
            rollingWindowManager,
            boardService);

        return (orchestrator, fakeModel);
    }

    // ── C-072 — Trigger keyword enrichment ────────────────────────────────────

    [Fact]
    public async Task SendAsync_TriggerKeyword_EnrichmentFilesLoaded()
    {
        var (orchestrator, fakeModel) = Build("OK");

        var index = TriggersIndex.CreateGlobal();
        index.AddEntry("coding-guide", ["refactor"]);
        await _triggersIndexRepo.SaveAsync(index);
        var enrichFile = MdFile.CreateGlobal("coding-guide", "ENRICHMENT CONTENT: always refactor cleanly");
        await _mdFileRepo.SaveAsync(enrichFile);

        await orchestrator.SendAsync("Please refactor this method");

        Assert.Contains("ENRICHMENT CONTENT", fakeModel.LastSystemMessage);
    }

    [Fact]
    public async Task SendAsync_NoTriggerKeyword_NoEnrichmentCall()
    {
        var (orchestrator, fakeModel) = Build("OK");

        var index = TriggersIndex.CreateGlobal();
        index.AddEntry("coding-guide", ["refactor"]);
        await _triggersIndexRepo.SaveAsync(index);
        var enrichFile = MdFile.CreateGlobal("coding-guide", "ENRICHMENT CONTENT: always refactor cleanly");
        await _mdFileRepo.SaveAsync(enrichFile);

        await orchestrator.SendAsync("Say hello");

        Assert.DoesNotContain("ENRICHMENT CONTENT", fakeModel.LastSystemMessage);
    }

    // ── C-075 — Token threshold / context reset ───────────────────────────────

    [Fact]
    public async Task SendAsync_TokenThresholdExceeded_ContextReset()
    {
        var (orchestrator, _) = Build("Response text that adds tokens");
        await _settingsRepo.SaveGlobalSettingsAsync(new GlobalSettings { TokenThresholdForContextReset = 1 });

        var session = ChatSession.CreateStandalone();
        await _sessionRepo.SaveAsync(session);
        orchestrator.SetCurrentSession(session.Id);

        await orchestrator.SendAsync("A message long enough to exceed the token threshold");

        var archivePath = Path.Combine(_tempDir, $"history-archive-{session.Id}.json");
        Assert.True(File.Exists(archivePath), "Context archive file should have been created after threshold exceeded.");
    }

    // ── C-077 — Gate validation ───────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_GateRuleActive_ResponseValidated()
    {
        // FakeModelProvider returns a valid gate-output response — validation should pass
        const string validResponse = "Analysis done.\n```gate-output\n{\"decision\": \"use pattern X\"}\n```";
        var (orchestrator, _) = Build(validResponse);

        var gateRule = GateRule.Create("phase-check", "Produce gate output", ["decision"]);
        await _gateRuleRepo.SaveAsync(gateRule);

        var result = await orchestrator.SendAsync("Analyze the approach");

        // If gate validation passed, the response comes through (no infinite retry loop)
        Assert.Contains("Analysis done", result.DisplayText);
    }

    [Fact]
    public async Task SendAsync_GateFailureAfterMaxRetries_RawResponseReturned()
    {
        // FakeModelProvider always returns a response WITHOUT the required gate key
        const string invalidResponse = "This is a plain response with no gate output block";
        var (orchestrator, _) = Build(invalidResponse);

        var gateRule = GateRule.Create("phase-check", "Produce gate output", ["decision"]);
        await _gateRuleRepo.SaveAsync(gateRule);

        var result = await orchestrator.SendAsync("Analyze the approach");

        // After MaxGateRetries, raw response is returned unchanged
        Assert.Contains("plain response", result.DisplayText);
        // History: user + assistant(invalid) + gate_retry_user + assistant(invalid) = 4 turns
        Assert.Equal(4, orchestrator.History.Count);
    }
}
