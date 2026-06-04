using AgentApp.Application.SystemMessage;
using AgentApp.Domain.Agent;
using AgentApp.Domain.Projects;

namespace AgentApp.Application.Tests.SystemMessage;

public class SystemMessageProviderTests
{
    private static AgentContext ContextWithProject(string name = "MyApp")
    {
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create(name, name.ToLowerInvariant(), $"C:/code/{name}"), $"C:/agent/{name}", $"C:/code/{name}");
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

    // ── InitializationPromptProvider ─────────────────────────────────────────

    [Fact]
    public void InitializationPromptProvider_IsApplicable_WhenNoProject()
    {
        var provider = new InitializationPromptProvider();
        Assert.True(provider.IsApplicable(new AgentContext()));
    }

    [Fact]
    public void InitializationPromptProvider_IsApplicable_WhenProjectHasNoCodeFolder()
    {
        // C-063: once a project exists (even partial init), InitializationPromptProvider
        // must NOT be active — FileToolsPromptProvider takes over to avoid conflicting instructions
        var provider = new InitializationPromptProvider();
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create("App", "app", ""), "C:/agent/app", "");
        Assert.False(provider.IsApplicable(ctx));
    }

    [Fact]
    public void InitializationPromptProvider_NotApplicable_WhenFullyInitialized()
    {
        var provider = new InitializationPromptProvider();
        Assert.False(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void InitializationPromptProvider_GetSection_NoProject_MentionsStartProject()
    {
        var provider = new InitializationPromptProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("STARTPROJECT", section);
    }

    // ── FileToolsPromptProvider ───────────────────────────────────────────────

    [Fact]
    public void FileToolsPromptProvider_IsApplicable_WhenFullyInitialized()
    {
        var provider = new FileToolsPromptProvider();
        Assert.True(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void FileToolsPromptProvider_NotApplicable_WhenNoProject()
    {
        var provider = new FileToolsPromptProvider();
        Assert.False(provider.IsApplicable(new AgentContext()));
    }

    [Fact]
    public void FileToolsPromptProvider_GetSection_ContainsProjectName()
    {
        var provider = new FileToolsPromptProvider();
        var section = provider.GetSection(ContextWithProject("MyApp"));
        Assert.Contains("MyApp", section);
        Assert.Contains("PATH_PERMISSION_REQUEST", section);
    }

    [Fact]
    public void FileToolsPromptProvider_IsApplicable_WhenPartialInit()
    {
        // C-063: FileToolsPromptProvider is active even when only partially initialized
        var provider = new FileToolsPromptProvider();
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create("App", "app", ""), "C:/agent/app", "");
        Assert.True(provider.IsApplicable(ctx));
    }

    [Fact]
    public void FileToolsPromptProvider_GetSection_PartialInit_MentionsFolderSelect()
    {
        // C-063: when no source control root is set, FileToolsPromptProvider explains how to set it
        var provider = new FileToolsPromptProvider();
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create("App", "app", ""), "C:/agent/app", "");
        var section = provider.GetSection(ctx);
        Assert.Contains("FOLDER_SELECT", section);
        Assert.Contains("App", section);
    }
}
