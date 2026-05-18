using Microsoft.Extensions.AI;

namespace AgentApp.Infrastructure.Tests.Fakes;

internal sealed class FakeChatClient : IChatClient
{
    private Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? _handler;
    private Exception? _exception;

    public IReadOnlyList<ChatMessage>? CapturedMessages { get; private set; }

    public ChatClientMetadata Metadata => new("fake", null, null);

    public void SetResponse(Func<IEnumerable<ChatMessage>, ChatResponse> handler)
        => _handler = (msgs, _, _) => Task.FromResult(handler(msgs));

    public void SetException(Exception ex) => _exception = ex;

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        CapturedMessages = messages.ToList();
        if (_exception != null) throw _exception;
        return _handler != null
            ? _handler(messages, options, cancellationToken)
            : Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Default")]));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
