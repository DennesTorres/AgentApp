namespace AgentApp.Domain.Chat;

public abstract record ChatCommand;

public record FolderSelectCommand(string Reason) : ChatCommand;

public record ProjectConfirmCommand(string ProjectName, string ProjectIntent) : ChatCommand;

public record ReadFileCommand(string Path) : ChatCommand;

public record WriteFileCommand(string Path, string Content) : ChatCommand;

public record ListDirectoryCommand(string Path) : ChatCommand;

public record PathPermissionRequestCommand(string Path, string Reason) : ChatCommand;

public record StateTransitionCommand(string Mode) : ChatCommand;
