# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

Open a project folder (under approved roots only), list/read/write/patch files, search code, run shell commands, and inspect git — all path-sandboxed.

| | |
|---|---|
| **Setup (Windows / WSL / keys / tunnel)** | **[SETUP.md](SETUP.md)** |
| **Tool reference** | [LocalCodingMcp/README.md](LocalCodingMcp/README.md) |
| **CI** | [![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml) |
| **Docker image** | `ghcr.io/dhhieu113pro/local-coding-mcp:latest` |
| **License** | [MIT](LICENSE) |

---

## Quick start

Full guide (API key, tunnel id, `.env`, Compose, ChatGPT, GitHub token, Windows volumes, Termux):

### → **[SETUP.md](SETUP.md)**

```powershell
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
copy .env.example .env
# edit .env → CONTROL_PLANE_API_KEY, CONTROL_PLANE_TUNNEL_ID, MCP_WORKSPACE=D:/wslc/workspaces

docker compose pull
docker compose up -d

curl http://127.0.0.1:5000/health
curl http://127.0.0.1:8080/readyz
# Tunnel UI: http://127.0.0.1:8080/ui
```

ChatGPT → Connection → **Tunnel** → same tunnel id → new chat → `OpenWorkspace` with `/workspace/...`.

---

## Compose stack

Default (`docker compose up -d`):

| Service | Container | Port | Role |
|---------|-----------|------|------|
| `local-coding-mcp` | `local-coding-mcp` | **5000** | MCP server (`/mcp`, `/health`) |
| `tunnel-client` | `local-coding-mcp-tunnel` | **8080** | OpenAI Secure MCP Tunnel (`ALLOW_REMOTE_UI=true`) |

Shared Docker network: **`mcp-net`**. Tunnel calls MCP at `http://local-coding-mcp:5000/mcp`.

Optional profiles:

| Profile | Command | Port | Role |
|---------|---------|------|------|
| `ide` | `docker compose --profile ide up -d` | **8443** | code-server (browser VS Code) |
| `termux` | `docker compose --profile termux run --rm termux` | — | Termux-like test shell |

```powershell
# Browser IDE
docker compose --profile ide up -d
# http://127.0.0.1:8443

# Termux test environment (interactive)
docker compose --profile termux run --rm termux
```

Secrets live in **`.env`** next to `docker-compose.yml` (see `.env.example`). Never commit `.env`.

---

## Typical tool flow

```text
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
| **OpenWorkspace** | Open folder under allowed roots → `workspace_id` |
| **ListWorkspaces** | List open workspaces |
| **GetAllowedRoots** | Show allowed roots |
| **ListDirectory** | List files/dirs |
| **ReadFile** | Read text file (optional line range) |
| **WriteFile** | Create/overwrite text file |
| **ApplyPatch** | Apply unified diff |
| **SearchFiles** | Regex/text search |
| **CreateDirectory** | Create directory |
| **MoveFile** | Move/rename |
| **DeleteFile** | Delete file or empty dir |
| **GitStatus** | `git status` |
| **GitDiff** | Unstaged/staged diff |
| **GitLog** | Recent commits |
| **RunCommand** | Shell command in workspace (timeout) |

Details + examples: **[LocalCodingMcp/README.md](LocalCodingMcp/README.md)**

---

## Safety

- Paths only under **AllowedRoots** (`/workspace` in Docker)
- Blocks path traversal, symlink escape, sensitive names (`.env`, keys, `*.pem`, …)
- Shell timeout; cwd = workspace root
- Do not expose ports 5000 / 8080 / 8443 publicly without protection

---

## Dev (dotnet, no Docker)

```bash
dotnet test LocalCodingMcp.sln -c Release
dotnet run --project LocalCodingMcp
```

Edit `LocalCodingMcp/appsettings.json` → `AllowedRoots`.

Prefer Docker + OpenAI tunnel for ChatGPT: **[SETUP.md](SETUP.md)**.

---

## Build image locally (optional)

```bash
docker build -t local-coding-mcp .
docker run --rm -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "D:/wslc/workspaces:/workspace" \
  local-coding-mcp
```
