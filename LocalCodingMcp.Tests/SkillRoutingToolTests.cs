using System.Reflection;
using System.Text.Json;
using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public sealed class SkillRoutingToolTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"local-coding-mcp-skill-tools-{Guid.NewGuid():N}");

    [Fact]
    public void RouteSkillsReturnsRankedMetadataWithoutFullContent()
    {
        var store = CreateStore();
        store.SetEnabled("hallmark", true);
        var tools = new SkillTools(store);

        var json = InvokeString(tools, "RouteSkills", "Improve responsive UI design for the settings page");
        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement.EnumerateArray().First();

        Assert.Equal("hallmark", first.GetProperty("name").GetString());
        Assert.True(first.TryGetProperty("description", out _));
        Assert.False(first.TryGetProperty("content", out _));
    }

    [Fact]
    public void LoadSkillsReturnsOnlyRequestedEnabledSkills()
    {
        var store = CreateStore();
        store.SetEnabled("hallmark", true);
        store.SetEnabled("superpowers", true);
        var tools = new SkillTools(store);

        var json = InvokeString(tools, "LoadSkills", new[] { "hallmark" });
        using var doc = JsonDocument.Parse(json);
        var names = doc.RootElement.EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToArray();

        Assert.Equal(["hallmark"], names);
    }

    [Fact]
    public void ServerInstructionsUseRoutingBeforeCodingTools()
    {
        Assert.Contains("route_skills", McpServerInstructions.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("load_skills", McpServerInstructions.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("call LoadEnabledSkills before other", McpServerInstructions.Text, StringComparison.OrdinalIgnoreCase);
    }

    private SkillStore CreateStore()
    {
        Directory.CreateDirectory(_root);
        return new SkillStore(_root);
    }

    private static string InvokeString(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
        return Assert.IsType<string>(method!.Invoke(target, args));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
