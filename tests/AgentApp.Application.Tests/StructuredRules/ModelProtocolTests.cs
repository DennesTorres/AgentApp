using AgentApp.Application.Chat;
using AgentApp.Application.ExecutionStates;
using AgentApp.Application.Learning;
using AgentApp.Application.Providers;
using AgentApp.Application.StructuredRules;
using AgentApp.Domain.Chat;
using AgentApp.Domain.ExecutionStates;
using AgentApp.Domain.Learning;
using AgentApp.Domain.Providers;
using AgentApp.Domain.StructuredRules;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;

namespace AgentApp.Application.Tests.StructuredRules;

/// <summary>
/// End-to-end tests verifying the model follows the structured protocol when
/// instructed via system messages. All tests call the real Azure model endpoint.
/// </summary>
public class ModelProtocolTests
{
    // ── shared infrastructure ────────────────────────────────────────────────

    private static OrchestratorPipeline BuildPipeline()
    {
        var chatClient = AzureClientFactory.BuildFromCredentials(new WindowsCredentialManager())!;
        var registry = new ProviderRegistry();
        registry.Register(new AzureChatClientProvider(chatClient));
        return new OrchestratorPipeline(registry);
    }

    private static ChatService BuildChatService() => new(BuildPipeline());

    private static ProviderRequest Request(string systemMessage, List<ChatTurn> history) =>
        ProviderRequest.Create(ProviderCapability.ModelCall, new Dictionary<string, object>
        {
            ["history"] = history,
            ["systemMessage"] = systemMessage
        });

    private static List<ChatTurn> Turn(string content) =>
        [new(ChatTurnRole.User, content, DateTimeOffset.UtcNow)];

    // ── gate protocol — single step ──────────────────────────────────────────

    [Fact]
    public async Task GateProtocol_SingleStep_ModelIncludesMarker()
    {
        const string sysMsg = """
            You are a test assistant following a mandatory gate protocol.
            After every response you MUST include this exact marker on its own line at the end:
            [GATE:{"rule":"ILH","step":1}]
            This marker is required. Never omit it.
            """;

        var response = await BuildPipeline().SendAsync(
            Request(sysMsg, Turn("Acknowledge this instruction with one sentence.")));

        Assert.True(response.Success);
        var gate = GateOutput.TryParse(response.Result["text"].ToString()!);
        Assert.NotNull(gate);
        Assert.Equal("ILH", gate.RuleId);
        Assert.Equal(1, gate.StepNumber);
    }

    [Fact]
    public async Task GateProtocol_SingleStep_TryParseExtractsCorrectFields()
    {
        const string sysMsg = """
            You are a test assistant. Every response MUST end with this marker:
            [GATE:{"rule":"SIP","step":3}]
            Never omit it.
            """;

        var response = await BuildPipeline().SendAsync(
            Request(sysMsg, Turn("Say 'understood'.")));

        Assert.True(response.Success);
        var gate = GateOutput.TryParse(response.Result["text"].ToString()!);
        Assert.NotNull(gate);
        Assert.Equal("SIP", gate.RuleId);
        Assert.Equal(3, gate.StepNumber);
    }

    // ── gate protocol — multi-step ───────────────────────────────────────────

    [Fact]
    public async Task GateProtocol_MultiStep_BothTurnsContainCorrectStepMarkers()
    {
        const string sysMsg = """
            You are a test assistant executing a 2-step rule named MULTI.
            When completing step 1, end your response with: [GATE:{"rule":"MULTI","step":1}]
            When completing step 2, end your response with: [GATE:{"rule":"MULTI","step":2}]
            Complete one step per turn. Never include a marker for the wrong step.
            """;
        var pipeline = BuildPipeline();

        // Turn 1 — step 1
        var history = Turn("Complete step 1 now.");
        var r1 = await pipeline.SendAsync(Request(sysMsg, history));
        Assert.True(r1.Success);
        var gate1 = GateOutput.TryParse(r1.Result["text"].ToString()!);
        Assert.NotNull(gate1);
        Assert.Equal("MULTI", gate1.RuleId);
        Assert.Equal(1, gate1.StepNumber);

        // Turn 2 — step 2
        history.Add(new(ChatTurnRole.Assistant, r1.Result["text"].ToString()!, DateTimeOffset.UtcNow));
        history.Add(new(ChatTurnRole.User, "Complete step 2 now.", DateTimeOffset.UtcNow));
        var r2 = await pipeline.SendAsync(Request(sysMsg, history));
        Assert.True(r2.Success);
        var gate2 = GateOutput.TryParse(r2.Result["text"].ToString()!);
        Assert.NotNull(gate2);
        Assert.Equal("MULTI", gate2.RuleId);
        Assert.Equal(2, gate2.StepNumber);
    }

    // ── gate protocol — tracker integration ─────────────────────────────────

    [Fact]
    public async Task GateProtocol_MultiStepGateTracker_ValidatesModelOutputInSequence()
    {
        var rule = StructuredRule.Create(
            "TRACKER-RULE", "Tracker Integration Test", "always",
            ["Acknowledge the request", "Provide the answer"]);
        var tracker = new MultiStepGateTracker();
        tracker.Activate(rule);

        const string sysMsg = """
            You are a test assistant with a 2-step rule named TRACKER-RULE.
            Step 1 response MUST end with: [GATE:{"rule":"TRACKER-RULE","step":1}]
            Step 2 response MUST end with: [GATE:{"rule":"TRACKER-RULE","step":2}]
            Complete exactly one step per turn.
            """;
        var pipeline = BuildPipeline();

        // Step 1
        var history = Turn("Execute step 1.");
        var r1 = await pipeline.SendAsync(Request(sysMsg, history));
        Assert.True(r1.Success);
        var gate1 = GateOutput.TryParse(r1.Result["text"].ToString()!);
        Assert.NotNull(gate1);
        Assert.True(tracker.ValidateStep(gate1));

        tracker.AdvanceStep();
        Assert.False(tracker.IsComplete);

        // Step 2
        history.Add(new(ChatTurnRole.Assistant, r1.Result["text"].ToString()!, DateTimeOffset.UtcNow));
        history.Add(new(ChatTurnRole.User, "Execute step 2.", DateTimeOffset.UtcNow));
        var r2 = await pipeline.SendAsync(Request(sysMsg, history));
        Assert.True(r2.Success);
        var gate2 = GateOutput.TryParse(r2.Result["text"].ToString()!);
        Assert.NotNull(gate2);
        Assert.True(tracker.ValidateStep(gate2));

        tracker.AdvanceStep();
        Assert.True(tracker.IsComplete);
    }

    // ── gate protocol — full StructuredRule JSON in system message ───────────

    [Fact]
    public async Task GateProtocol_StructuredRuleSerializedAsJson_ModelFollowsFormat()
    {
        var rule = StructuredRule.Create(
            "ILH", "Investigation Lookup Hierarchy",
            "before any investigation",
            [
                "Quote relevant entry from test-cycles.md (or state 'no entry')",
                "Check branch-records.md for context",
                "Read STORY-BEHAVIOR.md"
            ]);

        var sysMsg = $$"""
            You are a coding assistant following structured rules.
            Active rule (JSON): {{rule.ToJson()}}

            Gate protocol: when you complete a step, end your response with the marker for that step.
            For step 1: [GATE:{"rule":"ILH","step":1}]
            Never omit this marker. Complete step 1 now.
            """;

        var response = await BuildPipeline().SendAsync(
            Request(sysMsg, Turn("Begin investigation. test-cycles.md shows no entry for this item.")));

        Assert.True(response.Success);
        var gate = GateOutput.TryParse(response.Result["text"].ToString()!);
        Assert.NotNull(gate);
        Assert.Equal("ILH", gate.RuleId);
        Assert.Equal(1, gate.StepNumber);
    }

    // ── learning protocol ────────────────────────────────────────────────────

    [Fact]
    public async Task LearningProtocol_WithSystemMessage_ModelProducesRuleProposal()
    {
        const string sysMsg = """
            You are a learning assistant that helps improve AI behavioral rules.
            When given a rule violation, analyze it and propose a new rule or rule improvement.
            You MUST end your response with this exact marker:
            [RULE_PROPOSAL:{"fileName":"CLAUDE.md","ruleText":"<the proposed rule text>","humanSummary":"<one-sentence summary>","action":"add"}]
            All four fields are required. Never omit this marker.
            """;

        var orchestrator = new LearningOrchestrator(BuildPipeline());
        var session = LearningSession.Initiate(
            LearningTrigger.InternalGateFailure,
            "The assistant skipped ILH step 1 (quoting test-cycles.md) before forming a technical hypothesis.");

        var proposal = await orchestrator.RunLearningLoopAsync(session, systemMessage: sysMsg);

        Assert.NotNull(proposal);
    }

    [Fact]
    public async Task LearningProtocol_WithSystemMessage_ProposalContainsAllRequiredFields()
    {
        const string sysMsg = """
            You are a learning assistant that helps improve AI behavioral rules.
            When given a rule violation, analyze it and propose a rule improvement.
            You MUST end your response with this exact marker:
            [RULE_PROPOSAL:{"fileName":"CLAUDE.md","ruleText":"<the full proposed rule text, min 20 words>","humanSummary":"<one-sentence plain-English summary>","action":"add"}]
            Replace the placeholder text with real content. All four fields are required.
            """;

        var orchestrator = new LearningOrchestrator(BuildPipeline());
        var session = LearningSession.Initiate(
            LearningTrigger.ExternalUserError,
            "The assistant created hand-written fake objects instead of using real implementations, violating the no-mocks/no-fakes rule.");

        var proposal = await orchestrator.RunLearningLoopAsync(session, systemMessage: sysMsg);

        Assert.NotNull(proposal);
        Assert.False(string.IsNullOrWhiteSpace(proposal.FileName));
        Assert.False(string.IsNullOrWhiteSpace(proposal.RuleText));
        Assert.False(string.IsNullOrWhiteSpace(proposal.HumanSummary));
        Assert.False(string.IsNullOrWhiteSpace(proposal.Action));
    }

    // ── multi-turn conversation ──────────────────────────────────────────────

    [Fact]
    public async Task MultiTurnConversation_SecondTurnReferencesFirstTurnContent()
    {
        // Unique name that cannot appear by coincidence
        const string uniqueName = "Zxqvbnmkj";
        var service = BuildChatService();

        await service.SendAsync($"My name is {uniqueName}. Just say 'noted'.");
        var response = await service.SendAsync("What is my name? Answer in one word only.");

        Assert.Contains(uniqueName, response, StringComparison.OrdinalIgnoreCase);
    }

    // ── state-aware system message ───────────────────────────────────────────

    [Fact]
    public async Task StateAwareSystemMessage_ResearchState_ModelFollowsInvestigationInstructions()
    {
        var machine = new ExecutionStateMachine();
        machine.TransitionTo(ExecutionStateName.Research);
        var stateRegistry = new ExecutionStateRegistry();
        stateRegistry.Register(new ResearchStateProvider());
        var builder = new StateAwareSystemMessageBuilder(machine, stateRegistry);

        var contextFiles = builder.GetContextFileNames();
        var sysMsg = $"""
            You are a coding assistant in investigation mode.
            Active context files: {string.Join(", ", contextFiles)}.
            The ILH (Investigation Lookup Hierarchy) is your mandatory first step before any analysis.
            You must always mention ILH as your first action when asked to investigate anything.
            """;

        var response = await BuildPipeline().SendAsync(
            Request(sysMsg, Turn("How should I start investigating a reported bug?")));

        Assert.True(response.Success);
        var text = response.Result["text"].ToString()!;
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("ILH", text, StringComparison.OrdinalIgnoreCase);
    }
}
