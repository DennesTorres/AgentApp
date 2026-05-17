using AgentApp.Domain.Context;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Context;

public class ContextWindowManager
{
    private readonly IConversationHistoryRepository _historyRepository;
    private readonly ISettingsRepository _settingsRepository;

    public ContextWindowManager(IConversationHistoryRepository historyRepository,
        ISettingsRepository settingsRepository)
    {
        _historyRepository = historyRepository;
        _settingsRepository = settingsRepository;
    }

    public async Task<bool> NeedsResetAsync(ConversationContext context)
    {
        var settings = await _settingsRepository.GetGlobalSettingsAsync();
        return context.IsNearLimit(settings.TokenThresholdForContextReset);
    }

    public async Task ArchiveAndResetAsync(ConversationContext context, Guid sessionId, string summaryContent)
    {
        // Archive the current history (US-062)
        await _historyRepository.ArchiveAsync(sessionId, context.Messages.ToList());

        // Reset with summary as seed — orchestrator will reinject MD files (US-065)
        var seed = ConversationMessage.Create(MessageRole.System, summaryContent);
        context.Reset([seed]);

        // Persist the new (reset) context
        await _historyRepository.SaveAsync(sessionId, context.Messages.ToList());
    }
}
