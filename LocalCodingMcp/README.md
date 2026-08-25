# LocalCodingMcp

A secure **local coding MCP server** written in **C#** with the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) (Model Context Protocol).

It lets **ChatGPT**, **Grok**, and other MCP clients:

- Open a project folder under **approved roots only**
- List / read / write / patch files
- Search code (regex)
- Run shell commands (with timeout)
- Inspect **git** status / diff / log
- Manage reusable local skills, including built-in toggles, automatic routing, and explicit remote installation/update checks

All paths are **sandboxed**. Sensitive files (`.env`, keys, `*.pem`, …) are blocked.

[![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml)

**Docker / Windows / ngrok setup:** see root **[SETUP.md](../SETUP.md)** and **[README.md](../README.md)**.

---

## Requirements

- .NET 10 SDK or later
- `git` on `PATH` (optional, for git tools)

---

## Quick start (dotnet)

```bash
# 1. Edit allowed roots in appsettings.json
# 2. Run
dotnet run --project LocalCodingMcp

# MCP endpoint (check console for port)
# http://localhost:5000/mcp
```

### Example `appsettings.json`

```json
{
  "AllowedRoots": ["D:\\Work\\repos"],
  "CommandTimeoutSeconds": 30,
  "Skills": {
    "Directory": "data/skills",
    "Remote": {
      "MaxBytes": 1048576,
      "TimeoutSeconds": 15,
      "MaxRedirects": 3
    }
  },
  "ExecutionHistory": {
    "FilePath": "data/execution-history.jsonl",
    "MaxArgumentLength": 2000,
    "MaxFileSizeMb": 10
  }
}
```

### Connect to ChatGPT

Use **ngrok** (or another public HTTPS tunnel) and ChatGPT **Connection → URL**. See **[SETUP.md](../SETUP.md)**.

For coding, debugging, design, planning, or review work, the MCP server instructions tell clients to call `route_skills(task)`, then `load_skills` only for the recommendations, before using other LocalCodingMcp tools.

---

## Tools reference

The MCP C# SDK derives snake-case wire names from the C# methods, e.g. `RouteSkills` → `route_skills`.

### Workspace

`open_workspace`, `list_workspaces`, `get_allowed_roots`.

### Files

`list_directory`, `read_file`, `write_file`, `write_binary_file`, `read_binary_file`, `apply_patch`, `search_files`, `create_directory`, `move_file`, `delete_file`.

### Git / Shell

`git_status`, `git_diff`, `git_log`, `run_command`.

### Skills

Skills live under `Skills:Directory` (`data/skills` by default). Each skill is stored as `<name>/SKILL.md` with persistent state/provenance in `<name>/.skill.json`.

| Wire tool | Purpose |
|------|---------|
| `route_skills` | Rank relevant enabled skills without loading full instructions |
| `load_skills` | Load complete content only for selected enabled skills |
| `load_enabled_skills` | Backward-compatible full load of all enabled skills |
| `list_skills` | List skills with enabled/built-in/source/hash metadata |
| `get_skill` | Read one complete `SKILL.md` and its state |
| `set_skill_enabled` | Enable/disable a skill without deleting it |
| `create_skill` | Create a local skill from caller-supplied content; no network request |
| `update_skill` | Manually replace an existing `SKILL.md` |
| `install_skill` | Explicitly fetch, validate, hash, and install a remote HTTPS skill |
| `check_skill_updates` | Compare remote skills with upstream by SHA-256 without applying updates |
| `update_skill_from_source` | Explicitly refresh a remote skill from its recorded source |
| `delete_skill` | Delete custom skills; built-ins must be disabled instead |

Remote example:

```text
install_skill(
  source: "https://github.com/owner/repo/blob/main/skills/example/SKILL.md",
  enabled: true
)
check_skill_updates(name: "example")
update_skill_from_source(name: "example")
```

Remote sources are HTTPS-only. GitHub blob URLs are normalized to raw content; raw GitHub and generic direct HTTPS text/Markdown URLs are accepted. The server rejects embedded credentials, non-HTTPS redirect destinations, oversized/binary/empty responses, missing required `name`/`description` front matter, and name mismatches.

Provenance includes the original source URL, resolved fetch URL, SHA-256 content hash, optional ETag/Last-Modified, install/update timestamps, and optional license. Existing local `.skill.json` files remain compatible because the new fields are optional.

`check_skill_updates` never mutates a skill. `update_skill_from_source` is always explicit and preserves the installed skill's enabled state. Failed fetch or validation must leave the current skill unchanged. Remote instructions do not receive additional filesystem or shell privileges.

Built-ins are seeded automatically and start **disabled**:

- `caveman` — terse/token-efficient technical communication
- `hallmark` — anti-template UI design discipline
- `superpowers` — engineering workflow, testing, debugging, review, verification
- `ponytail` — anti-over-engineering/minimal implementation discipline

Custom and remotely installed enabled skills automatically participate in `route_skills` through their name/front-matter `description:`.

---

### Execution history

`get_execution_history` returns persisted calls newest-first. Sensitive content/token/password/secret fields are redacted.

---

## Safety model

| Control | Behavior |
|---------|----------|
| **Allowed roots** | Only paths under configured roots |
| **Path sandbox** | Blocks `../`, absolute escapes, symlink escapes |
| **Sensitive files** | Blocks `.env`, SSH keys, `*.pem` / `*.pfx`, credential JSON, … |
| **Commands** | Timeout; cwd = workspace root |
| **Skill names** | Restricted character set; cannot escape the configured skills directory |
| **Remote skills** | HTTPS only, bounded text downloads, explicit install/update, provenance/hash persisted |
| **Built-ins** | Cannot be deleted or remotely refreshed; disable instead |

---

## Development

```bash
dotnet build LocalCodingMcp.sln -c Release
dotnet test LocalCodingMcp.sln -c Release
```

## Notes

- ModelContextProtocol **2.2.0**
- Streamable HTTP at `/mcp`
- DNX package uses stdio
- Compose profiles: default MCP, `ngrok`, `ide`, `termux`

## License

[MIT](../LICENSE)
