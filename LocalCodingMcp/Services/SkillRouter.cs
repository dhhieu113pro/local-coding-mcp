using System.Text.RegularExpressions;

namespace LocalCodingMcp.Services;

public sealed record SkillRoute(
    string Name,
    string Description,
    string Reason,
    int Score);

public sealed partial class SkillRouter
{
    private static readonly IReadOnlyDictionary<string, string[]> BuiltInHints =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["superpowers"] =
            [
                "code", "coding", "debug", "debugging", "bug", "build", "ci", "github", "actions",
                "implement", "implementation", "plan", "planning", "review", "test", "testing", "refactor"
            ],
            ["hallmark"] =
            [
                "ui", "ux", "design", "redesign", "page", "component", "responsive", "interface",
                "layout", "landing", "accessibility", "visual"
            ],
            ["caveman"] =
            [
                "terse", "concise", "short", "brief", "filler", "verbosity", "compressed"
            ],
            ["ponytail"] =
            [
                "simple", "simplify", "minimal", "minimum", "reuse", "yagni", "overengineering", "over-engineering"
            ]
        };

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "before", "by", "for", "from", "in", "into",
        "is", "it", "of", "on", "or", "the", "this", "to", "with", "work"
    };

    private readonly SkillStore _skills;

    public SkillRouter(SkillStore skills)
    {
        _skills = skills ?? throw new ArgumentNullException(nameof(skills));
    }

    public IReadOnlyList<SkillRoute> Route(string task)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(task);
        var taskTokens = Tokenize(task);

        return _skills.ListEnabled()
            .Select(skill => Score(skill, taskTokens))
            .Where(route => route.Score > 0)
            .OrderByDescending(route => route.Score)
            .ThenBy(route => route.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SkillRoute Score(SkillDocument skill, HashSet<string> taskTokens)
    {
        var description = ReadDescription(skill.Content);
        var descriptionTokens = Tokenize($"{skill.Name} {description}");
        var matches = descriptionTokens.Intersect(taskTokens, StringComparer.OrdinalIgnoreCase).ToArray();
        var score = matches.Length * 2;

        if (BuiltInHints.TryGetValue(skill.Name, out var hints))
        {
            score += hints.Count(taskTokens.Contains) * 3;
        }

        var reason = matches.Length > 0
            ? $"Matched: {string.Join(", ", matches.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}"
            : "Matched task intent";

        return new SkillRoute(skill.Name, description, reason, score);
    }

    private static string ReadDescription(string content)
    {
        foreach (var line in content.Replace("\r\n", "\n").Split('\n'))
        {
            var match = DescriptionRegex().Match(line);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim().Trim('"', '\'');
            }
        }

        return string.Empty;
    }

    private static HashSet<string> Tokenize(string text)
    {
        return WordRegex().Matches(text.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(token => token.Length > 1 && !StopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^\s*description\s*:\s*(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DescriptionRegex();

    [GeneratedRegex(@"[a-z0-9]+(?:-[a-z0-9]+)?", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}
