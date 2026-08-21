# Setup guide — LocalCodingMcp

Step-by-step setup for **Windows (PowerShell / wslc)** and **WSL / Linux / macOS**.

Recommended path: **Docker Compose** (MCP + OpenAI tunnel-client on shared network `mcp-net`).

---

## 0. What you need

| Item | Required? | Where |
|------|-----------|--------|
| Docker Desktop (WSL2 backend on Windows) | Yes | https://www.docker.com/products/docker-desktop |
| OpenAI **tunnel id** | Yes (ChatGPT tunnel) | https://platform.openai.com/settings/organization/tunnels |
| OpenAI **runtime API key** | Yes (ChatGPT tunnel) | https://platform.openai.com/settings/organization/api-keys |
| Code folder on disk | Yes | e.g. `D:\\wslc\\workspaces` or `~/projects` |
| GitHub PAT | Only private repos | https://github.com/settings/tokens |

---

## 1. Get OpenAI tunnel id + API key

1. **Tunnels** → https://platform.openai.com/settings/organization/tunnels  
   - Create or select a tunnel.  
   - Copy **tunnel id** (`tunnel_` + 32 hex chars).

2. **API keys** → https://platform.openai.com/settings/organization/api-keys  
   - Create a **runtime** key with Tunnels **Read** + **Use**.  
   - Copy key (`sk-...`).

Do not paste keys into ChatGPT or commit them.

---

## 2. Clone repo & create `.env`

```powershell
cd D:\wslc
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
copy .env.example .env
notepad .env
```

WSL / Linux / macOS:

```bash
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
cp .env.example .env
nano .env   # or code .env
```

### `.env` contents

```env
CONTROL_PLANE_API_KEY=sk-proj-xxxxxxxx
CONTROL_PLANE_TUNNEL_ID=tunnel_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

# Windows — use forward slashes:
MCP_WORKSPACE=D:/wslc/workspaces

# WSL example:
# MCP_WORKSPACE=/home/you/projects

CODE_SERVER_PASSWORD=changeme
```

**Where is `.env`?** Same folder as `docker-compose.yml` (repo root).

**Windows volume rule:** `D:/wslc/workspaces` not `D:\\wslc\\workspaces`.

---

## 3. Start with Compose (recommended)

From the repo root (folder with `docker-compose.yml` + `.env`):

```powershell
docker compose pull
docker compose up -d
```

| Container | Port | Role |
|-----------|------|------|
| `local-coding-mcp` | 5000 | MCP server |
| `local-coding-mcp-tunnel` | 8080 | OpenAI tunnel-client |

Tunnel reaches MCP as `http://local-coding-mcp:5000/mcp` on network **`mcp-net`** (no `host.docker.internal`).

### Health checks

```powershell
curl http://127.0.0.1:5000/health
curl http://127.0.0.1:8080/readyz
```

Tunnel UI: http://127.0.0.1:8080/ui  
(`ALLOW_REMOTE_UI=true` is set in compose so the UI works via published port.)

### Logs / stop

```powershell
docker compose logs -f
docker compose logs -f local-coding-mcp
docker compose logs -f tunnel-client
docker compose down
```

---

## 4. Connect ChatGPT

1. ChatGPT → **Settings** → **Developer mode** (if needed).
2. **Connectors / Plugins** → add MCP app.
3. **Connection** → **Tunnel**.
4. Paste the same `CONTROL_PLANE_TUNNEL_ID` from `.env`.
5. Scan tools → **new chat**.

First tool call — use **container** path:

```json
{ "path": "/workspace/my-app" }
```

Not `D:\\...` or `C:\\...`.

---

## 5. Optional: GitHub token (private clone)

Public repos: no token.

Private repos — PAT: https://github.com/settings/tokens

```powershell
docker exec -it local-coding-mcp `
  git clone https://ghp_YOUR_TOKEN@github.com/org/private-repo.git /workspace/private-repo
```

---

## 6. Optional: browser IDE (code-server)

```powershell
docker compose --profile ide up -d
```

Open http://127.0.0.1:8443 — password = `CODE_SERVER_PASSWORD` from `.env`.  
Same files as MCP `/workspace`.

---

## 7. Optional: Termux test environment

Use a Termux-like shell in Docker to experiment (e.g. scripts you plan to run on Android Termux). **Not started by default.**

### Interactive shell (recommended for testing)

```powershell
docker compose --profile termux run --rm termux
```

Or one-liner without compose:

```powershell
docker run -it --rm `
  --network mcp-net `
  -v "D:/wslc/workspaces:/workspace" `
  -w /workspace `
  termux/termux-docker:latest
```

### Detached, then attach

```powershell
docker compose --profile termux up -d termux
docker exec -it local-coding-mcp-termux bash
# or: docker attach local-coding-mcp-termux
```

Inside Termux container:

- Your projects are at **`/workspace`** (same mount as MCP).
- On network **`mcp-net`** you can hit MCP at `http://local-coding-mcp:5000/health`.

Example:

```bash
curl http://local-coding-mcp:5000/health
ls /workspace
```

**Note:** `termux/termux-docker` is a **test shell**, not a full Android device. Real Termux on phone still needs its own install; use this profile to try commands in a similar environment.

---

## Alternative: manual `wslc` / `docker run` (no Compose)

### Create network

```powershell
wslc network create mcp-net
```

### MCP

```powershell
wslc run --rm -d --name local-coding-mcp `
  --network mcp-net `
  -p 5000:5000 `
  -e AllowedRoots__0=/workspace `
  -v "D:/wslc/workspaces:/workspace" `
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

### Tunnel

```powershell
wslc run --rm -d --name local-coding-mcp-tunnel `
  --network mcp-net `
  -p 8080:8080 `
  -e CONTROL_PLANE_API_KEY="sk-proj-..." `
  -e CONTROL_PLANE_TUNNEL_ID="tunnel_..." `
  -e MCP_SERVER_URL="http://local-coding-mcp:5000/mcp" `
  -e HEALTH_LISTEN_ADDR=":8080" `
  -e ALLOW_REMOTE_UI="true" `
  ghcr.io/openai/tunnel-client:latest
```

Prefer `http://local-coding-mcp:5000/mcp` on `mcp-net` (not `127.0.0.1` / `host.docker.internal`).

### Termux test (manual)

```powershell
wslc run -it --rm --name termux-test `
  --network mcp-net `
  -v "D:/wslc/workspaces:/workspace" `
  -w /workspace `
  termux/termux-docker:latest
```

### Volume format on Windows

| Good | Bad |
|------|-----|
| `-v "D:/wslc/workspaces:/workspace"` | `-v "D:\\wslc\\workspaces":/workspace` |
| `-v "/mnt/d/wslc/workspaces:/workspace"` | Missing `:/workspace` |

---

## MCP only (no tunnel)

```powershell
docker run --rm -d --name local-coding-mcp `
  -p 5000:5000 `
  -e AllowedRoots__0=/workspace `
  -v "D:/wslc/workspaces:/workspace" `
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

Optional public tunnel: `ngrok http 5000` → ChatGPT Connection **URL**.

---

## Secrets cheat sheet

| Secret | Env var | Goes to |
|--------|---------|---------|
| OpenAI runtime key | `CONTROL_PLANE_API_KEY` | **tunnel-client** only |
| OpenAI tunnel id | `CONTROL_PLANE_TUNNEL_ID` | tunnel-client + ChatGPT |
| Host code folder | `MCP_WORKSPACE` | compose volume |
| IDE password | `CODE_SERVER_PASSWORD` | code-server (profile `ide`) |
| GitHub PAT | in `git clone` URL | MCP container exec |

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `Invalid volume specifications` / `D:` | Use `D:/path:/workspace` as **one** quoted string |
| `lookup host.docker.internal: no such host` | Use compose or `--network mcp-net` + `http://local-coding-mcp:5000/mcp` |
| `admin UI is restricted to loopback` | Set `ALLOW_REMOTE_UI=true` on tunnel-client |
| `oauth discovery failed` / connection refused | MCP not running or wrong `MCP_SERVER_URL` |
| Tunnel `readyz` fails | Check `CONTROL_PLANE_*`; `docker compose logs tunnel-client` |
| ChatGPT cannot see tools | Tunnel id mismatch; tunnel not running; new chat after connect |

---

## Related

- [README.md](README.md) — overview & tools  
- [LocalCodingMcp/README.md](LocalCodingMcp/README.md) — full tool reference  
- [OpenAI Secure MCP Tunnels](https://developers.openai.com/api/docs/guides/secure-mcp-tunnels)  
- [openai/tunnel-client](https://github.com/openai/tunnel-client)  
- [termux/termux-docker](https://hub.docker.com/r/termux/termux-docker)
