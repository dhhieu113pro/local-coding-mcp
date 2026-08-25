# LocalCodingMcp via DNX

`LocalCodingMcp.Dnx` is the local stdio distribution of LocalCodingMcp. It uses the same tools, services, path sandbox, execution history, and skills implementation as the existing HTTP server.

## Run

Requires the .NET 10 SDK with `dnx`.

```bash
dnx LocalCodingMcp.Dnx --yes
```

Pin a version when reproducibility matters:

```bash
dnx LocalCodingMcp.Dnx@0.1.0 --yes
```

The DNX package uses MCP over **stdio**. It does not open the `/mcp` HTTP endpoint. For Streamable HTTP, Docker, TermuxHost, LAN, or ngrok deployments, continue using the existing `LocalCodingMcp` web host.

## MCP client configuration

```json
{
  "mcpServers": {
    "local-coding": {
      "type": "stdio",
      "command": "dnx",
      "args": ["LocalCodingMcp.Dnx", "--yes"]
    }
  }
}
```

Environment variables use the same configuration keys as the HTTP host. For example:

```text
AllowedRoots__0=/path/to/workspace
ExecutionHistory__FilePath=/path/to/execution-history.jsonl
Skills__Directory=/path/to/skills
```

If `AllowedRoots` is not configured, LocalCodingMcp defaults to the system temporary directory.

## Distribution

Every pull request packages and smoke-tests `LocalCodingMcp.Dnx` through `dnx`. A `v*` tag derives the NuGet version from the tag, validates the package and MCP stdio handshake, then publishes `LocalCodingMcp.Dnx` to NuGet.org using trusted publishing.

The existing TermuxHost ZIP release remains separate and unchanged.
