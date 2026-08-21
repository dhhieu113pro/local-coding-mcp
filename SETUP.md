# Setup guide — LocalCodingMcp

Step-by-step for **Windows (PowerShell / wslc)** and **WSL / Linux / macOS**.

Two ways to reach ChatGPT:

| Path | Public URL? | When to use |
|------|-------------|-------------|
| **A. OpenAI Secure MCP Tunnel** | No | Preferred for ChatGPT |
| **B. ngrok** | Yes (`https://.../mcp`) | You have an ngrok token / OpenAI tunnel unavailable |

---

## 0. What you need

| Item | Path A | Path B |
|------|--------|--------|
| Docker Desktop | Yes | Yes |
| Code folder | Yes | Yes |
| OpenAI tunnel id + runtime API key | Yes | No |
| ngrok authtoken | No | Yes |

- Tunnels: https://platform.openai.com/settings/organization/tunnels  
- API keys: https://platform.openai.com/settings/organization/api-keys  
- ngrok token: https://dashboard.ngrok.com/get-started/your-authtoken  

---

## 1. Clone & `.env`

```powershell
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
copy .env.example .env
notepad .env
```

```env
MCP_WORKSPACE=D:/wslc/workspaces

# Path A — OpenAI tunnel
CONTROL_PLANE_API_KEY=sk-proj-xxxxxxxx
CONTROL_PLANE_TUNNEL_ID=tunnel_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

# Path B — ngrok
NGROK_AUTHTOKEN=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

CODE_SERVER_PASSWORD=changeme
```

`.env` must sit next to `docker-compose.yml`. Use `D:/path` (forward slashes) on Windows.

---

## Path A — OpenAI Secure MCP Tunnel (preferred)

### Start

```powershell
docker compose pull
docker compose up -d
```

| Container | Port | Role |
|-----------|------|------|
| `local-coding-mcp` | 5000 | MCP |
| `local-coding-mcp-tunnel` | 8080 | OpenAI tunnel-client |

```powershell
curl http://127.0.0.1:5000/health
curl http://127.0.0.1:8080/readyz
# UI: http://127.0.0.1:8080/ui
```

### ChatGPT

1. Developer mode (if needed)  
2. Connectors → MCP app  
3. **Connection → Tunnel** → paste `CONTROL_PLANE_TUNNEL_ID`  
4. Scan tools → new chat  
5. `OpenWorkspace` with `{ "path": "/workspace/my-app" }`

---

## Path B — ngrok + MCP (public HTTPS)

You already have an **ngrok authtoken**. Put it in `.env` as `NGROK_AUTHTOKEN`.

### Start MCP + ngrok only (skip OpenAI tunnel)

```powershell
# MCP
docker compose up -d local-coding-mcp

# ngrok (profile)
docker compose --profile ngrok up -d
```

Or one line after `.env` is filled:

```powershell
docker compose up -d local-coding-mcp
docker compose --profile ngrok up -d ngrok
```

### Get the public URL

```powershell
docker compose logs ngrok
```

Look for a line like:

```text
https://xxxx.ngrok-free.app
```

Or open the inspector: **http://127.0.0.1:4040**

Free plan: one **assigned** domain on your account (stable for that account), not a new random name every restart.

### ChatGPT

1. Developer mode  
2. Connectors → MCP app  
3. **Connection → URL**  
4. Paste: `https://xxxx.ngrok-free.app/mcp`  
   (must include **`/mcp`**)  
5. Scan tools → new chat  
6. `OpenWorkspace` with `{ "path": "/workspace/my-app" }`

### Host ngrok (no Docker) alternative

If MCP is already on port 5000:

```powershell
ngrok config add-authtoken YOUR_TOKEN
ngrok http 5000
```

Then use `https://<host>/mcp` in ChatGPT.

### Stop ngrok only

```powershell
docker compose --profile ngrok stop ngrok
```

---

## Optional: code-server / Termux

```powershell
docker compose --profile ide up -d
# http://127.0.0.1:8443

docker compose --profile termux run --rm termux
```

---

## Optional: GitHub private clone

```powershell
docker exec -it local-coding-mcp `
  git clone https://ghp_YOUR_TOKEN@github.com/org/private-repo.git /workspace/private-repo
```

---

## Manual `wslc` (no Compose)

### MCP

```powershell
wslc network create mcp-net

wslc run --rm -d --name local-coding-mcp `
  --network mcp-net `
  -p 5000:5000 `
  -e AllowedRoots__0=/workspace `
  -v "D:/wslc/workspaces:/workspace" `
  ghcr.io/dhhieu113pro/local-coding-mcp:latest
```

### ngrok container

```powershell
wslc run --rm -d --name local-coding-mcp-ngrok `
  --network mcp-net `
  -e NGROK_AUTHTOKEN="YOUR_TOKEN" `
  -p 4040:4040 `
  ngrok/ngrok:latest http local-coding-mcp:5000 --log=stdout

wslc logs local-coding-mcp-ngrok
```

### OpenAI tunnel container (instead of ngrok)

```powershell
wslc run --rm -d --name local-coding-mcp-tunnel `
  --network mcp-net `
  -p 8080:8080 `
  -e CONTROL_PLANE_API_KEY="sk-..." `
  -e CONTROL_PLANE_TUNNEL_ID="tunnel_..." `
  -e MCP_SERVER_URL="http://local-coding-mcp:5000/mcp" `
  -e HEALTH_LISTEN_ADDR=":8080" `
  -e ALLOW_REMOTE_UI="true" `
  ghcr.io/openai/tunnel-client:latest
```

---

## Secrets cheat sheet

| Secret | Env | Used by |
|--------|-----|---------|
| OpenAI runtime key | `CONTROL_PLANE_API_KEY` | tunnel-client |
| OpenAI tunnel id | `CONTROL_PLANE_TUNNEL_ID` | tunnel-client + ChatGPT Tunnel mode |
| ngrok token | `NGROK_AUTHTOKEN` | ngrok container |
| Host folder | `MCP_WORKSPACE` | volume mount |
| IDE password | `CODE_SERVER_PASSWORD` | code-server |

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Volume / `D:` errors | `-v "D:/path:/workspace"` one string |
| `host.docker.internal` fail | Use `mcp-net` + `local-coding-mcp:5000` |
| ngrok auth failed | Set `NGROK_AUTHTOKEN` in `.env`; recreate container |
| ChatGPT URL mode fails | URL must end with `/mcp` |
| Tunnel UI loopback message | `ALLOW_REMOTE_UI=true` (already in compose) |
| OpenAI `readyz` fail | Check API key permissions (Tunnels Read + Use) |

---

## Related

- [README.md](README.md)  
- [LocalCodingMcp/README.md](LocalCodingMcp/README.md)  
- [OpenAI Secure MCP Tunnels](https://developers.openai.com/api/docs/guides/secure-mcp-tunnels)  
- [ngrok docs](https://ngrok.com/docs)
