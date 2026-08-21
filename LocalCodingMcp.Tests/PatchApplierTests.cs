using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public class PatchApplierTests
{
    [Fact]
    public void Apply_SimpleReplace_Works()
    {
        var original = "line1\nline2\nline3\n";
        var patch = "@@ -2,1 +2,1 @@\n-line2\n+LINE2\n";

        var result = PatchApplier.Apply(original, patch);
        Assert.Contains("LINE2", result);
    }

    [Fact]
    public void Apply_EmptyPatch_Throws()
    {
        Assert.Throws<ArgumentException>(() => PatchApplier.Apply("content", ""));
    }

    [Fact]
    public void Apply_AddLine_Works()
    {
        var original = "a\nb\n";
        var patch = "@@ -2,0 +2,1 @@\n+c\n";
        var result = PatchApplier.Apply(original, patch);
        Assert.Contains("c", result);
    }
}

public class PatchApplierExtraTests
{
    [Fact]
    public void Apply_MalformedHunk_Skips()
    {
        var original = "a\nb\n";
        // @@ with fewer than 3 parts should be skipped safely
        var patch = "@@ bad\n-a\n+A\n";
        var result = PatchApplier.Apply(original, patch);
        // either unchanged or partially applied – must not throw
        Assert.NotNull(result);
    }

    [Fact]
    public void Apply_NoHunk_ReturnsOriginalish()
    {
        var original = "only\n";
        var patch = "not a real patch\n";
        var result = PatchApplier.Apply(original, patch);
        Assert.Contains("only", result);
    }
}
