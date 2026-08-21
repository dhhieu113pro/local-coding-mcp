<p align="center">
  <img src="docs/logo.svg" alt="LocalCodingMcp logo" width="160" height="160"/>
</p>

# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

Open a project folder (under approved roots only), list/read/write/patch files, search code, run shell commands, and inspect git — all path-sandboxed.

| | |
|---|---|
| **Setup (ngrok / Windows / WSL)** | **[SETUP.md](SETUP.md)** |
| **Tool reference** | [LocalCodingMcp/README.md](LocalCodingMcp/README.md) |
| **CI** | [![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml) |
| **Docker image** | `ghcr.io/dhhieu113pro/local-coding-mcp:latest` |
| **License** | [MIT](LICENSE) |

---

## Quick start

Full guide: **[SETUP.md](SETUP.md)**

```powershell
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
copy .env.example .env
# edit .env → NGROK_AUTHTOKEN, MCP_WORKSPACE=D:/wslc/workspaces

docker compose up -d
docker compose --profile ngrok up -d

curl http://127.0.0.1:5000/health
docker compose logs ngrok
# copy https://xxxx.ngrok-free.app
```

ChatGPT → **Connection → URL** → `https://xxxx.ngrok-free.app/mcp` → new chat → `OpenWorkspace` with `/workspace/...`.

---

## Compose stack

| Service | Profile | Port | Role |
|---------|---------|------|------|
| `local-coding-mcp` | (default) | **5000** | MCP `/mcp` |
| `ngrok` | **`ngrok`** | **4040** inspector | Public HTTPS to MCP |
| `code-server` | **`ide`** | **8443** | Browser VS Code |
| `termux` | **`termux`** | — | Termux-like test shell |

Network: **`mcp-net`**. Secrets: **`.env`** (see `.env.example`).

---

## Typical tool flow

```text
OpenWorkspace(path)  →  workspace_id
ListDirectory / ReadFile / SearchFiles
WriteFile or ApplyPatch
RunCommand
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
| **ReadFile** | Read text file |
| **WriteFile** | Create/overwrite text file |
| **ApplyPatch** | Unified diff |
| **SearchFiles** | Regex/text search |
| **CreateDirectory** | Create directory |
| **MoveFile** | Move/rename |
| **DeleteFile** | Delete file/empty dir |
| **GitStatus** / **GitDiff** / **GitLog** | Git inspect |
| **RunCommand** | Shell in workspace |

Details: **[LocalCodingMcp/README.md](LocalCodingMcp/README.md)**

---

## Branding

- Logo (SVG): [docs/logo.svg](docs/logo.svg)
- ChatGPT connector icon: square **512×512 PNG** export of the logo

---

## Safety

- Paths only under **AllowedRoots** (`/workspace` in Docker)
- Blocks path traversal, symlink escape, sensitive names
- Do not expose 5000 / 8443 / ngrok URL without care

---

## Dev (dotnet)

```bash
dotnet test LocalCodingMcp.sln -c Release
dotnet run --project LocalCodingMcp
```

---

## Build image locally

```bash
docker build -t local-coding-mcp .
docker run --rm -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "D:/wslc/workspaces:/workspace" \
  local-coding-mcp
```
