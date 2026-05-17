using AgentApp.Domain.Findings;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonTechnicalFindingsRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonTechnicalFindingsRepository _sut;

    public JsonTechnicalFindingsRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonTechnicalFindingsRepository(_testFolder);
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        var findings = await _sut.GetAllAsync();

        Assert.Empty(findings);
    }

    [Fact]
    public async Task SaveAsync_Finding_CanBeRetrieved()
    {
        var finding = TechnicalFinding.Create("dotnet", "Always use async/await over .Result to avoid deadlocks.");

        await _sut.SaveAsync(finding);
        var all = await _sut.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("dotnet", all[0].Technology);
        Assert.Equal("Always use async/await over .Result to avoid deadlocks.", all[0].Content);
    }

    [Fact]
    public async Task SaveAsync_MultipleFindingsSameTechnology_AreAllStored()
    {
        var f1 = TechnicalFinding.Create("azure", "Use managed identity instead of connection strings.");
        var f2 = TechnicalFinding.Create("azure", "Always set retry policies on HTTP clients for resilience.");

        await _sut.SaveAsync(f1);
        await _sut.SaveAsync(f2);

        var all = await _sut.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
