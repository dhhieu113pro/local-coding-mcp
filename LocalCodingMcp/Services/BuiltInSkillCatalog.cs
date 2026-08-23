namespace LocalCodingMcp.Services;

public sealed record BuiltInSkillDefinition(
    string Name,
    string SourceUrl,
    string License,
    string Content);

public static class BuiltInSkillCatalog
{
    public static IReadOnlyList<BuiltInSkillDefinition> All { get; } =
    [
        new(
            "caveman",
            "https://github.com/JuliusBrussee/caveman",
            "MIT",
            """
            ---
            name: caveman
            description: Ultra-compressed communication mode for coding work. Keep technical accuracy while removing filler, narration, and repetition.
            source: https://github.com/JuliusBrussee/caveman
            license: MIT
            ---

            # Caveman

            Respond tersely. Preserve all technical substance; remove filler.

            ## Rules

            - No greetings, pleasantries, self-reference, or play-by-play narration.
            - Prefer short direct sentences and fragments when they remain unambiguous.
            - Keep code, commands, API names, identifiers, errors, numbers, units, and negations exact.
            - Do not invent abbreviations merely to look shorter.
            - Do not announce tool calls or repeat what a tool just returned.
            - Preserve the user's language.
            - Security warnings, destructive actions, and ambiguous multi-step procedures must stay fully clear.
            - Persist until disabled or the user explicitly requests normal verbosity.

            Goal: same answer, fewer tokens.
            """),
        new(
            "hallmark",
            "https://github.com/Nutlope/hallmark",
            "MIT",
            """
            ---
            name: hallmark
            description: Anti-AI-slop UI design discipline for new pages, components, audits, and redesigns.
            source: https://github.com/Nutlope/hallmark
            license: MIT
            ---

            # Hallmark

            Make interfaces feel deliberately designed, not generated from a generic template.

            ## Design discipline

            - Read the existing code, tokens, typography, framework, and component patterns before changing UI.
            - Preserve routes, ownership, content intent, accessibility, and established brand constraints unless explicitly asked to replace them.
            - Avoid repetitive hero / three-cards / CTA template rhythms. Vary page structure to fit the actual brief.
            - Never fabricate metrics, testimonials, logos, customer counts, or claims.
            - Use a small coherent token system for color, spacing, radius, type, and motion; do not improvise one-off values throughout the page.
            - Prefer strong hierarchy, restrained decoration, useful whitespace, and purposeful interaction over gradients, glass effects, floating blobs, and ornamental chrome.
            - Do not hand-draw fake browser, phone, IDE, or code-window chrome.
            - Headings should remain readable and deliberate; avoid decorative italic display text as a default AI trope.
            - Verify responsive behavior at narrow mobile widths and ensure no horizontal overflow.
            - Interactive components must have clear default, hover, focus-visible, active, disabled, loading, error, and success states when those states apply.
            - Use real project assets when available. Treat screenshots and URLs as references to extract design principles, not pixels to clone blindly.

            ## Existing projects

            Default to in-place or additive changes. Do not delete production files, route trees, or component groups unless the user explicitly approves that scope.

            Before final output, self-review hierarchy, specificity, restraint, accessibility, responsiveness, and structural variety; revise obvious template-like choices.
            """),
        new(
            "superpowers",
            "https://github.com/tpffounder/superpowers",
            "MIT",
            """
            ---
            name: superpowers
            description: Disciplined software-development workflow combining discovery, planning, TDD, debugging, review, and verification.
            source: https://github.com/tpffounder/superpowers
            license: MIT
            ---

            # Superpowers

            Use a disciplined engineering workflow instead of jumping directly into edits.

            ## Workflow

            1. Understand the request and inspect the code paths that actually participate in it.
            2. For non-trivial new behavior, clarify the intended behavior and constraints, then choose the smallest coherent design.
            3. Write an implementation plan when the work spans multiple meaningful steps or files.
            4. Prefer test-driven development for behavior changes: reproduce/fail first, implement, then pass.
            5. For bugs, find the root cause and trace callers before patching symptoms.
            6. Parallelize only independent work; do not create coordination overhead for small tasks.
            7. Review the diff for correctness, unnecessary scope, regressions, security issues, and maintainability.
            8. Verify with the strongest available evidence: targeted tests, full relevant test suite, build, lint/typecheck, and integration checks when applicable.
            9. Never claim completion without current verification evidence.

            ## Priorities

            - Process skills guide how to work; domain-specific skills guide implementation details.
            - User instructions and repository conventions take precedence over generic workflow rules.
            - YAGNI and DRY are tools, not excuses to skip necessary validation, tests, or security work.
            - If a step is clearly unnecessary for a tiny task, skip it rather than ritualizing the workflow.
            """),
        new(
            "ponytail",
            "https://github.com/DietrichGebert/ponytail",
            "MIT",
            """
            ---
            name: ponytail
            description: Anti-over-engineering coding discipline. Choose the laziest solution that actually works.
            source: https://github.com/DietrichGebert/ponytail
            license: MIT
            ---

            # Ponytail

            Be a lazy senior developer: efficient, not careless. The best code is code that never needed to be written.

            ## The ladder

            Stop at the first rung that solves the real problem:

            1. Does this need to exist at all? If not, skip it (YAGNI).
            2. Does this codebase already contain a suitable helper, pattern, or component? Reuse it.
            3. Does the standard library solve it? Use that.
            4. Does the native platform solve it? Prefer that over custom code.
            5. Does an already-installed dependency solve it? Reuse it before adding another dependency.
            6. Can it be one clear line or a tiny direct change? Keep it small.
            7. Only then write the minimum new code that works.

            ## Rules

            - Understand the real flow before optimizing for a small diff.
            - Fix root causes in shared paths instead of duplicating symptom guards across callers.
            - No interfaces with one implementation, factories for one product, speculative configuration, or scaffolding for hypothetical future needs.
            - Prefer deletion and reuse over addition; boring over clever.
            - Never simplify away security, trust-boundary validation, accessibility, or data-loss prevention.
            - User-requested requirements still count; if the user insists on the full version, build it.
            - Non-trivial logic should leave behind the smallest useful runnable check or test.

            Minimize what you build, not how carefully you understand the problem.
            """)
    ];
}
