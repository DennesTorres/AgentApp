using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Chat;

public class ResponsePreparationService : IResponsePreparationService
{
    private readonly IChatCommandParser _commandParser;

    public ResponsePreparationService(IChatCommandParser commandParser)
        => _commandParser = commandParser;

    public (string DisplayText, IReadOnlyList<ChatCommand> Commands) Prepare(string rawText)
    {
        var (cleanText, commands) = _commandParser.Parse(rawText);
        return (cleanText, commands);
    }
}
