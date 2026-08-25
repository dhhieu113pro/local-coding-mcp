<p align="center">
  <img src="docs/logo.svg" alt="LocalCodingMcp logo" width="160" height="160"/>
</p>

<p align="center">
  <img src="docs/how-it-works.svg" alt="How LocalCodingMcp works" width="900"/>
</p>

# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

Open a project folder (under approved roots only), list/read/write/patch files (including binary via base64), search code, run shell commands, inspect git, and manage reusable local skills — all path-sandboxed.

### How it works (short)

1. **Your PC** — mount a folder (e.g. `D:/wslc/workspaces`) into the container as `/workspace`
2. **Docker** — LocalCoding MCP listens on `:5000/mcp` with path sandbox + tools
3. **ngrok** — public HTTPS URL so remote clients can reach you
4. **ChatGPT / Grok** — connect with **URL** → `https://…/mcp`; for coding work the MCP instructions tell the client to call `LoadEnabledSkills` before other LocalCoding tools, then `OpenWorkspace`

| | |
|---|---|
| **Setup (ngrok / Windows / WSL)** | **[SETUP.md](SETUP.md)** |
| **DNX / local stdio** | **[DNX.md](DNX.md)** |
| **TermuxHost / Android ZIP** | **[TERMUXHOST.md](TERMUXHOST.md)** |
| **Tool reference** | [LocalCodingMcp/README.md](LocalCodingMcp/README.md) |
| **CI** | [![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml) |
| **Docker image** | `ghcr.io/dhhieu113pro/local-coding-mcp:latest` |
| **License** | [MIT](LICENSE) |

---

## Quick start

### Local stdio with DNX

With the .NET 10 SDK installed, run the NuGet-packaged MCP server directly:

```bash
dnx LocalCodingMcp.Dnx --yes
```

MCP client configuration:

```json
{
  "mcpServers": {
    "local-coding": {
      "type": "stdio",
      "command": "dnx",
      "args": ["LocalCodingMcp.Dnx", "--yes"]
    }
  }
}
```

DNX uses **stdio**. The existing web host remains the Streamable HTTP option for Docker, TermuxHost, LAN, and ngrok. See **[DNX.md](DNX.md)** for configuration and version pinning.

### HTTP / ngrok

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

ChatGPT / Grok → **Connection → URL** → `https://xxxx.ngrok-free.app/mcp` → new chat. For coding/debugging/design/planning/review tasks, the server advertises instructions to call `LoadEnabledSkills` before other LocalCoding tools; then use `OpenWorkspace` with `/workspace/...`.

### TermuxHost release ZIP

For native Android/Termux hosting, push a `v*` tag. GitHub Actions publishes and smoke-tests:

```text
local-coding-mcp-termux-aarch64.zip
local-coding-mcp-termux-aarch64.zip.sha256
```

The ZIP is framework-dependent and uses the .NET 10 runtime installed by TermuxHost. See **[TERMUXHOST.md](TERMUXHOST.md)** for deployment settings.

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
LoadEnabledSkills
OpenWorkspace(path)  →  workspace_id
ListDirectory / ReadFile / SearchFiles
WriteFile / WriteBinaryFile / ApplyPatch
RunCommand
GitStatus / GitDiff / GitLog

ListSkills
SetSkillEnabled("ponytail", true)
LoadEnabledSkills
CreateSkill(name, content)
GetSkill(name) / UpdateSkill(name, content) / DeleteSkill(name)
```

---

## Tool list (summary)

| Tool | What it does |
|------|----------------|
| **LoadEnabledSkills** | Load complete content for active skills before coding/debugging/design/planning/review work |
| **OpenWorkspace** | Open folder under allowed roots → `workspace_id` |
| **ListWorkspaces** | List open workspaces |
| **GetAllowedRoots** | Show configured allowed roots |
| **ListDirectory** | List files/dirs |
| **ReadFile** | Read text file |
| **WriteFile** | Create/overwrite text file |
| **WriteBinaryFile** | Write binary (PNG/JPG/…) from base64 |
| **ReadBinaryFile** | Read binary as base64 |
| **ApplyPatch** | Unified diff |
| **SearchFiles** | Regex/text search |
| **CreateDirectory** | Create directory |
| **MoveFile** | Move/rename |
| **DeleteFile** | Delete file/empty dir |
| **GitStatus** / **GitDiff** / **GitLog** | Git inspect |
| **RunCommand** | Shell in workspace |
| **GetExecutionHistory** | Recent persisted tool calls, status, and duration |
| **ListSkills** | List skills with enabled/built-in state and attribution |
| **SetSkillEnabled** | Persistently enable/disable any skill |
| **GetSkill** | Read complete `SKILL.md` content and state |
| **CreateSkill** | Create an enabled custom `<skills>/<name>/SKILL.md` |
| **UpdateSkill** | Replace an existing skill's `SKILL.md` |
| **DeleteSkill** | Delete a custom skill directory recursively; built-ins must be disabled instead |

Details: **[LocalCodingMcp/README.md](LocalCodingMcp/README.md)**

### Built-in skills

Four attributed built-in skills ship with the server and are **disabled by default**:

| Skill | Purpose | Upstream |
|------|---------|----------|
| `caveman` | Terse, token-efficient technical responses | `JuliusBrussee/caveman` |
| `hallmark` | Anti-template / anti-AI-slop UI design discipline | `Nutlope/hallmark` |
| `superpowers` | Structured engineering, TDD, debugging, review and verification workflow | `tpffounder/superpowers` |
| `ponytail` | Minimal, anti-over-engineering implementation discipline | `DietrichGebert/ponytail` |

Enable one without deleting or rewriting it:

```text
SetSkillEnabled(name: "caveman", enabled: true)
SetSkillEnabled(name: "caveman", enabled: false)
```

The server includes MCP initialization instructions telling clients to call `LoadEnabledSkills` before coding, debugging, design, planning, or review work, apply every relevant enabled skill, and follow `superpowers` process-selection rules first when that skill is enabled. Client/model compliance with server instructions still depends on the MCP host.

Enable state is stored in `<skill>/.skill.json`, so it survives process, Docker, and Termux restarts. Existing skills created before this feature have no metadata file and remain enabled by default for backward compatibility. Built-ins cannot be deleted; disable them instead.

Skills are stored under `LocalCodingMcp/data/skills` by default. Override the location with
`Skills__Directory`. Under Docker Compose, `/app/data` is already persisted by the existing
`${MCP_HISTORY:-./history}` volume, so skills survive container restarts together with execution history.

Every MCP tool call is appended to `LocalCodingMcp/data/execution-history.jsonl`. Sensitive
arguments such as file content, base64 data, tokens, passwords, and secrets are redacted.
The log rotates at 10 MiB by default so repeated LLM calls do not grow storage without limit.
Docker Compose persists it on the host in `./history` (override with `MCP_HISTORY`).

---

## Branding

- Logo: [docs/logo.svg](docs/logo.svg)
- How it works diagram: [docs/how-it-works.svg](docs/how-it-works.svg)
- ChatGPT connector icon: square **512×512 PNG** export of the logo

---

## Safety

- Paths only under **AllowedRoots** (`/workspace` in Docker)
- Blocks path traversal, symlink escape, sensitive names
- Skill names are restricted to letters, numbers, `.`, `_`, and `-` and cannot escape the configured skills directory
- Built-in skills are immutable at the catalog level and protected from deletion; their enabled state is local and persistent
- Do not expose 5000 / 8443 / ngrok URL without care

---

## Dev (dotnet)

```bash
dotnet test LocalCodingMcp.sln -c Release
dotnet run --project LocalCodingMcp
dotnet run --project LocalCodingMcp.Dnx
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
