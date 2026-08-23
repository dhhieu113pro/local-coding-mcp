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

    [McpServerTool, Description("List all locally stored skills. Each skill is stored in <skills-directory>/<name>/SKILL.md.")]
    public string ListSkills()
    {
        return JsonSerializer.Serialize(_skills.List().Select(skill => new
        {
            name = skill.Name,
            path = skill.Path,
            modified_at = skill.ModifiedAt
        }));
    }

    [McpServerTool, Description("Read a local skill and return its complete SKILL.md content.")]
    public string GetSkill([Description("Skill name")] string name)
    {
        var skill = _skills.Get(name);
        return JsonSerializer.Serialize(new
        {
            name = skill.Name,
            path = skill.Path,
            content = skill.Content,
            modified_at = skill.ModifiedAt
        });
    }

    [McpServerTool, Description("Create a new local skill as <skills-directory>/<name>/SKILL.md.")]
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
            modified_at = skill.ModifiedAt
        });
    }

    [McpServerTool, Description("Delete an existing local skill directory and all files inside it.")]
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
