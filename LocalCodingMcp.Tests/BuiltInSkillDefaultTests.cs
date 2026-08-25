using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public sealed class BuiltInSkillDefaultTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mcp-skill-default-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void ApplyBuiltInDefault_EnablesUntouchedBuiltIn()
    {
        var store = new SkillStore(_root);

        store.ApplyBuiltInDefault("codebase-memory", enabled: true);

        Assert.True(store.Get("codebase-memory").Enabled);
    }

    [Fact]
    public void ApplyBuiltInDefault_DoesNotOverrideExplicitUserDisable()
    {
        var store = new SkillStore(_root);
        store.SetEnabledFromUser("codebase-memory", false);

        store.ApplyBuiltInDefault("codebase-memory", enabled: true);

        Assert.False(store.Get("codebase-memory").Enabled);
    }

    [Fact]
    public void ApplyBuiltInDefault_DoesNotOverrideExplicitUserEnable()
    {
        var store = new SkillStore(_root);
        store.SetEnabledFromUser("codebase-memory", true);

        store.ApplyBuiltInDefault("codebase-memory", enabled: false);

        Assert.True(store.Get("codebase-memory").Enabled);
    }

    [Fact]
    public void ExplicitChoice_PersistsAcrossStoreInstances()
    {
        var store = new SkillStore(_root);
        store.SetEnabledFromUser("codebase-memory", false);

        var reopened = new SkillStore(_root);
        reopened.ApplyBuiltInDefault("codebase-memory", enabled: true);

        Assert.False(reopened.Get("codebase-memory").Enabled);
    }

    [Fact]
    public void LegacyEnabledBuiltIn_IsTreatedAsExistingUserChoice()
    {
        var store = new SkillStore(_root);
        store.SetEnabled("codebase-memory", true);

        var reopened = new SkillStore(_root);
        reopened.ApplyBuiltInDefault("codebase-memory", enabled: false);

        Assert.True(reopened.Get("codebase-memory").Enabled);
    }
}
