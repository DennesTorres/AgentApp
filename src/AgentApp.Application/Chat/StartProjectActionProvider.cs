using AgentApp.Application.Projects;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Chat;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.Chat;

public class StartProjectActionProvider : IActionProvider
{
    private readonly ProjectService _projectService;
    private readonly IScaffoldService _scaffoldService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IAgentContextService _contextService;
    private readonly IFilePermissionGate _fileGate;

    public StartProjectActionProvider(
        ProjectService projectService,
        IScaffoldService scaffoldService,
        ISettingsRepository settingsRepository,
        IAgentContextService contextService,
        IFilePermissionGate fileGate)
    {
        _projectService = projectService;
        _scaffoldService = scaffoldService;
        _settingsRepository = settingsRepository;
        _contextService = contextService;
        _fileGate = fileGate;
    }

    public bool CanHandle(ChatCommand command) => command is StartProjectCommand;

    public async Task HandleAsync(ChatCommand command, CancellationToken cancellationToken = default)
    {
        var cmd = (StartProjectCommand)command;
        var settings = await _settingsRepository.GetGlobalSettingsAsync();

        string codeFolder = string.Empty;
        if (!string.IsNullOrEmpty(settings.SourceControlRoot))
        {
            codeFolder = _scaffoldService.GetCodeFolderPath(cmd.FolderName, settings.SourceControlRoot);
            await _scaffoldService.CreateScaffoldAsync(cmd.FolderName, settings.SourceControlRoot);
        }

        var project = await _projectService.CreateProjectAsync(
            cmd.Name, cmd.FolderName, codeFolder, purpose: cmd.Intent);

        var agentFolder = _scaffoldService.GetAgentFolderPath(cmd.FolderName);

        if (!string.IsNullOrEmpty(codeFolder))
            _fileGate.SetProjectRoots(agentFolder, codeFolder);

        _contextService.SetProject(project, agentFolder, codeFolder);
    }
}
