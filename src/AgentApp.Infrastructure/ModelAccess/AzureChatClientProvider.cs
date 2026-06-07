using AgentApp.Domain.Chat;
using AgentApp.Domain.Providers;
using Microsoft.Extensions.AI;

namespace AgentApp.Infrastructure.ModelAccess;

public class AzureChatClientProvider : IProvider
{
    private readonly IChatClient _chatClient;

    public AzureChatClientProvider(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public ProviderCapability Capability => ProviderCapability.ModelCall;

    public async Task<ProviderResponse> HandleAsync(ProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Payload.TryGetValue("history", out var historyObj) ||
            historyObj is not List<ChatTurn> history)
            return ProviderResponse.Fail(request.RequestId, "Missing or invalid 'history' payload.");

        var messages = new List<ChatMessage>();

        if (request.Payload.TryGetValue("systemMessage", out var smObj) && smObj is string systemMessage
            && !string.IsNullOrEmpty(systemMessage))
            messages.Add(new ChatMessage(ChatRole.System, systemMessage));

        messages.AddRange(history.Select(t => new ChatMessage(
            t.Role == ChatTurnRole.User ? ChatRole.User : ChatRole.Assistant,
            t.Content)));

        ChatOptions? options = null;
        if (request.Payload.TryGetValue("tools", out var toolsObj) && toolsObj is IList<AITool> tools)
            options = new ChatOptions { Tools = [.. tools] };

        try
        {
            var response = await _chatClient.GetResponseAsync(messages, options,
                cancellationToken: cancellationToken);
            var text = response.Text ?? string.Empty;
            return ProviderResponse.Ok(request.RequestId,
                new Dictionary<string, object> { ["text"] = text });
        }
        catch (Exception ex)
        {
            return ProviderResponse.Fail(request.RequestId, ex.Message);
        }
    }
}
