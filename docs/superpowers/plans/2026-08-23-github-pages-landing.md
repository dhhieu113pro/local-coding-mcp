# LocalCodingMcp GitHub Pages Landing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish a Hallmark-designed LocalCodingMcp landing page at `https://dhhieu113pro.github.io/local-coding-mcp/`, wire it into the existing Pages assembler, and link it from the portfolio homepage.

**Architecture:** Keep product-owned page assets in `dhhieu113pro/local-coding-mcp/docs/`. Extend `dhhieu113pro/dhhieu113pro.github.io` using its existing Quay assembly pattern: check out the product repo during Pages deployment and copy `docs/` into `site/local-coding-mcp/`. Validate the product page in the existing cross-platform .NET CI with xUnit source-level contract tests, then verify the real Pages deployment after both changes land.

**Tech Stack:** Static HTML/CSS/vanilla JS, SVG, xUnit/.NET 10, GitHub Actions, GitHub Pages.

**Spec:** `docs/superpowers/specs/2026-08-23-github-pages-landing-design.md`

## Global Constraints

- Macrostructure is **Map / Diagram**.
- No fake browser, terminal, IDE, or app chrome.
- No invented adoption counts, performance claims, testimonials, logos, or metrics.
- All colors and font families in the page use named CSS tokens.
- Headings remain roman, not italic.
- Theme behavior is System → Light → Dark with an icon-only accessible toggle.
- `html, body { overflow-x: clip; }` and the page must work at 320, 375, 414, and 768 CSS pixels.
- Use semantic landmarks and clear `:focus-visible` styles.
- Do not delete existing production files.
- `/quay/` and the root portfolio must continue to deploy unchanged apart from the Local Coding MCP tile link.

---

### Task 1: Add a failing landing-page contract test

**Files:**
- Create: `LocalCodingMcp.Tests/LandingPageTests.cs`
- Produces: a cross-platform CI contract for the static page before the page exists.

**Interfaces:**
- Consumes: repository layout with `docs/` at the repository root.
- Produces: `LandingPageTests.Page_ContainsRequiredProductAndHallmarkContracts()` and `LandingPageTests.Page_ContainsResponsiveAndThemeContracts()`.

- [ ] **Step 1: Write the failing test**

Create `LocalCodingMcp.Tests/LandingPageTests.cs`:

```csharp
namespace LocalCodingMcp.Tests;

public class LandingPageTests
{
    private static string ReadPage()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "docs", "index.html"));
        return File.ReadAllText(path);
    }

    [Fact]
    public void Page_ContainsRequiredProductAndHallmarkContracts()
    {
        var html = ReadPage();

        Assert.Contains("Hallmark · macrostructure: Map / Diagram", html, StringComparison.Ordinal);
        Assert.Contains("Give your AI tools. Keep your code local.", html, StringComparison.Ordinal);
        Assert.Contains("id=\"how-it-works\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"skills\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"install\"", html, StringComparison.Ordinal);
        Assert.Contains("LoadEnabledSkills", html, StringComparison.Ordinal);
        Assert.Contains("superpowers", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hallmark", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("caveman", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ponytail", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AllowedRoots", html, StringComparison.Ordinal);
        Assert.Contains("https://github.com/dhhieu113pro/local-coding-mcp", html, StringComparison.Ordinal);
        Assert.Contains("https://github.com/dhhieu113pro/local-coding-mcp/releases/latest", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_ContainsResponsiveAndThemeContracts()
    {
        var html = ReadPage();

        Assert.Contains("overflow-x:clip", html.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prefers-color-scheme:dark", html.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prefers-reduced-motion:reduce", html.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"theme-toggle\"", html, StringComparison.Ordinal);
        Assert.Contains("focus-visible", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("font-style:italic", html.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run the focused test to verify RED**

Run:

```bash
dotnet test LocalCodingMcp.Tests/LocalCodingMcp.Tests.csproj -c Release --filter LandingPageTests
```

Expected: FAIL because `docs/index.html` does not exist.

- [ ] **Step 3: Commit the RED test**

```bash
git add LocalCodingMcp.Tests/LandingPageTests.cs
git commit -m "test: define landing page contracts"
```

---

### Task 2: Build the Hallmark Map / Diagram landing page

**Files:**
- Create: `docs/index.html`
- Create: `docs/.nojekyll`
- Reuse: `docs/logo.svg`
- Reuse: `docs/how-it-works.svg` until Task 3 replaces it.

**Interfaces:**
- Consumes: the product facts documented in `README.md`, `SETUP.md`, `TERMUXHOST.md`, `McpServerInstructions`, and the existing logo.
- Produces: a self-contained static landing page with relative asset links suitable for copying into `/local-coding-mcp/`.

- [ ] **Step 1: Create `docs/index.html` with the required page shell and locked tokens**

The CSS must begin with this stamp:

```css
/* Hallmark · macrostructure: Map / Diagram · genre: developer-tool · theme: Local Cyan
 * pre-emit critique: P5 H5 E5 S5 R5 V5
 */
```

The token block must define and then exclusively reference named color/font variables:

```css
:root {
  --color-bg: #f5f7fb;
  --color-surface: #ffffff;
  --color-surface-soft: #edf4ff;
  --color-text: #101828;
  --color-muted: #5d6677;
  --color-line: #ccd7e8;
  --color-accent: #136ef1;
  --color-accent-strong: #064fc4;
  --color-accent-soft: #dcecff;
  --color-map-node: #0b1838;
  --color-map-text: #f7fbff;
  --color-focus: #0b78ff;
  --color-shadow: rgba(17, 36, 72, .10);
  --font-display: "Segoe UI Variable Display", "Segoe UI", system-ui, -apple-system, sans-serif;
  --font-body: "Segoe UI Variable Text", "Segoe UI", system-ui, -apple-system, sans-serif;
  --font-mono: "Cascadia Code", "SFMono-Regular", Consolas, monospace;
}
```

Add corresponding dark-mode token overrides via `@media(prefers-color-scheme:dark)` and explicit `html[data-theme="light"]` / `html[data-theme="dark"]` blocks. No later rule may introduce raw colors or font-family literals.

- [ ] **Step 2: Add semantic page sections**

Use this exact content structure:

```html
<header class="site-header">…navigation + theme toggle…</header>
<main>
  <section class="hero" id="how-it-works">…hero + architecture map…</section>
  <section class="tool-surface" aria-labelledby="tools-title">…indexed tool list…</section>
  <section class="skill-flow" id="skills" aria-labelledby="skills-title">…LoadEnabledSkills flow…</section>
  <section class="guardrails" aria-labelledby="security-title">…security guardrails…</section>
  <section class="install" id="install" aria-labelledby="install-title">…commands + links…</section>
</main>
<footer>…project metadata…</footer>
```

The architecture map contains four real nodes labelled `YOUR CODE`, `LOCALCODINGMCP`, `HTTPS`, and `AI CLIENT`; desktop layout is horizontal and mobile layout becomes vertical.

- [ ] **Step 3: Add the real tool surface and skill flow**

Tool groups must be exactly: Workspace, Files, Search, Git, Shell, History, Skills.

Skill flow must visibly communicate:

```text
User request → LoadEnabledSkills → relevant enabled skills → coding tools
```

Include factual built-in labels `superpowers`, `hallmark`, `caveman`, `ponytail`, state that built-ins start disabled, state persists, and host/model compliance is not guaranteed by the server alone.

- [ ] **Step 4: Add security and install content**

Guardrails must include: AllowedRoots, traversal/symlink protection, sensitive-file filtering, shell timeout, sanitized persisted history, and constrained skill names.

Install block must contain:

```bash
git clone https://github.com/dhhieu113pro/local-coding-mcp.git
cd local-coding-mcp
docker compose up -d
docker compose --profile ngrok up -d
```

Add links to repository root, `/releases/latest`, `SETUP.md`, and `TERMUXHOST.md` using absolute GitHub URLs so they work after copying to the Pages repository.

- [ ] **Step 5: Add theme behavior and responsive rules**

Use the same localStorage key as the portfolio site, `site-theme`, and cycle `auto → light → dark → auto`. On auto, remove `document.documentElement.dataset.theme`; on explicit modes set it. The toggle updates `data-mode`, `aria-label`, and `title` to describe the current mode and next action.

CSS requirements:

```css
html,body{margin:0;overflow-x:clip}
.hero h1,.section-title{overflow-wrap:anywhere;min-width:0;font-style:normal}
.map-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr))}
@media(max-width:720px){.map-grid{grid-template-columns:minmax(0,1fr)}}
@media(prefers-reduced-motion:reduce){html{scroll-behavior:auto}*{transition-duration:.01ms!important;animation-duration:.01ms!important}}
```

Keep nav/CTA labels one line at 320px by hiding nonessential anchor links before shrinking the primary actions.

- [ ] **Step 6: Create `docs/.nojekyll`**

Create an empty file.

- [ ] **Step 7: Run the landing tests to verify GREEN**

```bash
dotnet test LocalCodingMcp.Tests/LocalCodingMcp.Tests.csproj -c Release --filter LandingPageTests
```

Expected: PASS, 0 failures.

- [ ] **Step 8: Commit the page**

```bash
git add docs/index.html docs/.nojekyll
git commit -m "feat: add LocalCodingMcp landing page"
```

---

### Task 3: Update the architecture diagram for skill-aware MCP

**Files:**
- Modify: `docs/how-it-works.svg`
- Modify: `LocalCodingMcp.Tests/LandingPageTests.cs`

**Interfaces:**
- Consumes: `LoadEnabledSkills` and server instructions already on `main`.
- Produces: source documentation/landing asset that reflects the current runtime flow.

- [ ] **Step 1: Extend the test before editing the SVG**

Add this test:

```csharp
[Fact]
public void ArchitectureDiagram_ShowsSkillAwareFlow()
{
    var path = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "docs", "how-it-works.svg"));
    var svg = File.ReadAllText(path);

    Assert.Contains("LoadEnabledSkills", svg, StringComparison.Ordinal);
    Assert.Contains("ChatGPT / Grok", svg, StringComparison.Ordinal);
    Assert.Contains("AllowedRoots", svg, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run focused test to verify RED**

```bash
dotnet test LocalCodingMcp.Tests/LocalCodingMcp.Tests.csproj -c Release --filter ArchitectureDiagram_ShowsSkillAwareFlow
```

Expected: FAIL because the current SVG still shows `OpenWorkspace` as the final client note and has no `LoadEnabledSkills` label.

- [ ] **Step 3: Edit `docs/how-it-works.svg`**

Preserve the four-stage architecture but update the client/MCP notes so the bottom flow reads:

```text
Connect → LoadEnabledSkills for coding work → use relevant skills → call sandboxed tools
```

Add a visible `LoadEnabledSkills` label near the MCP/client path and retain `AllowedRoots` in the safety copy. Do not add fake UI chrome.

- [ ] **Step 4: Run the focused test and full suite**

```bash
dotnet test LocalCodingMcp.Tests/LocalCodingMcp.Tests.csproj -c Release --filter ArchitectureDiagram_ShowsSkillAwareFlow
dotnet test LocalCodingMcp.sln -c Release
```

Expected: both commands PASS with 0 failures.

- [ ] **Step 5: Commit the diagram update**

```bash
git add docs/how-it-works.svg LocalCodingMcp.Tests/LandingPageTests.cs
git commit -m "docs: show skill-aware MCP architecture"
```

---

### Task 4: Wire LocalCodingMcp into the GitHub Pages assembler

**Repository:** `dhhieu113pro/dhhieu113pro.github.io`

**Files:**
- Modify: `.github/workflows/pages.yml`
- Modify: `index.html`

**Interfaces:**
- Consumes: `dhhieu113pro/local-coding-mcp` `main` with `docs/index.html` and product assets.
- Produces: `/local-coding-mcp/` in the assembled Pages artifact and a root-homepage link to it.

- [ ] **Step 1: Create a feature branch from current `main`**

```text
feat/local-coding-mcp-landing
```

- [ ] **Step 2: Extend `.github/workflows/pages.yml` without disturbing Quay**

Add after `Checkout Quay`:

```yaml
      - name: Checkout LocalCodingMcp
        uses: actions/checkout@v4
        with:
          repository: dhhieu113pro/local-coding-mcp
          ref: main
          path: local-coding-mcp-source
```

Replace the assemble body with:

```bash
set -euo pipefail
mkdir -p site/quay site/local-coding-mcp
cp pages/index.html site/index.html
cp -R quay-source/docs/. site/quay/
cp -R local-coding-mcp-source/docs/. site/local-coding-mcp/
test -f site/local-coding-mcp/index.html
test -f site/local-coding-mcp/logo.svg
test -f site/local-coding-mcp/how-it-works.svg
touch site/.nojekyll
```

- [ ] **Step 3: Update the Local Coding MCP tile link in `index.html`**

Change only the tile anchor URL from:

```html
href="https://github.com/dhhieu113pro/local-coding-mcp"
```

to:

```html
href="./local-coding-mcp/"
```

Keep the tile's text/content and the rest of the homepage unchanged.

- [ ] **Step 4: Review the Pages diff**

Verify the diff contains only:

- one new checkout in `pages.yml`
- one new output directory/copy + three `test -f` guards
- one homepage href change

- [ ] **Step 5: Commit and open a PR**

```bash
git add .github/workflows/pages.yml index.html
git commit -m "feat: publish LocalCodingMcp landing page"
```

Open the PR against `main` only after the LocalCodingMcp product PR is merged, because the Pages workflow checks out `local-coding-mcp@main`.

---

### Task 5: Hallmark review and final verification

**Files:**
- Modify only if review finds a concrete issue: `docs/index.html`
- Verify: LocalCodingMcp PR CI, Pages deployment, live URL.

**Interfaces:**
- Consumes: completed Tasks 1–4.
- Produces: evidence that the design and deploy meet the approved spec.

- [ ] **Step 1: Run Hallmark pre-emit review against the page source**

Score Philosophy, Hierarchy, Execution, Specificity, Restraint, Variety from 1–5. Any score below 3 requires revision before merge.

Explicitly check:

```text
no generic 3-card feature row
no fake browser/terminal/IDE chrome
no invented metrics/testimonials/logos
no italic headings
no raw color/font declarations outside token blocks
no horizontal-scroll-prone bare 1fr tracks for content-bearing grids
no two-line primary clickable labels at 320px
focus-visible present
auto/light/dark theme states present
reduced-motion present
```

- [ ] **Step 2: Verify LocalCodingMcp CI on the final PR head**

Require success for:

```text
Test (ubuntu-latest)
Test (macos-latest)
Test (windows-latest)
Coverage (Linux)
```

Do not claim success from an earlier commit.

- [ ] **Step 3: Merge the LocalCodingMcp PR**

Use squash merge after final-head CI is green.

- [ ] **Step 4: Rebase/recheck the Pages branch against current main and open/merge its PR**

Confirm the workflow still references `local-coding-mcp` `main`, then merge.

- [ ] **Step 5: Verify the GitHub Pages deployment run**

Require the `Pages` workflow on the Pages merge commit to finish with `conclusion: success`.

- [ ] **Step 6: Verify the live page**

Fetch `https://dhhieu113pro.github.io/local-coding-mcp/` and confirm it contains:

```text
LocalCodingMcp
Give your AI tools. Keep your code local.
LoadEnabledSkills
```

Also fetch `https://dhhieu113pro.github.io/` and confirm its Local Coding MCP tile links to `/local-coding-mcp/`.

Only after those checks report the feature complete.
