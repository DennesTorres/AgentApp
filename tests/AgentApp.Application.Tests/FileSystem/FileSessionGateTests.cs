using AgentApp.Application.FileSystem;

namespace AgentApp.Application.Tests.FileSystem;

public class FileSessionGateTests
{
    private static FileSessionGate BuildGate(string agentRoot = @"C:\agent", string codeRoot = @"C:\code")
    {
        var gate = new FileSessionGate();
        gate.SetProjectRoots(agentRoot, codeRoot);
        return gate;
    }

    [Fact]
    public void CanWrite_WithinAgentRoot_ReturnsTrue()
    {
        var gate = BuildGate(@"C:\agent", @"C:\code");
        Assert.True(gate.CanWrite(@"C:\agent\file.txt"));
    }

    [Fact]
    public void CanWrite_WithinCodeRoot_ReturnsTrue()
    {
        var gate = BuildGate(@"C:\agent", @"C:\code");
        Assert.True(gate.CanWrite(@"C:\code\src\file.cs"));
    }

    [Fact]
    public void CanWrite_OutsideRoots_ReturnsFalse()
    {
        var gate = BuildGate(@"C:\agent", @"C:\code");
        Assert.False(gate.CanWrite(@"C:\Windows\System32\bad.dll"));
    }

    [Fact]
    public void CanRead_WithinAgentRoot_ReturnsTrue()
    {
        var gate = BuildGate(@"C:\agent", @"C:\code");
        Assert.True(gate.CanRead(@"C:\agent\notes.md"));
    }

    [Fact]
    public void CanRead_WithinCodeRoot_ReturnsTrue()
    {
        var gate = BuildGate(@"C:\agent", @"C:\code");
        Assert.True(gate.CanRead(@"C:\code\src\Program.cs"));
    }

    [Fact]
    public void CanRead_UngrantedOutsideRoots_ReturnsFalse()
    {
        var gate = BuildGate(@"C:\agent", @"C:\code");
        Assert.False(gate.CanRead(@"C:\Users\user\Documents\secret.txt"));
    }

    [Fact]
    public void GrantReadAccess_AllowsSubpathReads()
    {
        var gate = BuildGate(@"C:\agent", @"C:\code");
        gate.GrantReadAccess(@"C:\Users\user\Documents");
        Assert.True(gate.CanRead(@"C:\Users\user\Documents\file.txt"));
    }

    [Fact]
    public void CanWrite_WhenNoRootsSet_ReturnsFalse()
    {
        var gate = new FileSessionGate(); // no roots set
        Assert.False(gate.CanWrite(@"C:\anything\file.txt"));
    }

    [Fact]
    public void PathCheck_IsCaseInsensitive()
    {
        var gate = BuildGate(@"C:\Agent", @"C:\Code");
        Assert.True(gate.CanWrite(@"c:\agent\file.txt"));
        Assert.True(gate.CanRead(@"c:\code\src\file.cs"));
    }

    // US-190: bypass mode
    [Fact]
    public void SetBypassMode_True_CanReadAnyPath()
    {
        var gate = new FileSessionGate();
        gate.SetBypassMode(true);

        Assert.True(gate.CanRead(@"C:\Windows\System32\anything.dll"));
    }

    [Fact]
    public void SetBypassMode_True_CanWriteAnyPath()
    {
        var gate = new FileSessionGate();
        gate.SetBypassMode(true);

        Assert.True(gate.CanWrite(@"C:\Windows\System32\anything.dll"));
    }

    [Fact]
    public void IsBypassMode_DefaultFalse()
    {
        var gate = new FileSessionGate();

        Assert.False(gate.IsBypassMode);
    }

    [Fact]
    public void SetBypassMode_False_RestoresNormalBehaviour()
    {
        var gate = BuildGate(@"C:\agent", @"C:\code");
        gate.SetBypassMode(true);
        gate.SetBypassMode(false);

        Assert.False(gate.CanRead(@"C:\Windows\System32\bad.dll"));
    }
}
