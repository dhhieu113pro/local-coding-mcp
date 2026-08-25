using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public sealed class CodebaseMemorySkillInstructionsTests
{
    private static string SkillContent => Assert.Single(
        BuiltInSkillCatalog.All,
        skill => string.Equals(skill.Name, "codebase-memory", StringComparison.Ordinal)).Content;

    [Fact]
    public void Skill_UsesAutomaticWorkspaceLifecycle()
    {
        var content = SkillContent;

        Assert.Contains("open_workspace", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("codebase_memory", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("refresh_codebase_memory_workspace", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stale", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Skill_DoesNotRequireStatusPreflightBeforeEveryUse()
    {
        var content = SkillContent;

        Assert.Contains("diagnostic", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Call `codebase_memory_status` before relying", content, StringComparison.OrdinalIgnoreCase);
    }
}
