using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Projects;

public class BoardService
{
    private readonly IKnowledgeRecordRepository _repository;

    public BoardService(IKnowledgeRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CreateRecordAsync(
        Guid projectId, string title, string description, KnowledgeRecordType recordType, Guid? parentId)
    {
        var record = KnowledgeRecord.Create(projectId, title, description, recordType, parentId);
        await _repository.SaveAsync(record);
        return record.Id;
    }

    public async Task TransitionStatusAsync(Guid recordId, KnowledgeRecordStatus newStatus)
    {
        var record = await _repository.GetByIdAsync(recordId)
            ?? throw new InvalidOperationException($"Knowledge record {recordId} not found.");

        record.TransitionTo(newStatus);
        await _repository.SaveAsync(record);
    }

    public async Task<IReadOnlyList<KnowledgeRecord>> GetBoardAsync(Guid projectId) =>
        await _repository.GetByProjectIdAsync(projectId);
}
