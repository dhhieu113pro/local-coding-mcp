# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

It lets the model open a project folder (under **approved roots only**), list/read/write/patch files, search code, run shell commands, and inspect git — all **path-sandboxed**.

| | |
|---|---|
| **Full docs & tool reference** | [LocalCodingMcp/README.md](LocalCodingMcp/README.md) |
| **CI** | [![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml) — Linux · macOS · Windows |
| **Docker** | `ghcr.io/dhhieu113pro/local-coding-mcp:latest` |
| **License** | [MIT](LICENSE) |

```bash
dotnet test LocalCodingMcp.sln -c Release
dotnet run --project LocalCodingMcp
```

---

## Quick guide

1. **Configure** allowed project folders in `LocalCodingMcp/appsettings.json` (`AllowedRoots`).
2. **Run** the server: `dotnet run --project LocalCodingMcp` → MCP at `http://localhost:5000/mcp` (check console for port).
3. **Tunnel** for ChatGPT web (optional): `ngrok http 5000` or Cloudflare Tunnel.
4. **Connect** the MCP URL in ChatGPT Developer Mode (or your MCP client).
5. **Always call `OpenWorkspace` first** with an absolute path under an allowed root → you get a `workspace_id`.
6. Use that `workspace_id` with file / git / shell tools.

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

Full parameters, **example input/output JSON**, and safety model: **[LocalCodingMcp/README.md](LocalCodingMcp/README.md)**.

---

## Safety (short)

- Paths only under **AllowedRoots**
- Blocks path traversal and symlink escapes
- Blocks sensitive names (`.env`, keys, `*.pem`, …)
- Shell commands run with timeout, cwd = workspace root

---

## WSL commands (cheat sheet)

Run these **inside WSL2** (Ubuntu recommended). Docker Desktop must use the **WSL2 backend**.

### Pull & run (public image)

```bash
# Pull latest
docker pull ghcr.io/dhhieu113pro/local-coding-mcp:latest

# Run — mount WSL projects
docker run --rm -d --name local-coding-mcp \
  -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "$HOME/projects":/workspace \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest

# Or mount a Windows folder (C:\Users\...)
docker run --rm -d --name local-coding-mcp \
  -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v /mnt/c/Users/$USER/Projects:/workspace \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

### Useful docker commands

```bash
# Logs
docker logs -f local-coding-mcp

# Health
curl http://localhost:5000/health

# Stop & remove
docker stop local-coding-mcp

# Shell inside container (debug / git clone)
docker exec -it local-coding-mcp bash

# Clone a repo into the mounted workspace
docker exec -it local-coding-mcp \
  git clone https://github.com/org/repo.git /workspace/repo
```

### Compose from WSL

```bash
# In repo root (or any folder with docker-compose.yml)
export MCP_WORKSPACE="$HOME/projects"          # WSL path
# export MCP_WORKSPACE="/mnt/c/Users/$USER/Projects"  # Windows path

docker compose pull
docker compose up -d

docker compose logs -f
docker compose down
```

### Tunnel for ChatGPT (from WSL)

```bash
ngrok http 5000
# or: cloudflared tunnel --url http://localhost:5000
```

Then in ChatGPT → Developer Mode → connector URL: `https://<tunnel-host>/mcp`

### OpenWorkspace path (important)

Use the **container path**, not the host path:

```json
{ "path": "/workspace/my-app" }
```

---

## Run with Docker (WSL / Docker Desktop)

Works well from **WSL2** or Windows with Docker Desktop (WSL2 backend).

### 0. Public image (no build)

CI publishes to **GitHub Container Registry** on every push to `main`:

```bash
docker pull ghcr.io/dhhieu113pro/local-coding-mcp:latest

docker run --rm -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v /home/you/projects:/workspace \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

Or compose (pulls public image by default):

```bash
export MCP_WORKSPACE=/home/you/projects
docker compose pull && docker compose up
```

> **One-time (owner):** after the first successful publish, open the package settings and set **Visibility → Public**:  
> https://github.com/users/dhhieu113pro/packages/container/local-coding-mcp/settings

### 1. Build & run

```bash
# From repo root (in WSL)
docker compose up --build
```

MCP: `http://localhost:5000/mcp`  
Health: `http://localhost:5000/health`

### 2. Mount your real project folder

By default compose mounts `./workspace` → `/workspace` inside the container.

**WSL home projects:**

```bash
export MCP_WORKSPACE=/home/you/projects
docker compose up --build
```

**Windows path via WSL:**

```bash
export MCP_WORKSPACE=/mnt/c/Users/you/Projects
docker compose up --build
```

Or edit the volume in `docker-compose.yml`.

### 3. Open workspace from the model

Call `OpenWorkspace` with a path **inside the container**, e.g.:

```json
{ "path": "/workspace/my-app" }
```

(not the host path).

### One-liner without compose

```bash
docker build -t local-coding-mcp .
docker run --rm -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v /home/you/projects:/workspace \
  local-coding-mcp
```

### Tunnel for ChatGPT web

```bash
# host side (WSL)
ngrok http 5000
# or: cloudflared tunnel --url http://localhost:5000
```

Then point ChatGPT Developer Mode connector to `https://<tunnel>/mcp`.
