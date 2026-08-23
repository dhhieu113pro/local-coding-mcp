using System.Text.Json;
using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

namespace LocalCodingMcp.Tests;

public class SkillToolsTests : IDisposable
{
    private readonly string _root;
    private readonly SkillStore _store;
    private readonly SkillTools _tools;

    public SkillToolsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-skill-tools-" + Guid.NewGuid().ToString("N"));
        _store = new SkillStore(_root, seedBuiltIns: false);
        _tools = new SkillTools(_store);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void CreateGetListToggleUpdateDelete_ReturnJsonPayloads()
    {
        using var created = JsonDocument.Parse(_tools.CreateSkill("review", "# Review"));
        Assert.True(created.RootElement.GetProperty("created").GetBoolean());
        Assert.Equal("review", created.RootElement.GetProperty("name").GetString());
        Assert.EndsWith("SKILL.md", created.RootElement.GetProperty("path").GetString());
        Assert.True(created.RootElement.GetProperty("enabled").GetBoolean());
        Assert.False(created.RootElement.GetProperty("built_in").GetBoolean());
        Assert.True(created.RootElement.TryGetProperty("modified_at", out _));

        using var read = JsonDocument.Parse(_tools.GetSkill("review"));
        Assert.Equal("# Review", read.RootElement.GetProperty("content").GetString());
        Assert.True(read.RootElement.GetProperty("enabled").GetBoolean());
        Assert.False(read.RootElement.GetProperty("built_in").GetBoolean());
        Assert.Equal(JsonValueKind.Null, read.RootElement.GetProperty("source_url").ValueKind);
        Assert.Equal(JsonValueKind.Null, read.RootElement.GetProperty("license").ValueKind);

        using var list = JsonDocument.Parse(_tools.ListSkills());
        var item = Assert.Single(list.RootElement.EnumerateArray());
        Assert.Equal("review", item.GetProperty("name").GetString());
        Assert.EndsWith("SKILL.md", item.GetProperty("path").GetString());
        Assert.True(item.GetProperty("enabled").GetBoolean());
        Assert.False(item.GetProperty("built_in").GetBoolean());
        Assert.True(item.TryGetProperty("modified_at", out _));

        using var disabled = JsonDocument.Parse(_tools.SetSkillEnabled("review", false));
        Assert.Equal("review", disabled.RootElement.GetProperty("name").GetString());
        Assert.False(disabled.RootElement.GetProperty("enabled").GetBoolean());
        Assert.False(disabled.RootElement.GetProperty("built_in").GetBoolean());

        using var enabledSkillsEmpty = JsonDocument.Parse(_tools.LoadEnabledSkills());
        Assert.Empty(enabledSkillsEmpty.RootElement.EnumerateArray());

        _tools.SetSkillEnabled("review", true);
        using var enabledSkills = JsonDocument.Parse(_tools.LoadEnabledSkills());
        var enabledItem = Assert.Single(enabledSkills.RootElement.EnumerateArray());
        Assert.Equal("review", enabledItem.GetProperty("name").GetString());
        Assert.Equal("# Review", enabledItem.GetProperty("content").GetString());
        Assert.False(enabledItem.GetProperty("built_in").GetBoolean());

        using var updated = JsonDocument.Parse(_tools.UpdateSkill("review", "# Updated"));
        Assert.True(updated.RootElement.GetProperty("updated").GetBoolean());
        Assert.Equal("review", updated.RootElement.GetProperty("name").GetString());
        Assert.True(updated.RootElement.GetProperty("enabled").GetBoolean());
        Assert.False(updated.RootElement.GetProperty("built_in").GetBoolean());

        using var deleted = JsonDocument.Parse(_tools.DeleteSkill("review"));
        Assert.True(deleted.RootElement.GetProperty("deleted").GetBoolean());
        Assert.Equal("review", deleted.RootElement.GetProperty("name").GetString());

        using var missingDelete = JsonDocument.Parse(_tools.DeleteSkill("review"));
        Assert.False(missingDelete.RootElement.GetProperty("deleted").GetBoolean());
    }

    [Fact]
    public void BuiltInSkill_ListAndEnabledPayload_ContainsAttribution()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcp-skill-tools-builtins-" + Guid.NewGuid().ToString("N"));
        try
        {
            var tools = new SkillTools(new SkillStore(root));

            using var list = JsonDocument.Parse(tools.ListSkills());
            Assert.Equal(4, list.RootElement.GetArrayLength());
            var caveman = list.RootElement.EnumerateArray().Single(x => x.GetProperty("name").GetString() == "caveman");
            Assert.False(caveman.GetProperty("enabled").GetBoolean());
            Assert.True(caveman.GetProperty("built_in").GetBoolean());
            Assert.Equal("MIT", caveman.GetProperty("license").GetString());
            Assert.Contains("JuliusBrussee/caveman", caveman.GetProperty("source_url").GetString());

            tools.SetSkillEnabled("caveman", true);
            using var enabled = JsonDocument.Parse(tools.LoadEnabledSkills());
            var active = Assert.Single(enabled.RootElement.EnumerateArray());
            Assert.Equal("caveman", active.GetProperty("name").GetString());
            Assert.True(active.GetProperty("built_in").GetBoolean());
            Assert.Contains("# Caveman", active.GetProperty("content").GetString());
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
