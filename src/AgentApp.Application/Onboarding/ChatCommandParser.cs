using System.Text.Json;
using System.Text.RegularExpressions;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Onboarding;

public class ChatCommandParser : IChatCommandParser
{
    private static readonly Regex FolderSelectPattern =
        new(@"\[FOLDER_SELECT:(\{[^}]*\})\]", RegexOptions.Compiled);

    private static readonly Regex ProjectConfirmPattern =
        new(@"\[PROJECT_CONFIRM:(\{[^}]*\})\]", RegexOptions.Compiled);

    public (string CleanText, IReadOnlyList<ChatCommand> Commands) Parse(string text)
    {
        var commands = new List<ChatCommand>();
        var cleaned = text;

        cleaned = FolderSelectPattern.Replace(cleaned, match =>
        {
            try
            {
                var json = JsonSerializer.Deserialize<FolderSelectPayload>(match.Groups[1].Value);
                if (json is not null)
                    commands.Add(new FolderSelectCommand(json.reason ?? string.Empty));
            }
            catch { /* malformed — skip */ }
            return string.Empty;
        });

        cleaned = ProjectConfirmPattern.Replace(cleaned, match =>
        {
            try
            {
                var json = JsonSerializer.Deserialize<ProjectConfirmPayload>(match.Groups[1].Value);
                if (json is not null)
                    commands.Add(new ProjectConfirmCommand(json.name ?? string.Empty, json.intent ?? string.Empty));
            }
            catch { /* malformed — skip */ }
            return string.Empty;
        });

        return (cleaned, commands);
    }

    private record FolderSelectPayload(string? reason);
    private record ProjectConfirmPayload(string? name, string? intent);
}
