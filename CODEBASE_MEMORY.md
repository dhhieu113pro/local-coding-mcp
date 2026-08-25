# Codebase Memory MCP sidecar

LocalCodingMcp can run [DeusData/codebase-memory-mcp](https://github.com/DeusData/codebase-memory-mcp) as an optional Docker Compose sidecar. Both MCP servers see the same workspace, but Codebase Memory mounts it read-only.

## Start

Set `MCP_WORKSPACE` in `.env`, then run:

```bash
docker compose --profile codebase-memory up -d
```

Endpoints:

```text
LocalCodingMcp:   http://127.0.0.1:5000/mcp
Codebase Memory: http://127.0.0.1:9750/mcp
```

Codebase Memory is intentionally bound to `127.0.0.1` by default. It is not exposed through the existing ngrok service.

## Enable the LocalCodingMcp skill

LocalCodingMcp also ships a `codebase-memory` built-in skill, disabled by default. Enable it when your MCP client is connected to both endpoints:

```text
set_skill_enabled(name: "codebase-memory", enabled: true)
```

After that, `route_skills` can recommend it for codebase exploration, architecture, indexing, semantic discovery, dependency/caller tracing, ADR, call-path, and impact-analysis tasks. `load_skills` gives the model instructions to use Codebase Memory for structural discovery first and LocalCodingMcp for exact source reads, edits, git/shell operations, tests, and verification.

The skill does not proxy or merge the two MCP transports. Your MCP host still needs both servers configured. If Codebase Memory tools are unavailable, the skill tells the model to fall back to normal LocalCodingMcp exploration instead of blocking the task.

## Architecture

`DeusData/codebase-memory-mcp` speaks MCP over stdio. The sidecar image installs the pinned upstream native portable Linux binary, verifies the release archive against the upstream `checksums.txt`, and executes `codebase-memory-mcp --version` during the build to catch runtime incompatibility. Supergateway then exposes that stdio server as stateful Streamable HTTP at `/mcp`.

```text
host workspace
   └── /workspace (read-only)
          ↓
codebase-memory-mcp (stdio)
          ↓
supergateway
          ↓
http://127.0.0.1:9750/mcp
```

The graph/cache lives under `/var/lib/codebase-memory` and is persisted by `CBM_DATA` (default `./codebase-memory`).

## Configuration

```text
CODEBASE_MEMORY_VERSION=v0.10.8
SUPERGATEWAY_VERSION=3.2.0
CBM_PORT=9750
CBM_DATA=./codebase-memory
CBM_MEMORY_LIMIT=1g
CBM_CPU_LIMIT=2.0
```

Override these in `.env`. The image supports Linux `amd64` and `arm64` through Docker BuildKit's `TARGETARCH`.

## MCP client

For a client that supports Streamable HTTP, add the Codebase Memory endpoint separately:

```text
http://127.0.0.1:9750/mcp
```

Keep LocalCodingMcp configured at its own `/mcp` endpoint. They provide complementary tool sets: LocalCodingMcp handles sandboxed files/git/shell/skills, while Codebase Memory provides persistent structural code intelligence.

## Security

The workspace is mounted `:ro` in the Codebase Memory container. Its graph/cache has a separate writable mount. Port 9750 is loopback-only by default. Do not change it to `0.0.0.0` or tunnel it publicly unless you deliberately want remote access to code-intelligence results.

The upstream server runs locally and indexes source into its own persistent graph. Review the upstream security documentation before exposing it beyond your machine.

## Verify

```bash
curl http://127.0.0.1:9750/healthz
docker compose --profile codebase-memory logs codebase-memory
```

CI builds the sidecar and performs a real MCP `initialize` followed by `tools/list` against the Streamable HTTP endpoint.
