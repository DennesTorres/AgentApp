using AgentApp.Application.Providers;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Providers;

namespace AgentApp.Application.Tests.Providers;

public class OrchestratorPipelineTests
{
    [Fact]
    public async Task SendAsync_DispatchesToMatchingProvider()
    {
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);
        var expectedResponse = ProviderResponse.Ok(request.RequestId);
        var provider = new FakeProvider(ProviderCapability.ModelCall, _ => expectedResponse);
        var registry = new ProviderRegistry();
        registry.Register(provider);

        var pipeline = new OrchestratorPipeline(registry);
        var response = await pipeline.SendAsync(request);

        Assert.Same(expectedResponse, response);
    }

    [Fact]
    public async Task SendAsync_NoProviderRegistered_ThrowsInvalidOperation()
    {
        var registry = new ProviderRegistry();
        var pipeline = new OrchestratorPipeline(registry);
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.SendAsync(request));
    }

    [Fact]
    public async Task SendAsync_PassesRequestToProvider()
    {
        var request = ProviderRequest.Create(ProviderCapability.EmbeddingSearch,
            new Dictionary<string, object> { ["query"] = "test" });
        var provider = new FakeProvider(ProviderCapability.EmbeddingSearch);
        var registry = new ProviderRegistry();
        registry.Register(provider);

        var pipeline = new OrchestratorPipeline(registry);
        await pipeline.SendAsync(request);

        Assert.Same(request, provider.LastRequest);
    }
}
