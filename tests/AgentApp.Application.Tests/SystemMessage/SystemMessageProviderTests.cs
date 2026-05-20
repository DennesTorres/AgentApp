using AgentApp.Application.SystemMessage;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Tests.SystemMessage;

public class SystemMessageProviderTests
{
    private static AgentContext ContextWithProject(string name = "MyApp")
    {
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create(name, $"C:/code/{name}"), $"C:/agent/{name}", $"C:/code/{name}");
        return ctx;
    }

    // ── NoProjectProvider ────────────────────────────────────────────────────

    [Fact]
    public void NoProjectProvider_IsApplicable_WhenNoProject()
    {
        var provider = new NoProjectProvider();
        Assert.True(provider.IsApplicable(new AgentContext()));
    }

    [Fact]
    public void NoProjectProvider_NotApplicable_WhenProjectSet()
    {
        var provider = new NoProjectProvider();
        Assert.False(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void NoProjectProvider_GetSection_MentionsTower()
    {
        var provider = new NoProjectProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("Tower", section);
    }

    [Fact]
    public void NoProjectProvider_GetSection_GuideToCreateProject()
    {
        var provider = new NoProjectProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.False(string.IsNullOrWhiteSpace(section));
    }

    // ── ActiveProjectProvider ────────────────────────────────────────────────

    [Fact]
    public void ActiveProjectProvider_IsApplicable_WhenProjectSet()
    {
        var provider = new ActiveProjectProvider();
        Assert.True(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void ActiveProjectProvider_NotApplicable_WhenNoProject()
    {
        var provider = new ActiveProjectProvider();
        Assert.False(provider.IsApplicable(new AgentContext()));
    }

    [Fact]
    public void ActiveProjectProvider_GetSection_ContainsProjectName()
    {
        var provider = new ActiveProjectProvider();
        var ctx = ContextWithProject("MyApp");
        var section = provider.GetSection(ctx);
        Assert.Contains("MyApp", section);
    }

    [Fact]
    public void ActiveProjectProvider_GetSection_ContainsPaths()
    {
        var provider = new ActiveProjectProvider();
        var ctx = ContextWithProject("MyApp");
        var section = provider.GetSection(ctx);
        Assert.Contains("C:/agent/MyApp", section);
        Assert.Contains("C:/code/MyApp", section);
    }

    // ── ExecutionStateProvider ───────────────────────────────────────────────

    [Fact]
    public void ExecutionStateProvider_IsApplicable_WhenProjectSet()
    {
        var provider = new ExecutionStateProvider();
        Assert.True(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void ExecutionStateProvider_NotApplicable_WhenNoProject()
    {
        var provider = new ExecutionStateProvider();
        Assert.False(provider.IsApplicable(new AgentContext()));
    }

    [Fact]
    public void ExecutionStateProvider_GetSection_ContainsMode()
    {
        var provider = new ExecutionStateProvider();
        var ctx = ContextWithProject();
        ctx.UpdateConversationState(ConversationState.Implementing);
        var section = provider.GetSection(ctx);
        Assert.Contains("implementing", section);
    }

    [Fact]
    public void ExecutionStateProvider_GetSection_DefaultMode_ContainsChat()
    {
        var provider = new ExecutionStateProvider();
        var ctx = ContextWithProject();
        var section = provider.GetSection(ctx);
        Assert.Contains("chat", section, StringComparison.OrdinalIgnoreCase);
    }
}
