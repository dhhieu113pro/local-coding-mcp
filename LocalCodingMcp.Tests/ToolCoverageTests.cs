using System.Text.Json;
using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;
using Microsoft.Extensions.Configuration;

namespace LocalCodingMcp.Tests;

/// <summary>
/// Covers every MCP tool: workspace, file, git, shell.
/// </summary>
public class ToolCoverageTests : IDisposable
{
    private readonly string _root;
    private readonly PathSandbox _sandbox;
    private readonly SensitiveFileFilter _filter;
    private readonly WorkspaceManager _workspaces;
    private readonly CommandRunner _runner;
    private readonly WorkspaceTools _workspaceTools;
    private readonly FileTools _fileTools;
    private readonly GitTools _gitTools;
    private readonly ShellTools _shellTools;
    private readonly string _workspaceId;

    public ToolCoverageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-tools-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        File.WriteAllText(Path.Combine(_root, "src", "main.cs"), "Console.WriteLine(\"hi\");\n");
        File.WriteAllText(Path.Combine(_root, "README.md"), "# Demo\nhello searchme\n");

        // init git for git tools
        RunSilent("git", "init", _root);
        RunSilent("git", "config user.email test@example.com", _root);
        RunSilent("git", "config user.name Test", _root);
        RunSilent("git", "add -A", _root);
        RunSilent("git", "commit -m init", _root);

        _sandbox = new PathSandbox(new[] { _root });
        _filter = new SensitiveFileFilter();
        _workspaces = new WorkspaceManager(_sandbox);
        _runner = new CommandRunner(15);

        var config = TestHelpers.Config(50);

        _workspaceTools = new WorkspaceTools(_workspaces, _sandbox);
        _fileTools = new FileTools(_workspaces, _sandbox, _filter, config);
        _gitTools = new GitTools(_workspaces, _runner);
        _shellTools = new ShellTools(_workspaces, _runner);

        var openResult = _workspaceTools.OpenWorkspace(_root);
        using var doc = JsonDocument.Parse(openResult);
        _workspaceId = doc.RootElement.GetProperty("workspace_id").GetString()!;
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    private static void RunSilent(string file, string args, string cwd)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var p = System.Diagnostics.Process.Start(psi);
        p?.WaitForExit(10000);
    }

    // ── Workspace tools ──────────────────────────────────

    [Fact]
    public void OpenWorkspace_ReturnsId()
    {
        Assert.False(string.IsNullOrEmpty(_workspaceId));
    }

    [Fact]
    public void ListWorkspaces_ContainsOpened()
    {
        var json = _workspaceTools.ListWorkspaces();
        Assert.Contains(_workspaceId, json);
    }

    [Fact]
    public void GetAllowedRoots_ReturnsRoot()
    {
        var json = _workspaceTools.GetAllowedRoots();
        Assert.Contains("mcp-tools-", json);
    }

    [Fact]
    public void OpenWorkspace_OutsideAllowed_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            _workspaceTools.OpenWorkspace("/etc"));
    }

    // ── File tools ───────────────────────────────────────

    [Fact]
    public void ListDirectory_Root_Works()
    {
        var json = _fileTools.ListDirectory(".", _workspaceId);
        Assert.Contains("README.md", json);
        Assert.Contains("src", json);
    }

    [Fact]
    public void ListDirectory_Subdir_Works()
    {
        var json = _fileTools.ListDirectory("src", _workspaceId);
        Assert.Contains("main.cs", json);
    }

    [Fact]
    public void ListDirectory_Missing_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            _fileTools.ListDirectory("nope", _workspaceId));
    }

    [Fact]
    public void ReadFile_Full_Works()
    {
        var content = _fileTools.ReadFile("README.md", _workspaceId);
        Assert.Contains("Demo", content);
        Assert.Contains("searchme", content);
    }

    [Fact]
    public void ReadFile_LineRange_Works()
    {
        var content = _fileTools.ReadFile("README.md", _workspaceId, start_line: 1, end_line: 1);
        Assert.Contains("Demo", content);
    }

    [Fact]
    public void ReadFile_Missing_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            _fileTools.ReadFile("missing.txt", _workspaceId));
    }

    [Fact]
    public void WriteFile_CreatesFile()
    {
        var result = _fileTools.WriteFile("new.txt", "created content", _workspaceId);
        Assert.Contains("Wrote", result);
        var read = _fileTools.ReadFile("new.txt", _workspaceId);
        Assert.Equal("created content", read);
    }

    [Fact]
    public void WriteFile_NestedPath_CreatesDirs()
    {
        _fileTools.WriteFile("deep/nested/file.txt", "nested", _workspaceId);
        Assert.Equal("nested", _fileTools.ReadFile("deep/nested/file.txt", _workspaceId));
    }

    [Fact]
    public void ApplyPatch_ModifiesFile()
    {
        _fileTools.WriteFile("patchme.txt", "alpha\nbeta\ngamma\n", _workspaceId);
        var patch = "@@ -2,1 +2,1 @@\n-beta\n+BETA\n";
        var result = _fileTools.ApplyPatch("patchme.txt", patch, _workspaceId);
        Assert.Contains("applied", result.ToLowerInvariant());
        var content = _fileTools.ReadFile("patchme.txt", _workspaceId);
        Assert.Contains("BETA", content);
    }

    [Fact]
    public void SearchFiles_FindsMatch()
    {
        var json = _fileTools.SearchFiles("searchme", _workspaceId);
        Assert.Contains("README.md", json);
        Assert.Contains("searchme", json);
    }

    [Fact]
    public void SearchFiles_NoMatch_EmptyArray()
    {
        var json = _fileTools.SearchFiles("zzznomatchzzz", _workspaceId);
        Assert.Equal("[]", json.Trim());
    }

    [Fact]
    public void CreateDirectory_Works()
    {
        var result = _fileTools.CreateDirectory("newdir/sub", _workspaceId);
        Assert.Contains("Created", result);
        Assert.True(Directory.Exists(Path.Combine(_root, "newdir", "sub")));
    }

    [Fact]
    public void MoveFile_Works()
    {
        _fileTools.WriteFile("moveme.txt", "x", _workspaceId);
        var result = _fileTools.MoveFile("moveme.txt", "moved.txt", _workspaceId);
        Assert.Contains("Moved", result);
        Assert.Equal("x", _fileTools.ReadFile("moved.txt", _workspaceId));
    }

    [Fact]
    public void DeleteFile_Works()
    {
        _fileTools.WriteFile("deleteme.txt", "bye", _workspaceId);
        var result = _fileTools.DeleteFile("deleteme.txt", _workspaceId);
        Assert.Contains("Deleted", result);
        Assert.ThrowsAny<Exception>(() => _fileTools.ReadFile("deleteme.txt", _workspaceId));
    }

    [Fact]
    public void WriteFile_Sensitive_Throws()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            _fileTools.WriteFile(".env", "SECRET=1", _workspaceId));
    }

    [Fact]
    public void ReadFile_Sensitive_Throws()
    {
        // create outside filter then try read through tool after placing blocked name
        File.WriteAllText(Path.Combine(_root, "id_rsa"), "key");
        Assert.Throws<UnauthorizedAccessException>(() =>
            _fileTools.ReadFile("id_rsa", _workspaceId));
    }

    [Fact]
    public void PathTraversal_ViaRead_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            _fileTools.ReadFile("../outside.txt", _workspaceId));
    }

    // ── Git tools ────────────────────────────────────────

    [Fact]
    public async Task GitStatus_Works()
    {
        var json = await _gitTools.GitStatus(_workspaceId);
        Assert.Contains("exit_code", json);
    }

    [Fact]
    public async Task GitDiff_Works()
    {
        _fileTools.WriteFile("dirty.txt", "dirty", _workspaceId);
        var json = await _gitTools.GitDiff(_workspaceId);
        Assert.Contains("exit_code", json);
    }

    [Fact]
    public async Task GitLog_Works()
    {
        var json = await _gitTools.GitLog(_workspaceId, count: 5);
        Assert.Contains("exit_code", json);
        // after our init commit, log should not be empty on success
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("exit_code").GetInt32());
    }

    // ── Shell tools ──────────────────────────────────────

    [Fact]
    public async Task RunCommand_Echo_Works()
    {
        var json = await _shellTools.RunCommand("echo hello-mcp", _workspaceId);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("exit_code").GetInt32());
        Assert.Contains("hello-mcp", doc.RootElement.GetProperty("stdout").GetString());
    }

    [Fact]
    public async Task RunCommand_FailExitCode()
    {
        var json = await _shellTools.RunCommand("exit 7", _workspaceId);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(7, doc.RootElement.GetProperty("exit_code").GetInt32());
    }

    [Fact]
    public async Task RunCommand_Empty_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _shellTools.RunCommand("  ", _workspaceId));
    }

    // ── Unknown workspace ────────────────────────────────

    [Fact]
    public void UnknownWorkspace_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            _fileTools.ListDirectory(".", "nonexistent"));
    }
}
