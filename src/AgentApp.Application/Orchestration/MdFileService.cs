using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Application.Orchestration;

public class MdFileService
{
    private readonly IMdFileRepository _repository;
    private readonly ITriggersIndexRepository _triggersIndexRepository;

    public MdFileService(IMdFileRepository repository, ITriggersIndexRepository triggersIndexRepository)
    {
        _repository = repository;
        _triggersIndexRepository = triggersIndexRepository;
    }

    // US-010
    public async Task<MdFile> CreateGlobalFileAsync(string name, string content)
    {
        var file = MdFile.CreateGlobal(name, content);
        await _repository.SaveAsync(file);
        return file;
    }

    // US-011
    public async Task<MdFile> CreateProjectFileAsync(string name, string content, Guid projectId)
    {
        var file = MdFile.CreateForProject(name, content, projectId);
        await _repository.SaveAsync(file);
        return file;
    }

    // US-012
    public async Task<MdFile> CreateTechnologyFileAsync(string name, string content)
    {
        var file = MdFile.CreateTechnology(name, content);
        await _repository.SaveAsync(file);
        return file;
    }

    // US-013
    public async Task RegisterTriggersAsync(string fileName, IEnumerable<string> triggers,
        MdFileScope scope, Guid? projectId)
    {
        var index = scope == MdFileScope.Project && projectId.HasValue
            ? await _triggersIndexRepository.GetForProjectAsync(projectId.Value)
            : await _triggersIndexRepository.GetGlobalAsync();

        index.AddEntry(fileName, triggers);
        await _triggersIndexRepository.SaveAsync(index);
    }

    public async Task<IReadOnlyList<MdFile>> GetAllGlobalAsync() =>
        await _repository.GetAllAsync(MdFileScope.Global);

    public async Task<IReadOnlyList<MdFile>> GetAllForProjectAsync(Guid projectId) =>
        await _repository.GetAllAsync(MdFileScope.Project, projectId);
}
