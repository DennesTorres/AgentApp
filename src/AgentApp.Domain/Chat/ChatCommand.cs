namespace AgentApp.Domain.Chat;

public abstract record ChatCommand;

public record FolderSelectCommand(string Reason) : ChatCommand;

public record ProjectConfirmCommand(string ProjectName, string ProjectIntent) : ChatCommand;

public record PathPermissionRequestCommand(string Path, string Reason) : ChatCommand;

public record StateTransitionCommand(string Mode) : ChatCommand;

public record StartProjectCommand(string Name, string FolderName, string Intent) : ChatCommand;
