namespace LocalCodingMcp.Tests;

public class LandingPageTests
{
    private static string ReadPage()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "docs", "index.html"));
        return File.ReadAllText(path);
    }

    [Fact]
    public void Page_ContainsRequiredProductAndHallmarkContracts()
    {
        var html = ReadPage();

        Assert.Contains("Hallmark · macrostructure: Map / Diagram", html, StringComparison.Ordinal);
        Assert.Contains("Give your AI tools. Keep your code local.", html, StringComparison.Ordinal);
        Assert.Contains("id=\"how-it-works\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"skills\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"install\"", html, StringComparison.Ordinal);
        Assert.Contains("LoadEnabledSkills", html, StringComparison.Ordinal);
        Assert.Contains("superpowers", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hallmark", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("caveman", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ponytail", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AllowedRoots", html, StringComparison.Ordinal);
        Assert.Contains("https://github.com/dhhieu113pro/local-coding-mcp", html, StringComparison.Ordinal);
        Assert.Contains("https://github.com/dhhieu113pro/local-coding-mcp/releases/latest", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_ContainsResponsiveAndThemeContracts()
    {
        var html = ReadPage();

        Assert.Contains("overflow-x:clip", html.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prefers-color-scheme:dark", html.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prefers-reduced-motion:reduce", html.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"theme-toggle\"", html, StringComparison.Ordinal);
        Assert.Contains("focus-visible", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("font-style:italic", html.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
    }
}
