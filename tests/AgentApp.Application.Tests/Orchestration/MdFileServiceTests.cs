using AgentApp.Application.Orchestration;
using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Orchestration;

public class MdFileServiceTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonMdFileRepository _repository;
    private readonly JsonTriggersIndexRepository _triggersIndexRepository;
    private readonly MdFileService _sut;

    public MdFileServiceTests()
    {
        _repository = new JsonMdFileRepository(_tempFolder);
        _triggersIndexRepository = new JsonTriggersIndexRepository(_tempFolder);
        _sut = new MdFileService(_repository, _triggersIndexRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task CreateGlobalFileAsync_ValidInput_SavesAndReturns()
    {
        var file = await _sut.CreateGlobalFileAsync("coding-standards", "# Standards");

        Assert.Equal(MdFileScope.Global, file.Scope);
        Assert.False(file.IsTechnology);
        var saved = await _repository.GetByNameAsync("coding-standards", MdFileScope.Global, null);
        Assert.NotNull(saved);
        Assert.Equal("coding-standards", saved.Name);
    }

    [Fact]
    public async Task CreateProjectFileAsync_ValidInput_SavesWithProjectScope()
    {
        var projectId = Guid.NewGuid();

        var file = await _sut.CreateProjectFileAsync("api-guidelines", "# API", projectId);

        Assert.Equal(MdFileScope.Project, file.Scope);
        Assert.Equal(projectId, file.ProjectId);
        var saved = await _repository.GetByNameAsync("api-guidelines", MdFileScope.Project, projectId);
        Assert.NotNull(saved);
        Assert.Equal(projectId, saved.ProjectId);
    }

    [Fact]
    public async Task CreateTechnologyFileAsync_ValidInput_IsTechnologyFlagSet()
    {
        var file = await _sut.CreateTechnologyFileAsync("dotnet-patterns", "# .NET");

        Assert.True(file.IsTechnology);
        var saved = await _repository.GetByNameAsync("dotnet-patterns", MdFileScope.Global, null);
        Assert.NotNull(saved);
        Assert.True(saved.IsTechnology);
    }

    [Fact]
    public async Task RegisterTriggerAsync_AddsEntryToIndex()
    {
        await _sut.RegisterTriggersAsync("coding-standards", ["refactor", "review"], scope: MdFileScope.Global, projectId: null);

        var savedIndex = await _triggersIndexRepository.GetGlobalAsync();
        Assert.True(savedIndex.HasTrigger("refactor"));
    }
}
