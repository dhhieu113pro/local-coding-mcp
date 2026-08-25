using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public class WorkspaceToolsTests : IDisposable
{
    private readonly string _root;
    private readonly PathSandbox _sandbox;
    private readonly WorkspaceManager _ws;
    private readonly WorkspaceTools _tools;

    public WorkspaceToolsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-wt-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        _sandbox = new PathSandbox(new[] { _root });
        _ws = new WorkspaceManager(_sandbox);
        var memory = new CodebaseMemoryLifecycle(new CodebaseMemoryClient(false, null, TimeSpan.FromSeconds(1)));
        _tools = new WorkspaceTools(_ws, _sandbox, memory);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public async Task OpenWorkspace_ReturnsId()
    {
        var json = await _tools.OpenWorkspace(_root);
        Assert.Contains("workspace_id", json);
        Assert.Contains("unavailable", json);
    }

    [Fact]
    public async Task ListWorkspaces_AfterOpen()
    {
        await _tools.OpenWorkspace(_root);
        var json = _tools.ListWorkspaces();
        Assert.Contains("workspace_id", json);
    }

    [Fact]
    public void GetAllowedRoots_ReturnsConfigured()
    {
        var json = _tools.GetAllowedRoots();
        Assert.Contains("mcp-wt-", json);
    }
}
