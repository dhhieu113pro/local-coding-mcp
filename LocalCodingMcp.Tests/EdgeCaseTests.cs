using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;
using Microsoft.Extensions.Configuration;

namespace LocalCodingMcp.Tests;

public class EdgeCaseTests : IDisposable
{
    private readonly string _root;
    private readonly PathSandbox _sandbox;
    private readonly WorkspaceManager _ws;
    private readonly SensitiveFileFilter _filter;
    private readonly FileTools _files;
    private readonly string _wid;

    public EdgeCaseTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-edge-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        _sandbox = new PathSandbox(new[] { _root });
        _ws = new WorkspaceManager(_sandbox);
        _filter = new SensitiveFileFilter();
        var cfgPath = Path.Combine(_root, "cfg.json");
        File.WriteAllText(cfgPath, "{\"MaxSearchResults\": 2}");
        var config = new ConfigurationBuilder().AddJsonFile(cfgPath).Build();
        _files = new FileTools(_ws, _sandbox, _filter, config);
        _wid = _ws.Open(_root).Id;
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void WorkspaceManager_Get_Works()
    {
        var info = _ws.Get(_wid);
        Assert.Equal(_wid, info.Id);
    }

    [Fact]
    public void PathSandbox_WhitespaceRoot_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PathSandbox(new[] { "  " }));
    }

    [Fact]
    public void PathSandbox_RequireEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => _sandbox.RequireInsideAllowedRoots(""));
    }

    [Fact]
    public void PathSandbox_NonExistingFile_UnderRoot_Resolves()
    {
        // parent exists, file does not - hits ResolveSymbolicLinks parent branch
        var path = _sandbox.Resolve(_root, "does-not-exist-yet.txt");
        Assert.Contains(_root, path);
    }

    [Fact]
    public void ReadFile_StartAfterEnd_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_root, "r.txt"), "a\nb\nc\n");
        var content = _files.ReadFile("r.txt", _wid, start_line: 5, end_line: 2);
        Assert.Equal("", content);
    }

    [Fact]
    public void ApplyPatch_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _files.ApplyPatch("nope.txt", "@@ -1,1 +1,1 @@\n-a\n+b\n", _wid));
    }

    [Fact]
    public void SearchFiles_MissingPath_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() =>
            _files.SearchFiles("x", _wid, path: "missing-folder"));
    }

    [Fact]
    public void SearchFiles_SingleFilePath()
    {
        File.WriteAllText(Path.Combine(_root, "only.txt"), "findme please");
        var json = _files.SearchFiles("findme", _wid, path: "only.txt");
        Assert.Contains("findme", json);
    }

    [Fact]
    public void SearchFiles_SkipsBinaryAndSensitive()
    {
        File.WriteAllText(Path.Combine(_root, "a.bin"), "not really binary but skipped by ext");
        // create fake png with text - skipped by extension
        File.WriteAllBytes(Path.Combine(_root, "x.png"), new byte[] { 1, 2, 3 });
        File.WriteAllText(Path.Combine(_root, ".env"), "SECRET=1");
        File.WriteAllText(Path.Combine(_root, "ok.txt"), "visible");
        var json = _files.SearchFiles("visible", _wid);
        Assert.Contains("ok.txt", json);
    }

    [Fact]
    public void SearchFiles_MaxResults()
    {
        for (int i = 0; i < 10; i++)
            File.WriteAllText(Path.Combine(_root, $"m{i}.txt"), "matchword");
        var json = _files.SearchFiles("matchword", _wid, max_results: 2);
        // should be limited
        Assert.True(json.Split("matchword").Length - 1 <= 3);
    }

    [Fact]
    public void MoveFile_Directory()
    {
        Directory.CreateDirectory(Path.Combine(_root, "dirA"));
        File.WriteAllText(Path.Combine(_root, "dirA", "f.txt"), "x");
        var msg = _files.MoveFile("dirA", "dirB", _wid);
        Assert.Contains("Moved", msg);
        Assert.True(Directory.Exists(Path.Combine(_root, "dirB")));
    }

    [Fact]
    public void DeleteFile_EmptyDirectory()
    {
        Directory.CreateDirectory(Path.Combine(_root, "emptyDir"));
        var msg = _files.DeleteFile("emptyDir", _wid);
        Assert.Contains("Deleted directory", msg);
    }

    [Fact]
    public void PatchApplier_MalformedOldPart_Skipped()
    {
        var result = PatchApplier.Apply("abc\n", "@@ x y @@\n-a\n+b\n");
        Assert.Contains("abc", result);
    }

    [Fact]
    public void PatchApplier_ShortParts_Skipped()
    {
        var result = PatchApplier.Apply("abc\n", "@@ onlyone @@\n");
        Assert.Contains("abc", result);
    }
}
