using AgentApp.Application.Providers;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Providers;

namespace AgentApp.Application.Chat;

public class ChatService
{
    private readonly OrchestratorPipeline _pipeline;
    private readonly List<ChatTurn> _history = [];

    public ChatService(OrchestratorPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task<string> SendAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatTurn(ChatTurnRole.User, userMessage, DateTimeOffset.UtcNow));

        var request = ProviderRequest.Create(ProviderCapability.ModelCall,
            new Dictionary<string, object> { ["history"] = _history.ToList() });

        var response = await _pipeline.SendAsync(request, cancellationToken);

        if (!response.Success)
            return $"Error: {response.ErrorMessage}";

        var text = (string)response.Result["text"];
        _history.Add(new ChatTurn(ChatTurnRole.Assistant, text, DateTimeOffset.UtcNow));
        return text;
    }

    public IReadOnlyList<ChatTurn> History => _history;

    public void ClearHistory() => _history.Clear();
}
