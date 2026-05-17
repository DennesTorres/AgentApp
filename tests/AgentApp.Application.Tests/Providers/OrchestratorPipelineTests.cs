using AgentApp.Application.Providers;
using AgentApp.Domain.Providers;
using NSubstitute;

namespace AgentApp.Application.Tests.Providers;

public class OrchestratorPipelineTests
{
    [Fact]
    public async Task SendAsync_DispatchesToMatchingProvider()
    {
        var registry = Substitute.For<IProviderRegistry>();
        var provider = Substitute.For<IProvider>();
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);
        var expectedResponse = ProviderResponse.Ok(request.RequestId);

        registry.Resolve(ProviderCapability.ModelCall).Returns(provider);
        provider.HandleAsync(request, Arg.Any<CancellationToken>()).Returns(expectedResponse);

        var pipeline = new OrchestratorPipeline(registry);
        var response = await pipeline.SendAsync(request);

        Assert.Same(expectedResponse, response);
    }

    [Fact]
    public async Task SendAsync_NoProviderRegistered_ThrowsInvalidOperation()
    {
        var registry = Substitute.For<IProviderRegistry>();
        registry.Resolve(Arg.Any<ProviderCapability>()).Returns((IProvider?)null);

        var pipeline = new OrchestratorPipeline(registry);
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.SendAsync(request));
    }

    [Fact]
    public async Task SendAsync_PassesRequestToProvider()
    {
        var registry = Substitute.For<IProviderRegistry>();
        var provider = Substitute.For<IProvider>();
        var request = ProviderRequest.Create(ProviderCapability.EmbeddingSearch,
            new Dictionary<string, object> { ["query"] = "test" });

        registry.Resolve(ProviderCapability.EmbeddingSearch).Returns(provider);
        provider.HandleAsync(request, Arg.Any<CancellationToken>())
            .Returns(ProviderResponse.Ok(request.RequestId));

        var pipeline = new OrchestratorPipeline(registry);
        await pipeline.SendAsync(request);

        await provider.Received(1).HandleAsync(request, Arg.Any<CancellationToken>());
    }
}
