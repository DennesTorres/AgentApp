using AgentApp.Domain.Interfaces;
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;

namespace AgentApp.Infrastructure.ModelAccess;

public static class AzureClientFactory
{
    private const string CredentialName = "AgentApp:AzureModelKey";
    private const string DefaultEndpoint = "https://msdnfoundry.services.ai.azure.com/models";
    private const string DefaultModelId = "Kimi-K2.5";

    public static IChatClient? BuildFromCredentials(
        ICredentialService credentialService,
        string? endpoint = null,
        string? modelId = null)
    {
        var apiKey = credentialService.ReadCredential(CredentialName);
        if (string.IsNullOrEmpty(apiKey))
            return null;

        var url = string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint;
        var model = string.IsNullOrWhiteSpace(modelId) ? DefaultModelId : modelId;

        return new ChatCompletionsClient(
                new Uri(url),
                new AzureKeyCredential(apiKey))
            .AsIChatClient(model)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}
