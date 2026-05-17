using AgentApp.Domain.Filters;
using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonFilterRuleRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonFilterRuleRepository _sut;

    public JsonFilterRuleRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonFilterRuleRepository(_testFolder);
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        var rules = await _sut.GetAllAsync(MdFileScope.Global, null);

        Assert.Empty(rules);
    }

    [Fact]
    public async Task SaveAsync_GlobalRule_CanBeRetrievedByScope()
    {
        var rule = FilterRule.Create("truncate-bash", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Truncate, 500, MdFileScope.Global, null);

        await _sut.SaveAsync(rule);
        var loaded = await _sut.GetAllAsync(MdFileScope.Global, null);

        Assert.Single(loaded);
        Assert.Equal("truncate-bash", loaded[0].Name);
        Assert.Equal(FilterTransformation.Truncate, loaded[0].Transformation);
        Assert.Equal(500, loaded[0].MaxLength);
    }

    [Fact]
    public async Task GetAllAsync_FiltersCorrectlyByScope()
    {
        var projectId = Guid.NewGuid();
        var globalRule = FilterRule.Create("global-rule", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Passthrough, null, MdFileScope.Global, null);
        var projectRule = FilterRule.Create("project-rule", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Truncate, 100, MdFileScope.Project, projectId);

        await _sut.SaveAsync(globalRule);
        await _sut.SaveAsync(projectRule);

        var globalRules = await _sut.GetAllAsync(MdFileScope.Global, null);
        var projectRules = await _sut.GetAllAsync(MdFileScope.Project, projectId);

        Assert.Single(globalRules);
        Assert.Equal("global-rule", globalRules[0].Name);
        Assert.Single(projectRules);
        Assert.Equal("project-rule", projectRules[0].Name);
    }

    [Fact]
    public async Task SaveAsync_ExistingRule_UpdatesInPlace()
    {
        var rule = FilterRule.Create("rule", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Passthrough, null, MdFileScope.Global, null);
        await _sut.SaveAsync(rule);

        var updated = FilterRule.Reconstitute(rule.Id, "rule", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Truncate, 100, MdFileScope.Global, null, rule.CreatedAt);
        await _sut.SaveAsync(updated);

        var all = await _sut.GetAllAsync(MdFileScope.Global, null);
        Assert.Single(all);
        Assert.Equal(FilterTransformation.Truncate, all[0].Transformation);
    }

    [Fact]
    public async Task DeleteAsync_ExistingRule_IsRemoved()
    {
        var rule = FilterRule.Create("rule", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Passthrough, null, MdFileScope.Global, null);
        await _sut.SaveAsync(rule);

        await _sut.DeleteAsync(rule.Id);

        var all = await _sut.GetAllAsync(MdFileScope.Global, null);
        Assert.Empty(all);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
