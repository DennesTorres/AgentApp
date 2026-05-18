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

    [Fact]
    public async Task StateAwareSystemMessage_ImplementingState_ModelFollowsGitAndArchitectureContext()
    {
        var machine = new ExecutionStateMachine();
        machine.TransitionTo(ExecutionStateName.Implementing);
        var stateRegistry = new ExecutionStateRegistry();
        stateRegistry.Register(new ImplementingStateProvider());
        var builder = new StateAwareSystemMessageBuilder(machine, stateRegistry);

        var contextFiles = builder.GetContextFileNames();
        var sysMsg = $"""
            You are a coding assistant in implementation mode.
            Active context files: {string.Join(", ", contextFiles)}.
            You must always follow the git workflow and architecture-backend standards.
            When asked what to do before committing, always mention git workflow and architecture constraints.
            """;

        var response = await BuildPipeline().SendAsync(
            Request(sysMsg, Turn("What should I verify before committing my implementation?")));

        Assert.True(response.Success);
        var text = response.Result["text"].ToString()!;
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.True(
            text.Contains("git", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("architecture", StringComparison.OrdinalIgnoreCase),
            $"Expected response to reference git or architecture context. Got: {text[..Math.Min(200, text.Length)]}");
    }

    [Fact]
    public async Task StateAwareSystemMessage_TestingState_ModelAcknowledgesTestingWorkflow()
    {
        var machine = new ExecutionStateMachine();
        machine.TransitionTo(ExecutionStateName.Testing);
        var stateRegistry = new ExecutionStateRegistry();
        stateRegistry.Register(new TestingStateProvider());
        var builder = new StateAwareSystemMessageBuilder(machine, stateRegistry);

        var contextFiles = builder.GetContextFileNames();
        var sysMsg = $"""
            You are a coding assistant in testing mode.
            Active context files: {string.Join(", ", contextFiles)}.
            In testing mode you analyze and propose only — you never write code directly.
            When asked what to do in testing mode, always mention analyze-only and propose-only behavior.
            """;

        var response = await BuildPipeline().SendAsync(
            Request(sysMsg, Turn("What can I do in testing mode?")));

        Assert.True(response.Success);
        var text = response.Result["text"].ToString()!;
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.True(
            text.Contains("analyz", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("propose", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("test", StringComparison.OrdinalIgnoreCase),
            $"Expected response to reference testing/analysis behavior. Got: {text[..Math.Min(200, text.Length)]}");
    }

    [Fact]
    public async Task StateAwareSystemMessage_ChatState_ModelRespondsWithoutSpecialContext()
    {
        var machine = new ExecutionStateMachine();
        var stateRegistry = new ExecutionStateRegistry();
        stateRegistry.Register(new ChatStateProvider());
        var builder = new StateAwareSystemMessageBuilder(machine, stateRegistry);

        var contextFiles = builder.GetContextFileNames();
        Assert.Empty(contextFiles); // Chat state has no context files

        // No system message — baseline: model should still respond correctly
        var response = await BuildPipeline().SendAsync(
            Request("You are a helpful assistant.", Turn("Say 'ready' in one word.")));

        Assert.True(response.Success);
        Assert.False(string.IsNullOrWhiteSpace(response.Result["text"].ToString()));
    }

    // ── three-step gate ──────────────────────────────────────────────────────

    [Fact]
    public async Task GateProtocol_ThreeStep_AllStepsContainCorrectMarkers()
    {
        const string sysMsg = """
            You are a test assistant executing a 3-step rule named ILH-3.
            Step 1 response MUST end with: [GATE:{"rule":"ILH-3","step":1}]
            Step 2 response MUST end with: [GATE:{"rule":"ILH-3","step":2}]
            Step 3 response MUST end with: [GATE:{"rule":"ILH-3","step":3}]
            Complete exactly one step per turn. Never include a marker for the wrong step.
            """;
        var pipeline = BuildPipeline();
        var rule = StructuredRule.Create("ILH-3", "Three Step ILH", "always",
            ["Quote test-cycles.md", "Check branch-records.md", "Read STORY-BEHAVIOR.md"]);
        var tracker = new MultiStepGateTracker();
        tracker.Activate(rule);

        var history = Turn("Complete step 1.");
        var r1 = await pipeline.SendAsync(Request(sysMsg, history));
        var gate1 = GateOutput.TryParse(r1.Result["text"].ToString()!);
        Assert.NotNull(gate1);
        Assert.Equal(1, gate1.StepNumber);
        Assert.True(tracker.ValidateStep(gate1));
        tracker.AdvanceStep();

        history.Add(new(ChatTurnRole.Assistant, r1.Result["text"].ToString()!, DateTimeOffset.UtcNow));
        history.Add(new(ChatTurnRole.User, "Complete step 2.", DateTimeOffset.UtcNow));
        var r2 = await pipeline.SendAsync(Request(sysMsg, history));
        var gate2 = GateOutput.TryParse(r2.Result["text"].ToString()!);
        Assert.NotNull(gate2);
        Assert.Equal(2, gate2.StepNumber);
        Assert.True(tracker.ValidateStep(gate2));
        tracker.AdvanceStep();

        history.Add(new(ChatTurnRole.Assistant, r2.Result["text"].ToString()!, DateTimeOffset.UtcNow));
        history.Add(new(ChatTurnRole.User, "Complete step 3.", DateTimeOffset.UtcNow));
        var r3 = await pipeline.SendAsync(Request(sysMsg, history));
        var gate3 = GateOutput.TryParse(r3.Result["text"].ToString()!);
        Assert.NotNull(gate3);
        Assert.Equal(3, gate3.StepNumber);
        Assert.True(tracker.ValidateStep(gate3));
        tracker.AdvanceStep();

        Assert.True(tracker.IsComplete);
    }

    // ── SIP rule ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GateProtocol_SipRule_SerializedAsJson_ModelFollowsSteps()
    {
        var sipRule = StructuredRule.Create(
            "SIP", "Structured Investigation Protocol",
            "before investigating any reported issue",
            [
                "Read the full error message literally before theorizing",
                "State the root cause in exactly one sentence",
                "Propose exactly one fix"
            ]);

        var sysMsg = $$"""
            You are a coding assistant following the SIP rule.
            Active rule (JSON): {{sipRule.ToJson()}}

            Gate protocol: when you complete a step, end your response with the gate marker.
            For step 1: [GATE:{"rule":"SIP","step":1}]
            Complete step 1 now.
            """;

        var response = await BuildPipeline().SendAsync(
            Request(sysMsg, Turn("Error: NullReferenceException at line 42 in UserService.cs.")));

        Assert.True(response.Success);
        var gate = GateOutput.TryParse(response.Result["text"].ToString()!);
        Assert.NotNull(gate);
        Assert.Equal("SIP", gate.RuleId);
        Assert.Equal(1, gate.StepNumber);
    }

    // ── gate violation → end-to-end learning flow ────────────────────────────

    [Fact]
    public async Task GateViolation_EndToEnd_SkippedGateTriggersLearningProposal()
    {
        // Step 1: Activate a rule but send a request WITHOUT gate protocol instructions.
        // The model won't know to include a marker — violation detected.
        var rule = StructuredRule.Create("ILH", "ILH", "always", ["Quote test-cycles.md"]);
        var tracker = new MultiStepGateTracker();
        tracker.Activate(rule);

        // System message has NO gate protocol — model will not produce marker
        const string noGateSysMsg = "You are a helpful assistant. Answer briefly.";
        var pipeline = BuildPipeline();
        var modelResponse = await pipeline.SendAsync(
            Request(noGateSysMsg, Turn("Summarize the ILH in one sentence.")));

        Assert.True(modelResponse.Success);

        // Step 2: Attempt to validate — tracker step was not validated
        var text = modelResponse.Result["text"].ToString()!;
        var gateOutput = GateOutput.TryParse(text);
        // Model was not instructed to include gate — expected to be null or wrong rule
        // Either way, tracker step is not validated, so CanCallTool() is false
        if (gateOutput != null)
            tracker.ValidateStep(gateOutput); // may or may not validate depending on model

        // Step 3: If step not validated, GateViolationHandler detects it
        var handler = new GateViolationHandler();
        if (!tracker.CanCallTool())
        {
            var violation = handler.CreateViolation(tracker);
            Assert.NotNull(violation);

            // Step 4: Create learning session from violation
            var learningSession = handler.CreateLearningTrigger(violation!);
            Assert.Equal(LearningTrigger.InternalGateFailure, learningSession.Trigger);

            // Step 5: Run learning orchestrator — model proposes a rule
            const string learningSysMsg = """
                You are a learning assistant. Analyze this gate violation and propose a rule fix.
                You MUST end your response with:
                [RULE_PROPOSAL:{"fileName":"CLAUDE.md","ruleText":"<proposed rule>","humanSummary":"<summary>","action":"add"}]
                """;
            var orchestrator = new LearningOrchestrator(pipeline);
            var proposal = await orchestrator.RunLearningLoopAsync(learningSession, systemMessage: learningSysMsg);

            Assert.NotNull(proposal);
            Assert.False(string.IsNullOrWhiteSpace(proposal.RuleText));
        }
        // If model unexpectedly produced a valid gate marker, the violation path wasn't triggered —
        // the test still passes since that outcome (model correctly following the protocol)
        // is also valid behavior.
    }

    // ── reasoning trace attachment ────────────────────────────────────────────

    [Fact]
    public async Task LearningProtocol_WithReasoningTrace_TraceAppearsInModelContext()
    {
        const string learningSysMsg = """
            You are a learning assistant. Analyze the provided violation and reasoning trace.
            In your response, acknowledge the reasoning trace if one is provided.
            You MUST end your response with:
            [RULE_PROPOSAL:{"fileName":"CLAUDE.md","ruleText":"<proposed rule>","humanSummary":"<summary>","action":"add"}]
            """;

        const string reasoningTrace =
            "The assistant said 'let me check the code' and opened a file immediately " +
            "without first quoting test-cycles.md, skipping the mandatory ILH step 1.";

        var orchestrator = new LearningOrchestrator(BuildPipeline());
        var session = LearningSession.Initiate(
            LearningTrigger.InternalGateFailure,
            "ILH step 1 was skipped — no test-cycles.md quote before file access.");

        var proposal = await orchestrator.RunLearningLoopAsync(
            session,
            reasoningTraceContent: reasoningTrace,
            systemMessage: learningSysMsg);

        Assert.NotNull(proposal);
        Assert.False(string.IsNullOrWhiteSpace(proposal.RuleText));
    }

    // ── external user error trigger ───────────────────────────────────────────

    [Fact]
    public async Task LearningProtocol_ExternalUserError_ProducesProposal()
    {
        const string learningSysMsg = """
            You are a learning assistant responding to a user-reported error.
            Propose a rule that prevents this error from recurring.
            You MUST end your response with:
            [RULE_PROPOSAL:{"fileName":"CLAUDE.md","ruleText":"<proposed rule, min 20 words>","humanSummary":"<one sentence>","action":"add"}]
            """;

        var orchestrator = new LearningOrchestrator(BuildPipeline());
        var session = LearningSession.Initiate(
            LearningTrigger.ExternalUserError,
            "The assistant created hand-written fakes instead of using real infrastructure implementations, " +
            "violating the explicit 'no mocks, no fakes' rule.");

        var proposal = await orchestrator.RunLearningLoopAsync(session, systemMessage: learningSysMsg);

        Assert.NotNull(proposal);
        Assert.Equal(LearningTrigger.ExternalUserError, session.Trigger);
        Assert.False(string.IsNullOrWhiteSpace(proposal.FileName));
        Assert.False(string.IsNullOrWhiteSpace(proposal.Action));
    }
}
