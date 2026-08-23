# LocalCodingMcp

A secure **local coding MCP server** written in **C#** with the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) (Model Context Protocol).

It lets **ChatGPT**, **Grok**, and other MCP clients:

- Open a project folder under **approved roots only**
- List / read / write / patch files
- Search code (regex)
- Run shell commands (with timeout)
- Inspect **git** status / diff / log
- Manage reusable local skills, including built-in skill toggles

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
  "AllowedRoots": [
    "C:\\Users\\you\\Projects",
    "D:\\Work\\repos"
  ],
  "CommandTimeoutSeconds": 30,
  "MaxSearchResults": 50,
  "Skills": {
    "Directory": "data/skills"
  },
  "ExecutionHistory": {
    "FilePath": "data/execution-history.jsonl",
    "MaxArgumentLength": 2000,
    "MaxFileSizeMb": 10
  },
  "BlockedFileNames": [
    ".env",
    "id_rsa",
    "*.pem",
    "*.pfx",
    "credentials.json"
  ]
}
```

Linux / macOS:

```json
{
  "AllowedRoots": [
    "/home/you/projects",
    "/tmp/mcp-workspace"
  ]
}
```

### Connect to ChatGPT

Use **ngrok** (or another public HTTPS tunnel) and ChatGPT **Connection → URL**.

See **[SETUP.md](../SETUP.md)**:

```bash
docker compose up -d
docker compose --profile ngrok up -d
# URL: https://xxxx.ngrok-free.app/mcp
```

Or:

```bash
ngrok http 5000
# or: cloudflared tunnel --url http://localhost:5000
```

For coding, debugging, design, planning, or review work, the MCP server instructions tell clients to call `LoadEnabledSkills` before other LocalCodingMcp tools. Then call `OpenWorkspace` with a path under allowed roots (in Docker: `/workspace/...`).

---

## Tools reference

Tool names follow C# method names (MCP C# SDK default).

### Workspace

#### `OpenWorkspace`

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | Absolute path under an allowed root |

**Example input**

```json
{ "path": "/workspace/my-app" }
```

**Example output**

```json
{
  "workspace_id": "a1b2c3d4e5f6",
  "root": "/workspace/my-app",
  "message": "Workspace opened. Use this workspace_id in subsequent tool calls."
}
```

#### `ListWorkspaces` / `GetAllowedRoots`

No required params beyond empty input where applicable.

---

### Files

`ListDirectory`, `ReadFile`, `WriteFile`, `ApplyPatch`, `SearchFiles`, `CreateDirectory`, `MoveFile`, `DeleteFile` — all take `workspace_id` and relative paths.

---

### Git / Shell

`GitStatus`, `GitDiff`, `GitLog`, `RunCommand` — `workspace_id` required; shell runs with timeout in workspace cwd.

---

### Skills

Skills live under `Skills:Directory` (`data/skills` by default). Each skill is stored as `<name>/SKILL.md` with persistent state in `<name>/.skill.json`.

| Tool | Purpose |
|------|---------|
| `LoadEnabledSkills` | Load complete content for active skills before coding/debugging/design/planning/review work |
| `ListSkills` | List all skills with `enabled`, `built_in`, source, and license metadata |
| `GetSkill` | Read one complete `SKILL.md` and its state |
| `SetSkillEnabled` | Enable/disable a skill without deleting it |
| `CreateSkill` | Create an enabled custom skill |
| `UpdateSkill` | Replace an existing `SKILL.md` |
| `DeleteSkill` | Delete custom skills; built-ins must be disabled instead |

Built-ins are seeded automatically and start **disabled**:

- `caveman` — terse/token-efficient technical communication
- `hallmark` — anti-template UI design discipline
- `superpowers` — engineering workflow, testing, debugging, review, verification
- `ponytail` — anti-over-engineering/minimal implementation discipline

Example:

```json
{ "name": "ponytail", "enabled": true }
```

The server advertises MCP initialization instructions requiring `LoadEnabledSkills` before coding, debugging, design, planning, or review work, requiring relevant enabled skills to be applied, and giving `superpowers` process-selection rules priority when enabled. Whether a host/model follows those instructions still depends on the MCP client.

Existing pre-metadata skills remain enabled by default for backward compatibility. Toggle state survives restarts.

---

### Execution history

`GetExecutionHistory` returns persisted calls newest-first. Use `count` (1–500), optional
exact `tool`, and optional `success` filters. Each entry contains its UTC timestamp, tool,
sanitized arguments, success status, duration, and short error message.

History is stored as append-only JSON Lines at `ExecutionHistory:FilePath`. When the active
file reaches `MaxFileSizeMb`, it rotates to `.1`. Argument values longer than
`MaxArgumentLength` are truncated; content, base64, token, password, secret, authorization,
credential, and private-key fields are always replaced with `[REDACTED]`.

---

## Safety model

| Control | Behavior |
|---------|----------|
| **Allowed roots** | Only paths under configured roots |
| **Path sandbox** | Blocks `../`, absolute escapes, symlink escapes |
| **Sensitive files** | Blocks `.env`, SSH keys, `*.pem` / `*.pfx`, credential JSON, … |
| **Commands** | Timeout; cwd = workspace root |
| **Skill names** | Restricted character set; cannot escape the configured skills directory |
| **Built-ins** | Cannot be deleted; disable instead |

---

## Project structure

```
LocalCodingMcp.sln
├── LocalCodingMcp/
├── LocalCodingMcp.Tests/
├── docker-compose.yml
├── SETUP.md
└── .github/workflows/ci.yml
```

---

## Development

```bash
dotnet build LocalCodingMcp.sln -c Release
dotnet test LocalCodingMcp.sln -c Release
```

### CI

| Job | Platforms |
|-----|-----------|
| **Test** | ubuntu, macOS, Windows (.NET 10) |
| **Coverage** | Linux |
| **Docker Publish** | `ghcr.io/dhhieu113pro/local-coding-mcp` on `main` |

---

## Notes

- ModelContextProtocol **2.2.0**
- Streamable HTTP at `/mcp`
- Compose profiles: default MCP, `ngrok`, `ide`, `termux`

---

## License

[MIT](../LICENSE)
