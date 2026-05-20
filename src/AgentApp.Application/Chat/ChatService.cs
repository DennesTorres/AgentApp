using AgentApp.Application.Providers;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;

namespace AgentApp.Application.Chat;

public class ChatService
{
    private readonly OrchestratorPipeline _pipeline;
    private readonly IChatCommandParser _commandParser;
    private readonly ISystemMessageProvider[] _systemMessageProviders;
    private readonly IAgentContextService _contextService;
    private readonly List<ChatTurn> _history = [];

    public ChatService(OrchestratorPipeline pipeline, IChatCommandParser commandParser,
        ISystemMessageProvider[] systemMessageProviders, IAgentContextService contextService)
    {
        _pipeline = pipeline;
        _commandParser = commandParser;
        _systemMessageProviders = systemMessageProviders;
        _contextService = contextService;
    }

    public async Task<ChatServiceResult> SendAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        // Step 1: assemble system message from applicable providers
        var context = _contextService.GetCurrent();
        var systemMessage = string.Join("\n\n", _systemMessageProviders
            .Where(p => p.IsApplicable(context))
            .Select(p => p.GetSection(context)));

        // Step 2: model call with system message + history
        _history.Add(new ChatTurn(ChatTurnRole.User, userMessage, DateTimeOffset.UtcNow));

        var payload = new Dictionary<string, object> { ["history"] = _history.ToList() };
        if (!string.IsNullOrEmpty(systemMessage))
            payload["systemMessage"] = systemMessage;

        var request = ProviderRequest.Create(ProviderCapability.ModelCall, payload);
        var response = await _pipeline.SendAsync(request, cancellationToken);

        if (!response.Success)
            return new ChatServiceResult($"Error: {response.ErrorMessage}", []);

        // Step 3: parse response commands
        var rawText = (string)response.Result["text"];
        var (displayText, commands) = _commandParser.Parse(rawText);

        // Step 4: detect STATE_TRANSITION — update AgentContext, filter from ViewModel result
        var stateTransition = commands.OfType<StateTransitionCommand>().FirstOrDefault();
        if (stateTransition is not null)
            _contextService.UpdateConversationState(ConversationState.FromMode(stateTransition.Mode));

        var visibleCommands = commands.Where(c => c is not StateTransitionCommand).ToArray();

        // Step 5: return display text + remaining commands to ViewModel
        _history.Add(new ChatTurn(ChatTurnRole.Assistant, rawText, DateTimeOffset.UtcNow));
        return new ChatServiceResult(displayText, visibleCommands);
    }

    public IReadOnlyList<ChatTurn> History => _history;

    public void ClearHistory() => _history.Clear();
}
