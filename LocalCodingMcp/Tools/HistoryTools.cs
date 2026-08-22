using System.ComponentModel;
using System.Text.Json;
using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tools;

[McpServerToolType]
public sealed class HistoryTools
{
    private readonly ExecutionHistoryStore _history;

    public HistoryTools(ExecutionHistoryStore history) => _history = history;

    [McpServerTool, Description("Return recent MCP tool execution history. Results are newest first and sensitive arguments are redacted.")]
    public async Task<string> GetExecutionHistory(
        [Description("Number of entries to return (1-500)")] int count = 50,
        [Description("Optional exact tool name filter")] string? tool = null,
        [Description("Optional success filter: true for successful calls, false for failed calls")] bool? success = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await _history.GetRecentAsync(count, tool, success, cancellationToken);
        return JsonSerializer.Serialize(new
        {
            count = entries.Count,
            history_file = _history.FilePath,
            entries
        });
    }
}
