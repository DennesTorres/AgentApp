using AgentApp.Application.Orchestration;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;
using NSubstitute;

namespace AgentApp.Application.Tests.Orchestration;

public class ContextAssemblerTests
{
    private readonly IMdFileRepository _mdFileRepository;
    private readonly ITriggersIndexRepository _triggersIndexRepository;
    private readonly ContextAssembler _sut;

    public ContextAssemblerTests()
    {
        _mdFileRepository = Substitute.For<IMdFileRepository>();
        _triggersIndexRepository = Substitute.For<ITriggersIndexRepository>();
        _sut = new ContextAssembler(_mdFileRepository, _triggersIndexRepository);
    }

    [Fact]
    public async Task AssembleBaseContextAsync_AlwaysIncludesCoreFile()
    {
        var coreFile = MdFile.CreateGlobal("core", "# Core rules");
        _mdFileRepository.GetByNameAsync("core", MdFileScope.Global, null)
            .Returns(coreFile);
        var globalIndex = TriggersIndex.CreateGlobal();
        _triggersIndexRepository.GetGlobalAsync().Returns(globalIndex);

        var context = await _sut.AssembleBaseContextAsync(projectId: null);

        Assert.Contains(context.LoadedFiles, f => f.IsCore);
        Assert.NotNull(context.TriggersIndex);
    }

    [Fact]
    public async Task AssembleBaseContextAsync_NoCoreFile_ReturnsContextWithoutCore()
    {
        _mdFileRepository.GetByNameAsync("core", MdFileScope.Global, null)
            .Returns((MdFile?)null);
        var globalIndex = TriggersIndex.CreateGlobal();
        _triggersIndexRepository.GetGlobalAsync().Returns(globalIndex);

        var context = await _sut.AssembleBaseContextAsync(projectId: null);

        Assert.Empty(context.LoadedFiles);
    }

    [Fact]
    public async Task NeedsEnrichmentCallAsync_MessageMatchesTrigger_ReturnsTrue()
    {
        var index = TriggersIndex.CreateGlobal();
        index.AddEntry("coding-standards", ["refactor"]);
        _triggersIndexRepository.GetGlobalAsync().Returns(index);

        var result = await _sut.NeedsEnrichmentCallAsync("Please refactor this method", projectId: null);

        Assert.True(result);
    }

    [Fact]
    public async Task NeedsEnrichmentCallAsync_NoTriggerMatch_ReturnsFalse()
    {
        var index = TriggersIndex.CreateGlobal();
        _triggersIndexRepository.GetGlobalAsync().Returns(index);

        var result = await _sut.NeedsEnrichmentCallAsync("Hello world", projectId: null);

        Assert.False(result);
    }

    [Fact]
    public async Task LoadFilesForNamesAsync_ValidNames_ReturnsRequestedFiles()
    {
        var fileA = MdFile.CreateGlobal("coding-standards", "# Standards");
        _mdFileRepository.GetByNameAsync("coding-standards", MdFileScope.Global, null)
            .Returns(fileA);

        var files = await _sut.LoadFilesForNamesAsync(["coding-standards"], projectId: null);

        Assert.Single(files);
        Assert.Equal("coding-standards", files[0].Name);
    }
}
