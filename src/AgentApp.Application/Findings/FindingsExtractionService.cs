using System.Text.RegularExpressions;
using AgentApp.Domain.Findings;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;

namespace AgentApp.Application.Findings;

public class FindingsExtractionService
{
    private readonly IMdFileRepository _mdFileRepository;
    private const int MinMeaningfulLength = 40;

    private static readonly Regex ConclusionBlockPattern = new(
        @"```implementation-conclusion\s*\n[\s\S]*?\n```",
        RegexOptions.Compiled);

    public FindingsExtractionService(IMdFileRepository mdFileRepository)
    {
        _mdFileRepository = mdFileRepository;
    }

    public bool IsMeaningful(IReadOnlyList<TechnicalFinding> findings) =>
        findings.Count > 0 && findings.Any(f => f.Content.Length >= MinMeaningfulLength);

    public bool IsImplementationConclusion(string modelResponse) =>
        ConclusionBlockPattern.IsMatch(modelResponse);

    public async Task WriteToTechnologyFilesAsync(IReadOnlyList<TechnicalFinding> findings)
    {
        foreach (var group in findings.GroupBy(f => f.Technology))
        {
            var fileName = $"technology-{group.Key}";
            var existing = await _mdFileRepository.GetByNameAsync(fileName, MdFileScope.Global, null);

            var dirty = false;
            foreach (var finding in group)
            {
                if (existing != null && existing.Content.Contains(finding.Content))
                    continue; // skip duplicates

                if (existing != null)
                {
                    existing.UpdateContent(existing.Content + "\n\n" + finding.Content);
                    dirty = true;
                }
                else
                {
                    existing = MdFile.CreateTechnology(fileName, finding.Content);
                    dirty = true;
                }
            }

            if (dirty)
                await _mdFileRepository.SaveAsync(existing!);
        }
    }
}
