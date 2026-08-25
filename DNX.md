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
Skills__Remote__MaxBytes=1048576
Skills__Remote__TimeoutSeconds=15
Skills__Remote__MaxRedirects=3
```

If `AllowedRoots` is not configured, LocalCodingMcp defaults to the system temporary directory.

## Automatic skill routing

The stdio server advertises the same skill-routing instructions as the HTTP host. For coding, debugging, design, planning, or review tasks, compatible MCP clients are instructed to:

```text
route_skills(task)
load_skills([recommended skill names])
...then use the coding tools
```

`route_skills` considers only enabled skills and returns compact ranking metadata rather than every full `SKILL.md`. `load_skills` then loads complete instructions only for the selected skills. Routing is deterministic and local; custom skills participate through their `name` and front-matter `description:`. `load_enabled_skills` remains available for backward compatibility.

MCP server instructions guide the client/model, but the MCP host ultimately decides whether those instructions are followed.

## Remote skill lifecycle

DNX exposes the same explicit remote-skill tools as the HTTP host:

```text
install_skill(
  source: "https://github.com/owner/repo/blob/main/skills/example/SKILL.md",
  enabled: true
)

check_skill_updates(name: "example")
update_skill_from_source(name: "example")
```

`CreateSkill` is the manual path where the caller supplies complete `SKILL.md` content. `InstallSkill` is different: the server performs an explicit HTTPS fetch, validates front matter, records the original/resolved source plus a SHA-256 content hash, and then stores the skill. GitHub blob URLs are normalized to raw content URLs; direct raw GitHub and generic HTTPS text/Markdown URLs are also supported.

Remote updates are never applied automatically. `check_skill_updates` only compares the installed hash with the current upstream content, while `update_skill_from_source` must be called explicitly. A remotely installed skill receives no extra filesystem or shell privileges; all normal LocalCodingMcp sandbox and tool protections still apply.

## Distribution

Every pull request packages and smoke-tests `LocalCodingMcp.Dnx` through `dnx`. A `v*` tag derives the NuGet version from the tag, validates the package and MCP stdio handshake, then publishes `LocalCodingMcp.Dnx` to NuGet.org using trusted publishing.

The existing TermuxHost ZIP release remains separate and unchanged.
