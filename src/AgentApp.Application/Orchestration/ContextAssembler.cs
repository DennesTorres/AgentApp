using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Application.Orchestration;

public class ContextAssembler
{
    private readonly IMdFileRepository _mdFileRepository;
    private readonly ITriggersIndexRepository _triggersIndexRepository;

    public ContextAssembler(IMdFileRepository mdFileRepository, ITriggersIndexRepository triggersIndexRepository)
    {
        _mdFileRepository = mdFileRepository;
        _triggersIndexRepository = triggersIndexRepository;
    }

    // US-014: Core/global MD file always loaded
    // US-015: Triggers index always loaded
    public async Task<OrchestratorContext> AssembleBaseContextAsync(Guid? projectId)
    {
        var loadedFiles = new List<MdFile>();

        var coreFile = await _mdFileRepository.GetByNameAsync("core", MdFileScope.Global, null);
        if (coreFile != null)
            loadedFiles.Add(coreFile);

        var triggersIndex = projectId.HasValue
            ? await _triggersIndexRepository.GetForProjectAsync(projectId.Value)
            : await _triggersIndexRepository.GetGlobalAsync();

        return new OrchestratorContext
        {
            LoadedFiles = loadedFiles,
            TriggersIndex = triggersIndex,
            ProjectId = projectId
        };
    }

    // US-016: Phase 1 enrichment needed when message matches any trigger keyword
    public async Task<bool> NeedsEnrichmentCallAsync(string userMessage, Guid? projectId)
    {
        var index = projectId.HasValue
            ? await _triggersIndexRepository.GetForProjectAsync(projectId.Value)
            : await _triggersIndexRepository.GetGlobalAsync();

        var words = userMessage.Split([' ', '\t', '\n', '\r', ',', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        return words.Any(word => index.HasTrigger(word));
    }

    // US-017: Load specific MD files by name (Phase 1 response)
    public async Task<IReadOnlyList<MdFile>> LoadFilesForNamesAsync(IEnumerable<string> fileNames, Guid? projectId)
    {
        var result = new List<MdFile>();
        foreach (var name in fileNames)
        {
            // Try project scope first, fall back to global
            MdFile? file = null;
            if (projectId.HasValue)
                file = await _mdFileRepository.GetByNameAsync(name, MdFileScope.Project, projectId);

            file ??= await _mdFileRepository.GetByNameAsync(name, MdFileScope.Global, null);

            if (file != null)
                result.Add(file);
        }
        return result;
    }
}
