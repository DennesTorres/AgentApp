using AgentApp.Application.Projects;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;
using NSubstitute;

namespace AgentApp.Application.Tests.Projects;

public class ProjectSwitchServiceTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectSwitchHandler _switchHandler;
    private readonly ProjectSwitchService _sut;

    public ProjectSwitchServiceTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _switchHandler = Substitute.For<IProjectSwitchHandler>();
        _sut = new ProjectSwitchService(_projectRepository, _switchHandler);
    }

    [Fact]
    public async Task SwitchToProjectAsync_ExistingProject_CallsResetOnHandler()
    {
        var project = Project.Create("MyProject", @"C:\Projects");
        _projectRepository.GetByIdAsync(project.Id).Returns(project);

        await _sut.SwitchToProjectAsync(project.Id);

        await _switchHandler.Received(1).ResetForProjectAsync(project);
    }

    [Fact]
    public async Task SwitchToProjectAsync_NonExistentProject_ThrowsNotFoundException()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Project?)null);

        await Assert.ThrowsAsync<AgentApp.Domain.Exceptions.DomainNotFoundException>(
            () => _sut.SwitchToProjectAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SwitchToNoneAsync_NoActiveProject_CallsResetOnHandlerWithNull()
    {
        await _sut.SwitchToNoneAsync();

        await _switchHandler.Received(1).ResetForStandaloneAsync();
    }
}
