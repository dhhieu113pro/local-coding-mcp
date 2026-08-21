# LocalCodingMcp

A secure **local coding MCP server** written in **C#** with the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) (Model Context Protocol).

It lets **ChatGPT**, **Grok**, and other MCP clients:

- Open a project folder under **approved roots only**
- List / read / write / patch files
- Search code (regex)
- Run shell commands (with timeout)
- Inspect **git** status / diff / log

All paths are **sandboxed**. Sensitive files (`.env`, keys, `*.pem`, …) are blocked.

[![CI](https://github.com/YOUR_USER/LocalCodingMcp/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USER/LocalCodingMcp/actions/workflows/ci.yml)

---

## Requirements

- .NET 10 SDK or later
- `git` on `PATH` (optional, for git tools)

---

## Quick start

```bash
# 1. Edit allowed roots in appsettings.json
# 2. Run
dotnet run --project LocalCodingMcp

# MCP endpoint (port may vary — check console)
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

### Connect to ChatGPT / remote clients

1. Start the server: `dotnet run --project LocalCodingMcp`
2. Expose it with a tunnel (ngrok, Cloudflare Tunnel, Pinggy, …):

```bash
ngrok http 5000
# or: cloudflared tunnel --url http://localhost:5000
```

3. In ChatGPT → **Settings → Developer Mode** → add connector  
   URL: `https://your-tunnel.example.com/mcp`

4. Call `OpenWorkspace` with a path under `AllowedRoots`, then use the returned `workspace_id`.

---

## Tools reference

Tool names follow C# method names (MCP C# SDK default).  
**Always call `OpenWorkspace` first** — almost every other tool needs `workspace_id`.

### Workspace

#### `OpenWorkspace`

Open a project folder and get a `workspace_id` for later calls.

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | Absolute path under an allowed root |

**Example input**

```json
{ "path": "/home/you/projects/my-app" }
```

**Example output**

```json
{
  "workspace_id": "a1b2c3d4e5f6",
  "root": "/home/you/projects/my-app",
  "message": "Workspace opened. Use this workspace_id in subsequent tool calls."
}
```

#### `ListWorkspaces`

List currently open workspaces.

**Example input:** `{}`

**Example output**

```json
[
  {
    "workspace_id": "a1b2c3d4e5f6",
    "root": "/home/you/projects/my-app",
    "opened_at": "2026-08-21T00:00:00+00:00"
  }
]
```

#### `GetAllowedRoots`

Show configured allowed roots from server config.

**Example input:** `{}`

**Example output**

```json
["/home/you/projects", "/tmp/mcp-workspace"]
```

---

### Files

#### `ListDirectory`

List files and directories under a path relative to the workspace.

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | Relative path (use `"."` for root) |
| `workspace_id` | string | yes | From `OpenWorkspace` |

**Example input**

```json
{ "path": "src", "workspace_id": "a1b2c3d4e5f6" }
```

**Example output**

```json
[
  { "name": "Program.cs", "type": "file", "size": 1234 },
  { "name": "Services", "type": "directory", "size": null }
]
```

#### `ReadFile`

Read a text file (optionally a line range).

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | Relative path |
| `workspace_id` | string | yes | Workspace id |
| `start_line` | int | no | 1-based start line |
| `end_line` | int | no | 1-based end line |

**Example input**

```json
{
  "path": "README.md",
  "workspace_id": "a1b2c3d4e5f6",
  "start_line": 1,
  "end_line": 20
}
```

**Example output**

```text
# My App

Local coding helper...
```

#### `WriteFile`

Create or overwrite a UTF-8 text file (creates parent directories).

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | Relative path |
| `content` | string | yes | Full file content |
| `workspace_id` | string | yes | Workspace id |

**Example input**

```json
{
  "path": "src/Hello.cs",
  "content": "Console.WriteLine(\"hi\");\n",
  "workspace_id": "a1b2c3d4e5f6"
}
```

**Example output**

```text
Wrote src/Hello.cs (28 chars)
```

#### `ApplyPatch`

Apply a **unified diff** patch to an existing file (safer than full rewrite).

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | Relative path |
| `patch` | string | yes | Unified diff body |
| `workspace_id` | string | yes | Workspace id |

**Example input**

```json
{
  "path": "src/Hello.cs",
  "patch": "@@ -1,1 +1,1 @@\n-Console.WriteLine(\"hi\");\n+Console.WriteLine(\"hello\");\n",
  "workspace_id": "a1b2c3d4e5f6"
}
```

**Example output**

```text
Patch applied to src/Hello.cs
```

#### `SearchFiles`

Search text/regex across files (skips binaries and blocked sensitive names).

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `query` | string | yes | Text or regex pattern |
| `workspace_id` | string | yes | Workspace id |
| `path` | string | no | Subfolder to search (default `"."`) |
| `max_results` | int | no | Cap results (default from config) |

**Example input**

```json
{
  "query": "OpenWorkspace",
  "workspace_id": "a1b2c3d4e5f6",
  "path": "Tools",
  "max_results": 20
}
```

**Example output**

```json
[
  {
    "file": "Tools/WorkspaceTools.cs",
    "line": 21,
    "text": "public string OpenWorkspace("
  }
]
```

#### `CreateDirectory`

Create a directory (and parents if needed).

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | Relative path |
| `workspace_id` | string | yes | Workspace id |

**Example input**

```json
{ "path": "src/NewFeature", "workspace_id": "a1b2c3d4e5f6" }
```

**Example output**

```text
Created directory src/NewFeature
```

#### `MoveFile`

Move or rename a file or directory.

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `source` | string | yes | Source relative path |
| `destination` | string | yes | Destination relative path |
| `workspace_id` | string | yes | Workspace id |

**Example input**

```json
{
  "source": "old.cs",
  "destination": "src/old.cs",
  "workspace_id": "a1b2c3d4e5f6"
}
```

**Example output**

```text
Moved old.cs → src/old.cs
```

#### `DeleteFile`

Delete a file or an **empty** directory.

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | Relative path |
| `workspace_id` | string | yes | Workspace id |

**Example input**

```json
{ "path": "tmp/scratch.txt", "workspace_id": "a1b2c3d4e5f6" }
```

**Example output**

```text
Deleted file tmp/scratch.txt
```

---

### Git

#### `GitStatus`

Run `git status` in the workspace.

| Param | Type | Required |
|-------|------|----------|
| `workspace_id` | string | yes |

**Example output**

```json
{
  "exit_code": 0,
  "output": "## main\n M src/Hello.cs\n",
  "error": ""
}
```

#### `GitDiff`

Show unstaged (or staged) diff.

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `workspace_id` | string | yes | Workspace id |
| `staged` | bool | no | `true` → `git diff --cached` |

**Example input**

```json
{ "workspace_id": "a1b2c3d4e5f6", "staged": false }
```

**Example output**

```json
{
  "exit_code": 0,
  "diff": "diff --git a/src/Hello.cs b/src/Hello.cs\n...",
  "error": ""
}
```

#### `GitLog`

Recent commits (`git log --oneline`).

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `workspace_id` | string | yes | Workspace id |
| `count` | int | no | Number of commits (default 10, max 50) |

**Example output**

```json
{
  "exit_code": 0,
  "log": "a1b2c3d init\nb2c3d4e add hello\n",
  "error": ""
}
```

---

### Shell

#### `RunCommand`

Run a shell command **inside the workspace** (timeout from config, default 30s).

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `command` | string | yes | Command string |
| `workspace_id` | string | yes | Workspace id |

**Example input**

```json
{ "command": "dotnet build", "workspace_id": "a1b2c3d4e5f6" }
```

**Example output**

```json
{
  "exit_code": 0,
  "stdout": "Build succeeded.\n",
  "stderr": "",
  "duration_ms": 1234.5
}
```

---

## Safety model

| Control | Behavior |
|---------|----------|
| **Allowed roots** | Only paths under configured roots |
| **Path sandbox** | Blocks `../`, absolute escapes, symlink escapes |
| **Sensitive files** | Blocks `.env`, SSH keys, `*.pem` / `*.pfx`, credential JSON, … |
| **Commands** | Timeout (configurable); no unrestricted host access outside workspace cwd |
| **Patches** | Prefer `ApplyPatch` over full `WriteFile` for edits |

---

## Project structure

```
LocalCodingMcp.sln
├── LocalCodingMcp/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Services/
│   │   ├── PathSandbox.cs
│   │   ├── SensitiveFileFilter.cs
│   │   ├── WorkspaceManager.cs
│   │   ├── CommandRunner.cs
│   │   └── PatchApplier.cs
│   └── Tools/
│       ├── WorkspaceTools.cs
│       ├── FileTools.cs
│       ├── GitTools.cs
│       └── ShellTools.cs
├── LocalCodingMcp.Tests/
└── .github/workflows/ci.yml
```

---

## Development

```bash
# Restore + build
dotnet build LocalCodingMcp.sln -c Release

# Run unit tests
dotnet test LocalCodingMcp.sln -c Release

# Tests + coverage (Linux-friendly)
dotnet test LocalCodingMcp.sln -c Release \
  --collect:"XPlat Code Coverage" \
  --settings LocalCodingMcp.Tests/coverlet.runsettings
```

### CI

GitHub Actions (`.github/workflows/ci.yml`) runs on every push/PR:

| Job | Platforms |
|-----|-----------|
| **Test** | `ubuntu-latest`, `macos-latest`, `windows-latest` (.NET 10) |
| **Coverage** | Linux only (after tests pass) |

---

## Notes

- Packages target **ModelContextProtocol 2.2.0** (stable).
- `ApplyPatch` uses a simple unified-diff applier — good for focused edits; large/complex patches may need a stronger library.
- This project exposes **Streamable HTTP** (`/mcp`) for ChatGPT web + tunnel. For pure local stdio (desktop / Grok Build), add a stdio host entry later if needed.

---

MIT License — use and modify freely.
