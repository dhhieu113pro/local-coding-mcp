using ModelContextProtocol.Server;

namespace LocalCodingMcp.Services;

public static class McpServerInstructions
{
    public const string Text = """
        This server provides reusable coding skills.

        Before coding, debugging, design, planning, or review work with LocalCodingMcp, call route_skills with the user's current task before other LocalCodingMcp coding tools. Then call load_skills with only the recommended enabled skill names and follow those skill instructions. Do not load every enabled skill unless the user explicitly asks for all skills or routing cannot determine relevance. If superpowers is recommended, follow its process-selection rules first, then apply other routed domain-specific skills.
        """;

    public static void Apply(McpServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ServerInstructions = Text;
    }
}
