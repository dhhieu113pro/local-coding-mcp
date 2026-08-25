using System.ComponentModel;
using System.Text.Json;
using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tools;

[McpServerToolType]
public sealed class WorkspaceTools
{
    private readonly WorkspaceManager _workspaces;
    private readonly PathSandbox _sandbox;
    private readonly CodebaseMemoryLifecycle _codebaseMemory;

    public WorkspaceTools(WorkspaceManager workspaces, PathSandbox sandbox, CodebaseMemoryLifecycle codebaseMemory)
    {
        _workspaces = workspaces;
        _sandbox = sandbox;
        _codebaseMemory = codebaseMemory;
    }

    [McpServerTool, Description("Open a project folder and return a workspace_id. When Codebase Memory is available, reuse its healthy index or index a missing workspace; stale indexes are reported but not rebuilt automatically.")]
    public async Task<string> OpenWorkspace(
        [Description("Absolute path to the project folder (must be under an allowed root)")] string path,
        CancellationToken cancellationToken = default)
    {
        var info = _workspaces.Open(path);
        var memory = await _codebaseMemory.EnsureWorkspaceAsync(info.RootPath, cancellationToken);
        return JsonSerializer.Serialize(new
        {
            workspace_id = info.Id,
            root = info.RootPath,
            codebase_memory = memory,
            message = "Workspace opened. Use this workspace_id in subsequent tool calls."
        });
    }

    [McpServerTool, Description("Explicitly refresh the Codebase Memory index for an open workspace. Use this when open_workspace reports a stale index or after a large external change.")]
    public async Task<string> RefreshCodebaseMemoryWorkspace(
        [Description("Workspace id returned by open_workspace")] string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var root = _workspaces.GetRoot(workspaceId);
        var memory = await _codebaseMemory.RefreshWorkspaceAsync(root, cancellationToken);
        return JsonSerializer.Serialize(memory);
    }

    [McpServerTool, Description("List currently open workspaces")]
    public string ListWorkspaces()
    {
        var list = _workspaces.List().Select(w => new
        {
            workspace_id = w.Id,
            root = w.RootPath,
            opened_at = w.OpenedAt
        });
        return JsonSerializer.Serialize(list);
    }

    [McpServerTool, Description("Show configured allowed roots")]
    public string GetAllowedRoots()
    {
        return JsonSerializer.Serialize(_sandbox.AllowedRoots);
    }
}
