using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Sessions;

namespace AgentApp.Application.Sessions;

public class SessionService
{
    private readonly ISessionRepository _repository;

    public event EventHandler<ChatSession>? SessionCreated;

    public SessionService(ISessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<ChatSession> StartStandaloneSessionAsync()
    {
        var session = ChatSession.CreateStandalone();
        await _repository.SaveAsync(session);
        SessionCreated?.Invoke(this, session);
        return session;
    }

    public async Task LinkSessionToProjectAsync(Guid sessionId, Guid projectId)
    {
        var session = await _repository.GetByIdAsync(sessionId)
            ?? throw new DomainNotFoundException($"Session '{sessionId}' not found.");
        session.LinkToProject(projectId);
        await _repository.SaveAsync(session);
    }

    public async Task<ChatSession> CreateForProjectAsync(Guid projectId)
    {
        var session = ChatSession.CreateForProject(projectId);
        await _repository.SaveAsync(session);
        return session;
    }

    public async Task<IReadOnlyList<ChatSession>> GetByProjectIdAsync(Guid projectId)
    {
        var all = await _repository.GetAllAsync();
        return all.Where(s => s.ProjectId == projectId).ToList();
    }

    public async Task<IReadOnlyList<ChatSession>> GetAllActiveAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.Where(s => !s.IsArchived).ToList();
    }

    public async Task RenameAsync(Guid sessionId, string newName)
    {
        var session = await _repository.GetByIdAsync(sessionId)
            ?? throw new DomainNotFoundException($"Session '{sessionId}' not found.");
        session.Rename(newName);
        await _repository.SaveAsync(session);
    }

    public async Task ArchiveAsync(Guid sessionId)
    {
        var session = await _repository.GetByIdAsync(sessionId)
            ?? throw new DomainNotFoundException($"Session '{sessionId}' not found.");
        session.Archive();
        await _repository.SaveAsync(session);
    }
}
