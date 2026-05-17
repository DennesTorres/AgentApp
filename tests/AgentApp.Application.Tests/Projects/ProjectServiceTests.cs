using AgentApp.Application.Projects;
using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;
using AgentApp.Domain.Settings;
using NSubstitute;

namespace AgentApp.Application.Tests.Projects;

public class ProjectServiceTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _settingsRepository = Substitute.For<ISettingsRepository>();
        _sut = new ProjectService(_projectRepository, _settingsRepository);
    }

    [Fact]
    public async Task CreateProjectAsync_ValidInput_SavesAndReturnsProject()
    {
        _settingsRepository.GetGlobalSettingsAsync().Returns(new GlobalSettings
        {
            RootProjectFolderPath = @"C:\Projects"
        });

        var project = await _sut.CreateProjectAsync("MyProject", @"C:\Projects\MyProject");

        Assert.NotNull(project);
        Assert.Equal("MyProject", project.Name);
        await _projectRepository.Received(1).SaveAsync(Arg.Is<Project>(p => p.Name == "MyProject"));
    }

    [Fact]
    public async Task CreateProjectAsync_EmptyName_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<DomainValidationException>(
            () => _sut.CreateProjectAsync("", @"C:\Projects\MyProject"));
    }

    [Fact]
    public async Task GetAllProjectsAsync_ReturnsAllProjects()
    {
        var projects = new List<Project>
        {
            Project.Create("A", @"C:\Projects\A"),
            Project.Create("B", @"C:\Projects\B")
        };
        _projectRepository.GetAllAsync().Returns(projects);

        var result = await _sut.GetAllProjectsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetProjectByIdAsync_ExistingProject_ReturnsProject()
    {
        var project = Project.Create("MyProject", @"C:\Projects\MyProject");
        _projectRepository.GetByIdAsync(project.Id).Returns(project);

        var result = await _sut.GetProjectByIdAsync(project.Id);

        Assert.Equal(project.Id, result.Id);
    }

    [Fact]
    public async Task GetProjectByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _projectRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Project?)null);

        await Assert.ThrowsAsync<DomainNotFoundException>(
            () => _sut.GetProjectByIdAsync(Guid.NewGuid()));
    }
}
