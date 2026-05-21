using AgentApp.Application.Providers;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;

namespace AgentApp.Application.Chat;

public class ChatService
{
    private readonly OrchestratorPipeline _pipeline;
    private readonly IChatCommandParser _commandParser;
    private readonly List<ChatTurn> _history = [];

    public ChatService(OrchestratorPipeline pipeline, IChatCommandParser commandParser)
    {
        _pipeline = pipeline;
        _commandParser = commandParser;
    }

    public async Task<ChatServiceResult> SendAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatTurn(ChatTurnRole.User, userMessage, DateTimeOffset.UtcNow));

        var request = ProviderRequest.Create(ProviderCapability.ModelCall,
            new Dictionary<string, object> { ["history"] = _history.ToList() });

        var response = await _pipeline.SendAsync(request, cancellationToken);

        if (!response.Success)
            return new ChatServiceResult($"Error: {response.ErrorMessage}", []);

        var rawText = (string)response.Result["text"];
        var (displayText, commands) = _commandParser.Parse(rawText);

        _history.Add(new ChatTurn(ChatTurnRole.Assistant, rawText, DateTimeOffset.UtcNow));
        return new ChatServiceResult(displayText, commands);
    }

    public IReadOnlyList<ChatTurn> History => _history;

    public void ClearHistory() => _history.Clear();
}
