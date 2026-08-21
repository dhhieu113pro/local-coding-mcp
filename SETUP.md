# Setup guide — LocalCodingMcp

Step-by-step for **Windows (PowerShell / wslc)** and **WSL / Linux / macOS**.

Connect ChatGPT with **ngrok** (public HTTPS URL ending in `/mcp`).

---

## 0. What you need

| Item | Required? | Where |
|------|-----------|--------|
| Docker Desktop (WSL2 on Windows) | Yes | https://www.docker.com/products/docker-desktop |
| Code folder on disk | Yes | e.g. `D:\wslc\workspaces` |
| ngrok authtoken | Yes (for ChatGPT) | https://dashboard.ngrok.com/get-started/your-authtoken |
| GitHub PAT | Only private clone | https://github.com/settings/tokens |

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
NGROK_AUTHTOKEN=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
CODE_SERVER_PASSWORD=changeme
```

`.env` must sit next to `docker-compose.yml`. On Windows use forward slashes: `D:/wslc/workspaces`.

---

## 2. Start MCP + ngrok

```powershell
docker compose pull
docker compose up -d
docker compose --profile ngrok up -d
```

| Container | Port | Role |
|-----------|------|------|
| `local-coding-mcp` | **5000** | MCP server |
| `local-coding-mcp-ngrok` | **4040** inspector | Public HTTPS |

### Health

```powershell
curl http://127.0.0.1:5000/health
```

### Public URL

```powershell
docker compose logs ngrok
```

Or open **http://127.0.0.1:4040** and copy the HTTPS URL.

Example: `https://xxxx.ngrok-free.app` or `https://xxxx.ngrok-free.dev`

---

## 3. Connect ChatGPT

1. ChatGPT → **Settings** → **Developer mode** (if needed)  
2. **Connectors / Plugins** → add MCP app  
3. **Connection → URL**  
4. Paste: `https://xxxx.ngrok-free.app/mcp`  
   (**must include `/mcp`**, no port number)  
5. Scan tools → **new chat**  
6. Call `OpenWorkspace` with:

```json
{ "path": "/workspace/my-app" }
```

Use the **container** path `/workspace/...`, not `D:\...`.

### Free ngrok browser warning

Browsers may show ngrok’s trust page (`ERR_NGROK_6024`). That is normal on Free. ChatGPT URL mode uses the `/mcp` endpoint; if tools fail after a good 200 in the ngrok inspector, try a new chat or a paid ngrok plan (no interstitial).

---

## 4. Optional: host ngrok (no Docker for tunnel)

If MCP is already on port 5000:

```powershell
ngrok config add-authtoken YOUR_TOKEN
ngrok http 5000
```

ChatGPT URL: `https://<host>/mcp`

---

## 5. Optional: code-server / Termux

```powershell
docker compose --profile ide up -d
# http://127.0.0.1:8443

docker compose --profile termux run --rm termux
```

---

## 6. Optional: GitHub private clone

```powershell
docker exec -it local-coding-mcp `
  git clone https://ghp_YOUR_TOKEN@github.com/org/private-repo.git /workspace/private-repo
```

---

## Manual `wslc` (no Compose)

```powershell
wslc network create mcp-net

wslc run --rm -d --name local-coding-mcp `
  --network mcp-net `
  -p 5000:5000 `
  -e AllowedRoots__0=/workspace `
  -v "D:/wslc/workspaces:/workspace" `
  ghcr.io/dhhieu113pro/local-coding-mcp:latest

wslc run --rm -d --name local-coding-mcp-ngrok `
  --network mcp-net `
  -e NGROK_AUTHTOKEN="YOUR_TOKEN" `
  -p 4040:4040 `
  ngrok/ngrok:latest http local-coding-mcp:5000 --log=stdout

wslc logs local-coding-mcp-ngrok
```

**Volume tip (Windows):** one quoted string with forward slashes:

```text
-v "D:/wslc/workspaces:/workspace"
```

---

## Secrets cheat sheet

| Secret | Env | Used by |
|--------|-----|---------|
| ngrok token | `NGROK_AUTHTOKEN` | ngrok container |
| Host code folder | `MCP_WORKSPACE` | volume mount |
| IDE password | `CODE_SERVER_PASSWORD` | code-server |

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Volume / `D:` errors | `-v "D:/path:/workspace"` as one string |
| ngrok auth failed | Set `NGROK_AUTHTOKEN` in `.env`; recreate ngrok container |
| ChatGPT cannot connect | URL must end with `/mcp`; MCP healthy; new chat after scan |
| `ERR_NGROK_6024` in browser | Free interstitial — ignore for API, or upgrade ngrok |
| Tools empty in ChatGPT | Confirm ngrok traffic shows `GET /mcp` → 200 |

---

## Related

- [README.md](README.md)  
- [LocalCodingMcp/README.md](LocalCodingMcp/README.md)  
- [ngrok docs](https://ngrok.com/docs)
