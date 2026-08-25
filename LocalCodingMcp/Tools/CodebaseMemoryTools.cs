using System.ComponentModel;
using System.Text.Json;
using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tools;

[McpServerToolType]
public sealed class CodebaseMemoryTools
{
    private readonly ICodebaseMemoryClient _client;

    public CodebaseMemoryTools(ICodebaseMemoryClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    [McpServerTool, Description("Check whether the optional Codebase Memory sidecar is configured and reachable through LocalCodingMcp.")]
    public async Task<string> CodebaseMemoryStatus(CancellationToken cancellationToken = default)
    {
        var status = await _client.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new
        {
            configured = status.Configured,
            available = status.Available,
            endpoint = status.Endpoint,
            error = status.Error
        });
    }

    [McpServerTool, Description("List tools currently advertised by the configured Codebase Memory MCP sidecar, including each tool's input schema. Use this before codebase_memory_call when the required arguments are uncertain.")]
    public async Task<string> CodebaseMemoryListTools(CancellationToken cancellationToken = default)
    {
        var tools = await _client.ListToolsAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(tools.Select(tool => new
        {
            name = tool.Name,
            description = tool.Description,
            input_schema = ParseSchema(tool.InputSchemaJson)
        }));
    }

    [McpServerTool, Description("Invoke a tool advertised by the Codebase Memory MCP sidecar through LocalCodingMcp. The tool must appear in codebase_memory_list_tools. Pass arguments_json as a JSON object matching that tool's input schema.")]
    public async Task<string> CodebaseMemoryCall(
        [Description("Exact Codebase Memory tool name returned by codebase_memory_list_tools, such as get_architecture, search_graph, trace_path, detect_changes, or check_index_coverage.")] string tool,
        [Description("JSON object containing the remote tool arguments. Defaults to {}.")] string argumentsJson = "{}",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tool);
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentsJson);

        using var document = JsonDocument.Parse(argumentsJson);
        if (document.RootElement.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException("arguments_json must be a JSON object.", nameof(argumentsJson));

        var availableTools = await _client.ListToolsAsync(cancellationToken).ConfigureAwait(false);
        if (!availableTools.Any(candidate => string.Equals(candidate.Name, tool, StringComparison.Ordinal)))
            throw new InvalidOperationException($"Codebase Memory does not advertise tool '{tool}'. Call codebase_memory_list_tools to inspect the current tool catalog.");

        return await _client.CallToolAsync(tool, document.RootElement.Clone(), cancellationToken).ConfigureAwait(false);
    }

    private static object? ParseSchema(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
