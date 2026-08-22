using System.Text.Json;
using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public sealed class ExecutionHistoryStoreTests : IDisposable
{
    private readonly string _root = TestHelpers.CreateTempRoot();

    [Fact]
    public async Task RecordAndRead_RedactsSensitiveValuesAndPersists()
    {
        var path = Path.Combine(_root, "history.jsonl");
        var store = new ExecutionHistoryStore(path);
        var arguments = new Dictionary<string, JsonElement>
        {
            ["command"] = JsonSerializer.SerializeToElement("dotnet test"),
            ["api_token"] = JsonSerializer.SerializeToElement("do-not-store"),
            ["content"] = JsonSerializer.SerializeToElement("private file body")
        };

        await store.RecordAsync("RunCommand", arguments, true, 42, null);

        var reloaded = new ExecutionHistoryStore(path);
        var entry = Assert.Single(await reloaded.GetRecentAsync());
        Assert.Equal("RunCommand", entry.Tool);
        Assert.True(entry.Success);
        Assert.Equal(42, entry.DurationMs);
        Assert.Equal("dotnet test", entry.Arguments["command"].GetString());
        Assert.Equal("[REDACTED]", entry.Arguments["api_token"].GetString());
        Assert.Equal("[REDACTED]", entry.Arguments["content"].GetString());
        Assert.DoesNotContain("do-not-store", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task GetRecent_FiltersAndReturnsNewestFirst()
    {
        var store = new ExecutionHistoryStore(Path.Combine(_root, "history.jsonl"));
        await store.RecordAsync("ReadFile", null, true, 1, null);
        await store.RecordAsync("RunCommand", null, false, 2, "exit 1");
        await store.RecordAsync("RunCommand", null, true, 3, null);

        var entries = await store.GetRecentAsync(10, "RunCommand", success: true);

        var entry = Assert.Single(entries);
        Assert.Equal(3, entry.DurationMs);
    }

    [Fact]
    public async Task Record_TruncatesLongArguments()
    {
        var store = new ExecutionHistoryStore(Path.Combine(_root, "history.jsonl"), 128);
        var arguments = new Dictionary<string, JsonElement>
        {
            ["command"] = JsonSerializer.SerializeToElement(new string('x', 500))
        };

        await store.RecordAsync("RunCommand", arguments, true, 1, null);

        var entry = Assert.Single(await store.GetRecentAsync());
        var value = entry.Arguments["command"].GetString()!;
        Assert.Contains("[truncated]", value);
        Assert.True(value.Length < 200);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }
}
