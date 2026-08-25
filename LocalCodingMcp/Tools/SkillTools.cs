using System.ComponentModel;
using System.Text.Json;
using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tools;

[McpServerToolType]
public sealed class SkillTools
{
    private readonly SkillStore _skills;
    private readonly SkillRouter _router;
    private readonly RemoteSkillService? _remoteSkills;

    public SkillTools(SkillStore skills) : this(skills, null) { }

    public SkillTools(SkillStore skills, RemoteSkillService? remoteSkills)
    {
        _skills = skills;
        _router = new SkillRouter(skills);
        _remoteSkills = remoteSkills;
    }

    [McpServerTool, Description("List all locally stored skills, including whether each skill is enabled, built in, and remotely sourced.")]
    public string ListSkills()
    {
        return JsonSerializer.Serialize(_skills.List().Select(skill => new
        {
            name = skill.Name,
            path = skill.Path,
            modified_at = skill.ModifiedAt,
            enabled = skill.Enabled,
            built_in = skill.BuiltIn,
            source_url = skill.SourceUrl,
            resolved_source_url = skill.ResolvedSourceUrl,
            content_sha256 = skill.ContentSha256,
            license = skill.License
        }));
    }

    [McpServerTool, Description("Rank enabled skills for a task without loading full SKILL.md content. Call this first for coding, debugging, design, planning, or review work, then load only the recommended skills with load_skills.")]
    public string RouteSkills([Description("The user's current task or goal")] string task)
    {
        return JsonSerializer.Serialize(_router.Route(task).Select(route => new
        {
            name = route.Name,
            description = route.Description,
            reason = route.Reason,
            score = route.Score
        }));
    }

    [McpServerTool, Description("Load complete SKILL.md content for selected enabled skills returned by route_skills. Disabled skills are ignored.")]
    public string LoadSkills([Description("Skill names to load")] string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var selected = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(_skills.Get)
            .Where(skill => skill.Enabled)
            .Select(skill => new
            {
                name = skill.Name,
                content = skill.Content,
                built_in = skill.BuiltIn,
                source_url = skill.SourceUrl,
                license = skill.License
            });
        return JsonSerializer.Serialize(selected);
    }

    [McpServerTool, Description("Load complete content for every enabled skill. Kept for backward compatibility; prefer route_skills followed by load_skills to avoid loading unrelated skills.")]
    public string LoadEnabledSkills()
    {
        return JsonSerializer.Serialize(_skills.ListEnabled().Select(skill => new
        {
            name = skill.Name,
            content = skill.Content,
            built_in = skill.BuiltIn,
            source_url = skill.SourceUrl,
            license = skill.License
        }));
    }

    [McpServerTool, Description("Install a skill from an explicitly supplied HTTPS SKILL.md source. Validates front matter, records source provenance and SHA-256, and optionally enables it.")]
    public async Task<string> InstallSkill(
        [Description("HTTPS URL to a raw SKILL.md, GitHub blob SKILL.md, or direct text/Markdown skill document.")] string source,
        [Description("Whether the installed skill should be enabled immediately.")] bool enabled = true,
        [Description("Optional expected skill name; must match SKILL.md front matter when supplied.")] string? name = null,
        CancellationToken cancellationToken = default)
    {
        var remote = RequireRemote();
        var skill = await remote.InstallAsync(source, enabled, name, cancellationToken);
        return JsonSerializer.Serialize(new
        {
            installed = true,
            name = skill.Name,
            enabled = skill.Enabled,
            source_url = skill.SourceUrl,
            resolved_source_url = skill.ResolvedSourceUrl,
            content_sha256 = skill.ContentSha256,
            license = skill.License
        });
    }

    [McpServerTool, Description("Check remotely installed skills for upstream changes without applying updates. Returns per-skill up_to_date, update_available, or unavailable status.")]
    public async Task<string> CheckSkillUpdates(
        [Description("Optional skill name. Omit to check all remotely installed custom skills.")] string? name = null,
        CancellationToken cancellationToken = default)
    {
        var statuses = await RequireRemote().CheckUpdatesAsync(name, cancellationToken);
        return JsonSerializer.Serialize(statuses.Select(status => new
        {
            name = status.Name,
            source_url = status.SourceUrl,
            installed_sha256 = status.InstalledSha256,
            remote_sha256 = status.RemoteSha256,
            status = status.Status,
            message = status.Message
        }));
    }

    [McpServerTool, Description("Explicitly refresh a remotely installed custom skill from its recorded HTTPS source. Preserves enabled state and never updates automatically.")]
    public async Task<string> UpdateSkillFromSource(
        [Description("Existing remotely installed skill name.")] string name,
        CancellationToken cancellationToken = default)
    {
        var result = await RequireRemote().UpdateFromSourceAsync(name, cancellationToken);
        return JsonSerializer.Serialize(new
        {
            name = result.Name,
            old_sha256 = result.OldSha256,
            new_sha256 = result.NewSha256,
            changed = result.Changed,
            enabled = result.Skill.Enabled,
            source_url = result.Skill.SourceUrl
        });
    }

    [McpServerTool, Description("Read a local skill and return its complete SKILL.md content and state.")]
    public string GetSkill([Description("Skill name")] string name)
    {
        var skill = _skills.Get(name);
        return JsonSerializer.Serialize(new
        {
            name = skill.Name,
            path = skill.Path,
            content = skill.Content,
            modified_at = skill.ModifiedAt,
            enabled = skill.Enabled,
            built_in = skill.BuiltIn,
            source_url = skill.SourceUrl,
            resolved_source_url = skill.ResolvedSourceUrl,
            content_sha256 = skill.ContentSha256,
            license = skill.License
        });
    }

    [McpServerTool, Description("Create a new local skill from caller-supplied content as <skills-directory>/<name>/SKILL.md. This does not fetch the network. New custom skills are enabled by default.")]
    public string CreateSkill(
        [Description("Skill name. Use letters, numbers, '.', '_' or '-' only, max 64 characters.")] string name,
        [Description("Complete SKILL.md content, including any front matter and instructions.")] string content)
    {
        var skill = _skills.Create(name, content);
        return JsonSerializer.Serialize(new
        {
            created = true,
            name = skill.Name,
            path = skill.Path,
            enabled = skill.Enabled,
            built_in = skill.BuiltIn,
            modified_at = skill.ModifiedAt
        });
    }

    [McpServerTool, Description("Replace the complete SKILL.md content for an existing local skill.")]
    public string UpdateSkill(
        [Description("Existing skill name")] string name,
        [Description("Replacement SKILL.md content")] string content)
    {
        var skill = _skills.Update(name, content);
        return JsonSerializer.Serialize(new
        {
            updated = true,
            name = skill.Name,
            path = skill.Path,
            enabled = skill.Enabled,
            built_in = skill.BuiltIn,
            modified_at = skill.ModifiedAt
        });
    }

    [McpServerTool, Description("Enable or disable a skill without deleting it. State persists across server restarts and is treated as an explicit user preference.")]
    public string SetSkillEnabled(
        [Description("Existing skill name")] string name,
        [Description("true to enable, false to disable")] bool enabled)
    {
        var skill = _skills.SetEnabledFromUser(name, enabled);
        return JsonSerializer.Serialize(new { name = skill.Name, enabled = skill.Enabled, built_in = skill.BuiltIn });
    }

    [McpServerTool, Description("Delete an existing custom skill directory and all files inside it. Built-in skills must be disabled instead.")]
    public string DeleteSkill([Description("Skill name")] string name)
    {
        var deleted = _skills.Delete(name);
        return JsonSerializer.Serialize(new { deleted, name });
    }

    private RemoteSkillService RequireRemote()
        => _remoteSkills ?? throw new InvalidOperationException("Remote skill services are not configured for this SkillTools instance.");
}
