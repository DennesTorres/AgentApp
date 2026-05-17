using AgentApp.Domain.Orchestration;
using AgentApp.Infrastructure.FileSystem;

namespace AgentApp.Infrastructure.Tests.FileSystem;

public class JsonOrchestratorSessionRepositoryTests : IDisposable
{
    private readonly string _testFolder;
    private readonly JsonOrchestratorSessionRepository _sut;

    public JsonOrchestratorSessionRepositoryTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _sut = new JsonOrchestratorSessionRepository(_testFolder);
    }

    [Fact]
    public async Task GetByProjectIdAsync_EmptyStore_ReturnsEmpty()
    {
        var result = await _sut.GetByProjectIdAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_Session_CanBeRetrievedById()
    {
        var session = OrchestratorSession.Create(Guid.NewGuid(), AgentType.Implementation);

        await _sut.SaveAsync(session);
        var result = await _sut.GetByIdAsync(session.Id);

        Assert.NotNull(result);
        Assert.Equal(session.Id, result.Id);
        Assert.Equal(AgentType.Implementation, result.AgentType);
        Assert.Equal(OrchestratorStatus.Running, result.Status);
    }

    [Fact]
    public async Task SaveAsync_CompletedSession_PersistsStatus()
    {
        var session = OrchestratorSession.Create(Guid.NewGuid(), AgentType.Review);
        session.Complete();

        await _sut.SaveAsync(session);
        var result = await _sut.GetByIdAsync(session.Id);

        Assert.Equal(OrchestratorStatus.Completed, result!.Status);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task GetByProjectIdAsync_MultipleProjects_ReturnsOnlyMatchingProject()
    {
        var project1 = Guid.NewGuid();
        var project2 = Guid.NewGuid();
        await _sut.SaveAsync(OrchestratorSession.Create(project1, AgentType.Implementation));
        await _sut.SaveAsync(OrchestratorSession.Create(project2, AgentType.Review));

        var result = await _sut.GetByProjectIdAsync(project1);

        Assert.Single(result);
        Assert.Equal(AgentType.Implementation, result[0].AgentType);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }
}
