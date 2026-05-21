using AgentApp.Application.Findings;
using AgentApp.Domain.Findings;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Findings;

public class FindingsExtractionServiceTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly IMdFileRepository _mdFileRepository;
    private readonly FindingsExtractionService _sut;

    public FindingsExtractionServiceTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempFolder);
        _mdFileRepository = new JsonMdFileRepository(_tempFolder);
        _sut = new FindingsExtractionService(_mdFileRepository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public void IsMeaningful_NonEmptyFindingsWithSubstantialContent_ReturnsTrue()
    {
        var findings = new List<TechnicalFinding>
        {
            TechnicalFinding.Create("dotnet", "Always use async/await over .Result to avoid deadlocks.")
        };

        Assert.True(_sut.IsMeaningful(findings));
    }

    [Fact]
    public void IsMeaningful_EmptyList_ReturnsFalse()
    {
        Assert.False(_sut.IsMeaningful([]));
    }

    [Fact]
    public void IsMeaningful_AllFindingsBelowMinLength_ReturnsFalse()
    {
        var shortFindings = new List<TechnicalFinding>
        {
            TechnicalFinding.Create("dotnet", "Too short finding text here!")
        };

        Assert.False(_sut.IsMeaningful(shortFindings));
    }

    [Fact]
    public void IsImplementationConclusion_ResponseWithBlock_ReturnsTrue()
    {
        var response = "Great work!\n```implementation-conclusion\n{\"status\": \"complete\"}\n```\nDone.";

        Assert.True(_sut.IsImplementationConclusion(response));
    }

    [Fact]
    public void IsImplementationConclusion_ResponseWithoutBlock_ReturnsFalse()
    {
        var response = "The implementation is progressing well.";

        Assert.False(_sut.IsImplementationConclusion(response));
    }

    [Fact]
    public async Task WriteToTechnologyFilesAsync_NewFinding_CreatesNewTechnologyFile()
    {
        var findings = new List<TechnicalFinding>
        {
            TechnicalFinding.Create("dotnet", "Always use async/await over .Result to avoid deadlocks.")
        };

        await _sut.WriteToTechnologyFilesAsync(findings);

        var saved = await _mdFileRepository.GetByNameAsync("technology-dotnet", MdFileScope.Global, null);
        Assert.NotNull(saved);
        Assert.True(saved.IsTechnology);
    }

    [Fact]
    public async Task WriteToTechnologyFilesAsync_DuplicateFinding_SkipsWrite()
    {
        var content = "Always use async/await over .Result to avoid deadlocks.";
        var existingFile = MdFile.CreateTechnology("technology-dotnet", content);
        await _mdFileRepository.SaveAsync(existingFile);

        var findings = new List<TechnicalFinding>
        {
            TechnicalFinding.Create("dotnet", content)
        };

        await _sut.WriteToTechnologyFilesAsync(findings);

        var saved = await _mdFileRepository.GetByNameAsync("technology-dotnet", MdFileScope.Global, null);
        Assert.NotNull(saved);
        Assert.Equal(content, saved.Content);
    }

    [Fact]
    public async Task WriteToTechnologyFilesAsync_ExistingFileNonDuplicate_AppendsContent()
    {
        var existingFile = MdFile.CreateTechnology("technology-dotnet", "## Existing finding\nContent here.");
        await _mdFileRepository.SaveAsync(existingFile);

        var findings = new List<TechnicalFinding>
        {
            TechnicalFinding.Create("dotnet", "Always use async/await over .Result to avoid deadlocks.")
        };

        await _sut.WriteToTechnologyFilesAsync(findings);

        var saved = await _mdFileRepository.GetByNameAsync("technology-dotnet", MdFileScope.Global, null);
        Assert.NotNull(saved);
        Assert.Contains("Existing finding", saved.Content);
        Assert.Contains("async/await", saved.Content);
    }
}
