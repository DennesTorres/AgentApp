using AgentApp.Domain.Rules;

namespace AgentApp.Domain.Tests.Rules;

public class TriggersIndexTests
{
    [Fact]
    public void CreateGlobal_ValidEntry_AddsToIndex()
    {
        var index = TriggersIndex.CreateGlobal();
        index.AddEntry("coding-standards", ["refactor", "code review"]);

        Assert.True(index.HasTrigger("refactor"));
        Assert.Equal("coding-standards", index.GetFileNameForTrigger("refactor"));
    }

    [Fact]
    public void CreateForProject_ScopedToProject()
    {
        var projectId = Guid.NewGuid();
        var index = TriggersIndex.CreateForProject(projectId);

        Assert.Equal(MdFileScope.Project, index.Scope);
        Assert.Equal(projectId, index.ProjectId);
    }

    [Fact]
    public void HasTrigger_UnknownTrigger_ReturnsFalse()
    {
        var index = TriggersIndex.CreateGlobal();

        Assert.False(index.HasTrigger("unknown-trigger"));
    }

    [Fact]
    public void RemoveEntry_ExistingEntry_RemovesAllItsTriggers()
    {
        var index = TriggersIndex.CreateGlobal();
        index.AddEntry("coding-standards", ["refactor", "review"]);

        index.RemoveEntry("coding-standards");

        Assert.False(index.HasTrigger("refactor"));
        Assert.False(index.HasTrigger("review"));
    }

    [Fact]
    public void GetAllFileNames_ReturnsDistinctFileNames()
    {
        var index = TriggersIndex.CreateGlobal();
        index.AddEntry("file-a", ["trigger1"]);
        index.AddEntry("file-b", ["trigger2", "trigger3"]);

        var names = index.GetAllFileNames();

        Assert.Contains("file-a", names);
        Assert.Contains("file-b", names);
        Assert.Equal(2, names.Count);
    }
}
