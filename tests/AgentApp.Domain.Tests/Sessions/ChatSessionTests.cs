using AgentApp.Domain.Sessions;

namespace AgentApp.Domain.Tests.Sessions;

public class ChatSessionTests
{
    [Fact]
    public void CreateStandalone_ReturnsSessionWithNoProject()
    {
        var session = ChatSession.CreateStandalone();

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Null(session.ProjectId);
        Assert.False(session.IsLinkedToProject);
        Assert.True(session.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateStandalone_TwoCallsReturnDifferentIds()
    {
        var s1 = ChatSession.CreateStandalone();
        var s2 = ChatSession.CreateStandalone();

        Assert.NotEqual(s1.Id, s2.Id);
    }

    [Fact]
    public void CreateForProject_ReturnsSessionLinkedToProject()
    {
        var projectId = Guid.NewGuid();
        var session = ChatSession.CreateForProject(projectId);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(projectId, session.ProjectId);
        Assert.True(session.IsLinkedToProject);
    }

    [Fact]
    public void LinkToProject_StandaloneSession_BecomesLinked()
    {
        var session = ChatSession.CreateStandalone();
        var projectId = Guid.NewGuid();

        session.LinkToProject(projectId);

        Assert.Equal(projectId, session.ProjectId);
        Assert.True(session.IsLinkedToProject);
    }

    [Fact]
    public void LinkToProject_AlreadyLinkedSession_Throws()
    {
        var session = ChatSession.CreateForProject(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => session.LinkToProject(Guid.NewGuid()));
    }

    [Fact]
    public void CreateStandalone_HasDefaultName()
    {
        var session = ChatSession.CreateStandalone();
        Assert.False(string.IsNullOrWhiteSpace(session.Name));
    }

    [Fact]
    public void Rename_UpdatesName()
    {
        var session = ChatSession.CreateStandalone();
        session.Rename("My Session");
        Assert.Equal("My Session", session.Name);
    }

    [Fact]
    public void Rename_EmptyName_Throws()
    {
        var session = ChatSession.CreateStandalone();
        Assert.Throws<ArgumentException>(() => session.Rename(""));
    }

    [Fact]
    public void Archive_SetsIsArchivedTrue()
    {
        var session = ChatSession.CreateStandalone();
        session.Archive();
        Assert.True(session.IsArchived);
    }

    [Fact]
    public void Archive_AlreadyArchived_Throws()
    {
        var session = ChatSession.CreateStandalone();
        session.Archive();
        Assert.Throws<InvalidOperationException>(() => session.Archive());
    }
}
