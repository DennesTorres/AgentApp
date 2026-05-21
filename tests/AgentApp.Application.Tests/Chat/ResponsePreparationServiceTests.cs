using AgentApp.Application.Chat;
using AgentApp.Application.Onboarding;
using AgentApp.Domain.Chat;

namespace AgentApp.Application.Tests.Chat;

public class ResponsePreparationServiceTests
{
    private static IResponsePreparationService Build() =>
        new ResponsePreparationService(new ChatCommandParser());

    [Fact]
    public void Prepare_PlainText_ReturnsTextUnchangedAndNoCommands()
    {
        var svc = Build();
        var (display, commands) = svc.Prepare("Hello, how can I help?");
        Assert.Equal("Hello, how can I help?", display);
        Assert.Empty(commands);
    }

    [Fact]
    public void Prepare_WithFolderSelectMarker_StripsMarkerAndExtractsCommand()
    {
        var svc = Build();
        var input = "Please pick a folder. [FOLDER_SELECT:{\"reason\":\"Need root\"}]";
        var (display, commands) = svc.Prepare(input);

        Assert.Single(commands);
        Assert.IsType<FolderSelectCommand>(commands[0]);
        Assert.DoesNotContain("[FOLDER_SELECT:", display);
    }

    [Fact]
    public void Prepare_WithMultipleMarkers_ExtractsAllCommands()
    {
        var svc = Build();
        var input = "[FOLDER_SELECT:{\"reason\":\"r\"}] [PROJECT_CONFIRM:{\"name\":\"App\",\"intent\":\"desc\"}]";
        var (_, commands) = svc.Prepare(input);
        Assert.Equal(2, commands.Count);
    }
}
