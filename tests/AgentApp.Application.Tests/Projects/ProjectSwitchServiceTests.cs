using AgentApp.Application.Projects;
using AgentApp.Domain.Projects;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.ProjectSwitch;

namespace AgentApp.Application.Tests.Projects;

public class ProjectSwitchServiceTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonProjectRepository _projectRepository;
    private readonly NullProjectSwitchHandler _switchHandler = new();
    private readonly ProjectSwitchService _sut;

    public ProjectSwitchServiceTests()
    {
        _projectRepository = new JsonProjectRepository(_tempFolder);
        _sut = new ProjectSwitchService(_projectRepository, _switchHandler);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task SwitchToProjectAsync_ExistingProject_CompletesWithoutException()
    {
        var project = Project.Create("MyProject", @"C:\Projects");
        await _projectRepository.SaveAsync(project);

        await _sut.SwitchToProjectAsync(project.Id);
    }

    [Fact]
    public async Task SwitchToProjectAsync_NonExistentProject_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<AgentApp.Domain.Exceptions.DomainNotFoundException>(
            () => _sut.SwitchToProjectAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SwitchToNoneAsync_CompletesWithoutException()
    {
        await _sut.SwitchToNoneAsync();
    }
}
