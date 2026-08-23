using ModelContextProtocol.Server;

namespace LocalCodingMcp.Services;

public static class McpServerInstructions
{
    public const string Text = """
        This server provides reusable coding skills.

        Before starting coding, debugging, design, planning, or review work with LocalCodingMcp, call LoadEnabledSkills before other LocalCodingMcp tools. Apply every relevant enabled skill to the task. If the superpowers skill is enabled, follow its process-selection rules first, then apply other relevant enabled skills.
        """;

    public static void Apply(McpServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ServerInstructions = Text;
    }
}
