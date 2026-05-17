using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Orchestration;

namespace AgentApp.Domain.Tests.Orchestration;

public class OrchestratorSessionTests
{
    [Fact]
    public void Create_ValidInputs_SetsAllProperties()
    {
        var projectId = Guid.NewGuid();

        var session = OrchestratorSession.Create(projectId, AgentType.Implementation);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(projectId, session.ProjectId);
        Assert.Equal(AgentType.Implementation, session.AgentType);
        Assert.Equal(OrchestratorStatus.Running, session.Status);
        Assert.Null(session.CompletedAt);
    }

    [Fact]
    public void Complete_RunningSession_TransitionsToCompleted()
    {
        var session = OrchestratorSession.Create(Guid.NewGuid(), AgentType.Review);

        session.Complete();

        Assert.Equal(OrchestratorStatus.Completed, session.Status);
        Assert.NotNull(session.CompletedAt);
    }

    [Fact]
    public void Fail_RunningSession_TransitionsToFailed()
    {
        var session = OrchestratorSession.Create(Guid.NewGuid(), AgentType.Implementation);

        session.Fail();

        Assert.Equal(OrchestratorStatus.Failed, session.Status);
        Assert.NotNull(session.CompletedAt);
    }

    [Fact]
    public void Complete_AlreadyCompleted_ThrowsDomainValidationException()
    {
        var session = OrchestratorSession.Create(Guid.NewGuid(), AgentType.Review);
        session.Complete();

        Assert.Throws<DomainValidationException>(() => session.Complete());
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var completedAt = DateTimeOffset.UtcNow;

        var session = OrchestratorSession.Reconstitute(
            id, projectId, AgentType.Review, OrchestratorStatus.Completed, startedAt, completedAt);

        Assert.Equal(id, session.Id);
        Assert.Equal(projectId, session.ProjectId);
        Assert.Equal(AgentType.Review, session.AgentType);
        Assert.Equal(OrchestratorStatus.Completed, session.Status);
        Assert.Equal(completedAt, session.CompletedAt);
    }
}
