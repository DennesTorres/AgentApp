using AgentApp.Application.Agent;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Tests.Agent;

public class AgentContextServiceTests
{
    [Fact]
    public void GetCurrent_ReturnsInitialContext()
    {
        var service = new AgentContextService();
        var ctx = service.GetCurrent();
        Assert.NotNull(ctx);
        Assert.False(ctx.HasProject);
        Assert.Equal(ConversationState.Initial.Mode, ctx.ConversationState.Mode);
    }

    [Fact]
    public void SetProject_UpdatesContext()
    {
        var service = new AgentContextService();
        var project = Project.Create("App", "app", "C:/code/App");

        service.SetProject(project, "C:/agent/App", "C:/code/App");

        Assert.True(service.GetCurrent().HasProject);
        Assert.Equal("App", service.GetCurrent().CurrentProject!.Name);
        Assert.Equal("C:/agent/App", service.GetCurrent().AgentFolderPath);
    }

    [Fact]
    public void UpdateConversationState_UpdatesContext()
    {
        var service = new AgentContextService();
        service.UpdateConversationState(ConversationState.Testing);
        Assert.Equal("testing", service.GetCurrent().ConversationState.Mode);
    }

    [Fact]
    public void GetCurrent_ReturnsSameInstance()
    {
        var service = new AgentContextService();
        var ctx1 = service.GetCurrent();
        var ctx2 = service.GetCurrent();
        Assert.Same(ctx1, ctx2);
    }
}
