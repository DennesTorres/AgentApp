using AgentApp.Application.Orchestration;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;
using NSubstitute;

namespace AgentApp.Application.Tests.Orchestration;

public class MdFileServiceTests
{
    private readonly IMdFileRepository _repository;
    private readonly ITriggersIndexRepository _triggersIndexRepository;
    private readonly MdFileService _sut;

    public MdFileServiceTests()
    {
        _repository = Substitute.For<IMdFileRepository>();
        _triggersIndexRepository = Substitute.For<ITriggersIndexRepository>();
        _sut = new MdFileService(_repository, _triggersIndexRepository);
    }

    [Fact]
    public async Task CreateGlobalFileAsync_ValidInput_SavesAndReturns()
    {
        var file = await _sut.CreateGlobalFileAsync("coding-standards", "# Standards");

        Assert.Equal(MdFileScope.Global, file.Scope);
        Assert.False(file.IsTechnology);
        await _repository.Received(1).SaveAsync(Arg.Is<MdFile>(f => f.Name == "coding-standards"));
    }

    [Fact]
    public async Task CreateProjectFileAsync_ValidInput_SavesWithProjectScope()
    {
        var projectId = Guid.NewGuid();

        var file = await _sut.CreateProjectFileAsync("api-guidelines", "# API", projectId);

        Assert.Equal(MdFileScope.Project, file.Scope);
        Assert.Equal(projectId, file.ProjectId);
        await _repository.Received(1).SaveAsync(Arg.Is<MdFile>(f => f.ProjectId == projectId));
    }

    [Fact]
    public async Task CreateTechnologyFileAsync_ValidInput_IsTechnologyFlagSet()
    {
        var file = await _sut.CreateTechnologyFileAsync("dotnet-patterns", "# .NET");

        Assert.True(file.IsTechnology);
        await _repository.Received(1).SaveAsync(Arg.Any<MdFile>());
    }

    [Fact]
    public async Task RegisterTriggerAsync_AddsEntryToIndex()
    {
        var index = TriggersIndex.CreateGlobal();
        _triggersIndexRepository.GetGlobalAsync().Returns(index);

        await _sut.RegisterTriggersAsync("coding-standards", ["refactor", "review"], scope: MdFileScope.Global, projectId: null);

        await _triggersIndexRepository.Received(1).SaveAsync(Arg.Is<TriggersIndex>(i => i.HasTrigger("refactor")));
    }
}
