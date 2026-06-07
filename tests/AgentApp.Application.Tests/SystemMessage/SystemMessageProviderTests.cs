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

    // ── AgentFoundationProvider ───────────────────────────────────────────────

    [Fact]
    public void AgentFoundationProvider_IsApplicable_Always_WhenNoProject()
    {
        var provider = new AgentFoundationProvider();
        Assert.True(provider.IsApplicable(new AgentContext()));
    }

    [Fact]
    public void AgentFoundationProvider_IsApplicable_Always_WhenProjectSet()
    {
        var provider = new AgentFoundationProvider();
        Assert.True(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void AgentFoundationProvider_GetSection_MentionsTower()
    {
        var provider = new AgentFoundationProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("Tower", section);
    }

    [Fact]
    public void AgentFoundationProvider_GetSection_MentionsStartProject()
    {
        var provider = new AgentFoundationProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("STARTPROJECT", section);
    }

    [Fact]
    public void AgentFoundationProvider_GetSection_MentionsCommandFormat()
    {
        var provider = new AgentFoundationProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("COMMAND_NAME", section);
    }

    // ── NameConfirmationProvider ──────────────────────────────────────────────

    [Fact]
    public void NameConfirmationProvider_IsApplicable_WhenHasProjectAndNotConfirmed()
    {
        var provider = new NameConfirmationProvider();
        Assert.True(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void NameConfirmationProvider_NotApplicable_WhenNoProject()
    {
        var provider = new NameConfirmationProvider();
        Assert.False(provider.IsApplicable(new AgentContext()));
    }

    [Fact]
    public void NameConfirmationProvider_NotApplicable_WhenNameConfirmed()
    {
        var provider = new NameConfirmationProvider();
        var ctx = ContextWithProject("MyApp");
        ctx.ConfirmProjectName();
        Assert.False(provider.IsApplicable(ctx));
    }

    [Fact]
    public void NameConfirmationProvider_GetSection_ContainsProjectName()
    {
        var provider = new NameConfirmationProvider();
        var section = provider.GetSection(ContextWithProject("MyApp"));
        Assert.Contains("MyApp", section);
    }

    [Fact]
    public void NameConfirmationProvider_GetSection_ContainsNameConfirmedToken()
    {
        var provider = new NameConfirmationProvider();
        var section = provider.GetSection(ContextWithProject("MyApp"));
        Assert.Contains("NAME_CONFIRMED", section);
    }

    // ── FileToolsPromptProvider ───────────────────────────────────────────────

    [Fact]
    public void FileToolsPromptProvider_IsApplicable_ReturnsFalse_WhenNoProject()
    {
        var provider = new FileToolsPromptProvider();
        Assert.False(provider.IsApplicable(new AgentContext()));
    }

    [Fact]
    public void FileToolsPromptProvider_IsApplicable_ReturnsTrue_WhenProjectSet()
    {
        var provider = new FileToolsPromptProvider();
        Assert.True(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void FileToolsPromptProvider_GetSection_ContainsPathPermissionRequest()
    {
        var provider = new FileToolsPromptProvider();
        var section = provider.GetSection(ContextWithProject());
        Assert.Contains("PATH_PERMISSION_REQUEST", section);
    }

    [Fact]
    public void FileToolsPromptProvider_GetSection_PartialInit_MentionsFolderSelect()
    {
        var provider = new FileToolsPromptProvider();
        var ctx = new AgentContext();
        ctx.SetProject(Project.Create("App", "app", ""), "C:/agent/app", "");
        var section = provider.GetSection(ctx);
        Assert.Contains("FOLDER_SELECT", section);
    }

    // ── AgentContextProvider ──────────────────────────────────────────────────

    [Fact]
    public void AgentContextProvider_SectionRef_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AgentContextProvider.SectionRef));
    }

    [Fact]
    public void AgentContextProvider_IsApplicable_Always()
    {
        var provider = new AgentContextProvider();
        Assert.True(provider.IsApplicable(new AgentContext()));
        Assert.True(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void AgentContextProvider_GetSection_ContainsPreamble()
    {
        var provider = new AgentContextProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("Consult this section before responding", section);
    }

    [Fact]
    public void AgentContextProvider_GetSection_NoProject_ContainsStage1()
    {
        var provider = new AgentContextProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("stage: 1", section);
    }

    [Fact]
    public void AgentContextProvider_GetSection_WithUnconfirmedName_ContainsStage2()
    {
        var provider = new AgentContextProvider();
        var section = provider.GetSection(ContextWithProject());
        Assert.Contains("stage: 2", section);
    }

    [Fact]
    public void AgentContextProvider_GetSection_WithConfirmedName_ContainsStage3()
    {
        var provider = new AgentContextProvider();
        var ctx = ContextWithProject();
        ctx.ConfirmProjectName();
        var section = provider.GetSection(ctx);
        Assert.Contains("stage: 3", section);
    }

    [Fact]
    public void AgentContextProvider_GetSection_NoProject_SaysNone()
    {
        var provider = new AgentContextProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("project: none", section);
    }

    [Fact]
    public void AgentContextProvider_GetSection_WithProject_ContainsName()
    {
        var provider = new AgentContextProvider();
        var section = provider.GetSection(ContextWithProject("MyApp"));
        Assert.Contains("MyApp", section);
    }

    [Fact]
    public void AgentContextProvider_GetSection_WithProject_ContainsPaths()
    {
        var provider = new AgentContextProvider();
        var section = provider.GetSection(ContextWithProject("MyApp"));
        Assert.Contains("C:/agent/MyApp", section);
        Assert.Contains("C:/code/MyApp", section);
    }

    [Fact]
    public void AgentContextProvider_GetSection_ReflectsNameConfirmed()
    {
        var provider = new AgentContextProvider();
        var ctx = ContextWithProject("MyApp");
        ctx.ConfirmProjectName();
        var section = provider.GetSection(ctx);
        Assert.Contains("name_confirmed: true", section);
    }

    [Fact]
    public void AgentContextProvider_GetSection_ContainsConversationMode()
    {
        var provider = new AgentContextProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.Contains("conversation_mode:", section);
    }

    // ── StagePromptProvider ───────────────────────────────────────────────────

    [Fact]
    public void StagePromptProvider_IsApplicable_Always()
    {
        var provider = new StagePromptProvider();
        Assert.True(provider.IsApplicable(new AgentContext()));
        Assert.True(provider.IsApplicable(ContextWithProject()));
    }

    [Fact]
    public void StagePromptProvider_Stage1_GetSection_NotEmpty()
    {
        var provider = new StagePromptProvider();
        var section = provider.GetSection(new AgentContext());
        Assert.False(string.IsNullOrWhiteSpace(section));
    }

    [Fact]
    public void StagePromptProvider_Stage2_GetSection_ReturnsEmpty()
    {
        var provider = new StagePromptProvider();
        var section = provider.GetSection(ContextWithProject());
        Assert.Equal(string.Empty, section);
    }

    [Fact]
    public void StagePromptProvider_Stage3_GetSection_ReturnsEmpty()
    {
        var provider = new StagePromptProvider();
        var ctx = ContextWithProject();
        ctx.ConfirmProjectName();
        var section = provider.GetSection(ctx);
        Assert.Equal(string.Empty, section);
    }
}
