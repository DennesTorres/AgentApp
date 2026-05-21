using AgentApp.Domain.Interfaces;
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;

namespace AgentApp.Infrastructure.ModelAccess;

public static class AzureClientFactory
{
    private const string Endpoint = "https://msdnfoundry.services.ai.azure.com/models";
    private const string ModelId = "Kimi-K2.5";
    private const string CredentialName = "AgentApp:AzureModelKey";

    public static IChatClient? BuildFromCredentials(ICredentialService credentialService)
    {
        var apiKey = credentialService.ReadCredential(CredentialName);
        if (string.IsNullOrEmpty(apiKey))
            return null;

        return new ChatCompletionsClient(
                new Uri(Endpoint),
                new AzureKeyCredential(apiKey))
            .AsIChatClient(ModelId)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}
