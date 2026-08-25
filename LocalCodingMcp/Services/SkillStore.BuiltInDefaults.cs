namespace LocalCodingMcp.Services;

public sealed partial class SkillStore
{
    private const string UserEnabledChoiceFileName = ".enabled-by-user";

    public SkillDocument ApplyBuiltInDefault(string name, bool enabled)
    {
        var skill = Get(name);
        if (!skill.BuiltIn)
            throw new InvalidOperationException($"Skill '{name}' is not built in.");

        var directory = GetSkillDirectory(name);
        var userChoicePath = Path.Combine(directory, UserEnabledChoiceFileName);
        if (File.Exists(userChoicePath)) return skill;

        // Built-ins were historically seeded disabled. An already-enabled built-in
        // therefore represents a legacy user choice made before preference markers
        // existed; preserve it rather than applying a new system default over it.
        if (skill.Enabled) return skill;

        return skill.Enabled == enabled ? skill : SetEnabled(name, enabled);
    }

    public SkillDocument SetEnabledFromUser(string name, bool enabled)
    {
        var skill = SetEnabled(name, enabled);
        var directory = GetSkillDirectory(name);
        File.WriteAllText(Path.Combine(directory, UserEnabledChoiceFileName), enabled ? "true" : "false");
        return skill;
    }
}
