# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

It lets the model open a project folder (under **approved roots only**), list/read/write/patch files, search code, run shell commands, and inspect git — all **path-sandboxed**.

| | |
|---|---|
| **Full docs & tool reference** | [LocalCodingMcp/README.md](LocalCodingMcp/README.md) |
| **CI** | [![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml) — Linux · macOS · Windows |
| **Docker** | `ghcr.io/dhhieu113pro/local-coding-mcp:latest` |
| **License** | [MIT](LICENSE) |

---

## WSL / Docker commands

Run inside **WSL2** (Docker Desktop → WSL2 backend).

### Pull & run

```bash
docker pull ghcr.io/dhhieu113pro/local-coding-mcp:latest

# Mount WSL projects folder
docker run --rm -d --name local-coding-mcp \
  -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "$HOME/projects":/workspace \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest

# Or mount Windows path (C:\Users\...)
docker run --rm -d --name local-coding-mcp \
  -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v /mnt/c/Users/$USER/Projects:/workspace \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

MCP: `http://localhost:5000/mcp` · Health: `http://localhost:5000/health`

### Useful commands

```bash
docker logs -f local-coding-mcp
curl http://localhost:5000/health
docker stop local-coding-mcp

# Shell / git clone inside container
docker exec -it local-coding-mcp bash
docker exec -it local-coding-mcp \
  git clone https://github.com/org/repo.git /workspace/repo
```

### Compose

```bash
export MCP_WORKSPACE="$HOME/projects"
# export MCP_WORKSPACE="/mnt/c/Users/$USER/Projects"

docker compose pull && docker compose up -d
docker compose logs -f
docker compose down
```

### Tunnel (ChatGPT web)

```bash
ngrok http 5000
# or: cloudflared tunnel --url http://localhost:5000
```

Connector URL: `https://<tunnel-host>/mcp`

### OpenWorkspace path

Use the **container** path:

```json
{ "path": "/workspace/my-app" }
```

> **Owner once:** set package visibility to Public →  
> https://github.com/users/dhhieu113pro/packages/container/local-coding-mcp/settings

---

## Quick guide (dotnet)

```bash
dotnet test LocalCodingMcp.sln -c Release
dotnet run --project LocalCodingMcp
```

1. Configure `AllowedRoots` in `LocalCodingMcp/appsettings.json`.
2. Run server → `http://localhost:5000/mcp`.
3. Optional tunnel: `ngrok http 5000`.
4. Connect MCP URL in ChatGPT Developer Mode / other client.
5. Call **`OpenWorkspace` first** → use returned `workspace_id` for other tools.

### Typical flow

```
OpenWorkspace(path)  →  workspace_id
ListDirectory / ReadFile / SearchFiles
WriteFile or ApplyPatch
RunCommand (e.g. tests)
GitStatus / GitDiff / GitLog
```

---

## Tool list (summary)

| Tool | What it does |
|------|----------------|
| **OpenWorkspace** | Open a project folder under allowed roots → returns `workspace_id` |
| **ListWorkspaces** | List currently open workspaces |
| **GetAllowedRoots** | Show configured allowed root directories |
| **ListDirectory** | List files/dirs relative to workspace |
| **ReadFile** | Read a text file (optional line range) |
| **WriteFile** | Create or overwrite a UTF-8 text file |
| **ApplyPatch** | Apply a unified-diff patch (preferred for edits) |
| **SearchFiles** | Regex/text search across files (skips binaries & sensitive names) |
| **CreateDirectory** | Create directory (and parents) |
| **MoveFile** | Move or rename file/directory |
| **DeleteFile** | Delete a file or empty directory |
| **GitStatus** | `git status` in the workspace |
| **GitDiff** | Unstaged or staged diff |
| **GitLog** | Recent commits (`git log --oneline`) |
| **RunCommand** | Run a shell command inside the workspace (with timeout) |

Full parameters + **example input/output**: **[LocalCodingMcp/README.md](LocalCodingMcp/README.md)**.

---

## Safety (short)

- Paths only under **AllowedRoots**
- Blocks path traversal and symlink escapes
- Blocks sensitive names (`.env`, keys, `*.pem`, …)
- Shell commands run with timeout, cwd = workspace root

---

## Build image locally (optional)

```bash
docker build -t local-coding-mcp .
docker run --rm -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "$HOME/projects":/workspace \
  local-coding-mcp
```
