using System.Text.RegularExpressions;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Onboarding;

public class ChatCommandParser : IChatCommandParser
{
    private static readonly Regex CommandPattern =
        new(@"\[(?<cmd>FOLDER_SELECT|PROJECT_CONFIRM|PATH_PERMISSION_REQUEST):(?<json>\{[^}]*\})\]",
            RegexOptions.Compiled);

    public (string CleanText, IReadOnlyList<ChatCommand> Commands) Parse(string text)
    {
        var commands = new List<ChatCommand>();

        var cleaned = CommandPattern.Replace(text, match =>
        {
            var cmd = match.Groups["cmd"].Value;
            var json = match.Groups["json"].Value;
            try
            {
                var command = ParseCommand(cmd, json);
                if (command is not null)
                    commands.Add(command);
            }
            catch { /* malformed — skip */ }
            return string.Empty;
        });

        return (cleaned, commands);
    }

    private static ChatCommand? ParseCommand(string commandName, string json) =>
        commandName switch
        {
            "FOLDER_SELECT" => System.Text.Json.JsonSerializer.Deserialize<FolderSelectPayload>(json) is { } p
                ? new FolderSelectCommand(p.reason ?? string.Empty) : null,
            "PROJECT_CONFIRM" => System.Text.Json.JsonSerializer.Deserialize<ProjectConfirmPayload>(json) is { } p
                ? new ProjectConfirmCommand(p.name ?? string.Empty, p.intent ?? string.Empty) : null,
            "PATH_PERMISSION_REQUEST" => System.Text.Json.JsonSerializer.Deserialize<PathPermissionPayload>(json) is { } p
                ? new PathPermissionRequestCommand(p.path ?? string.Empty, p.reason ?? string.Empty) : null,
            _ => null
        };

    private record FolderSelectPayload(string? reason);
    private record ProjectConfirmPayload(string? name, string? intent);
    private record PathPermissionPayload(string? path, string? reason);
}
