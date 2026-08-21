using System.ComponentModel;
using System.Text.Json;
using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tools;

[McpServerToolType]
public sealed class ShellTools
{
    private readonly WorkspaceManager _workspaces;
    private readonly CommandRunner _runner;

    public ShellTools(WorkspaceManager workspaces, CommandRunner runner)
    {
        _workspaces = workspaces;
        _runner = runner;
    }

    [McpServerTool, Description("Run a shell command inside the workspace (with timeout)")]
    public async Task<string> RunCommand(
        [Description("Command to run")] string command,
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var result = await _runner.RunAsync(command, root);

        return JsonSerializer.Serialize(new
        {
            exit_code = result.ExitCode,
            stdout = result.Stdout,
            stderr = result.Stderr,
            duration_ms = result.DurationMs
        });
    }
}
