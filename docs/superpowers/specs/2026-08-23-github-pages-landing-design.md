# LocalCodingMcp GitHub Pages Landing Design

**Date:** 2026-08-23

## Goal

Publish a dedicated LocalCodingMcp landing page at `https://dhhieu113pro.github.io/local-coding-mcp/` that explains the product through its real architecture, highlights skill-aware MCP behavior, gives factual setup/download paths, and visually fits the existing `dhhieu113pro.github.io` site without becoming another generic SaaS page.

## Ownership and deployment

The product page source lives with the product in `dhhieu113pro/local-coding-mcp/docs/`. The `dhhieu113pro/dhhieu113pro.github.io` repository remains the site assembler and deployer, matching the existing Quay pattern: its Pages workflow checks out the product repository and copies `docs/` into `site/local-coding-mcp/`.

No production files are deleted.

## Hallmark direction

- **Macrostructure:** Map / Diagram.
- **Primary idea:** the architecture is the hero: local workspace → LocalCodingMcp → HTTPS tunnel → ChatGPT/Grok.
- Avoid the generic hero → three feature cards → CTA → footer rhythm.
- Use the real LocalCodingMcp logo and architecture concepts; do not draw fake browser, terminal, IDE, or app chrome.
- Keep copy factual. Do not invent adoption counts, performance claims, testimonials, logos, or metrics.
- Use named CSS tokens for all colors and font families.
- Headings remain roman, not italic.
- Use restrained motion and honor `prefers-reduced-motion`.

## Visual system

Use the existing logo's deep navy and cyan as the LocalCodingMcp identity, adapted into light and dark token sets. The page should share the parent site's understated Segoe/Cascadia family and its system/light/dark theme behavior so navigation feels related, but the page structure and accent treatment should be product-specific.

The header contains:

- `LocalCodingMcp` wordmark/home link
- `How it works`
- `Skills`
- `Install`
- `GitHub ↗`
- icon-only theme toggle cycling System → Light → Dark

## Page structure

### 1. Architecture-first hero

Opening copy: **“Give your AI tools. Keep your code local.”**

Supporting copy explains that LocalCodingMcp is a secure .NET 10 MCP server for ChatGPT, Grok, and other MCP clients, exposing approved local workspaces through sandboxed file, git, shell, search, history, and skill tools.

The hero's dominant visual is a responsive semantic system map with four nodes:

1. `YOUR CODE` — approved project directory / `AllowedRoots`
2. `LOCALCODINGMCP` — local .NET 10 MCP server / `/mcp`
3. `HTTPS` — ngrok or another HTTPS tunnel
4. `AI CLIENT` — ChatGPT, Grok, or another MCP client

Arrows communicate the path. The diagram must collapse into a vertical flow on narrow screens.

Primary links below the map:

- `Get started` → install section
- `View on GitHub ↗`

### 2. Tool surface

Use a compact indexed list rather than equal feature cards. Show the real tool groups:

- Workspace — open only configured roots
- Files — list/read/write/patch/binary operations
- Search — regex/text code search
- Git — status/diff/log
- Shell — commands inside the workspace with timeout
- History — persisted sanitized tool-call history
- Skills — reusable `SKILL.md` content and enable/disable state

### 3. Skill-aware flow

Make the new skill behavior a first-class diagram:

`User request → LoadEnabledSkills → relevant enabled skills → coding tools`

Show the built-ins factually: `superpowers`, `hallmark`, `caveman`, `ponytail`. State that built-ins are disabled by default and enabled state persists. Explain that MCP server instructions tell compatible clients to load enabled skills before coding/debugging/design/planning/review tasks; final compliance still depends on the MCP host/model.

### 4. Security guardrails

Use a dense horizontal/stacked guardrail list, not marketing cards:

- AllowedRoots sandbox
- path traversal / symlink escape protection
- sensitive-file filtering
- shell timeout
- sanitized persisted execution history
- skill names constrained to safe local directories

### 5. Install / run

Use plain semantic `<pre><code>` blocks with no fake terminal chrome.

Docker quick start:

```bash
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
docker compose up -d
docker compose --profile ngrok up -d
```

Also surface:

- GitHub repository
- latest GitHub release
- TermuxHost release ZIP documentation
- setup documentation

Do not embed release version numbers that can become stale; link to `/releases/latest`.

### 6. Footer

Compact metadata-only footer with project name, `.NET 10 · MCP · MIT`, GitHub link, and a link back to the main project index.

## Parent homepage change

Update the existing `Local Coding MCP` project tile on `dhhieu113pro.github.io/index.html` so its primary link becomes `./local-coding-mcp/` rather than the GitHub repository. Keep the tile copy and layout otherwise unchanged unless needed for accessibility.

## Deployment workflow change

Extend `dhhieu113pro.github.io/.github/workflows/pages.yml`:

- check out `dhhieu113pro/local-coding-mcp` at `main` into `local-coding-mcp-source`
- create `site/local-coding-mcp`
- copy `local-coding-mcp-source/docs/.` into that directory
- keep current root and Quay assembly behavior unchanged

## Responsive and accessibility requirements

Verify the page at 320, 375, 414, and 768 CSS pixels plus desktop.

- `html, body { overflow-x: clip; }`
- no horizontal scrolling
- clickable nav/CTA text remains one line at mobile widths
- diagram nodes use `minmax(0, 1fr)` where grid tracks contain content
- display headings use `overflow-wrap: anywhere; min-width: 0`
- clear `:focus-visible` treatment for links/buttons
- theme toggle has an accessible label/title describing current/next mode
- semantic landmarks (`header`, `main`, `section`, `footer`)
- meaningful link text and no color-only state communication

## Verification

LocalCodingMcp repository:

- existing .NET CI remains green
- static page sanity checks assert the Hallmark stamp, required sections/copy, theme controls, mobile overflow rule, and factual links

Pages repository:

- Pages workflow syntax remains valid
- workflow assembles root, `/quay/`, and `/local-coding-mcp/`
- after merge, GitHub Pages deployment succeeds
- public URL returns the new page and its expected title/content

## Success criteria

The feature is complete when the LocalCodingMcp page is publicly reachable at `/local-coding-mcp/`, the root portfolio links to it, the page accurately communicates the skill-aware architecture and security model, light/dark/system theme behavior works, responsive requirements are covered, and the deployment workflow succeeds.