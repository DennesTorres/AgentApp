using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Learning;
using AgentApp.Domain.Rules;

namespace AgentApp.Application.Learning;

public class LearningProcessService
{
    private readonly ILearningSessionRepository _sessionRepository;
    private readonly ILearningLogger _logger;
    private readonly IMdFileRepository _mdFileRepository;

    public LearningProcessService(ILearningSessionRepository sessionRepository,
        ILearningLogger logger, IMdFileRepository mdFileRepository)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
        _mdFileRepository = mdFileRepository;
    }

    public async Task<LearningSession> InitiateAsync(LearningTrigger trigger,
        string violationDescription, Guid? reasoningTraceId = null)
    {
        var session = LearningSession.Initiate(trigger, violationDescription, reasoningTraceId);
        await _sessionRepository.SaveAsync(session);
        await _logger.LogAsync($"[{DateTimeOffset.UtcNow:u}] INITIATED trigger={trigger} id={session.Id} violation={violationDescription}");
        return session;
    }

    public async Task<LearningSession> ProposeChangeAsync(Guid sessionId, ProposedRuleChange change)
    {
        var session = await RequireSessionAsync(sessionId);
        session.ProposeChange(change);
        await _sessionRepository.SaveAsync(session);
        await _logger.LogAsync($"[{DateTimeOffset.UtcNow:u}] PROPOSED id={sessionId} file={change.FileName} action={change.Action} iteration={session.IterationCount}");
        return session;
    }

    public async Task<LearningSession> ApproveAsync(Guid sessionId)
    {
        var session = await RequireSessionAsync(sessionId);
        session.Approve(); // throws if no proposed change or wrong state

        var change = session.ProposedChange!;
        var existing = await _mdFileRepository.GetByNameAsync(change.FileName, MdFileScope.Global, null);
        if (existing != null)
        {
            existing.UpdateContent(existing.Content + "\n\n" + change.RuleText);
            await _mdFileRepository.SaveAsync(existing);
        }
        else
        {
            var newFile = MdFile.CreateGlobal(change.FileName, change.RuleText);
            await _mdFileRepository.SaveAsync(newFile);
        }

        await _sessionRepository.SaveAsync(session);
        await _logger.LogAsync($"[{DateTimeOffset.UtcNow:u}] APPROVED id={sessionId} file={change.FileName}");
        return session;
    }

    public async Task<LearningSession> RejectAsync(Guid sessionId, string? reason = null)
    {
        var session = await RequireSessionAsync(sessionId);
        session.Reject(reason);
        await _sessionRepository.SaveAsync(session);
        await _logger.LogAsync($"[{DateTimeOffset.UtcNow:u}] REJECTED id={sessionId} reason={reason ?? "(none)"}");
        return session;
    }

    public async Task<LearningSession> CancelAsync(Guid sessionId)
    {
        var session = await RequireSessionAsync(sessionId);
        session.Cancel();
        await _sessionRepository.SaveAsync(session);
        await _logger.LogAsync($"[{DateTimeOffset.UtcNow:u}] CANCELLED id={sessionId}");
        return session;
    }

    private async Task<LearningSession> RequireSessionAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId)
            ?? throw new DomainNotFoundException($"Learning session {sessionId} not found.");
        return session;
    }
}
