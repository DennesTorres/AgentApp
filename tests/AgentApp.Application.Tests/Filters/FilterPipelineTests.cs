using AgentApp.Application.Filters;
using AgentApp.Domain.Filters;
using AgentApp.Domain.Rules;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Application.Tests.Filters;

public class FilterPipelineTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonFilterRuleRepository _repository;
    private readonly FilterPipeline _sut;

    public FilterPipelineTests()
    {
        _repository = new JsonFilterRuleRepository(_tempFolder);
        _sut = new FilterPipeline(_repository);
    }

    public void Dispose() => Directory.Delete(_tempFolder, recursive: true);

    [Fact]
    public async Task ApplyFilter1Async_NoMatchingRule_ReturnsUnchanged()
    {
        var result = await _sut.ApplyFilter1Async("tool output", "bash-output", null);

        Assert.Equal("tool output", result);
    }

    [Fact]
    public async Task ApplyFilter1Async_TruncateRule_TruncatesLongContent()
    {
        var rule = FilterRule.Create("trunc", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Truncate, maxLength: 10, MdFileScope.Global, null);
        await _repository.SaveAsync(rule);

        var result = await _sut.ApplyFilter1Async("Hello World Extra", "bash-output", null);

        Assert.Equal(10, result.Length);
        Assert.EndsWith("...", result);
    }

    [Fact]
    public async Task ApplyFilter1Async_TruncateRule_ShortContentUnchanged()
    {
        var rule = FilterRule.Create("trunc", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Truncate, maxLength: 100, MdFileScope.Global, null);
        await _repository.SaveAsync(rule);

        var result = await _sut.ApplyFilter1Async("short", "bash-output", null);

        Assert.Equal("short", result);
    }

    [Fact]
    public async Task ApplyFilter2Async_StripGateBlocksRule_RemovesGateOutput()
    {
        var rule = FilterRule.Create("strip-gate", FilterTarget.Filter2, "model-response",
            FilterTransformation.StripGateBlocks, null, MdFileScope.Global, null);
        await _repository.SaveAsync(rule);

        var response = @"User text
```gate-output
{""action"": ""load""}
```
More text";
        var result = await _sut.ApplyFilter2Async(response, "model-response", null);

        Assert.DoesNotContain("gate-output", result);
        Assert.Contains("User text", result);
        Assert.Contains("More text", result);
    }

    [Fact]
    public async Task ApplyFilter1Async_ProjectRuleOverridesGlobal()
    {
        var projectId = Guid.NewGuid();
        var globalRule = FilterRule.Create("global-trunc", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Truncate, maxLength: 10, MdFileScope.Global, null);
        var projectRule = FilterRule.Create("project-trunc", FilterTarget.Filter1, "bash-output",
            FilterTransformation.Truncate, maxLength: 50, MdFileScope.Project, projectId);

        await _repository.SaveAsync(globalRule);
        await _repository.SaveAsync(projectRule);

        var result = await _sut.ApplyFilter1Async("Hello World Extra Content", "bash-output", projectId);

        Assert.True(result.Length > 10);
    }

    [Fact]
    public async Task ApplyFilter2Async_Filter1RuleIgnoredForFilter2()
    {
        var rule = FilterRule.Create("filter1-rule", FilterTarget.Filter1, "model-response",
            FilterTransformation.Truncate, maxLength: 5, MdFileScope.Global, null);
        await _repository.SaveAsync(rule);

        var content = "This is a long model response";
        var result = await _sut.ApplyFilter2Async(content, "model-response", null);

        Assert.Equal(content, result);
    }
}