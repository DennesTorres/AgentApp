using AgentApp.Domain.Agent;
using AgentApp.Domain.Projects;

namespace AgentApp.Domain.Tests.Agent;

public class AgentContextTests
{
    [Fact]
    public void NewAgentContext_HasNoProject()
    {
        var ctx = new AgentContext();
        Assert.False(ctx.HasProject);
        Assert.Null(ctx.CurrentProject);
        Assert.Equal(ConversationState.Initial.Mode, ctx.ConversationState.Mode);
    }

    [Fact]
    public void SetProject_SetsProjectAndPaths()
    {
        var ctx = new AgentContext();
        var project = Project.Create("TestApp", "test-app", "C:/code/TestApp");

        ctx.SetProject(project, "C:/agent/TestApp", "C:/code/TestApp");

        Assert.True(ctx.HasProject);
        Assert.Equal("TestApp", ctx.CurrentProject!.Name);
        Assert.Equal("C:/agent/TestApp", ctx.AgentFolderPath);
        Assert.Equal("C:/code/TestApp", ctx.CodeFolderPath);
    }

    [Fact]
    public void SetProject_ToNull_ClearsProject()
    {
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create("App", "app", "C:/code"), "C:/agent/App", "C:/code/App");

        ctx.SetProject(null, string.Empty, string.Empty);

        Assert.False(ctx.HasProject);
        Assert.Null(ctx.CurrentProject);
    }

    [Fact]
    public void UpdateConversationState_ChangesState()
    {
        var ctx = new AgentContext();
        ctx.UpdateConversationState(ConversationState.Implementing);
        Assert.Equal("implementing", ctx.ConversationState.Mode);
    }

    [Fact]
    public void ConversationState_FromMode_NormalizesToLower()
    {
        var state = ConversationState.FromMode("TESTING");
        Assert.Equal("testing", state.Mode);
    }

    [Fact]
    public void NewAgentContext_NameConfirmed_IsFalse()
    {
        var ctx = new AgentContext();
        Assert.False(ctx.NameConfirmed);
    }

    [Fact]
    public void SetProject_SetsNameConfirmedFalse()
    {
        var ctx = new AgentContext();
        var project = Project.Create("App", "app", "C:/code");
        ctx.SetProject(project, "C:/agent/App", "C:/code/App");
        Assert.False(ctx.NameConfirmed);
    }

    [Fact]
    public void ConfirmProjectName_SetsNameConfirmedTrue()
    {
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create("App", "app", "C:/code"), "C:/agent/App", "C:/code/App");
        ctx.ConfirmProjectName();
        Assert.True(ctx.NameConfirmed);
    }

    [Fact]
    public void SetProject_AfterConfirm_ResetsNameConfirmedFalse()
    {
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create("App", "app", "C:/code"), "C:/agent/App", "C:/code/App");
        ctx.ConfirmProjectName();
        ctx.SetProject(Project.Create("NewApp", "new-app", "C:/code"), "C:/agent/NewApp", "C:/code/NewApp");
        Assert.False(ctx.NameConfirmed);
    }
}
