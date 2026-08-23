using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tests;

public class McpServerInstructionsTests
{
    [Fact]
    public void Text_RequiresLoadingSkillsBeforeCodingActions()
    {
        var text = McpServerInstructions.Text;

        Assert.Contains("LoadEnabledSkills", text, StringComparison.Ordinal);
        Assert.Contains("before", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("coding", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("debugging", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("design", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("planning", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("review", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Text_RequiresRelevantSkillsAndSuperpowersPriority()
    {
        var text = McpServerInstructions.Text;

        Assert.Contains("relevant enabled skill", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("superpowers", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("first", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_SetsServerInstructionsOnMcpOptions()
    {
        var options = new McpServerOptions();

        McpServerInstructions.Apply(options);

        Assert.Equal(McpServerInstructions.Text, options.ServerInstructions);
    }
}
