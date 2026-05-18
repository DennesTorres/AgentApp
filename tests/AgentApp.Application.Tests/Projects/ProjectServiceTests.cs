using AgentApp.Application.Projects;
using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Projects;
using AgentApp.Domain.Settings;
using AgentApp.Infrastructure.FileSystem;
using AgentApp.Infrastructure.Persistence;

namespace AgentApp.Application.Tests.Projects;

public class ProjectServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly IProjectRepository _projectRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
        _projectRepository = new JsonProjectRepository(_tempFolder);
        _settingsRepository = new JsonSettingsRepository(_tempFolder);
        _sut = new ProjectService(_projectRepository, _settingsRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task CreateProjectAsync_ValidInput_SavesAndReturnsProject()
    {
        await _settingsRepository.SaveGlobalSettingsAsync(new GlobalSettings
        {
            RootProjectFolderPath = @"C:\Projects"
        });

        var project = await _sut.CreateProjectAsync("MyProject", @"C:\Projects\MyProject");

        Assert.NotNull(project);
        Assert.Equal("MyProject", project.Name);
        var saved = await _projectRepository.GetByIdAsync(project.Id);
        Assert.NotNull(saved);
        Assert.Equal("MyProject", saved.Name);
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
        await _projectRepository.SaveAsync(Project.Create("A", @"C:\Projects\A"));
        await _projectRepository.SaveAsync(Project.Create("B", @"C:\Projects\B"));

        var result = await _sut.GetAllProjectsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetProjectByIdAsync_ExistingProject_ReturnsProject()
    {
        var project = Project.Create("MyProject", @"C:\Projects\MyProject");
        await _projectRepository.SaveAsync(project);

        var result = await _sut.GetProjectByIdAsync(project.Id);

        Assert.Equal(project.Id, result.Id);
    }

    [Fact]
    public async Task GetProjectByIdAsync_NotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<DomainNotFoundException>(
            () => _sut.GetProjectByIdAsync(Guid.NewGuid()));
    }
}
