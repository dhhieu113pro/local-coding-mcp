# Codebase Memory MCP sidecar

LocalCodingMcp can run [DeusData/codebase-memory-mcp](https://github.com/DeusData/codebase-memory-mcp) as an optional Docker Compose sidecar. Both services see the same workspace, but Codebase Memory mounts source read-only.

## Start

Set `MCP_WORKSPACE` in `.env`, then run:

```bash
docker compose --profile codebase-memory up -d
```

Your MCP client only needs LocalCodingMcp:

```text
http://127.0.0.1:5000/mcp
```

Codebase Memory remains internal to the Compose network at `http://codebase-memory:9750/mcp`. Port 9750 is also loopback-bound for local diagnostics, but ChatGPT/Codex/Grok do not need it configured separately.

## Workspace lifecycle

`open_workspace` coordinates the Codebase Memory index automatically when the sidecar is available:

- If the workspace already has a healthy index, LocalCodingMcp reuses it.
- If the workspace is not indexed yet, LocalCodingMcp calls `index_repository` once for that workspace.
- If Codebase Memory reports the index as stale/outdated, LocalCodingMcp reports `codebase_memory.state = "stale"` and does **not** silently rebuild it.
- If the sidecar is unavailable or disabled, opening the workspace still succeeds and reports `codebase_memory.state = "unavailable"`.

For a stale index or a deliberate refresh after a large external change, call:

```text
refresh_codebase_memory_workspace(workspace_id)
```

This keeps normal workspace opening fast while making first-use indexing automatic and refreshes explicit.

## Skill activation

When `CodebaseMemory:Enabled=true`, LocalCodingMcp automatically enables the built-in `codebase-memory` skill so `route_skills` can recommend structural discovery without a separate setup step. When the proxy is disabled, the untouched built-in remains disabled.

An explicit user choice made with `set_skill_enabled` takes precedence and persists across restarts. This means a user can disable Codebase Memory routing even while the proxy remains available, or keep the skill enabled intentionally.

The skill covers architecture, codebase exploration, semantic discovery, dependency/caller tracing, ADRs, call paths, and impact analysis. It uses the LocalCodingMcp proxy tools for structural discovery, then normal LocalCodingMcp file/git/shell tools for exact source inspection, edits, tests, and verification.

## Proxy tools

LocalCodingMcp exposes three tools for the sidecar:

```text
codebase_memory_status
codebase_memory_list_tools
codebase_memory_call
```

Typical flow after `open_workspace`:

```text
codebase_memory_list_tools()
codebase_memory_call(
  tool: "get_architecture",
  arguments_json: "{}"
)
```

Use `codebase_memory_status` for diagnostics when needed. `codebase_memory_call` only forwards names currently advertised by the sidecar's own `tools/list` response. This prevents the generic proxy from becoming an arbitrary HTTP/RPC escape hatch.

If the sidecar is stopped, `codebase_memory_status` returns `available: false` instead of making the whole LocalCodingMcp server unavailable.

## Architecture

```text
ChatGPT / Codex / Grok
          |
          | one MCP connection
          v
LocalCodingMcp :5000/mcp
   | files / git / shell / skills
   |
   +-- internal MCP client
          |
          v
codebase-memory :9750/mcp
          |
          v
persistent graph/cache
```

`DeusData/codebase-memory-mcp` speaks MCP over stdio. The sidecar image installs the pinned portable Linux binary, verifies it against upstream `checksums.txt`, and checks `codebase-memory-mcp --version` during the image build. Supergateway exposes the process as Streamable HTTP inside the Compose network.

## Configuration

```text
CODEBASE_MEMORY_VERSION=v0.10.8
SUPERGATEWAY_VERSION=3.2.0
CBM_PORT=9750
CBM_DATA=./codebase-memory
CBM_MEMORY_LIMIT=1g
CBM_CPU_LIMIT=2.0

CODEBASE_MEMORY_PROXY_ENABLED=true
CODEBASE_MEMORY_PROXY_ENDPOINT=http://codebase-memory:9750/mcp
CODEBASE_MEMORY_PROXY_TIMEOUT_SECONDS=15
```

For non-Compose hosting, equivalent .NET configuration keys are:

```text
CodebaseMemory__Enabled=true
CodebaseMemory__Endpoint=http://127.0.0.1:9750/mcp
CodebaseMemory__ConnectionTimeoutSeconds=15
```

The proxy is disabled by default outside the supplied Docker Compose configuration, so DNX/local stdio works normally without a sidecar.

## Security

The Codebase Memory workspace mount is read-only. Its graph/cache has a separate writable mount. The sidecar is not exposed through ngrok; only LocalCodingMcp's existing endpoint is public when you enable the ngrok profile.

Remote tool names are validated against Codebase Memory's current advertised catalog before invocation. LocalCodingMcp still owns its normal path sandbox, sensitive-file filtering, execution history, and skill controls.

## Verify

```bash
curl http://127.0.0.1:5000/health
curl http://127.0.0.1:9750/healthz
docker compose --profile codebase-memory logs
```

CI builds both images, initializes both MCP servers, verifies Codebase Memory's real tool catalog, verifies that LocalCodingMcp advertises the proxy tools, and checks `codebase_memory_status` through the single LocalCodingMcp endpoint.
