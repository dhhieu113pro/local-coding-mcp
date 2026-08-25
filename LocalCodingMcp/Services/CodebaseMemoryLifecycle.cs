using System.Text.Json;

namespace LocalCodingMcp.Services;

public sealed record CodebaseMemoryWorkspaceState(string State, string? Project, bool RefreshRecommended, string? Message = null);

public sealed class CodebaseMemoryLifecycle
{
    private readonly ICodebaseMemoryClient _client;

    public CodebaseMemoryLifecycle(ICodebaseMemoryClient client) => _client = client;

    public async Task<CodebaseMemoryWorkspaceState> EnsureWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        var status = await _client.GetStatusAsync(cancellationToken);
        if (!status.Available) return new("unavailable", null, false, status.Error);

        var projectsRaw = await _client.CallToolAsync("list_projects", JsonSerializer.SerializeToElement(new { include_details = true }), cancellationToken);
        var projects = ExtractPayload(projectsRaw);
        var project = FindProject(projects, workspacePath);
        if (project is null) return await IndexAsync(workspacePath, cancellationToken);

        var indexRaw = await _client.CallToolAsync("index_status", JsonSerializer.SerializeToElement(new { project }), cancellationToken);
        var index = ExtractPayload(indexRaw);
        if (IsStale(index)) return new("stale", project, true, "Index may be stale; refresh explicitly when needed.");
        return new("ready", project, false);
    }

    public Task<CodebaseMemoryWorkspaceState> RefreshWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
        => IndexAsync(workspacePath, cancellationToken);

    private async Task<CodebaseMemoryWorkspaceState> IndexAsync(string workspacePath, CancellationToken cancellationToken)
    {
        var raw = await _client.CallToolAsync("index_repository", JsonSerializer.SerializeToElement(new { repo_path = workspacePath }), cancellationToken);
        var payload = ExtractPayload(raw);
        var project = GetString(payload, "project") ?? GetString(payload, "name") ?? Path.GetFileName(workspacePath.TrimEnd('/', '\\'));
        return new("indexed", project, false);
    }

    private static string? FindProject(JsonElement payload, string workspacePath)
    {
        if (!payload.TryGetProperty("projects", out var projects) || projects.ValueKind != JsonValueKind.Array) return null;
        var target = Normalize(workspacePath);
        foreach (var item in projects.EnumerateArray())
        {
            var root = GetString(item, "root_path") ?? GetString(item, "root") ?? GetString(item, "path");
            var name = GetString(item, "name") ?? GetString(item, "project");
            if (root is not null && string.Equals(Normalize(root), target, StringComparison.OrdinalIgnoreCase))
                return name ?? Path.GetFileName(target);
        }
        return null;
    }

    private static bool IsStale(JsonElement payload)
    {
        if (payload.TryGetProperty("stale", out var stale) && stale.ValueKind is JsonValueKind.True or JsonValueKind.False) return stale.GetBoolean();
        var status = GetString(payload, "status");
        return string.Equals(status, "stale", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "outdated", StringComparison.OrdinalIgnoreCase);
    }

    private static JsonElement ExtractPayload(string serialized)
    {
        using var outer = JsonDocument.Parse(serialized);
        var root = outer.RootElement;
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in content.EnumerateArray())
            {
                var text = GetString(block, "text");
                if (text is null) continue;
                using var inner = JsonDocument.Parse(text);
                return inner.RootElement.Clone();
            }
        }
        return root.Clone();
    }

    private static string? GetString(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');
}
