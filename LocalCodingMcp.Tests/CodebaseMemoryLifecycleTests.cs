using System.Text.Json;
using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public sealed class CodebaseMemoryLifecycleTests
{
    [Fact]
    public async Task EnsureWorkspaceAsync_ReturnsUnavailable_WhenSidecarIsDown()
    {
        var client = new FakeClient(status: new(false, false, "http://cbm/mcp", "down"));
        var lifecycle = new CodebaseMemoryLifecycle(client);

        var result = await lifecycle.EnsureWorkspaceAsync("/workspace/app");

        Assert.Equal("unavailable", result.State);
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task EnsureWorkspaceAsync_ReusesExistingHealthyIndex()
    {
        var client = new FakeClient();
        client.Results["list_projects"] = ToolText("{\"projects\":[{\"name\":\"app\",\"root_path\":\"/workspace/app\"}]}");
        client.Results["index_status"] = ToolText("{\"project\":\"app\",\"status\":\"ready\",\"stale\":false}");
        var lifecycle = new CodebaseMemoryLifecycle(client);

        var result = await lifecycle.EnsureWorkspaceAsync("/workspace/app");

        Assert.Equal("ready", result.State);
        Assert.Equal("app", result.Project);
        Assert.DoesNotContain(client.Calls, call => call.Tool == "index_repository");
    }

    [Fact]
    public async Task EnsureWorkspaceAsync_IndexesWorkspace_WhenMissing()
    {
        var client = new FakeClient();
        client.Results["list_projects"] = ToolText("{\"projects\":[]}");
        client.Results["index_repository"] = ToolText("{\"project\":\"app\",\"status\":\"indexed\"}");
        var lifecycle = new CodebaseMemoryLifecycle(client);

        var result = await lifecycle.EnsureWorkspaceAsync("/workspace/app");

        Assert.Equal("indexed", result.State);
        var call = Assert.Single(client.Calls.Where(call => call.Tool == "index_repository"));
        Assert.Equal("/workspace/app", call.Arguments.GetProperty("repo_path").GetString());
    }

    [Fact]
    public async Task EnsureWorkspaceAsync_DoesNotAutoReindex_WhenIndexIsStale()
    {
        var client = new FakeClient();
        client.Results["list_projects"] = ToolText("{\"projects\":[{\"name\":\"app\",\"root_path\":\"/workspace/app\"}]}");
        client.Results["index_status"] = ToolText("{\"project\":\"app\",\"status\":\"ready\",\"stale\":true}");
        var lifecycle = new CodebaseMemoryLifecycle(client);

        var result = await lifecycle.EnsureWorkspaceAsync("/workspace/app");

        Assert.Equal("stale", result.State);
        Assert.True(result.RefreshRecommended);
        Assert.DoesNotContain(client.Calls, call => call.Tool == "index_repository");
    }

    [Fact]
    public async Task RefreshWorkspaceAsync_ExplicitlyReindexes()
    {
        var client = new FakeClient();
        client.Results["index_repository"] = ToolText("{\"project\":\"app\",\"status\":\"indexed\"}");
        var lifecycle = new CodebaseMemoryLifecycle(client);

        var result = await lifecycle.RefreshWorkspaceAsync("/workspace/app");

        Assert.Equal("indexed", result.State);
        Assert.Single(client.Calls.Where(call => call.Tool == "index_repository"));
    }

    private static string ToolText(string text) => JsonSerializer.Serialize(new
    {
        content = new[] { new { type = "text", text } },
        isError = false
    });

    private sealed class FakeClient : ICodebaseMemoryClient
    {
        private readonly CodebaseMemoryStatus _status;
        public Dictionary<string, string> Results { get; } = new(StringComparer.Ordinal);
        public List<(string Tool, JsonElement Arguments)> Calls { get; } = [];

        public FakeClient(CodebaseMemoryStatus? status = null)
        {
            _status = status ?? new(true, true, "http://cbm/mcp", null);
        }

        public Task<CodebaseMemoryStatus> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(_status);

        public Task<IReadOnlyList<CodebaseMemoryToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CodebaseMemoryToolInfo>>([]);

        public Task<string> CallToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default)
        {
            Calls.Add((toolName, arguments.Clone()));
            return Task.FromResult(Results.TryGetValue(toolName, out var result) ? result : ToolText("{}"));
        }
    }
}
