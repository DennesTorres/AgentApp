using AgentApp.Domain.Gates;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonGateRuleRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonGateRuleRepository _sut;

    public JsonGateRuleRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonGateRuleRepository(_testFolder);
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmptyList()
    {
        var rules = await _sut.GetAllAsync();

        Assert.Empty(rules);
    }

    [Fact]
    public async Task SaveAsync_NewRule_CanBeRetrievedByName()
    {
        var rule = GateRule.Create("phase1-check", "Return JSON", ["action"]);

        await _sut.SaveAsync(rule);
        var loaded = await _sut.GetByNameAsync("phase1-check");

        Assert.NotNull(loaded);
        Assert.Equal("phase1-check", loaded.Name);
        Assert.Equal("Return JSON", loaded.RuleText);
        Assert.Contains("action", loaded.RequiredOutputKeys);
    }

    [Fact]
    public async Task SaveAsync_ExistingRule_UpdatesInPlace()
    {
        var rule = GateRule.Create("phase1-check", "Return JSON", ["action"]);
        await _sut.SaveAsync(rule);

        var updated = GateRule.Reconstitute(rule.Id, "phase1-check", "Updated text", ["action", "files"], rule.CreatedAt);
        await _sut.SaveAsync(updated);

        var all = await _sut.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Updated text", all[0].RuleText);
    }

    [Fact]
    public async Task DeleteAsync_ExistingRule_IsRemoved()
    {
        var rule = GateRule.Create("phase1-check", "Return JSON", ["action"]);
        await _sut.SaveAsync(rule);

        await _sut.DeleteAsync(rule.Id);

        var loaded = await _sut.GetByNameAsync("phase1-check");
        Assert.Null(loaded);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
