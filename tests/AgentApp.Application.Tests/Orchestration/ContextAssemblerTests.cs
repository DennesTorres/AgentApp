using AgentApp.Application.Orchestration;
using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Orchestration;

public class ContextAssemblerTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonMdFileRepository _mdFileRepository;
    private readonly JsonTriggersIndexRepository _triggersIndexRepository;
    private readonly ContextAssembler _sut;

    public ContextAssemblerTests()
    {
        _mdFileRepository = new JsonMdFileRepository(_tempFolder);
        _triggersIndexRepository = new JsonTriggersIndexRepository(_tempFolder);
        _sut = new ContextAssembler(_mdFileRepository, _triggersIndexRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task AssembleBaseContextAsync_AlwaysIncludesCoreFile()
    {
        var coreFile = MdFile.CreateGlobal("core", "# Core rules");
        await _mdFileRepository.SaveAsync(coreFile);

        var context = await _sut.AssembleBaseContextAsync(projectId: null);

        Assert.Contains(context.LoadedFiles, f => f.IsCore);
        Assert.NotNull(context.TriggersIndex);
    }

    [Fact]
    public async Task AssembleBaseContextAsync_NoCoreFile_ReturnsContextWithoutCore()
    {
        var context = await _sut.AssembleBaseContextAsync(projectId: null);

        Assert.Empty(context.LoadedFiles);
    }

    [Fact]
    public async Task NeedsEnrichmentCallAsync_MessageMatchesTrigger_ReturnsTrue()
    {
        var index = await _triggersIndexRepository.GetGlobalAsync();
        index.AddEntry("coding-standards", ["refactor"]);
        await _triggersIndexRepository.SaveAsync(index);

        var result = await _sut.NeedsEnrichmentCallAsync("Please refactor this method", projectId: null);

        Assert.True(result);
    }

    [Fact]
    public async Task NeedsEnrichmentCallAsync_NoTriggerMatch_ReturnsFalse()
    {
        var result = await _sut.NeedsEnrichmentCallAsync("Hello world", projectId: null);

        Assert.False(result);
    }

    [Fact]
    public async Task LoadFilesForNamesAsync_ValidNames_ReturnsRequestedFiles()
    {
        var fileA = MdFile.CreateGlobal("coding-standards", "# Standards");
        await _mdFileRepository.SaveAsync(fileA);

        var files = await _sut.LoadFilesForNamesAsync(["coding-standards"], projectId: null);

        Assert.Single(files);
        Assert.Equal("coding-standards", files[0].Name);
    }
}
