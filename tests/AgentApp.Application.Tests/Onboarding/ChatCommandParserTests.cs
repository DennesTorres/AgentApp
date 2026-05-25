using AgentApp.Application.Onboarding;
using AgentApp.Domain.Chat;

namespace AgentApp.Application.Tests.Onboarding;

public class ChatCommandParserTests
{
    private static ChatCommandParser BuildParser() => new();

    [Fact]
    public void Parse_NoCommands_ReturnsTextUnchanged()
    {
        var parser = BuildParser();
        var (text, commands) = parser.Parse("Hello, what would you like to build?");
        Assert.Equal("Hello, what would you like to build?", text);
        Assert.Empty(commands);
    }

    [Fact]
    public void Parse_FolderSelectCommand_ExtractsReasonAndStripsMarker()
    {
        var parser = BuildParser();
        var input = "Please select a folder. [FOLDER_SELECT:{\"reason\":\"Choose source control root\"}] Thank you.";
        var (text, commands) = parser.Parse(input);

        Assert.Single(commands);
        var cmd = Assert.IsType<FolderSelectCommand>(commands[0]);
        Assert.Equal("Choose source control root", cmd.Reason);
        Assert.DoesNotContain("[FOLDER_SELECT:", text);
    }

    [Fact]
    public void Parse_ProjectConfirmCommand_ExtractsNameIntentAndStripsMarker()
    {
        var parser = BuildParser();
        var input = "I propose this project. [PROJECT_CONFIRM:{\"name\":\"MyApp\",\"intent\":\"A todo list app\"}]";
        var (text, commands) = parser.Parse(input);

        Assert.Single(commands);
        var cmd = Assert.IsType<ProjectConfirmCommand>(commands[0]);
        Assert.Equal("MyApp", cmd.ProjectName);
        Assert.Equal("A todo list app", cmd.ProjectIntent);
        Assert.DoesNotContain("[PROJECT_CONFIRM:", text);
    }

    [Fact]
    public void Parse_MultipleCommands_ExtractsAll()
    {
        var parser = BuildParser();
        var input = "[FOLDER_SELECT:{\"reason\":\"Pick folder\"}] Some text. [PROJECT_CONFIRM:{\"name\":\"App\",\"intent\":\"desc\"}]";
        var (_, commands) = parser.Parse(input);

        Assert.Equal(2, commands.Count);
        Assert.IsType<FolderSelectCommand>(commands[0]);
        Assert.IsType<ProjectConfirmCommand>(commands[1]);
    }

    [Fact]
    public void Parse_TextAfterCommandRemoval_IsTrimmed()
    {
        var parser = BuildParser();
        var input = "  [FOLDER_SELECT:{\"reason\":\"r\"}]  ";
        var (text, _) = parser.Parse(input);
        Assert.Equal(string.Empty, text.Trim());
    }

    [Fact]
    public void Parse_PathPermissionRequestCommand_ExtractsPathAndReason()
    {
        var parser = BuildParser();
        var (_, commands) = parser.Parse("[PATH_PERMISSION_REQUEST:{\"path\":\"/docs\",\"reason\":\"Need readme\"}]");
        Assert.Single(commands);
        var cmd = Assert.IsType<PathPermissionRequestCommand>(commands[0]);
        Assert.Equal("/docs", cmd.Path);
        Assert.Equal("Need readme", cmd.Reason);
    }

    [Fact]
    public void Parse_StartProjectCommand_ExtractsAllFields()
    {
        var parser = BuildParser();
        var (_, commands) = parser.Parse("[STARTPROJECT:{\"name\":\"My App\",\"folderName\":\"my-app\",\"intent\":\"A todo list\"}]");
        Assert.Single(commands);
        var cmd = Assert.IsType<StartProjectCommand>(commands[0]);
        Assert.Equal("My App", cmd.Name);
        Assert.Equal("my-app", cmd.FolderName);
        Assert.Equal("A todo list", cmd.Intent);
    }
}
