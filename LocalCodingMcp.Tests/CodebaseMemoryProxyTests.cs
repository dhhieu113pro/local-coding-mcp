using System.Text.Json;
using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public sealed class CodebaseMemoryProxyTests
{
    [Fact]
    public async Task Status_ReturnsUnavailableWithoutThrowing()
    {
        var client = new FakeCodebaseMemoryClient { Available = false };
        var tools = new CodebaseMemoryTools(client);

        using var json = JsonDocument.Parse(await tools.CodebaseMemoryStatus());

        Assert.False(json.RootElement.GetProperty("available").GetBoolean());
    }

    [Fact]
    public async Task ListTools_ProxiesRemoteCatalog()
    {
        var client = new FakeCodebaseMemoryClient
        {
            Available = true,
            ToolNames = ["get_architecture", "search_graph"]
        };
        var tools = new CodebaseMemoryTools(client);

        using var json = JsonDocument.Parse(await tools.CodebaseMemoryListTools());

        Assert.Equal(2, json.RootElement.GetArrayLength());
        Assert.Contains(json.RootElement.EnumerateArray(), item => item.GetProperty("name").GetString() == "get_architecture");
    }

    [Fact]
    public async Task Call_RejectsToolNotAdvertisedBySidecar()
    {
        var client = new FakeCodebaseMemoryClient
        {
            Available = true,
            ToolNames = ["get_architecture"]
        };
        var tools = new CodebaseMemoryTools(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tools.CodebaseMemoryCall("run_command", "{}"));
    }

    [Fact]
    public async Task Call_ForwardsJsonArgumentsAndReturnsRemoteResult()
    {
        var client = new FakeCodebaseMemoryClient
        {
            Available = true,
            ToolNames = ["search_graph"],
            Result = "{\"content\":[{\"type\":\"text\",\"text\":\"found\"}]}"
        };
        var tools = new CodebaseMemoryTools(client);

        var result = await tools.CodebaseMemoryCall("search_graph", "{\"query\":\"workspace manager\"}");

        Assert.Equal(client.Result, result);
        Assert.Equal("search_graph", client.LastTool);
        Assert.Equal("workspace manager", client.LastArguments!.Value.GetProperty("query").GetString());
    }

    private sealed class FakeCodebaseMemoryClient : ICodebaseMemoryClient
    {
        public bool Available { get; set; }
        public string[] ToolNames { get; set; } = [];
        public string Result { get; set; } = "{}";
        public string? LastTool { get; private set; }
        public JsonElement? LastArguments { get; private set; }

        public Task<CodebaseMemoryStatus> GetStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new CodebaseMemoryStatus(true, Available, "http://codebase-memory:9750/mcp", Available ? null : "unavailable"));

        public Task<IReadOnlyList<CodebaseMemoryToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CodebaseMemoryToolInfo>>(ToolNames.Select(name => new CodebaseMemoryToolInfo(name, name, null)).ToArray());

        public Task<string> CallToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default)
        {
            LastTool = toolName;
            LastArguments = arguments.Clone();
            return Task.FromResult(Result);
        }
    }
}
