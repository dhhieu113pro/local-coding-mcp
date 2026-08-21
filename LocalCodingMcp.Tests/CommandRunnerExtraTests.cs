using System.Diagnostics;
using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public class CommandRunnerExtraTests
{
    [Fact]
    public void ConfigureShell_Windows()
    {
        var psi = new ProcessStartInfo();
        CommandRunner.ConfigureShell(psi, "echo hi", isWindows: true);
        Assert.Equal("cmd.exe", psi.FileName);
        Assert.Contains("/c", psi.Arguments);
        Assert.Contains("echo hi", psi.Arguments);
    }

    [Fact]
    public void ConfigureShell_Unix()
    {
        var psi = new ProcessStartInfo();
        CommandRunner.ConfigureShell(psi, "echo hi", isWindows: false);
        Assert.Equal("/bin/bash", psi.FileName);
        Assert.Contains("-c", psi.Arguments);
        Assert.Contains("echo hi", psi.Arguments);
    }

    [Fact]
    public async Task RunAsync_WithExplicitUnixFlag()
    {
        var runner = new CommandRunner(timeoutSeconds: 10, isWindows: false);
        var root = TestHelpers.CreateTempRoot();
        try
        {
            var result = await runner.RunAsync("echo cover-unix", root);
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("cover-unix", result.Stdout);
        }
        finally
        {
            TestHelpers.SafeDelete(root);
        }
    }
}
