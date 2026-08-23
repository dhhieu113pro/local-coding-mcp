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
        _store = new SkillStore(_root);
        _tools = new SkillTools(_store);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void CreateGetListUpdateDelete_ReturnJsonPayloads()
    {
        using var created = JsonDocument.Parse(_tools.CreateSkill("review", "# Review"));
        Assert.True(created.RootElement.GetProperty("created").GetBoolean());
        Assert.Equal("review", created.RootElement.GetProperty("name").GetString());
        Assert.EndsWith("SKILL.md", created.RootElement.GetProperty("path").GetString());
        Assert.True(created.RootElement.TryGetProperty("modified_at", out _));

        using var read = JsonDocument.Parse(_tools.GetSkill("review"));
        Assert.Equal("# Review", read.RootElement.GetProperty("content").GetString());

        using var list = JsonDocument.Parse(_tools.ListSkills());
        var item = Assert.Single(list.RootElement.EnumerateArray());
        Assert.Equal("review", item.GetProperty("name").GetString());
        Assert.EndsWith("SKILL.md", item.GetProperty("path").GetString());
        Assert.True(item.TryGetProperty("modified_at", out _));

        using var updated = JsonDocument.Parse(_tools.UpdateSkill("review", "# Updated"));
        Assert.True(updated.RootElement.GetProperty("updated").GetBoolean());
        Assert.Equal("review", updated.RootElement.GetProperty("name").GetString());

        using var deleted = JsonDocument.Parse(_tools.DeleteSkill("review"));
        Assert.True(deleted.RootElement.GetProperty("deleted").GetBoolean());
        Assert.Equal("review", deleted.RootElement.GetProperty("name").GetString());

        using var missingDelete = JsonDocument.Parse(_tools.DeleteSkill("review"));
        Assert.False(missingDelete.RootElement.GetProperty("deleted").GetBoolean());
    }
}
