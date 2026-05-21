using AgentApp.Application.Sessions;
using AgentApp.Application.Tests.Fakes;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Sessions;

namespace AgentApp.Application.Tests.Sessions;

public class SessionServiceTests
{
    private readonly ISessionRepository _repository;
    private readonly SessionService _sut;

    public SessionServiceTests()
    {
        _repository = new InMemorySessionRepository();
        _sut = new SessionService(_repository);
    }

    [Fact]
    public async Task StartStandaloneSessionAsync_SavesAndReturnsSession()
    {
        var session = await _sut.StartStandaloneSessionAsync();

        Assert.NotNull(session);
        Assert.False(session.IsLinkedToProject);
        var saved = await _repository.GetByIdAsync(session.Id);
        Assert.NotNull(saved);
        Assert.Equal(session.Id, saved.Id);
    }

    [Fact]
    public async Task LinkSessionToProjectAsync_StandaloneSession_LinksAndSaves()
    {
        var session = ChatSession.CreateStandalone();
        await _repository.SaveAsync(session);
        var projectId = Guid.NewGuid();

        await _sut.LinkSessionToProjectAsync(session.Id, projectId);

        var saved = await _repository.GetByIdAsync(session.Id);
        Assert.NotNull(saved);
        Assert.True(saved.IsLinkedToProject);
        Assert.Equal(projectId, saved.ProjectId);
    }

    [Fact]
    public async Task LinkSessionToProjectAsync_SessionNotFound_Throws()
    {
        await Assert.ThrowsAsync<AgentApp.Domain.Exceptions.DomainNotFoundException>(
            () => _sut.LinkSessionToProjectAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
