using AgentApp.Domain.Providers;

namespace AgentApp.Domain.Tests.Providers;

public class ProviderProtocolTests
{
    [Fact]
    public void ProviderRequest_Create_SetsCapabilityAndRequestId()
    {
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);

        Assert.Equal(ProviderCapability.ModelCall, request.Capability);
        Assert.NotEmpty(request.RequestId);
    }

    [Fact]
    public void ProviderRequest_Create_WithPayload_StoresPayload()
    {
        var payload = new Dictionary<string, object> { ["key"] = "value" };

        var request = ProviderRequest.Create(ProviderCapability.ModelCall, payload);

        Assert.Equal("value", request.Payload["key"]);
    }

    [Fact]
    public void ProviderRequest_Create_WithoutPayload_HasEmptyPayload()
    {
        var request = ProviderRequest.Create(ProviderCapability.ModelCall);

        Assert.Empty(request.Payload);
    }

    [Fact]
    public void ProviderRequest_TwoRequests_HaveDistinctIds()
    {
        var r1 = ProviderRequest.Create(ProviderCapability.ModelCall);
        var r2 = ProviderRequest.Create(ProviderCapability.ModelCall);

        Assert.NotEqual(r1.RequestId, r2.RequestId);
    }

    [Fact]
    public void ProviderResponse_Ok_IsSuccessWithResult()
    {
        var response = ProviderResponse.Ok("req-1", new Dictionary<string, object> { ["output"] = "hello" });

        Assert.True(response.Success);
        Assert.Equal("req-1", response.RequestId);
        Assert.Equal("hello", response.Result["output"]);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public void ProviderResponse_Ok_WithoutResult_HasEmptyResult()
    {
        var response = ProviderResponse.Ok("req-1");

        Assert.True(response.Success);
        Assert.Empty(response.Result);
    }

    [Fact]
    public void ProviderResponse_Fail_IsNotSuccessWithMessage()
    {
        var response = ProviderResponse.Fail("req-1", "Something went wrong");

        Assert.False(response.Success);
        Assert.Equal("req-1", response.RequestId);
        Assert.Equal("Something went wrong", response.ErrorMessage);
        Assert.Empty(response.Result);
    }
}
