using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Interfaces;

public interface IMdFileRepository
{
    Task<MdFile?> GetByNameAsync(string name, MdFileScope scope, Guid? projectId = null);
    Task<IReadOnlyList<MdFile>> GetAllAsync(MdFileScope scope, Guid? projectId = null);
    Task SaveAsync(MdFile file);
    Task DeleteAsync(Guid id);
}
