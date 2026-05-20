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
    public void Parse_ReadFileCommand_ExtractsPath()
    {
        var parser = BuildParser();
        var (_, commands) = parser.Parse("[READ_FILE:{\"path\":\"/src/Program.cs\"}]");
        Assert.Single(commands);
        var cmd = Assert.IsType<ReadFileCommand>(commands[0]);
        Assert.Equal("/src/Program.cs", cmd.Path);
    }

    [Fact]
    public void Parse_WriteFileCommand_ExtractsPathAndContent()
    {
        var parser = BuildParser();
        var (_, commands) = parser.Parse("[WRITE_FILE:{\"path\":\"/src/Hello.cs\",\"content\":\"class Hello { }\"}]");
        Assert.Single(commands);
        var cmd = Assert.IsType<WriteFileCommand>(commands[0]);
        Assert.Equal("/src/Hello.cs", cmd.Path);
        Assert.Equal("class Hello { }", cmd.Content);
    }

    [Fact]
    public void Parse_WriteFileCommand_ContentWithNestedBraces_ExtractsCorrectly()
    {
        var parser = BuildParser();
        var content = "public class Foo { public void Bar() { return; } }";
        var input = $"[WRITE_FILE:{{\"path\":\"/src/Foo.cs\",\"content\":\"{content}\"}}]";
        var (_, commands) = parser.Parse(input);
        Assert.Single(commands);
        var cmd = Assert.IsType<WriteFileCommand>(commands[0]);
        Assert.Equal(content, cmd.Content);
    }

    [Fact]
    public void Parse_ListDirCommand_ExtractsPath()
    {
        var parser = BuildParser();
        var (_, commands) = parser.Parse("[LIST_DIR:{\"path\":\"/src\"}]");
        Assert.Single(commands);
        var cmd = Assert.IsType<ListDirectoryCommand>(commands[0]);
        Assert.Equal("/src", cmd.Path);
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
}
