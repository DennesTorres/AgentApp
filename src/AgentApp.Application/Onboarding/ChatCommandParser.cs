using System.Text.Json;
using System.Text.RegularExpressions;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Onboarding;

public class ChatCommandParser : IChatCommandParser
{
    // Commands whose JSON payload may contain nested braces (e.g. file content with code)
    // are extracted with brace-depth tracking; others use simple regex.
    private static readonly Regex SimpleCommandPattern =
        new(@"\[(?<cmd>FOLDER_SELECT|PROJECT_CONFIRM|READ_FILE|LIST_DIR|PATH_PERMISSION_REQUEST|STATE_TRANSITION):(?<json>\{[^}]*\})\]",
            RegexOptions.Compiled);

    public (string CleanText, IReadOnlyList<ChatCommand> Commands) Parse(string text)
    {
        var commands = new List<ChatCommand>();
        var cleaned = text;

        // Extract WRITE_FILE first (content may contain braces)
        cleaned = ExtractWriteFileCommands(cleaned, commands);

        // Extract remaining commands via simple pattern
        cleaned = SimpleCommandPattern.Replace(cleaned, match =>
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

    private static string ExtractWriteFileCommands(string text, List<ChatCommand> commands)
    {
        const string marker = "[WRITE_FILE:";
        var result = new System.Text.StringBuilder();
        var pos = 0;

        while (pos < text.Length)
        {
            var idx = text.IndexOf(marker, pos, StringComparison.Ordinal);
            if (idx < 0)
            {
                result.Append(text[pos..]);
                break;
            }

            result.Append(text[pos..idx]);

            var jsonStart = idx + marker.Length;
            if (jsonStart >= text.Length || text[jsonStart] != '{')
            {
                result.Append(text[idx]);
                pos = idx + 1;
                continue;
            }

            var jsonEnd = FindMatchingBrace(text, jsonStart);
            if (jsonEnd < 0 || jsonEnd + 1 >= text.Length || text[jsonEnd + 1] != ']')
            {
                result.Append(text[idx]);
                pos = idx + 1;
                continue;
            }

            var json = text[jsonStart..(jsonEnd + 1)];
            try
            {
                var payload = JsonSerializer.Deserialize<WriteFilePayload>(json);
                if (payload is not null)
                    commands.Add(new WriteFileCommand(payload.path ?? string.Empty, payload.content ?? string.Empty));
            }
            catch { /* malformed — skip */ }

            pos = jsonEnd + 2; // skip past ']'
        }

        return result.ToString();
    }

    private static int FindMatchingBrace(string text, int openPos)
    {
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = openPos; i < text.Length; i++)
        {
            var c = text[i];
            if (escape) { escape = false; continue; }
            if (c == '\\' && inString) { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    private static ChatCommand? ParseCommand(string commandName, string json) =>
        commandName switch
        {
            "FOLDER_SELECT" => JsonSerializer.Deserialize<FolderSelectPayload>(json) is { } p
                ? new FolderSelectCommand(p.reason ?? string.Empty) : null,
            "PROJECT_CONFIRM" => JsonSerializer.Deserialize<ProjectConfirmPayload>(json) is { } p
                ? new ProjectConfirmCommand(p.name ?? string.Empty, p.intent ?? string.Empty) : null,
            "READ_FILE" => JsonSerializer.Deserialize<ReadFilePayload>(json) is { } p
                ? new ReadFileCommand(p.path ?? string.Empty) : null,
            "LIST_DIR" => JsonSerializer.Deserialize<ListDirPayload>(json) is { } p
                ? new ListDirectoryCommand(p.path ?? string.Empty) : null,
            "PATH_PERMISSION_REQUEST" => JsonSerializer.Deserialize<PathPermissionPayload>(json) is { } p
                ? new PathPermissionRequestCommand(p.path ?? string.Empty, p.reason ?? string.Empty) : null,
            "STATE_TRANSITION" => JsonSerializer.Deserialize<StateTransitionPayload>(json) is { } p
                ? new StateTransitionCommand(p.mode ?? string.Empty) : null,
            _ => null
        };

    private record FolderSelectPayload(string? reason);
    private record ProjectConfirmPayload(string? name, string? intent);
    private record ReadFilePayload(string? path);
    private record WriteFilePayload(string? path, string? content);
    private record ListDirPayload(string? path);
    private record PathPermissionPayload(string? path, string? reason);
    private record StateTransitionPayload(string? mode);
}
