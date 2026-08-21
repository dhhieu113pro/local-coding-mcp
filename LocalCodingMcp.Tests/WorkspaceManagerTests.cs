using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public class WorkspaceManagerTests : IDisposable
{
    private readonly string _root;
    private readonly PathSandbox _sandbox;
    private readonly WorkspaceManager _manager;

    public WorkspaceManagerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-ws-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        _sandbox = new PathSandbox(new[] { _root });
        _manager = new WorkspaceManager(_sandbox);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void Open_ValidPath_ReturnsId()
    {
        var info = _manager.Open(_root);
        Assert.False(string.IsNullOrEmpty(info.Id));
        Assert.Equal(12, info.Id.Length);
        Assert.True(Directory.Exists(info.RootPath));
    }

    [Fact]
    public void GetRoot_AfterOpen_Succeeds()
    {
        var info = _manager.Open(_root);
        var root = _manager.GetRoot(info.Id);
        Assert.Equal(info.RootPath, root);
    }

    [Fact]
    public void GetRoot_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetRoot("doesnotexist"));
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() => _manager.Get("nope"));
    }

    [Fact]
    public void Open_MissingDirectory_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            _manager.Open(Path.Combine(_root, "missing-dir")));
    }

    [Fact]
    public void Open_OutsideAllowed_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            _manager.Open(Path.GetTempPath()));
    }

    [Fact]
    public void List_ReturnsOpened()
    {
        var info = _manager.Open(_root);
        var list = _manager.List();
        Assert.Contains(list, w => w.Id == info.Id);
    }
}
