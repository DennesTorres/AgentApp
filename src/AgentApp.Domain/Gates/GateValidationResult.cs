namespace AgentApp.Domain.Gates;

public class GateValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyDictionary<string, object?> ExtractedOutput { get; }
    public IReadOnlyList<string> MissingKeys { get; }

    private GateValidationResult(bool isValid, IReadOnlyDictionary<string, object?> extractedOutput,
        IReadOnlyList<string> missingKeys)
    {
        IsValid = isValid;
        ExtractedOutput = extractedOutput;
        MissingKeys = missingKeys;
    }

    public static GateValidationResult Pass(IReadOnlyDictionary<string, object?> extractedOutput) =>
        new(true, extractedOutput, []);

    public static GateValidationResult Fail(IReadOnlyList<string> missingKeys) =>
        new(false, new Dictionary<string, object?>(), missingKeys);
}
