using System.ComponentModel;
using System.Text.Json;
using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tools;

[McpServerToolType]
public sealed class GitTools
{
    private readonly WorkspaceManager _workspaces;
    private readonly CommandRunner _runner;

    public GitTools(WorkspaceManager workspaces, CommandRunner runner)
    {
        _workspaces = workspaces;
        _runner = runner;
    }

    [McpServerTool, Description("Show git status of the workspace")]
    public async Task<string> GitStatus(
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var result = await _runner.RunAsync("git status --porcelain=v1 -b", root);

        return JsonSerializer.Serialize(new
        {
            exit_code = result.ExitCode,
            output = result.Stdout,
            error = result.Stderr
        });
    }

    [McpServerTool, Description("Show git diff (unstaged by default)")]
    public async Task<string> GitDiff(
        [Description("Workspace id")] string workspace_id,
        [Description("If true, show staged diff")] bool staged = false)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var cmd = staged ? "git diff --cached" : "git diff";
        var result = await _runner.RunAsync(cmd, root);

        return JsonSerializer.Serialize(new
        {
            exit_code = result.ExitCode,
            diff = result.Stdout,
            error = result.Stderr
        });
    }

    [McpServerTool, Description("Show recent git log")]
    public async Task<string> GitLog(
        [Description("Workspace id")] string workspace_id,
        [Description("Number of commits")] int count = 10)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var result = await _runner.RunAsync($"git log -n {Math.Clamp(count, 1, 50)} --oneline", root);

        return JsonSerializer.Serialize(new
        {
            exit_code = result.ExitCode,
            log = result.Stdout,
            error = result.Stderr
        });
    }
}
