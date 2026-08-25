# Remote Skill Install and Update Design

## Goal

Add safe remote skill installation and update/provenance support to LocalCodingMcp without changing the existing local/manual skill CRUD behavior.

## User experience

The MCP exposes three new operations:

- `install_skill(source, enabled = true, name = null)` — install a remote `SKILL.md` from a supported source.
- `check_skill_updates(name = null)` — compare installed remote skills with their recorded upstream state. With no name, check every remotely installed skill.
- `update_skill_from_source(name)` — re-fetch the recorded source, validate it, and replace the local `SKILL.md` while preserving the skill's enabled state.

Typical flow:

```text
install_skill("https://github.com/owner/repo/blob/main/path/SKILL.md")
  -> validate source
  -> resolve/fetch raw SKILL.md
  -> validate front matter and skill name
  -> compute SHA-256 content hash
  -> persist SKILL.md + provenance metadata
  -> enabled and immediately eligible for route_skills

check_skill_updates("my-skill")
  -> fetch source
  -> compare SHA-256 against installed hash
  -> return up_to_date / update_available / unavailable

update_skill_from_source("my-skill")
  -> fetch + validate new content
  -> atomically replace SKILL.md
  -> refresh provenance metadata
  -> preserve enabled state
```

## Supported sources

Initial scope supports HTTPS only:

1. `raw.githubusercontent.com/.../SKILL.md`
2. GitHub `github.com/<owner>/<repo>/blob/<ref>/<path>` URLs, normalized to the corresponding raw-content URL.
3. Generic HTTPS URLs that directly return Markdown/plain-text skill content.

No `file://`, local paths, redirects to non-HTTPS destinations, embedded credentials, or arbitrary protocols.

The fetcher follows only a small redirect limit and rejects downgrade redirects from HTTPS to HTTP.

## Trust and validation

Remote installation is an explicit tool call; the server never auto-installs a skill because another skill asks it to.

Before writing anything, LocalCodingMcp validates:

- HTTPS source URL and supported scheme.
- Maximum response size (default 1 MiB) to avoid unbounded downloads.
- Successful text response; binary content is rejected.
- `SKILL.md` content is non-empty.
- YAML-style front matter contains a valid `name` and non-empty `description`.
- Resolved skill name obeys the existing `SkillStore` name constraints.
- If caller supplied `name`, it must match the front-matter name unless an explicit future override feature is introduced.

Remote content is treated as instructions only. Installing/updating a skill does not grant it filesystem or shell permissions beyond the MCP's existing tool/sandbox protections.

## Provenance metadata

Extend `.skill.json` compatibly with optional fields:

```json
{
  "enabled": true,
  "builtIn": false,
  "sourceUrl": "https://github.com/owner/repo/blob/main/path/SKILL.md",
  "resolvedSourceUrl": "https://raw.githubusercontent.com/owner/repo/main/path/SKILL.md",
  "license": null,
  "contentSha256": "...",
  "sourceEtag": "...",
  "sourceLastModified": "...",
  "installedAt": "2026-08-25T...Z",
  "updatedAt": "2026-08-25T...Z"
}
```

All new fields are optional so existing `.skill.json` files deserialize exactly as before. Locally created skills continue to have no remote provenance.

`sourceUrl` is the user-facing original URL. `resolvedSourceUrl` is the normalized fetch URL. `contentSha256` is authoritative for update comparison; ETag/Last-Modified are optimization hints only.

## Components

### `RemoteSkillFetcher`

A small service wrapping `HttpClient` responsibilities:

- URL validation/normalization.
- Bounded HTTP download.
- redirect policy.
- response text validation.
- SHA-256 calculation.
- conditional requests using ETag/Last-Modified when available.

It returns a transport-neutral record containing original URL, resolved URL, content, hash, ETag and Last-Modified.

### `SkillDocumentParser`

Extracts only the metadata LocalCodingMcp needs from front matter: `name`, `description`, and optional `license`. It does not implement a general YAML engine unless the existing dependencies already provide one; the initial parser should remain narrow and deterministic.

### `SkillStore`

Add store operations for remote installation/update metadata and atomic replacement. Existing `Create`, `Update`, `Delete`, `SetEnabled`, `List`, `Get`, and built-in behavior remain compatible.

Atomic update strategy: write the new skill content/metadata to temporary files in the skill directory, then replace the target files only after all validation succeeds. A failed download or invalid skill must leave the installed skill unchanged.

### `SkillTools`

Add MCP tool methods with snake-case wire names derived by the MCP SDK:

- `InstallSkill`
- `CheckSkillUpdates`
- `UpdateSkillFromSource`

Existing `CreateSkill` remains the manual/content-based installation path.

## Update semantics

`check_skill_updates` returns per-skill records containing at least:

```text
name
source_url
installed_sha256
remote_sha256
status: up_to_date | update_available | unavailable
message (when unavailable)
```

Network or upstream errors in a multi-skill check should be reported per skill and should not abort checking the remaining skills.

`update_skill_from_source` refuses built-in skills and skills with no recorded remote source. It preserves `enabled`, refreshes content/provenance, and returns old/new hashes plus whether content actually changed.

## Routing integration

No routing algorithm changes are required. Once a remotely installed skill is enabled, its front-matter description automatically participates in the existing deterministic `route_skills` flow.

## Configuration

Add optional configuration with conservative defaults:

```text
Skills__Remote__MaxBytes=1048576
Skills__Remote__TimeoutSeconds=15
Skills__Remote__MaxRedirects=3
```

No allowlist is required initially, but only HTTPS is accepted. Existing sandbox settings are unrelated and remain unchanged.

## Tests

Follow TDD. Coverage must include:

- GitHub blob URL normalization.
- raw GitHub and generic HTTPS installation.
- HTTP/non-HTTPS rejection.
- redirect downgrade rejection.
- oversized/binary/empty response rejection.
- malformed/missing front matter rejection.
- invalid or mismatched skill name rejection.
- provenance/hash persisted across store instances.
- installed remote skill participates in routing when enabled.
- `check_skill_updates`: current, changed, unavailable, and mixed multi-skill results.
- `update_skill_from_source`: preserves enabled state, refreshes metadata, rejects local-only/built-in skills, and leaves current files intact on invalid upstream content.
- both HTTP and DNX hosts resolve the same remote-skill services/tools.

CI and DNX package smoke tests must remain green.

## Documentation

Update the root README, `DNX.md`, and tool reference with install/check/update examples, provenance behavior, and the security model. The docs should explicitly distinguish `CreateSkill` (caller supplies content) from `InstallSkill` (server fetches an explicitly supplied HTTPS source).

## Out of scope

- Skill marketplaces/search/catalog browsing.
- Automatic installation of dependencies declared by a skill.
- Git cloning repositories.
- Authentication for private GitHub repositories.
- Scheduled/background update polling.
- Automatic updates without an explicit MCP call.
- Structured `triggers`, `priority`, or `requires` routing metadata; that remains a later change.
