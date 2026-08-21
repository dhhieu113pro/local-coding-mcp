# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

Open a project folder (under approved roots only), list/read/write/patch files, search code, run shell commands, and inspect git — all path-sandboxed.

| | |
|---|---|
| **Setup (Windows / WSL / tunnel / keys)** | **[SETUP.md](SETUP.md)** |
| **Tool reference** | [LocalCodingMcp/README.md](LocalCodingMcp/README.md) |
| **CI** | [![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml) |
| **Docker image** | `ghcr.io/dhhieu113pro/local-coding-mcp:latest` |
| **License** | [MIT](LICENSE) |

---

## Quick start

Full steps (API key, tunnel id, `.env`, Compose, ChatGPT, GitHub token, Windows volume tips):

### → **[SETUP.md](SETUP.md)**

Short version:

```powershell
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
copy .env.example .env
# edit .env → CONTROL_PLANE_API_KEY, CONTROL_PLANE_TUNNEL_ID, MCP_WORKSPACE=D:/wslc/workspaces

docker compose pull
docker compose up -d

curl http://127.0.0.1:5000/health
curl http://127.0.0.1:8080/readyz
```

ChatGPT → Connection → **Tunnel** → same tunnel id → new chat → `OpenWorkspace` path `/workspace/...`.

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

---

## Build image locally (optional)

```bash
docker build -t local-coding-mcp .
docker run --rm -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "D:/wslc/workspaces:/workspace" \
  local-coding-mcp
```
