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

### Pull & run (MCP only)

```bash
docker pull ghcr.io/dhhieu113pro/local-coding-mcp:latest

docker run --rm -d --name local-coding-mcp \
  -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "$HOME/projects":/workspace \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

MCP: `http://127.0.0.1:5000/mcp` · Health: `http://127.0.0.1:5000/health`

### Useful commands

```bash
docker logs -f local-coding-mcp
curl http://127.0.0.1:5000/health
docker stop local-coding-mcp

docker exec -it local-coding-mcp bash
docker exec -it local-coding-mcp \
  git clone https://github.com/org/repo.git /workspace/repo
```

### OpenWorkspace path

Use the **container** path:

```json
{ "path": "/workspace/my-app" }
```

> **Owner once:** set package visibility to Public →  
> https://github.com/users/dhhieu113pro/packages/container/local-coding-mcp/settings

---

## Connect to ChatGPT (tunnel-client **in container**)

Preferred path: **no public URL**, no host install. Compose runs:

1. `local-coding-mcp` — your MCP on port 5000  
2. `tunnel-client` — official image `ghcr.io/openai/tunnel-client`, points at the MCP service

```text
ChatGPT / Codex
      →  OpenAI-hosted tunnel endpoint
      ←  outbound HTTPS (tunnel-client container)
      →  http://local-coding-mcp:5000/mcp
```

### 1. Create tunnel + runtime key (OpenAI Platform)

- Tunnels: https://platform.openai.com/settings/organization/tunnels  
- Runtime API keys: https://platform.openai.com/settings/organization/api-keys  

You need:
- `CONTROL_PLANE_TUNNEL_ID` — e.g. `tunnel_` + 32 hex chars  
- `CONTROL_PLANE_API_KEY` — runtime key (Tunnels **Read** + **Use**)

### 2. Start stack (WSL)

```bash
export CONTROL_PLANE_API_KEY="sk-..."
export CONTROL_PLANE_TUNNEL_ID="tunnel_..."
export MCP_WORKSPACE="$HOME/projects"
# export MCP_WORKSPACE="/mnt/c/Users/$USER/Projects"

docker compose pull
docker compose up -d

# MCP health
curl -fsS http://127.0.0.1:5000/health

# tunnel-client operator UI
curl -fsS http://127.0.0.1:8080/healthz
curl -fsS http://127.0.0.1:8080/readyz
# open http://127.0.0.1:8080/ui
```

Logs:

```bash
docker compose logs -f local-coding-mcp
docker compose logs -f tunnel-client
docker compose down
```

### 3. ChatGPT connector

1. ChatGPT → **Settings** → **Developer mode** (if needed)  
2. **Connectors / Plugins** → create MCP app  
3. **Connection → Tunnel** → select / paste your `tunnel_id`  
4. Scan tools → new chat  
5. `OpenWorkspace` with `{ "path": "/workspace/my-app" }`

Guides: [Secure MCP Tunnels](https://developers.openai.com/api/docs/guides/secure-mcp-tunnels) · [tunnel-client](https://github.com/openai/tunnel-client)

### Fallback: public tunnel (ngrok)

Only if Secure MCP Tunnel is unavailable:

```bash
# MCP only
docker run --rm -d --name local-coding-mcp -p 5000:5000 \
  -e AllowedRoots__0=/workspace -v "$HOME/projects":/workspace \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest

ngrok http 5000
```

ChatGPT → **Connection → URL**: `https://<public-host>/mcp`

---

## Quick guide (dotnet)

```bash
dotnet test LocalCodingMcp.sln -c Release
dotnet run --project LocalCodingMcp
```

1. Configure `AllowedRoots` in `LocalCodingMcp/appsettings.json`.
2. Run server → `http://127.0.0.1:5000/mcp`.
3. Prefer **compose + tunnel-client container** (above); else ngrok.
4. Connect in ChatGPT.
5. Call **`OpenWorkspace` first** → use `workspace_id` for other tools.

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
