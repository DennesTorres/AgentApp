namespace AgentApp.Domain.Context;

public class ConversationContext
{
    private readonly List<ConversationMessage> _messages = [];

    public IReadOnlyList<ConversationMessage> Messages => _messages;
    public int TokenEstimate => _messages.Sum(m => m.TokenEstimate);

    public void AddMessage(ConversationMessage message) => _messages.Add(message);

    public bool IsNearLimit(int threshold) => TokenEstimate >= threshold;

    public void Reset(IEnumerable<ConversationMessage>? seed = null)
    {
        _messages.Clear();
        if (seed != null)
            _messages.AddRange(seed);
    }
}
