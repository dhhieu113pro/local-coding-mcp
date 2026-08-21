using System.Collections.Concurrent;

namespace LocalCodingMcp.Services;

public sealed class WorkspaceInfo
{
    public required string Id { get; init; }
    public required string RootPath { get; init; }
    public DateTimeOffset OpenedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Tracks open workspaces (workspace_id → absolute root path).
/// </summary>
public sealed class WorkspaceManager
{
    private readonly PathSandbox _sandbox;
    private readonly ConcurrentDictionary<string, WorkspaceInfo> _workspaces = new();

    public WorkspaceManager(PathSandbox sandbox)
    {
        _sandbox = sandbox;
    }

    public WorkspaceInfo Open(string path)
    {
        var absolute = _sandbox.RequireInsideAllowedRoots(path);

        if (!Directory.Exists(absolute))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        var id = Guid.NewGuid().ToString("N")[..12];
        var info = new WorkspaceInfo
        {
            Id = id,
            RootPath = absolute
        };

        _workspaces[id] = info;
        return info;
    }

    public string GetRoot(string workspaceId)
    {
        if (!_workspaces.TryGetValue(workspaceId, out var info))
            throw new ArgumentException($"Unknown workspace_id: {workspaceId}. Call open_workspace first.");

        return info.RootPath;
    }

    public WorkspaceInfo Get(string workspaceId)
    {
        if (!_workspaces.TryGetValue(workspaceId, out var info))
            throw new ArgumentException($"Unknown workspace_id: {workspaceId}. Call open_workspace first.");

        return info;
    }

    public IReadOnlyCollection<WorkspaceInfo> List() => _workspaces.Values.ToList();
}
