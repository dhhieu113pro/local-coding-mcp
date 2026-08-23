using System.ComponentModel;
using System.Text.Json;
using LocalCodingMcp.Services;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tools;

[McpServerToolType]
public sealed class SkillTools
{
    private readonly SkillStore _skills;

    public SkillTools(SkillStore skills)
    {
        _skills = skills;
    }

    [McpServerTool, Description("List all locally stored skills, including whether each skill is enabled and built in.")]
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
            license = skill.License
        }));
    }

    [McpServerTool, Description("Return complete content for every currently enabled skill.")]
    public string GetEnabledSkills()
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
            license = skill.License
        });
    }

    [McpServerTool, Description("Create a new local skill as <skills-directory>/<name>/SKILL.md. New custom skills are enabled by default.")]
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

    [McpServerTool, Description("Enable or disable a skill without deleting it. State persists across server restarts.")]
    public string SetSkillEnabled(
        [Description("Existing skill name")] string name,
        [Description("true to enable, false to disable")] bool enabled)
    {
        var skill = _skills.SetEnabled(name, enabled);
        return JsonSerializer.Serialize(new
        {
            name = skill.Name,
            enabled = skill.Enabled,
            built_in = skill.BuiltIn
        });
    }

    [McpServerTool, Description("Delete an existing custom skill directory and all files inside it. Built-in skills must be disabled instead.")]
    public string DeleteSkill([Description("Skill name")] string name)
    {
        var deleted = _skills.Delete(name);
        return JsonSerializer.Serialize(new
        {
            deleted,
            name
        });
    }
}
