using AgentApp.Application.Sessions;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Sessions;
using NSubstitute;

namespace AgentApp.Application.Tests.Sessions;

public class SessionServiceTests
{
    private readonly ISessionRepository _repository;
    private readonly SessionService _sut;

    public SessionServiceTests()
    {
        _repository = Substitute.For<ISessionRepository>();
        _sut = new SessionService(_repository);
    }

    [Fact]
    public async Task StartStandaloneSessionAsync_SavesAndReturnsSession()
    {
        var session = await _sut.StartStandaloneSessionAsync();

        Assert.NotNull(session);
        Assert.False(session.IsLinkedToProject);
        await _repository.Received(1).SaveAsync(Arg.Is<ChatSession>(s => s.Id == session.Id));
    }

    [Fact]
    public async Task LinkSessionToProjectAsync_StandaloneSession_LinksAndSaves()
    {
        var session = ChatSession.CreateStandalone();
        var projectId = Guid.NewGuid();
        _repository.GetByIdAsync(session.Id).Returns(session);

        await _sut.LinkSessionToProjectAsync(session.Id, projectId);

        Assert.True(session.IsLinkedToProject);
        Assert.Equal(projectId, session.ProjectId);
        await _repository.Received(1).SaveAsync(session);
    }

    [Fact]
    public async Task LinkSessionToProjectAsync_SessionNotFound_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((ChatSession?)null);

        await Assert.ThrowsAsync<AgentApp.Domain.Exceptions.DomainNotFoundException>(
            () => _sut.LinkSessionToProjectAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
