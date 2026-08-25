<p align="center">
  <img src="docs/logo.svg" alt="LocalCodingMcp logo" width="160" height="160"/>
</p>

<p align="center">
  <img src="docs/how-it-works.svg" alt="How LocalCodingMcp works" width="900"/>
</p>

# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

Open a project folder (under approved roots only), list/read/write/patch files (including binary via base64), search code, run shell commands, inspect git, manage reusable local skills, and optionally proxy Codebase Memory code intelligence — all through one LocalCodingMcp endpoint.

### How it works (short)

1. **Your PC** — mount a folder (e.g. `D:/wslc/workspaces`) into the container as `/workspace`
2. **Docker** — LocalCoding MCP listens on `:5000/mcp` with path sandbox + tools
3. **Optional Codebase Memory** — a sidecar indexes the same workspace read-only; LocalCodingMcp proxies its MCP tools internally
4. **ngrok** — public HTTPS URL so remote clients can reach LocalCodingMcp
5. **ChatGPT / Grok** — connect with **one URL** → `https://…/mcp`; skill routing and Codebase Memory proxying stay behind that endpoint

| | |
|---|---|
| **Setup (ngrok / Windows / WSL)** | **[SETUP.md](SETUP.md)** |
| **DNX / local stdio** | **[DNX.md](DNX.md)** |
| **Codebase Memory sidecar** | **[CODEBASE_MEMORY.md](CODEBASE_MEMORY.md)** |
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

DNX uses **stdio**. The existing web host remains the Streamable HTTP option for Docker, TermuxHost, LAN, and ngrok. Codebase Memory proxying is disabled by default outside the supplied Compose configuration unless `CodebaseMemory__Enabled=true` and an endpoint are configured. See **[DNX.md](DNX.md)** for configuration and version pinning.

### HTTP / ngrok

Full guide: **[SETUP.md](SETUP.md)**

```powershell
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
copy .env.example .env
# edit .env → NGROK_AUTHTOKEN, MCP_WORKSPACE=D:/wslc/workspaces

docker compose --profile codebase-memory up -d
docker compose --profile ngrok up -d

curl http://127.0.0.1:5000/health
docker compose logs ngrok
# copy https://xxxx.ngrok-free.app
```

ChatGPT / Grok → **Connection → URL** → `https://xxxx.ngrok-free.app/mcp` → new chat. You do not need to configure the Codebase Memory port separately: LocalCodingMcp exposes `codebase_memory_status`, `codebase_memory_list_tools`, and `codebase_memory_call` through the same `/mcp` endpoint.

For coding/debugging/design/planning/review tasks, the server advertises instructions to call `route_skills` first and `load_skills` only for the recommendations; then use `open_workspace` with `/workspace/...`.

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
| `local-coding-mcp` | (default) | **5000** | Client-facing MCP `/mcp`; files/git/shell/skills + Codebase Memory proxy |
| `codebase-memory` | **`codebase-memory`** | **9750** localhost | Internal structural code-intelligence MCP sidecar |
| `ngrok` | **`ngrok`** | **4040** inspector | Public HTTPS to LocalCodingMcp only |
| `code-server` | **`ide`** | **8443** | Browser VS Code |
| `termux` | **`termux`** | — | Termux-like test shell |

Network: **`mcp-net`**. Secrets: **`.env`** (see `.env.example`).

---

## Typical tool flow

```text
route_skills(task)
load_skills([recommended skill names])
open_workspace(path)  →  workspace_id
list_directory / read_file / search_files
write_file / write_binary_file / apply_patch
run_command
git_status / git_diff / git_log

# Optional structural code intelligence through the same LocalCodingMcp connection
codebase_memory_status
codebase_memory_list_tools
codebase_memory_call(tool, arguments_json)

list_skills
set_skill_enabled("ponytail", true)
route_skills(task)
load_skills(["ponytail"])
create_skill(name, content)
install_skill(source, enabled)
check_skill_updates(name)
update_skill_from_source(name)
get_skill(name) / update_skill(name, content) / delete_skill(name)
```

`load_enabled_skills` remains available for backward compatibility when a client explicitly wants every enabled skill.

---

## Codebase Memory proxy

Start the optional sidecar with:

```bash
docker compose --profile codebase-memory up -d
```

LocalCodingMcp connects internally to `http://codebase-memory:9750/mcp`. Clients continue using only `http://127.0.0.1:5000/mcp` (or the existing ngrok URL).

The proxy surface is intentionally small:

```text
codebase_memory_status()
codebase_memory_list_tools()
codebase_memory_call(tool, arguments_json)
```

`codebase_memory_call` validates the requested name against the sidecar's current `tools/list` result before forwarding it. Use `codebase_memory_list_tools` to inspect the exact upstream schema instead of guessing arguments. When the sidecar is unavailable, `codebase_memory_status` reports that state without breaking LocalCodingMcp.

Configuration:

```text
CodebaseMemory__Enabled=true
CodebaseMemory__Endpoint=http://codebase-memory:9750/mcp
CodebaseMemory__ConnectionTimeoutSeconds=15
```

Compose maps these from `CODEBASE_MEMORY_PROXY_*` values in `.env`. See **[CODEBASE_MEMORY.md](CODEBASE_MEMORY.md)** for architecture, security, and verification details.

---

## Remote skill install and updates

Install directly from an explicit HTTPS `SKILL.md` source:

```text
install_skill(
  source: "https://github.com/owner/repo/blob/main/skills/example/SKILL.md",
  enabled: true
)
```

Then check and explicitly refresh it later:

```text
check_skill_updates(name: "example")
update_skill_from_source(name: "example")
```

`CreateSkill` and `InstallSkill` are intentionally different. `CreateSkill` stores content supplied by the caller and performs no network request. `InstallSkill` fetches an explicitly supplied HTTPS URL, validates the `SKILL.md` front matter, records the original/resolved source URL and a SHA-256 content hash, then stores the skill. GitHub blob URLs are normalized to raw GitHub content URLs; raw GitHub and generic HTTPS text/Markdown URLs are also supported.

Remote updates are **never automatic**. `check_skill_updates` only compares hashes; `update_skill_from_source` must be called explicitly. A downloaded skill is still only instructions and receives no extra filesystem or shell permissions beyond the existing LocalCodingMcp sandbox/tool protections.

Remote fetch defaults can be configured with:

```text
Skills__Remote__MaxBytes=1048576
Skills__Remote__TimeoutSeconds=15
Skills__Remote__MaxRedirects=3
```

---

## Tool list (summary)

| Tool | What it does |
|------|----------------|
| **RouteSkills** | Rank relevant enabled skills for a task without loading full `SKILL.md` content |
| **LoadSkills** | Load complete content only for selected enabled skills |
| **LoadEnabledSkills** | Backward-compatible full load of every enabled skill |
| **InstallSkill** | Explicitly install and validate a remote HTTPS `SKILL.md`, recording provenance/hash |
| **CheckSkillUpdates** | Compare installed remote-skill hashes with upstream without applying changes |
| **UpdateSkillFromSource** | Explicitly refresh a remote skill while preserving enabled state |
| **CodebaseMemoryStatus** | Report whether the optional Codebase Memory proxy is configured/reachable |
| **CodebaseMemoryListTools** | Return the sidecar's current MCP tool catalog and input schemas |
| **CodebaseMemoryCall** | Forward an advertised Codebase Memory tool call through LocalCodingMcp |
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
| **ListSkills** | List skills with enabled/built-in state and attribution/provenance |
| **SetSkillEnabled** | Persistently enable/disable any skill |
| **GetSkill** | Read complete `SKILL.md` content and state |
| **CreateSkill** | Create an enabled custom `<skills>/<name>/SKILL.md` from caller-supplied content |
| **UpdateSkill** | Replace an existing skill's `SKILL.md` manually |
| **DeleteSkill** | Delete a custom skill directory recursively; built-ins must be disabled instead |

Details: **[LocalCodingMcp/README.md](LocalCodingMcp/README.md)**

### Built-in skills

Five attributed built-in skills ship with the server and are **disabled by default**:

| Skill | Purpose | Upstream |
|------|---------|----------|
| `caveman` | Terse, token-efficient technical responses | `JuliusBrussee/caveman` |
| `hallmark` | Anti-template / anti-AI-slop UI design discipline | `Nutlope/hallmark` |
| `superpowers` | Structured engineering, TDD, debugging, review and verification workflow | `tpffounder/superpowers` |
| `ponytail` | Minimal, anti-over-engineering implementation discipline | `DietrichGebert/ponytail` |
| `codebase-memory` | Codebase architecture, semantic exploration, call-path and impact-analysis workflow | `DeusData/codebase-memory-mcp` |

Enable one without deleting or rewriting it:

```text
SetSkillEnabled(name: "codebase-memory", enabled: true)
SetSkillEnabled(name: "codebase-memory", enabled: false)
```

When `codebase-memory` is enabled, routing can select it for codebase exploration, architecture, indexing, dependency/caller tracing, ADR, and impact-analysis tasks. Its instructions now use the LocalCodingMcp proxy tools (`codebase_memory_status` → `codebase_memory_list_tools` → `codebase_memory_call`) rather than requiring the MCP client to connect to a second server. After structural discovery it returns to LocalCodingMcp file/git/shell tools for exact source inspection, editing, tests, and verification. If the sidecar is unavailable, the skill explicitly falls back to normal LocalCodingMcp exploration.

The server includes MCP initialization instructions telling clients to call `route_skills` before coding, debugging, design, planning, or review work. Routing is deterministic and local: it scores only enabled skills using their name/front-matter description plus small built-in intent hints, then `load_skills` returns full content only for the selected skills. Custom and remotely installed skills participate automatically when their `description:` front matter matches the task. Client/model compliance with server instructions still depends on the MCP host.

Enable state is stored in `<skill>/.skill.json`, so it survives process, Docker, and Termux restarts. Existing skills created before this feature have no metadata file and remain enabled by default for backward compatibility. Built-ins cannot be deleted; disable them instead.

Skills are stored under `LocalCodingMcp/data/skills` by default. Override the location with `Skills__Directory`. Under Docker Compose, `/app/data` is already persisted by the existing `${MCP_HISTORY:-./history}` volume, so skills survive container restarts together with execution history.

Every MCP tool call is appended to `LocalCodingMcp/data/execution-history.jsonl`. Sensitive arguments such as file content, base64 data, tokens, passwords, and secrets are redacted. The log rotates at 10 MiB by default so repeated LLM calls do not grow storage without limit. Docker Compose persists it on the host in `./history` (override with `MCP_HISTORY`).

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
- Remote skill sources and redirects must remain HTTPS; embedded URL credentials, oversized/binary/empty responses, and malformed front matter are rejected
- Remote installs/updates are explicit operations and do not grant skills extra tool privileges
- Built-in skills are immutable at the catalog level and protected from deletion; their enabled state is local and persistent
- Codebase Memory proxy calls are limited to tool names currently advertised by the configured sidecar; the sidecar workspace is mounted read-only
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
