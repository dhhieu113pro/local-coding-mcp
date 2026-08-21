using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public class CommandRunnerTests
{
    private readonly CommandRunner _runner = new(timeoutSeconds: 15);
    private readonly string _cwd = Path.GetTempPath();

    [Fact]
    public async Task Run_Echo_Succeeds()
    {
        var cmd = OperatingSystem.IsWindows() ? "echo hello" : "echo hello";
        var result = await _runner.RunAsync(cmd, _cwd);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello", result.Stdout);
        Assert.True(result.DurationMs >= 0);
    }

    [Fact]
    public async Task Run_EmptyCommand_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _runner.RunAsync("", _cwd));
        await Assert.ThrowsAsync<ArgumentException>(() => _runner.RunAsync("   ", _cwd));
    }

    [Fact]
    public async Task Run_FailingCommand_NonZeroExit()
    {
        var cmd = OperatingSystem.IsWindows() ? "cmd /c exit 7" : "exit 7";
        // On Windows our runner already uses cmd /c, so just "exit 7"
        if (OperatingSystem.IsWindows())
            cmd = "exit 7";
        else
            cmd = "bash -c 'exit 7'";

        // Our runner wraps with bash -c / cmd /c, so:
        cmd = OperatingSystem.IsWindows() ? "exit 42" : "exit 42";
        var result = await _runner.RunAsync(cmd, _cwd);
        Assert.True(result.ExitCode != 0);
    }

    [Fact]
    public async Task Run_Timeout_Throws()
    {
        var shortRunner = new CommandRunner(timeoutSeconds: 5);
        var cmd = OperatingSystem.IsWindows()
            ? "ping -n 30 127.0.0.1"
            : "sleep 30";

        await Assert.ThrowsAsync<TimeoutException>(() =>
            shortRunner.RunAsync(cmd, _cwd));
    }
}
