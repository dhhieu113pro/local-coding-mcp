# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

Open a project folder (under approved roots only), list/read/write/patch files, search code, run shell commands, and inspect git — all path-sandboxed.

| | |
|---|---|
| **Tool reference** | [LocalCodingMcp/README.md](LocalCodingMcp/README.md) |
| **CI** | [![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml) |
| **Docker image** | `ghcr.io/dhhieu113pro/local-coding-mcp:latest` |
| **License** | [MIT](LICENSE) |

---

## Quick start (step by step)

Follow these steps in order.

### Step 0 — What you need

| Item | Required? | Where to get it |
|------|-----------|-----------------|
| Docker Desktop (WSL2 backend) | Yes | https://www.docker.com/products/docker-desktop |
| OpenAI **tunnel id** | Yes (for ChatGPT tunnel) | https://platform.openai.com/settings/organization/tunnels |
| OpenAI **runtime API key** | Yes (for ChatGPT tunnel) | https://platform.openai.com/settings/organization/api-keys |
| Folder for your code | Yes | e.g. `D:\wslc\workspaces` or `~/projects` |
| GitHub token (PAT) | Only for **private** repos | https://github.com/settings/tokens |

---

### Step 1 — Create OpenAI tunnel + API key

1. Open **Tunnels**: https://platform.openai.com/settings/organization/tunnels  
   - Create a tunnel (or pick an existing one).  
   - Copy the **tunnel id** (looks like `tunnel_` + 32 hex characters).

2. Open **API keys**: https://platform.openai.com/settings/organization/api-keys  
   - Create a **runtime** key.  
   - The key needs permission: Tunnels **Read** + **Use**.  
   - Copy the key (starts with `sk-...`).

Keep both values private. You will put them in a `.env` file next.

---

### Step 2 — Create `.env` file

In the repo folder (or any folder where you run `docker compose`):

```bash
cp .env.example .env
```

Edit `.env`:

```env
# Required for ChatGPT tunnel
CONTROL_PLANE_TUNNEL_ID=tunnel_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
CONTROL_PLANE_API_KEY=sk-proj-xxxxxxxx

# Your code folder on the host (use forward slashes on Windows)
# Windows example:
MCP_WORKSPACE=D:/wslc/workspaces
# WSL example:
# MCP_WORKSPACE=/home/you/projects

# Optional: browser VS Code password
CODE_SERVER_PASSWORD=changeme

# Optional: only needed to git clone private GitHub repos
# GITHUB_TOKEN=ghp_xxxxxxxx
```

**Windows path rule:** write `D:/wslc/workspaces` (forward slashes), not `D:\wslc\workspaces`.

Do **not** commit `.env`.

---

### Step 3 — Start the containers

From the folder that has `docker-compose.yml` and `.env`:

```bash
docker compose pull
docker compose up -d
```

This starts:

| Container | Port | Role |
|-----------|------|------|
| `local-coding-mcp` | **5000** | MCP server |
| `local-coding-mcp-tunnel` | **8080** | OpenAI tunnel-client |

Check health:

```bash
curl http://127.0.0.1:5000/health
curl http://127.0.0.1:8080/readyz
```

Optional operator UI: http://127.0.0.1:8080/ui

Logs if something fails:

```bash
docker compose logs -f local-coding-mcp
docker compose logs -f tunnel-client
```

Stop everything:

```bash
docker compose down
```

---

### Step 4 — Connect ChatGPT

1. ChatGPT → **Settings** → turn on **Developer mode** (if needed).
2. **Connectors / Plugins** → create / add an MCP app.
3. **Connection** → choose **Tunnel**.
4. Paste the same `CONTROL_PLANE_TUNNEL_ID` from your `.env`.
5. Scan tools → open a **new chat**.

In the chat, first call:

```json
{ "path": "/workspace/my-app" }
```

Use the **container** path (`/workspace/...`), not `D:\...` or `C:\...`.

---

### Step 5 — (Optional) GitHub token for private repos

Public `git clone` needs **no** token.

For **private** repos, create a fine-scoped PAT: https://github.com/settings/tokens  
(Contents: Read is enough for clone.)

**Clone inside the MCP container:**

```bash
# replace TOKEN and the repo URL
docker exec -it local-coding-mcp \
  git clone https://TOKEN@github.com/org/private-repo.git /workspace/private-repo
```

Or set in `.env`:

```env
GITHUB_TOKEN=ghp_xxxxxxxx
```

Then:

```bash
docker exec -it local-coding-mcp bash -c \
  'git clone https://$GITHUB_TOKEN@github.com/org/private-repo.git /workspace/private-repo'
```

(Only works if you also pass `GITHUB_TOKEN` into the MCP service environment — by default compose does not inject it; the one-line `TOKEN@github.com` form is simplest.)

---

### Step 6 — (Optional) Browser IDE (code-server)

Not started by default.

```bash
# ensure CODE_SERVER_PASSWORD is set in .env
docker compose --profile ide up -d
```

Open: **http://127.0.0.1:8443**  
Password = value of `CODE_SERVER_PASSWORD`  
Files = same folder as MCP `/workspace`

---

## Run MCP only (no tunnel)

If you only want the local MCP (e.g. test with ngrok later):

**Windows PowerShell:**

```powershell
docker run --rm -d --name local-coding-mcp `
  -p 5000:5000 `
  -e AllowedRoots__0=/workspace `
  -v "D:/wslc/workspaces:/workspace" `
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

**WSL / Linux / macOS:**

```bash
docker run --rm -d --name local-coding-mcp \
  -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "$HOME/projects:/workspace" \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

Health: `curl http://127.0.0.1:5000/health`

**Volume tip (Windows):** always use one string with forward slashes:

```text
-v "D:/wslc/workspaces:/workspace"
```

Wrong (breaks on `D:`):

```text
-v "D:\wslc\workspaces":/workspace
```

---

## Fallback: public URL (ngrok)

Only if Secure MCP Tunnel is not available on your OpenAI org:

```bash
docker run --rm -d --name local-coding-mcp -p 5000:5000 \
  -e AllowedRoots__0=/workspace \
  -v "D:/wslc/workspaces:/workspace" \
  ghcr.io/dhhieu113pro/local-coding-mcp:latest

ngrok http 5000
```

ChatGPT → Connection → **URL** → `https://<ngrok-host>/mcp`

---

## Secrets cheat sheet

| Secret | Env var | Used by | Required |
|--------|---------|---------|----------|
| OpenAI runtime key | `CONTROL_PLANE_API_KEY` | tunnel-client container | Yes for tunnel |
| OpenAI tunnel id | `CONTROL_PLANE_TUNNEL_ID` | tunnel-client + ChatGPT connector | Yes for tunnel |
| Host code folder | `MCP_WORKSPACE` | compose volume mount | Yes |
| code-server password | `CODE_SERVER_PASSWORD` | code-server (profile `ide`) | Only if using IDE |
| GitHub PAT | (manual in clone URL) | `git clone` private repos | Only if private clone |

Put long-lived values in `.env` next to `docker-compose.yml`.  
Never commit `.env`. Never paste keys into ChatGPT prompts.

---

## Typical tool flow

```text
OpenWorkspace(path)  →  workspace_id
ListDirectory / ReadFile / SearchFiles
WriteFile or ApplyPatch
RunCommand (e.g. tests)
GitStatus / GitDiff / GitLog
```

Full tool params + examples: **[LocalCodingMcp/README.md](LocalCodingMcp/README.md)**

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

---

## Safety

- Paths only under **AllowedRoots** (`/workspace` in Docker)
- Blocks `../`, symlink escape, sensitive names (`.env`, keys, `*.pem`, …)
- Shell has timeout; cwd = workspace root
- Do not expose ports 5000 / 8080 / 8443 to the public internet without protection

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
