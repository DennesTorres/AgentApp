using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Reasoning;

namespace AgentApp.Application.Reasoning;

public class ReasoningTraceService
{
    private readonly IReasoningTraceRepository _repository;

    public ReasoningTraceService(IReasoningTraceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CaptureAsync(Guid sessionId, Guid messageId, string traceContent)
    {
        var trace = ReasoningTrace.Capture(sessionId, messageId, traceContent);
        await _repository.SaveAsync(trace);
        return trace.Id;
    }

    public async Task<ReasoningTrace?> GetByReferenceIdAsync(Guid referenceId) =>
        await _repository.GetByIdAsync(referenceId);
}
