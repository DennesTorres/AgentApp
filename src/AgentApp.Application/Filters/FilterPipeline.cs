using System.Text.RegularExpressions;
using AgentApp.Domain.Filters;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Application.Filters;

public class FilterPipeline
{
    private readonly IFilterRuleRepository _repository;

    private static readonly Regex GateBlockPattern = new(
        @"```gate-output\s*\n[\s\S]*?\n```",
        RegexOptions.Compiled);

    public FilterPipeline(IFilterRuleRepository repository)
    {
        _repository = repository;
    }

    public Task<string> ApplyFilter1Async(string content, string messageType, Guid? projectId) =>
        ApplyAsync(content, FilterTarget.Filter1, messageType, projectId);

    public Task<string> ApplyFilter2Async(string content, string messageType, Guid? projectId) =>
        ApplyAsync(content, FilterTarget.Filter2, messageType, projectId);

    private async Task<string> ApplyAsync(string content, FilterTarget target, string messageType, Guid? projectId)
    {
        var rule = await GetEffectiveRuleAsync(target, messageType, projectId);
        if (rule == null)
            return content;

        return rule.Transformation switch
        {
            FilterTransformation.Truncate => Truncate(content, rule.MaxLength ?? int.MaxValue),
            FilterTransformation.StripGateBlocks => GateBlockPattern.Replace(content, string.Empty).Trim(),
            _ => content
        };
    }

    private async Task<FilterRule?> GetEffectiveRuleAsync(FilterTarget target, string messageType, Guid? projectId)
    {
        if (projectId.HasValue)
        {
            var projectRules = await _repository.GetAllAsync(MdFileScope.Project, projectId.Value);
            var projectRule = projectRules.FirstOrDefault(r => r.Target == target && r.MessageType == messageType);
            if (projectRule != null)
                return projectRule;
        }

        var globalRules = await _repository.GetAllAsync(MdFileScope.Global, null);
        return globalRules.FirstOrDefault(r => r.Target == target && r.MessageType == messageType);
    }

    private static string Truncate(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;
        return content[..(maxLength - 3)] + "...";
    }
}
