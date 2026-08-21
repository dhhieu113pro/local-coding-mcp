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

    public WorkspaceTools(WorkspaceManager workspaces, PathSandbox sandbox)
    {
        _workspaces = workspaces;
        _sandbox = sandbox;
    }

    [McpServerTool, Description("Open a project folder and return a workspace_id. All later tools require this id.")]
    public string OpenWorkspace(
        [Description("Absolute path to the project folder (must be under an allowed root)")] string path)
    {
        var info = _workspaces.Open(path);
        return JsonSerializer.Serialize(new
        {
            workspace_id = info.Id,
            root = info.RootPath,
            message = "Workspace opened. Use this workspace_id in subsequent tool calls."
        });
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
