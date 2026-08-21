using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;
using Microsoft.Extensions.Configuration;

namespace LocalCodingMcp.Tests;

public class FileToolsTests : IDisposable
{
    private readonly string _root;
    private readonly PathSandbox _sandbox;
    private readonly WorkspaceManager _ws;
    private readonly SensitiveFileFilter _filter;
    private readonly FileTools _tools;
    private readonly string _workspaceId;

    public FileToolsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-ft-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "hello.txt"), "hello world\nline2\nline3\n");
        Directory.CreateDirectory(Path.Combine(_root, "subdir"));

        _sandbox = new PathSandbox(new[] { _root });
        _ws = new WorkspaceManager(_sandbox);
        _filter = new SensitiveFileFilter();
        // Write temp appsettings for config
        var cfgPath = Path.Combine(_root, "testsettings.json");
        File.WriteAllText(cfgPath, "{\"MaxSearchResults\": 50}");
        var config = new ConfigurationBuilder()
            .AddJsonFile(cfgPath, optional: false)
            .Build();
        _tools = new FileTools(_ws, _sandbox, _filter, config);
        _workspaceId = _ws.Open(_root).Id;
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void ListDirectory_Root()
    {
        var json = _tools.ListDirectory(".", _workspaceId);
        Assert.Contains("hello.txt", json);
        Assert.Contains("subdir", json);
    }

    [Fact]
    public void ListDirectory_Missing_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            _tools.ListDirectory("no-such-dir", _workspaceId));
    }

    [Fact]
    public void ReadFile_Full()
    {
        var content = _tools.ReadFile("hello.txt", _workspaceId);
        Assert.Contains("hello world", content);
        Assert.Contains("line2", content);
    }

    [Fact]
    public void ReadFile_LineRange()
    {
        var content = _tools.ReadFile("hello.txt", _workspaceId, start_line: 2, end_line: 2);
        Assert.Contains("line2", content);
        Assert.DoesNotContain("hello world", content);
    }

    [Fact]
    public void ReadFile_Missing_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _tools.ReadFile("missing.txt", _workspaceId));
    }

    [Fact]
    public void WriteFile_Creates()
    {
        var msg = _tools.WriteFile("new.txt", "content here", _workspaceId);
        Assert.Contains("Wrote", msg);
        Assert.Equal("content here", File.ReadAllText(Path.Combine(_root, "new.txt")));
    }

    [Fact]
    public void WriteFile_NestedCreatesDirs()
    {
        _tools.WriteFile("deep/nested/file.txt", "x", _workspaceId);
        Assert.True(File.Exists(Path.Combine(_root, "deep", "nested", "file.txt")));
    }

    [Fact]
    public void ApplyPatch_UpdatesFile()
    {
        var patch = """
            @@ -1,1 +1,1 @@
            -hello world
            +hello patched
            """;
        var msg = _tools.ApplyPatch("hello.txt", patch, _workspaceId);
        Assert.Contains("Patch applied", msg);
        var text = File.ReadAllText(Path.Combine(_root, "hello.txt"));
        Assert.Contains("hello patched", text);
    }

    [Fact]
    public void SearchFiles_FindsMatch()
    {
        var json = _tools.SearchFiles("hello", _workspaceId);
        Assert.Contains("hello.txt", json);
        Assert.Contains("hello world", json);
    }

    [Fact]
    public void CreateDirectory_Works()
    {
        var msg = _tools.CreateDirectory("brand-new", _workspaceId);
        Assert.Contains("Created", msg);
        Assert.True(Directory.Exists(Path.Combine(_root, "brand-new")));
    }

    [Fact]
    public void MoveFile_Works()
    {
        _tools.WriteFile("src.txt", "data", _workspaceId);
        var msg = _tools.MoveFile("src.txt", "dst.txt", _workspaceId);
        Assert.Contains("Moved", msg);
        Assert.False(File.Exists(Path.Combine(_root, "src.txt")));
        Assert.True(File.Exists(Path.Combine(_root, "dst.txt")));
    }

    [Fact]
    public void DeleteFile_Works()
    {
        _tools.WriteFile("todelete.txt", "x", _workspaceId);
        var msg = _tools.DeleteFile("todelete.txt", _workspaceId);
        Assert.Contains("Deleted", msg);
        Assert.False(File.Exists(Path.Combine(_root, "todelete.txt")));
    }

    [Fact]
    public void DeleteFile_Missing_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _tools.DeleteFile("ghost.txt", _workspaceId));
    }

    [Fact]
    public void WriteFile_Sensitive_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            _tools.WriteFile(".env", "SECRET=1", _workspaceId));
    }

    [Fact]
    public void ReadFile_Sensitive_Throws()
    {
        File.WriteAllText(Path.Combine(_root, ".env"), "x");
        Assert.Throws<UnauthorizedAccessException>(() =>
            _tools.ReadFile(".env", _workspaceId));
    }
}
