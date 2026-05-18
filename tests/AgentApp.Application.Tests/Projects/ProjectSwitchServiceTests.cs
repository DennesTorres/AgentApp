using AgentApp.Application.Projects;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Projects;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Application.Tests.Projects;

public class ProjectSwitchServiceTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonProjectRepository _projectRepository;
    private readonly FakeProjectSwitchHandler _switchHandler = new();
    private readonly ProjectSwitchService _sut;

    public ProjectSwitchServiceTests()
    {
        _projectRepository = new JsonProjectRepository(_tempFolder);
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
