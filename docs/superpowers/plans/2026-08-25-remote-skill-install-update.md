# Remote Skill Install and Update Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add explicit HTTPS skill installation plus provenance-aware update checking and source refresh while preserving existing manual skill CRUD and automatic routing behavior.

**Architecture:** Introduce a focused remote fetcher and narrow SKILL.md front-matter parser, then extend `SkillStore` with compatible provenance metadata and atomic remote install/update operations. `SkillTools` exposes install/check/update MCP methods, while both HTTP and DNX hosts receive the same registrations through the existing shared `McpServerRegistration` path.

**Tech Stack:** .NET 10, C#, `HttpClient`, `System.Security.Cryptography`, ModelContextProtocol C# SDK, xUnit, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-08-25-remote-skill-install-update-design.md`

## Global Constraints

- Remote sources are HTTPS only.
- Support raw GitHub URLs, GitHub blob URLs normalized to raw content, and generic direct HTTPS text URLs.
- Reject embedded credentials, non-HTTPS redirects, oversized responses, binary content, empty content, malformed front matter, invalid names, and caller/front-matter name mismatches.
- Defaults: `Skills__Remote__MaxBytes=1048576`, `Skills__Remote__TimeoutSeconds=15`, `Skills__Remote__MaxRedirects=3`.
- `contentSha256` is authoritative for update comparison; ETag/Last-Modified are hints only.
- Existing local `CreateSkill`, `UpdateSkill`, `DeleteSkill`, `SetSkillEnabled`, routing, built-ins, HTTP host, DNX host, and existing `.skill.json` files remain backward compatible.
- Remote update is atomic: failed fetch/validation/write must leave the current installed skill unchanged.
- No automatic/background update, marketplace search, private GitHub auth, git clone, dependency install, or routing metadata changes.

---

### Task 1: Remote skill document parsing

**Files:**
- Create: `LocalCodingMcp/Services/SkillDocumentParser.cs`
- Create: `LocalCodingMcp.Tests/SkillDocumentParserTests.cs`

**Interfaces:**
- Produces: `SkillFrontMatter Parse(string content)`
- Produces: `sealed record SkillFrontMatter(string Name, string Description, string? License)`

- [ ] **Step 1: Write failing parser tests**

Cover valid `name`/`description`/optional `license`, missing front matter, missing name, missing description, blank values, malformed delimiter, and preserving body content outside parser concerns.

```csharp
var parsed = SkillDocumentParser.Parse("""
---
name: hallmark
description: UI design discipline
license: MIT
---
# Hallmark
""");
Assert.Equal("hallmark", parsed.Name);
Assert.Equal("UI design discipline", parsed.Description);
Assert.Equal("MIT", parsed.License);
```

- [ ] **Step 2: Run the focused tests and confirm RED**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter SkillDocumentParserTests`

Expected: compile/test failure because `SkillDocumentParser` does not exist.

- [ ] **Step 3: Implement the minimal narrow parser**

Parse only the first YAML-style front-matter block delimited by `---`. Accept simple `key: value` lines for `name`, `description`, and optional `license`; reject missing/blank required values with `InvalidDataException`. Do not add a YAML dependency.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter SkillDocumentParserTests`

Expected: all parser tests pass.

- [ ] **Step 5: Commit**

```bash
git add LocalCodingMcp/Services/SkillDocumentParser.cs LocalCodingMcp.Tests/SkillDocumentParserTests.cs
git commit -m "feat: parse skill front matter"
```

---

### Task 2: HTTPS fetcher, normalization, bounds, and hashing

**Files:**
- Create: `LocalCodingMcp/Services/RemoteSkillFetcher.cs`
- Create: `LocalCodingMcp.Tests/RemoteSkillFetcherTests.cs`

**Interfaces:**
- Produces: `sealed record RemoteSkillFetchResult(string SourceUrl, string ResolvedSourceUrl, string Content, string ContentSha256, string? ETag, DateTimeOffset? LastModified)`
- Produces: `Task<RemoteSkillFetchResult> FetchAsync(string sourceUrl, string? etag = null, DateTimeOffset? lastModified = null, CancellationToken cancellationToken = default)`
- Constructor consumes `HttpClient`, `maxBytes`, and `maxRedirects`; timeout is configured on the client during DI registration.

- [ ] **Step 1: Write failing fetcher tests with a fake `HttpMessageHandler`**

Tests must cover:

```text
https://github.com/owner/repo/blob/main/path/SKILL.md
→ https://raw.githubusercontent.com/owner/repo/main/path/SKILL.md
```

and raw GitHub/generic HTTPS success, `http://` rejection, embedded credentials rejection, HTTPS→HTTP redirect rejection, redirect-count overflow, response larger than 1 MiB rejection, binary/content-type rejection, empty body rejection, SHA-256 output, ETag/Last-Modified capture, and conditional request headers.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter RemoteSkillFetcherTests`

Expected: failure because the fetcher does not exist.

- [ ] **Step 3: Implement URL normalization and safe HTTP flow**

Use `HttpCompletionOption.ResponseHeadersRead`, manually process redirects up to the configured limit, require HTTPS on every hop, reject URI user-info, enforce the content-length limit when present and a streaming byte cap otherwise, and decode text only for Markdown/plain-text-compatible responses. Compute lowercase hex SHA-256 from the exact UTF-8 content bytes returned to the caller.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter RemoteSkillFetcherTests`

Expected: all fetcher tests pass.

- [ ] **Step 5: Commit**

```bash
git add LocalCodingMcp/Services/RemoteSkillFetcher.cs LocalCodingMcp.Tests/RemoteSkillFetcherTests.cs
git commit -m "feat: add safe remote skill fetcher"
```

---

### Task 3: Provenance-compatible `SkillStore` persistence

**Files:**
- Modify: `LocalCodingMcp/Services/SkillStore.cs`
- Modify: `LocalCodingMcp.Tests/SkillStoreTests.cs`

**Interfaces:**
- Extend skill metadata/document records with optional: `ResolvedSourceUrl`, `ContentSha256`, `SourceEtag`, `SourceLastModified`, `InstalledAt`, `UpdatedAt`.
- Produce store operation similar to:

```csharp
SkillDocument InstallRemote(
    string content,
    SkillFrontMatter frontMatter,
    RemoteSkillFetchResult source,
    bool enabled);

SkillDocument ReplaceRemote(
    string name,
    string content,
    SkillFrontMatter frontMatter,
    RemoteSkillFetchResult source);
```

- [ ] **Step 1: Add failing persistence and atomicity tests**

Verify remote metadata survives a fresh `SkillStore` instance, existing old `.skill.json` still deserializes, caller-provided enable state persists, replacement preserves enabled state, and injected invalid/mismatched content leaves existing `SKILL.md` and `.skill.json` unchanged.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter SkillStoreTests`

Expected: new provenance assertions fail.

- [ ] **Step 3: Extend metadata compatibly and implement atomic writes**

Keep every new metadata field nullable. For install/update, write temporary skill and metadata files in the destination directory, flush/close them, then replace final files only after all validation has succeeded. For a new install, clean temporary files/directories on failure. `ReplaceRemote` must reject built-ins and local-only skills before writing.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter SkillStoreTests`

Expected: all store tests pass, including legacy behavior.

- [ ] **Step 5: Commit**

```bash
git add LocalCodingMcp/Services/SkillStore.cs LocalCodingMcp.Tests/SkillStoreTests.cs
git commit -m "feat: persist remote skill provenance"
```

---

### Task 4: Remote skill service orchestration and update semantics

**Files:**
- Create: `LocalCodingMcp/Services/RemoteSkillService.cs`
- Create: `LocalCodingMcp.Tests/RemoteSkillServiceTests.cs`

**Interfaces:**
- Produces:

```csharp
Task<SkillDocument> InstallAsync(string source, bool enabled = true, string? name = null, CancellationToken cancellationToken = default);
Task<IReadOnlyList<SkillUpdateStatus>> CheckUpdatesAsync(string? name = null, CancellationToken cancellationToken = default);
Task<SkillUpdateResult> UpdateFromSourceAsync(string name, CancellationToken cancellationToken = default);
```

- `SkillUpdateStatus` includes `Name`, `SourceUrl`, `InstalledSha256`, `RemoteSha256`, `Status`, `Message`.
- `SkillUpdateResult` includes `Name`, `OldSha256`, `NewSha256`, `Changed`, and the refreshed `SkillDocument`.

- [ ] **Step 1: Write failing orchestration tests**

Cover install success, caller/front-matter name mismatch, enabled routing participation after install, check current, check changed, unavailable remote, mixed multi-skill results that continue after one failure, update preserves enabled state, no-op update when hashes match, local-only rejection, built-in rejection, and invalid upstream content leaving installed files unchanged.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter RemoteSkillServiceTests`

Expected: failure because orchestration service is absent.

- [ ] **Step 3: Implement minimal orchestration**

`InstallAsync` fetches → parses → validates optional caller name equality → installs. `CheckUpdatesAsync` selects only remotely sourced skills, fetches each independently, compares `contentSha256`, and converts exceptions into per-skill `unavailable` records. `UpdateFromSourceAsync` re-fetches the recorded source, parses/validates same name, then calls atomic replacement and reports hash change.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter RemoteSkillServiceTests`

Expected: all orchestration tests pass.

- [ ] **Step 5: Commit**

```bash
git add LocalCodingMcp/Services/RemoteSkillService.cs LocalCodingMcp.Tests/RemoteSkillServiceTests.cs
git commit -m "feat: orchestrate remote skill installs and updates"
```

---

### Task 5: Shared HTTP/DNX dependency registration

**Files:**
- Modify: `LocalCodingMcp/Hosting/McpServerRegistration.cs`
- Modify: `LocalCodingMcp.Tests/Hosting/McpServerRegistrationTests.cs`
- If required by current linked-source DNX packaging: modify `LocalCodingMcp.Dnx/LocalCodingMcp.Dnx.csproj`

**Interfaces:**
- Register one configured `HttpClient`/`RemoteSkillFetcher`/`RemoteSkillService` path shared by both `LocalCodingMcpTransport.Http` and `LocalCodingMcpTransport.Stdio`.

- [ ] **Step 1: Add failing registration tests**

Build service providers for HTTP and stdio configurations and assert both can resolve `RemoteSkillFetcher` and `RemoteSkillService`, with configured max bytes/timeout/redirect defaults and overrides.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter McpServerRegistrationTests`

Expected: remote services are not registered yet.

- [ ] **Step 3: Implement shared registration**

Read:

```text
Skills:Remote:MaxBytes       default 1048576
Skills:Remote:TimeoutSeconds default 15
Skills:Remote:MaxRedirects   default 3
```

Clamp values to safe positive ranges, configure the dedicated `HttpClient`, and register remote services once in `AddLocalCodingMcp` so both transports inherit them.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter McpServerRegistrationTests`

Expected: both host configurations resolve the same remote-skill services.

- [ ] **Step 5: Commit**

```bash
git add LocalCodingMcp/Hosting/McpServerRegistration.cs LocalCodingMcp.Tests/Hosting/McpServerRegistrationTests.cs LocalCodingMcp.Dnx/LocalCodingMcp.Dnx.csproj
git commit -m "feat: register remote skill services"
```

---

### Task 6: MCP tools for install/check/update

**Files:**
- Modify: `LocalCodingMcp/Tools/SkillTools.cs`
- Create or modify: `LocalCodingMcp.Tests/RemoteSkillToolTests.cs`

**Interfaces:**
- Add `InstallSkill(string source, bool enabled = true, string? name = null)`
- Add `CheckSkillUpdates(string? name = null)`
- Add `UpdateSkillFromSource(string name)`
- MCP SDK derives wire names `install_skill`, `check_skill_updates`, `update_skill_from_source`.

- [ ] **Step 1: Add failing MCP tool contract tests**

Assert methods exist with MCP attributes/descriptions, serialize provenance/update status fields, `install_skill` returns installed name/hash/source/enabled state, `check_skill_updates` exposes per-skill status without full skill content, and `update_skill_from_source` exposes old/new hashes plus `changed`.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter RemoteSkillToolTests`

Expected: missing methods fail.

- [ ] **Step 3: Implement thin tool wrappers**

Inject `RemoteSkillService`; keep tool methods orchestration-free beyond argument validation, service call, and JSON serialization. Existing manual CRUD and routing tools remain unchanged.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter RemoteSkillToolTests`

Expected: all tool-contract tests pass.

- [ ] **Step 5: Commit**

```bash
git add LocalCodingMcp/Tools/SkillTools.cs LocalCodingMcp.Tests/RemoteSkillToolTests.cs
git commit -m "feat: expose remote skill MCP tools"
```

---

### Task 7: Documentation and MCP guidance

**Files:**
- Modify: `README.md`
- Modify: `DNX.md`
- Modify: `LocalCodingMcp/README.md`
- Modify tests that assert documentation/tool lists if present.

**Interfaces:**
- Document exact MCP calls and clarify trust boundaries.

- [ ] **Step 1: Add/update documentation contract tests if the repository uses them**

Expected documentation must contain `install_skill`, `check_skill_updates`, `update_skill_from_source`, `CreateSkill` vs `InstallSkill`, HTTPS-only source policy, provenance/hash behavior, and no automatic updates.

- [ ] **Step 2: Run relevant tests and confirm RED if docs are contract-tested**

Run: `dotnet test LocalCodingMcp.Tests -c Release --filter "LandingPageTests|Documentation"`

- [ ] **Step 3: Update documentation**

Include examples:

```text
install_skill(
  source: "https://github.com/owner/repo/blob/main/skills/example/SKILL.md",
  enabled: true
)

check_skill_updates(name: "example")
update_skill_from_source(name: "example")
```

State explicitly that install/update are user-invoked network operations, source content does not gain extra filesystem/shell privileges, and updates are never applied automatically.

- [ ] **Step 4: Run documentation-related tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add README.md DNX.md LocalCodingMcp/README.md LocalCodingMcp.Tests
git commit -m "docs: document remote skill lifecycle"
```

---

### Task 8: Full verification and PR readiness

**Files:**
- Review all files changed by Tasks 1–7.

**Interfaces:**
- No new interfaces; verification gate only.

- [ ] **Step 1: Run the complete Release test suite**

```bash
dotnet test LocalCodingMcp.sln -c Release
```

Expected: all tests pass on the development environment.

- [ ] **Step 2: Build the full solution**

```bash
dotnet build LocalCodingMcp.sln -c Release --no-restore
```

Expected: zero errors; resolve new warnings introduced by this feature.

- [ ] **Step 3: Pack the DNX project**

```bash
dotnet pack LocalCodingMcp.Dnx/LocalCodingMcp.Dnx.csproj -c Release --no-build
```

Expected: `LocalCodingMcp.Dnx` package is produced and includes the new shared services/tools source required by the stdio host.

- [ ] **Step 4: Run the repository's DNX MCP smoke-test path**

Use the same package-local `dnx` invocation and MCP initialization/tool-list assertion currently used by `.github/workflows/dnx.yml`. Extend the expected tool list to include:

```text
install_skill
check_skill_updates
update_skill_from_source
```

Expected: stdio initialization succeeds and all three tools are advertised.

- [ ] **Step 5: Review the diff against the spec**

Confirm: no HTTP/local source support, no auto-update/background work, old skill metadata remains readable, built-ins cannot refresh remotely, multi-skill update checks isolate failures, and remote install is immediately routeable when enabled.

- [ ] **Step 6: Push implementation branch and open a draft PR**

PR description must summarize security model, migration compatibility, tests, and note that merge waits for both CI and DNX Package workflows to be green.
