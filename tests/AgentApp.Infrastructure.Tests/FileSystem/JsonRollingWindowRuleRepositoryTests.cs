using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonRollingWindowRuleRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonRollingWindowRuleRepository _sut;

    public JsonRollingWindowRuleRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonRollingWindowRuleRepository(_testFolder);
    }

    [Fact]
    public async Task GetByFileTypeAsync_EmptyStore_ReturnsNull()
    {
        var result = await _sut.GetByFileTypeAsync("session-memory", MdFileScope.Global, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_NewRule_CanBeRetrievedByFileType()
    {
        var rule = RollingWindowRule.Create("session-memory", 50000, 30, MdFileScope.Global, null);

        await _sut.SaveAsync(rule);
        var result = await _sut.GetByFileTypeAsync("session-memory", MdFileScope.Global, null);

        Assert.NotNull(result);
        Assert.Equal(rule.Id, result.Id);
        Assert.Equal(50000, result.MaxSizeChars);
    }

    [Fact]
    public async Task SaveAsync_UpdateExistingRule_OverwritesInPlace()
    {
        var rule = RollingWindowRule.Create("impl-notes", 10000, 7, MdFileScope.Global, null);
        await _sut.SaveAsync(rule);

        var updated = RollingWindowRule.Reconstitute(
            rule.Id, "impl-notes", 20000, 14, MdFileScope.Global, null, rule.CreatedAt);
        await _sut.SaveAsync(updated);

        var result = await _sut.GetByFileTypeAsync("impl-notes", MdFileScope.Global, null);
        Assert.Equal(20000, result!.MaxSizeChars);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRule()
    {
        var rule = RollingWindowRule.Create("session-memory", 50000, 30, MdFileScope.Global, null);
        await _sut.SaveAsync(rule);

        await _sut.DeleteAsync(rule.Id);

        var result = await _sut.GetByFileTypeAsync("session-memory", MdFileScope.Global, null);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByFileTypeAsync_ProjectScopedRule_OnlyReturnsMatchingScope()
    {
        var projectId = Guid.NewGuid();
        var globalRule = RollingWindowRule.Create("session-memory", 50000, 30, MdFileScope.Global, null);
        var projectRule = RollingWindowRule.Create("session-memory", 10000, 7, MdFileScope.Project, projectId);

        await _sut.SaveAsync(globalRule);
        await _sut.SaveAsync(projectRule);

        var globalResult = await _sut.GetByFileTypeAsync("session-memory", MdFileScope.Global, null);
        var projectResult = await _sut.GetByFileTypeAsync("session-memory", MdFileScope.Project, projectId);

        Assert.Equal(globalRule.Id, globalResult!.Id);
        Assert.Equal(projectRule.Id, projectResult!.Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
