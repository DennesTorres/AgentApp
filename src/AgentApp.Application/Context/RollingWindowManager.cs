using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Application.Context;

public class RollingWindowManager
{
    private readonly IRollingWindowStore _store;
    private readonly IRollingWindowRuleRepository _ruleRepository;

    public RollingWindowManager(IRollingWindowStore store, IRollingWindowRuleRepository ruleRepository)
    {
        _store = store;
        _ruleRepository = ruleRepository;
    }

    public async Task AppendAsync(string fileType, string content, MdFileScope scope, Guid? projectId)
    {
        var existing = await _store.ReadActiveAsync(fileType);
        var updated = string.IsNullOrEmpty(existing) ? content : existing + "\n\n" + content;
        await _store.WriteActiveAsync(fileType, updated);
        await ArchiveIfNeededAsync(fileType, scope, projectId);
    }

    public async Task ArchiveIfNeededAsync(string fileType, MdFileScope scope, Guid? projectId)
    {
        var rule = await _ruleRepository.GetByFileTypeAsync(fileType, scope, projectId);
        if (rule == null) return;

        var content = await _store.ReadActiveAsync(fileType);
        if (content.Length < rule.MaxSizeChars) return;

        await _store.ArchiveAsync(fileType, content);
        await _store.WriteActiveAsync(fileType, string.Empty);
    }

    public async Task<IReadOnlyList<string>> SearchArchivesAsync(string query, string fileType, Guid? projectId) =>
        await _store.SearchArchivesAsync(query, fileType);
}
