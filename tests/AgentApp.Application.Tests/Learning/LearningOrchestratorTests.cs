using AgentApp.Application.Learning;
using AgentApp.Application.Providers;
using AgentApp.Domain.Learning;
using AgentApp.Domain.Providers;
using NSubstitute;

namespace AgentApp.Application.Tests.Learning;

public class LearningOrchestratorTests
{
    private static OrchestratorPipeline BuildPipelineWithResponse(string responseText)
    {
        var provider = Substitute.For<IProvider>();
        provider.Capability.Returns(ProviderCapability.ModelCall);

        var registry = Substitute.For<IProviderRegistry>();
        registry.Resolve(ProviderCapability.ModelCall).Returns(provider);

        provider.HandleAsync(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var req = ci.Arg<ProviderRequest>();
                return ProviderResponse.Ok(req.RequestId,
                    new Dictionary<string, object> { ["text"] = responseText });
            });

        return new OrchestratorPipeline(registry);
    }

    [Fact]
    public async Task RunLearningLoopAsync_CallsPipeline_WithModelCallCapability()
    {
        var provider = Substitute.For<IProvider>();
        provider.Capability.Returns(ProviderCapability.ModelCall);
        var registry = Substitute.For<IProviderRegistry>();
        registry.Resolve(ProviderCapability.ModelCall).Returns(provider);
        provider.HandleAsync(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => ProviderResponse.Ok(ci.Arg<ProviderRequest>().RequestId,
                new Dictionary<string, object> { ["text"] = "no proposal" }));

        var pipeline = new OrchestratorPipeline(registry);
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "test violation");

        await orchestrator.RunLearningLoopAsync(session);

        await provider.Received(1).HandleAsync(
            Arg.Is<ProviderRequest>(r => r.Capability == ProviderCapability.ModelCall),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunLearningLoopAsync_WithProposalInResponse_ReturnsParsedProposal()
    {
        var responseText = "Analysis complete. " +
            "[RULE_PROPOSAL:{\"fileName\":\"CLAUDE.md\",\"ruleText\":\"Always read X first.\",\"humanSummary\":\"Read X before acting\",\"action\":\"add\"}]";

        var pipeline = BuildPipelineWithResponse(responseText);
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
        var pipeline = BuildPipelineWithResponse("I cannot propose a rule change at this time.");
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

        var pipeline = BuildPipelineWithResponse(responseText);
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "violation");

        await orchestrator.RunLearningLoopAsync(session);

        // Session should now have a proposed change
        Assert.NotNull(session.ProposedChange);
    }

    [Fact]
    public async Task RunLearningLoopAsync_IncludesViolationDescription_InRequest()
    {
        var provider = Substitute.For<IProvider>();
        provider.Capability.Returns(ProviderCapability.ModelCall);
        var registry = Substitute.For<IProviderRegistry>();
        registry.Resolve(ProviderCapability.ModelCall).Returns(provider);
        provider.HandleAsync(Arg.Any<ProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci => ProviderResponse.Ok(ci.Arg<ProviderRequest>().RequestId,
                new Dictionary<string, object> { ["text"] = "no proposal" }));

        var pipeline = new OrchestratorPipeline(registry);
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "gate step was skipped");

        await orchestrator.RunLearningLoopAsync(session);

        await provider.Received(1).HandleAsync(
            Arg.Is<ProviderRequest>(r =>
                r.Payload.ContainsKey("violation") &&
                r.Payload["violation"].ToString()!.Contains("gate step was skipped")),
            Arg.Any<CancellationToken>());
    }
}
