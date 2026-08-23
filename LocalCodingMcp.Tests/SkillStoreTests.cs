using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public class SkillStoreTests : IDisposable
{
    private readonly string _root;
    private readonly SkillStore _store;

    public SkillStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "mcp-skills-" + Guid.NewGuid().ToString("N"));
        _store = new SkillStore(_root, seedBuiltIns: false);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void Constructor_CreatesRoot_AndExposesFullPath()
    {
        Assert.True(Directory.Exists(_root));
        Assert.Equal(Path.GetFullPath(_root), _store.RootPath);
    }

    [Fact]
    public void Constructor_EmptyRoot_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SkillStore(" "));
    }

    [Fact]
    public void CreateGetUpdateDelete_RoundTripsSkill()
    {
        var created = _store.Create("dotnet-review", "# v1");

        Assert.Equal("dotnet-review", created.Name);
        Assert.Equal("# v1", created.Content);
        Assert.True(created.Enabled);
        Assert.False(created.BuiltIn);
        Assert.Null(created.SourceUrl);
        Assert.Null(created.License);
        Assert.EndsWith(Path.Combine("dotnet-review", "SKILL.md"), created.Path);
        Assert.True(File.Exists(created.Path));

        var read = _store.Get("dotnet-review");
        Assert.Equal("# v1", read.Content);

        var updated = _store.Update("dotnet-review", "# v2");
        Assert.Equal("# v2", updated.Content);
        Assert.Equal("# v2", File.ReadAllText(updated.Path));

        File.WriteAllText(Path.Combine(Path.GetDirectoryName(updated.Path)!, "asset.txt"), "asset");
        Assert.True(_store.Delete("dotnet-review"));
        Assert.False(Directory.Exists(Path.GetDirectoryName(updated.Path)));
        Assert.False(_store.Delete("dotnet-review"));
    }

    [Fact]
    public void Create_Duplicate_Throws()
    {
        _store.Create("skill", "first");
        Assert.Throws<InvalidOperationException>(() => _store.Create("skill", "second"));
    }

    [Fact]
    public void GetMissing_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => _store.Get("missing"));
    }

    [Fact]
    public void UpdateMissing_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => _store.Update("missing", "content"));
    }

    [Fact]
    public void SetEnabledMissing_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => _store.SetEnabled("missing", true));
    }

    [Fact]
    public void NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _store.Create("create-null", null!));
        _store.Create("update-null", "content");
        Assert.Throws<ArgumentNullException>(() => _store.Update("update-null", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("../escape")]
    [InlineData("bad/name")]
    [InlineData("bad\\name")]
    [InlineData("-starts-with-symbol")]
    public void InvalidNames_Throw(string name)
    {
        Assert.Throws<ArgumentException>(() => _store.Create(name, "content"));
    }

    [Fact]
    public void NameLongerThan64Characters_Throws()
    {
        Assert.Throws<ArgumentException>(() => _store.Create(new string('a', 65), "content"));
    }

    [Fact]
    public void List_ReturnsOnlySkillDirectories_InNameOrder()
    {
        _store.Create("zeta", "z");
        _store.Create("Alpha", "a");
        Directory.CreateDirectory(Path.Combine(_root, "not-a-skill"));
        File.WriteAllText(Path.Combine(_root, "loose.txt"), "ignored");

        var skills = _store.List();

        Assert.Equal(2, skills.Count);
        Assert.Equal("Alpha", skills[0].Name);
        Assert.Equal("zeta", skills[1].Name);
        Assert.All(skills, skill => Assert.EndsWith("SKILL.md", skill.Path));
        Assert.All(skills, skill => Assert.True(skill.Enabled));
    }

    [Fact]
    public void List_WhenRootRemoved_ReturnsEmpty()
    {
        Directory.Delete(_root, true);
        Assert.Empty(_store.List());
    }

    [Fact]
    public void Toggle_PersistsAcrossStoreInstances_AndFiltersEnabledList()
    {
        _store.Create("one", "# one");
        _store.Create("two", "# two");

        var disabled = _store.SetEnabled("one", false);
        Assert.False(disabled.Enabled);
        Assert.Equal("two", Assert.Single(_store.ListEnabled()).Name);

        var reopened = new SkillStore(_root, seedBuiltIns: false);
        Assert.False(reopened.Get("one").Enabled);
        Assert.True(reopened.Get("two").Enabled);

        var enabled = reopened.SetEnabled("one", true);
        Assert.True(enabled.Enabled);
        Assert.Equal(2, reopened.ListEnabled().Count);
    }

    [Fact]
    public void ExistingSkillWithoutMetadata_DefaultsToEnabledCustomSkill()
    {
        var directory = Path.Combine(_root, "legacy");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"), "# legacy");

        var skill = _store.Get("legacy");

        Assert.True(skill.Enabled);
        Assert.False(skill.BuiltIn);
        Assert.Null(skill.SourceUrl);
        Assert.Null(skill.License);
    }

    [Fact]
    public void InvalidMetadata_DefaultsToEnabledCustomSkill()
    {
        var directory = Path.Combine(_root, "broken-meta");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"), "# skill");
        File.WriteAllText(Path.Combine(directory, ".skill.json"), "{not-json");

        var skill = _store.Get("broken-meta");

        Assert.True(skill.Enabled);
        Assert.False(skill.BuiltIn);
    }

    [Fact]
    public void BuiltIns_AreSeededDisabled_WithAttribution_AndCannotBeDeleted()
    {
        var builtInRoot = Path.Combine(Path.GetTempPath(), "mcp-builtins-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SkillStore(builtInRoot);
            var skills = store.List();

            Assert.Equal(new[] { "caveman", "hallmark", "ponytail", "superpowers" }, skills.Select(x => x.Name));
            Assert.All(skills, skill => Assert.False(skill.Enabled));
            Assert.All(skills, skill => Assert.True(skill.BuiltIn));
            Assert.All(skills, skill => Assert.Equal("MIT", skill.License));
            Assert.All(skills, skill => Assert.StartsWith("https://github.com/", skill.SourceUrl));
            Assert.Empty(store.ListEnabled());

            var caveman = store.Get("caveman");
            Assert.Contains("# Caveman", caveman.Content);
            Assert.Throws<InvalidOperationException>(() => store.Delete("caveman"));

            var enabled = store.SetEnabled("hallmark", true);
            Assert.True(enabled.Enabled);
            Assert.Equal("hallmark", Assert.Single(store.ListEnabled()).Name);

            var reopened = new SkillStore(builtInRoot);
            Assert.True(reopened.Get("hallmark").Enabled);
        }
        finally
        {
            try { Directory.Delete(builtInRoot, true); } catch { }
        }
    }
}
