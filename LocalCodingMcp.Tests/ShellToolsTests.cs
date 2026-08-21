using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public class ShellToolsTests : IDisposable
{
    private readonly string _root;
    private readonly ShellTools _tools;
    private readonly string _workspaceId;

    public ShellToolsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-sh-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        var sandbox = new PathSandbox(new[] { _root });
        var ws = new WorkspaceManager(sandbox);
        var runner = new CommandRunner(15);
        _tools = new ShellTools(ws, runner);
        _workspaceId = ws.Open(_root).Id;
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public async Task RunCommand_Echo()
    {
        var json = await _tools.RunCommand("echo shell-ok", _workspaceId);
        Assert.Contains("shell-ok", json);
        Assert.Contains("exit_code", json);
        Assert.Contains("duration_ms", json);
    }
}
