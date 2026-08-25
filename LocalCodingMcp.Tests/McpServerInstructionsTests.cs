using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tests;

public class McpServerInstructionsTests
{
    [Fact]
    public void Text_RequiresRoutingAndSelectiveLoadingBeforeCodingActions()
    {
        var text = McpServerInstructions.Text;

        Assert.Contains("route_skills", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("load_skills", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("before", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("coding", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("debugging", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("design", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("planning", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("review", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Text_RequiresOnlyRoutedEnabledSkillsAndSuperpowersPriority()
    {
        var text = McpServerInstructions.Text;

        Assert.Contains("enabled", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("recommended", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("superpowers", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("first", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("call LoadEnabledSkills before other", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_SetsServerInstructionsOnMcpOptions()
    {
        var options = new McpServerOptions();

        McpServerInstructions.Apply(options);

        Assert.Equal(McpServerInstructions.Text, options.ServerInstructions);
    }
}
