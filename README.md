# LocalCodingMcp

Secure local coding **MCP server** (C# / .NET 10) for **ChatGPT**, **Grok**, and other MCP clients.

It lets the model open a project folder (under **approved roots only**), list/read/write/patch files, search code, run shell commands, and inspect git — all **path-sandboxed**.

| | |
|---|---|
| **Full docs & tool reference** | [LocalCodingMcp/README.md](LocalCodingMcp/README.md) |
| **CI** | [![CI](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/dhhieu113pro/local-coding-mcp/actions/workflows/ci.yml) — Linux · macOS · Windows |
| **License** | [MIT](LICENSE) |

```bash
dotnet test LocalCodingMcp.sln -c Release
dotnet run --project LocalCodingMcp
```

---

## Quick guide

1. **Configure** allowed project folders in `LocalCodingMcp/appsettings.json` (`AllowedRoots`).
2. **Run** the server: `dotnet run --project LocalCodingMcp` → MCP at `http://localhost:5000/mcp` (check console for port).
3. **Tunnel** for ChatGPT web (optional): `ngrok http 5000` or Cloudflare Tunnel.
4. **Connect** the MCP URL in ChatGPT Developer Mode (or your MCP client).
5. **Always call `OpenWorkspace` first** with an absolute path under an allowed root → you get a `workspace_id`.
6. Use that `workspace_id` with file / git / shell tools.

### Typical flow

```
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
| **OpenWorkspace** | Open a project folder under allowed roots → returns `workspace_id` |
| **ListWorkspaces** | List currently open workspaces |
| **GetAllowedRoots** | Show configured allowed root directories |
| **ListDirectory** | List files/dirs relative to workspace |
| **ReadFile** | Read a text file (optional line range) |
| **WriteFile** | Create or overwrite a UTF-8 text file |
| **ApplyPatch** | Apply a unified-diff patch (preferred for edits) |
| **SearchFiles** | Regex/text search across files (skips binaries & sensitive names) |
| **CreateDirectory** | Create directory (and parents) |
| **MoveFile** | Move or rename file/directory |
| **DeleteFile** | Delete a file or empty directory |
| **GitStatus** | `git status` in the workspace |
| **GitDiff** | Unstaged or staged diff |
| **GitLog** | Recent commits (`git log --oneline`) |
| **RunCommand** | Run a shell command inside the workspace (with timeout) |

Full parameters, **example input/output JSON**, and safety model: **[LocalCodingMcp/README.md](LocalCodingMcp/README.md)**.

---

## Safety (short)

- Paths only under **AllowedRoots**
- Blocks path traversal and symlink escapes
- Blocks sensitive names (`.env`, keys, `*.pem`, …)
- Shell commands run with timeout, cwd = workspace root
