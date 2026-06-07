using AgentApp.Domain.Chat;

namespace AgentApp.UI.Services;

public record PresenterResult(
    string DisplayText,
    FolderSelectCommand? PendingFolderSelect,
    PathPermissionRequestCommand? PendingPermissionRequest);
