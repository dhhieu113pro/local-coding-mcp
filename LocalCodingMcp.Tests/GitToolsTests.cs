using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public class GitToolsTests : IDisposable
{
    private readonly string _root;
    private readonly PathSandbox _sandbox;
    private readonly WorkspaceManager _ws;
    private readonly CommandRunner _runner;
    private readonly GitTools _tools;
    private readonly string _workspaceId;
    private readonly bool _gitAvailable;

    public GitToolsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-git-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        _sandbox = new PathSandbox(new[] { _root });
        _ws = new WorkspaceManager(_sandbox);
        _runner = new CommandRunner(15);
        _tools = new GitTools(_ws, _runner);
        _workspaceId = _ws.Open(_root).Id;

        // Init git repo if git exists
        try
        {
            var init = _runner.RunAsync("git init", _root).GetAwaiter().GetResult();
            _gitAvailable = init.ExitCode == 0;
            if (_gitAvailable)
            {
                _runner.RunAsync("git config user.email test@example.com", _root).GetAwaiter().GetResult();
                _runner.RunAsync("git config user.name Test", _root).GetAwaiter().GetResult();
                File.WriteAllText(Path.Combine(_root, "f.txt"), "v1");
                _runner.RunAsync("git add f.txt", _root).GetAwaiter().GetResult();
                _runner.RunAsync("git commit -m init", _root).GetAwaiter().GetResult();
            }
        }
        catch
        {
            _gitAvailable = false;
        }
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public async Task GitStatus_Runs()
    {
        if (!_gitAvailable) return; // skip if no git
        var json = await _tools.GitStatus(_workspaceId);
        Assert.Contains("exit_code", json);
    }

    [Fact]
    public async Task GitDiff_Runs()
    {
        if (!_gitAvailable) return;
        File.WriteAllText(Path.Combine(_root, "f.txt"), "v2");
        var json = await _tools.GitDiff(_workspaceId);
        Assert.Contains("exit_code", json);
    }

    [Fact]
    public async Task GitLog_Runs()
    {
        if (!_gitAvailable) return;
        var json = await _tools.GitLog(_workspaceId, count: 5);
        Assert.Contains("exit_code", json);
    }
}
