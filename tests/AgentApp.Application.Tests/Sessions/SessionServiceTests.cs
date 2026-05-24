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

    [Fact]
    public async Task CreateForProjectAsync_SavesAndReturnsLinkedSession()
    {
        var projectId = Guid.NewGuid();
        var session = await _sut.CreateForProjectAsync(projectId);

        Assert.NotNull(session);
        Assert.True(session.IsLinkedToProject);
        Assert.Equal(projectId, session.ProjectId);
        var saved = await _repository.GetByIdAsync(session.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetByProjectIdAsync_ReturnsOnlyMatchingSessions()
    {
        var projectId = Guid.NewGuid();
        await _sut.CreateForProjectAsync(projectId);
        await _sut.CreateForProjectAsync(projectId);
        await _sut.CreateForProjectAsync(Guid.NewGuid());

        var result = await _sut.GetByProjectIdAsync(projectId);

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(projectId, s.ProjectId));
    }

    [Fact]
    public async Task RenameAsync_UpdatesSessionName()
    {
        var session = await _sut.StartStandaloneSessionAsync();
        await _sut.RenameAsync(session.Id, "New Name");

        var saved = await _repository.GetByIdAsync(session.Id);
        Assert.Equal("New Name", saved!.Name);
    }

    [Fact]
    public async Task ArchiveAsync_SetsSessionArchived()
    {
        var session = await _sut.StartStandaloneSessionAsync();
        await _sut.ArchiveAsync(session.Id);

        var saved = await _repository.GetByIdAsync(session.Id);
        Assert.True(saved!.IsArchived);
    }

    [Fact]
    public async Task GetAllActiveAsync_ExcludesArchivedSessions()
    {
        var active = await _sut.StartStandaloneSessionAsync();
        var archived = await _sut.StartStandaloneSessionAsync();
        await _sut.ArchiveAsync(archived.Id);

        var result = await _sut.GetAllActiveAsync();

        Assert.Contains(result, s => s.Id == active.Id);
        Assert.DoesNotContain(result, s => s.Id == archived.Id);
    }
}
