# LocalCodingMcp

A secure **local coding MCP server** written in **C#** with the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) (Model Context Protocol).

It lets **ChatGPT**, **Grok**, and other MCP clients:

- Open a project folder under **approved roots only**
- List / read / write / patch files
- Search code (regex)
- Run shell commands (with timeout)
- Inspect **git** status / diff / log

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

Always call `OpenWorkspace` first with a path under allowed roots (in Docker: `/workspace/...`).

---

## Tools reference

Tool names follow C# method names (MCP C# SDK default).  
**Always call `OpenWorkspace` first** — almost every other tool needs `workspace_id`.

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

`ListDirectory`, `ReadFile`, `WriteFile`, `ApplyPatch`, `SearchFiles`, `CreateDirectory`, `MoveFile`, `DeleteFile` — all take `workspace_id` and relative paths. See prior docs examples in git history or call tools from a live server.

---

### Git / Shell

`GitStatus`, `GitDiff`, `GitLog`, `RunCommand` — `workspace_id` required; shell runs with timeout in workspace cwd.

---

## Safety model

| Control | Behavior |
|---------|----------|
| **Allowed roots** | Only paths under configured roots |
| **Path sandbox** | Blocks `../`, absolute escapes, symlink escapes |
| **Sensitive files** | Blocks `.env`, SSH keys, `*.pem` / `*.pfx`, credential JSON, … |
| **Commands** | Timeout; cwd = workspace root |

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
