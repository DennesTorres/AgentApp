using AgentApp.Application.Findings;
using AgentApp.Domain.Findings;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;
using NSubstitute;

namespace AgentApp.Application.Tests.Findings;

public class FindingsExtractionServiceTests
{
    private readonly IMdFileRepository _mdFileRepository;
    private readonly FindingsExtractionService _sut;

    public FindingsExtractionServiceTests()
    {
        _mdFileRepository = Substitute.For<IMdFileRepository>();
        _sut = new FindingsExtractionService(_mdFileRepository);

        _mdFileRepository.GetByNameAsync(Arg.Any<string>(), Arg.Any<MdFileScope>(), Arg.Any<Guid?>())
            .Returns((MdFile?)null);
    }

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
        var findings = new List<TechnicalFinding>
        {
            TechnicalFinding.Create("dotnet", "Short but valid finding text here!!")
        };
        // Content below 40 chars threshold → not meaningful
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

        await _mdFileRepository.Received(1).SaveAsync(Arg.Is<MdFile>(f =>
            f.Name == "technology-dotnet" && f.IsTechnology));
    }

    [Fact]
    public async Task WriteToTechnologyFilesAsync_DuplicateFinding_SkipsWrite()
    {
        var content = "Always use async/await over .Result to avoid deadlocks.";
        var existingFile = MdFile.CreateTechnology("technology-dotnet", content);
        _mdFileRepository.GetByNameAsync("technology-dotnet", MdFileScope.Global, null)
            .Returns(existingFile);

        var findings = new List<TechnicalFinding>
        {
            TechnicalFinding.Create("dotnet", content)
        };

        await _sut.WriteToTechnologyFilesAsync(findings);

        await _mdFileRepository.DidNotReceive().SaveAsync(Arg.Any<MdFile>());
    }

    [Fact]
    public async Task WriteToTechnologyFilesAsync_ExistingFileNonDuplicate_AppendsContent()
    {
        var existingFile = MdFile.CreateTechnology("technology-dotnet", "## Existing finding\nContent here.");
        _mdFileRepository.GetByNameAsync("technology-dotnet", MdFileScope.Global, null)
            .Returns(existingFile);

        var findings = new List<TechnicalFinding>
        {
            TechnicalFinding.Create("dotnet", "Always use async/await over .Result to avoid deadlocks.")
        };

        await _sut.WriteToTechnologyFilesAsync(findings);

        await _mdFileRepository.Received(1).SaveAsync(Arg.Is<MdFile>(f =>
            f.Content.Contains("Existing finding") && f.Content.Contains("async/await")));
    }
}
