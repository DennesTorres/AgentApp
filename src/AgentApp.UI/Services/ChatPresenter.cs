using AgentApp.Application.Chat;
using AgentApp.Domain.Chat;

namespace AgentApp.UI.Services;

/// <summary>
/// Intermediary between ChatViewModel and ChatOrchestrator.
/// Currently a pass-through; owns message display format definition.
/// Future: markdown rendering, code block formatting, typing indicators.
/// </summary>
public class ChatPresenter
{
    private readonly ChatOrchestrator _orchestrator;

    public ChatPresenter(ChatOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<InitializeResult> InitializeAsync()
        => _orchestrator.InitializeAsync();

    public Task<ChatServiceResult> SendAsync(string userMessage, CancellationToken ct = default)
        => _orchestrator.SendAsync(userMessage, ct);

    public Task<(string ProjectName, string Message)> ConfirmProjectAsync(ProjectConfirmCommand cmd)
        => _orchestrator.ConfirmProjectAsync(cmd);

    public Task<ChatServiceResult> GrantPermissionAsync(string path)
        => _orchestrator.GrantPermissionAsync(path);

    public Task<ChatServiceResult> HandleFolderSelectedAsync(string path)
        => _orchestrator.HandleFolderSelectedAsync(path);

    public Task<ChatServiceResult> HandleFolderCancelledAsync()
        => _orchestrator.HandleFolderCancelledAsync();
}
