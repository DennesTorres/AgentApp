using AgentApp.Application.Learning;
using AgentApp.Application.Providers;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Learning;
using AgentApp.Domain.Providers;

namespace AgentApp.Application.Tests.Learning;

public class LearningOrchestratorTests
{
    private static (OrchestratorPipeline pipeline, FakeProvider provider) BuildPipelineWithResponse(string responseText)
    {
        var provider = new FakeProvider(ProviderCapability.ModelCall, r => ProviderResponse.Ok(r.RequestId,
            new Dictionary<string, object> { ["text"] = responseText }));
        var registry = new ProviderRegistry();
        registry.Register(provider);
        return (new OrchestratorPipeline(registry), provider);
    }

    [Fact]
    public async Task RunLearningLoopAsync_CallsPipeline_WithModelCallCapability()
    {
        var (pipeline, provider) = BuildPipelineWithResponse("no proposal");
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "test violation");

        await orchestrator.RunLearningLoopAsync(session);

        Assert.Equal(1, provider.CallCount);
        Assert.NotNull(provider.LastRequest);
        Assert.Equal(ProviderCapability.ModelCall, provider.LastRequest!.Capability);
    }

    [Fact]
    public async Task RunLearningLoopAsync_WithProposalInResponse_ReturnsParsedProposal()
    {
        var responseText = "Analysis complete. " +
            "[RULE_PROPOSAL:{\"fileName\":\"CLAUDE.md\",\"ruleText\":\"Always read X first.\",\"humanSummary\":\"Read X before acting\",\"action\":\"add\"}]";

        var (pipeline, _) = BuildPipelineWithResponse(responseText);
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");

        var proposal = await orchestrator.RunLearningLoopAsync(session);

        Assert.NotNull(proposal);
        Assert.Equal("CLAUDE.md", proposal!.FileName);
        Assert.Contains("Read X before acting", proposal.HumanSummary);
    }

    [Fact]
    public async Task RunLearningLoopAsync_NoProposalInResponse_ReturnsNull()
    {
        var (pipeline, _) = BuildPipelineWithResponse("I cannot propose a rule change at this time.");
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");

        var proposal = await orchestrator.RunLearningLoopAsync(session);

        Assert.Null(proposal);
    }

    [Fact]
    public async Task RunLearningLoopAsync_WithProposal_CallsSessionProposeChange()
    {
        var responseText =
            "[RULE_PROPOSAL:{\"fileName\":\"CLAUDE.md\",\"ruleText\":\"Rule text.\",\"humanSummary\":\"Summary.\",\"action\":\"add\"}]";

        var (pipeline, _) = BuildPipelineWithResponse(responseText);
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");

        await orchestrator.RunLearningLoopAsync(session);

        Assert.NotNull(session.ProposedChange);
    }

    [Fact]
    public async Task RunLearningLoopAsync_IncludesViolationDescription_InRequest()
    {
        var (pipeline, provider) = BuildPipelineWithResponse("no proposal");
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "gate step was skipped");

        await orchestrator.RunLearningLoopAsync(session);

        Assert.NotNull(provider.LastRequest);
        Assert.True(provider.LastRequest!.Payload.ContainsKey("violation"));
        Assert.Contains("gate step was skipped", provider.LastRequest.Payload["violation"].ToString()!);
    }
}
