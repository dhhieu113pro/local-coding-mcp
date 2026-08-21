using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public class GitAndShellToolsTests : IDisposable
{
    private readonly string _root;
    private readonly PathSandbox _sandbox;
    private readonly WorkspaceManager _ws;
    private readonly CommandRunner _runner;
    private readonly string _workspaceId;
    private readonly ShellTools _shell;
    private readonly GitTools _git;

    public GitAndShellToolsTests()
    {
        (_root, _sandbox, _ws, _, _runner) = TestHelpers.CreateEnv();
        _workspaceId = _ws.Open(_root).Id;
        _shell = new ShellTools(_ws, _runner);
        _git = new GitTools(_ws, _runner);
    }

    public void Dispose() => TestHelpers.SafeDelete(_root);

    [Fact]
    public async Task RunCommand_Echo()
    {
        var json = await _shell.RunCommand("echo tool-test", _workspaceId);
        Assert.Contains("tool-test", json);
        Assert.Contains("exit_code", json);
    }

    [Fact]
    public async Task GitStatus_InNonRepo_DoesNotCrash()
    {
        // Not a git repo – should still return structured JSON (non-zero exit ok)
        var json = await _git.GitStatus(_workspaceId);
        Assert.Contains("exit_code", json);
    }

    [Fact]
    public async Task GitDiff_InNonRepo_DoesNotCrash()
    {
        var json = await _git.GitDiff(_workspaceId);
        Assert.Contains("exit_code", json);
    }

    [Fact]
    public async Task GitLog_InNonRepo_DoesNotCrash()
    {
        var json = await _git.GitLog(_workspaceId, 5);
        Assert.Contains("exit_code", json);
    }

    [Fact]
    public async Task GitTools_InInitializedRepo()
    {
        // init repo if git exists
        var init = await _runner.RunAsync("git init", _root);
        if (init.ExitCode != 0)
        {
            // git not available – skip soft
            return;
        }

        await _runner.RunAsync("git config user.email test@example.com", _root);
        await _runner.RunAsync("git config user.name Test", _root);
        File.WriteAllText(Path.Combine(_root, "f.txt"), "v1");
        await _runner.RunAsync("git add f.txt", _root);
        await _runner.RunAsync("git commit -m init", _root);

        var status = await _git.GitStatus(_workspaceId);
        Assert.Contains("exit_code", status);

        File.WriteAllText(Path.Combine(_root, "f.txt"), "v2");
        var diff = await _git.GitDiff(_workspaceId, staged: false);
        Assert.Contains("exit_code", diff);

        var log = await _git.GitLog(_workspaceId, 3);
        Assert.Contains("exit_code", log);
    }
}
