using AgentApp.Application.Chat;
using AgentApp.Domain.Chat;

namespace AgentApp.UI.Services;

/// <summary>
/// Intermediary between ChatViewModel and ChatOrchestrator.
/// Translates ChatServiceResult into typed PresenterResult for the ViewModel.
/// </summary>
public class ChatPresenter
{
    private readonly ChatOrchestrator _orchestrator;

    public ChatPresenter(ChatOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<InitializeResult> InitializeAsync()
        => _orchestrator.InitializeAsync();

    public async Task<PresenterResult> SendAsync(string userMessage, CancellationToken ct = default)
    {
        var result = await _orchestrator.SendAsync(userMessage, ct);
        return ToPresenterResult(result);
    }

    public Task<ChatServiceResult> GrantPermissionAsync(string path)
        => _orchestrator.GrantPermissionAsync(path);

    // US-189: persist permission across sessions
    public async Task<PresenterResult> GrantPermissionAlwaysAsync(string path)
    {
        var result = await _orchestrator.GrantPermissionAlwaysAsync(path);
        return ToPresenterResult(result);
    }

    // US-190: session bypass mode
    public void SetBypassMode(bool bypass) => _orchestrator.SetBypassMode(bypass);
    public bool IsBypassMode => _orchestrator.IsBypassMode;

    public async Task<PresenterResult> HandleFolderSelectedAsync(string path)
    {
        var result = await _orchestrator.HandleFolderSelectedAsync(path);
        return ToPresenterResult(result);
    }

    public async Task<PresenterResult> HandleFolderCancelledAsync()
    {
        var result = await _orchestrator.HandleFolderCancelledAsync();
        return ToPresenterResult(result);
    }

    private static PresenterResult ToPresenterResult(ChatServiceResult result) =>
        new(result.DisplayText,
            result.Commands.OfType<FolderSelectCommand>().FirstOrDefault(),
            result.Commands.OfType<PathPermissionRequestCommand>().FirstOrDefault());
}
