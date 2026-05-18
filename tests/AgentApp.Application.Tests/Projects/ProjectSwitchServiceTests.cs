using AgentApp.Application.Projects;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Application.Tests.Projects;

public class ProjectSwitchServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly IProjectRepository _projectRepository;
    private readonly FakeProjectSwitchHandler _switchHandler;
    private readonly ProjectSwitchService _sut;

    public ProjectSwitchServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
        _projectRepository = new JsonProjectRepository(_tempFolder);
        _switchHandler = new FakeProjectSwitchHandler();
        _sut = new ProjectSwitchService(_projectRepository, _switchHandler);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task SwitchToProjectAsync_ExistingProject_CallsResetOnHandler()
    {
        var project = Project.Create("MyProject", @"C:\Projects");
        await _projectRepository.SaveAsync(project);

        await _sut.SwitchToProjectAsync(project.Id);

        Assert.NotNull(_switchHandler.LastSwitchedTo);
        Assert.Equal(project.Id, _switchHandler.LastSwitchedTo.Id);
    }

    [Fact]
    public async Task SwitchToProjectAsync_NonExistentProject_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<AgentApp.Domain.Exceptions.DomainNotFoundException>(
            () => _sut.SwitchToProjectAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SwitchToNoneAsync_NoActiveProject_CallsResetOnHandlerWithNull()
    {
        await _sut.SwitchToNoneAsync();

        Assert.True(_switchHandler.StandaloneResetCalled);
    }
}
