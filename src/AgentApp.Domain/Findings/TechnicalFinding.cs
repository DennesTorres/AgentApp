using AgentApp.Domain.Exceptions;

namespace AgentApp.Domain.Findings;

public class TechnicalFinding
{
    public Guid Id { get; private set; }
    public string Technology { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset ExtractedAt { get; private set; }

    private TechnicalFinding() { }

    public static TechnicalFinding Create(string technology, string content)
    {
        if (string.IsNullOrWhiteSpace(technology))
            throw new DomainValidationException("Technical finding technology must not be empty.");
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainValidationException("Technical finding content must not be empty.");

        return new TechnicalFinding
        {
            Id = Guid.NewGuid(),
            Technology = technology.Trim(),
            Content = content.Trim(),
            ExtractedAt = DateTimeOffset.UtcNow
        };
    }

    public static TechnicalFinding Reconstitute(Guid id, string technology, string content,
        DateTimeOffset extractedAt) => new()
    {
        Id = id,
        Technology = technology,
        Content = content,
        ExtractedAt = extractedAt
    };
}
