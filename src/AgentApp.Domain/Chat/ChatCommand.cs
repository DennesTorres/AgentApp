namespace AgentApp.Domain.Chat;

public abstract record ChatCommand;

public record FolderSelectCommand(string Reason) : ChatCommand;

public record ProjectConfirmCommand(string ProjectName, string ProjectIntent) : ChatCommand;

public record PathPermissionRequestCommand(string Path, string Reason) : ChatCommand;
