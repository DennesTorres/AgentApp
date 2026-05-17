using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Sessions;

namespace AgentApp.Application.Sessions;

public class SessionService
{
    private readonly ISessionRepository _repository;

    public SessionService(ISessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<ChatSession> StartStandaloneSessionAsync()
    {
        var session = ChatSession.CreateStandalone();
        await _repository.SaveAsync(session);
        return session;
    }

    public async Task LinkSessionToProjectAsync(Guid sessionId, Guid projectId)
    {
        var session = await _repository.GetByIdAsync(sessionId)
            ?? throw new DomainNotFoundException($"Session '{sessionId}' not found.");

        session.LinkToProject(projectId);
        await _repository.SaveAsync(session);
    }
}
