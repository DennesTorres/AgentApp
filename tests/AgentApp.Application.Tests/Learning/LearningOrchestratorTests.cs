using AgentApp.Application.Learning;
using AgentApp.Application.Providers;
using AgentApp.Domain.Learning;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;

namespace AgentApp.Application.Tests.Learning;

public class LearningOrchestratorTests
{
    private static OrchestratorPipeline BuildPipeline()
    {
        var chatClient = AzureClientFactory.BuildFromCredentials(new WindowsCredentialManager())!;
        var provider = new AzureChatClientProvider(chatClient);
        var registry = new ProviderRegistry();
        registry.Register(provider);
        return new OrchestratorPipeline(registry);
    }

    [Fact]
    public async Task RunLearningLoopAsync_CompletesWithoutException()
    {
        var pipeline = BuildPipeline();
        var orchestrator = new LearningOrchestrator(pipeline);
        var session = LearningSession.Initiate(LearningTrigger.InternalGateFailure, "test violation");

        await orchestrator.RunLearningLoopAsync(session);
    }
}
