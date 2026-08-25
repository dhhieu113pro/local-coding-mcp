namespace LocalCodingMcp.Services;

public sealed record SkillFrontMatter(string Name, string Description, string? License);

public static class SkillDocumentParser
{
    public static SkillFrontMatter Parse(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        using var reader = new StringReader(content);
        if (!string.Equals(reader.ReadLine()?.Trim(), "---", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Skill content must start with YAML-style front matter.");
        }

        string? name = null;
        string? description = null;
        string? license = null;
        var closed = false;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.Equals(line.Trim(), "---", StringComparison.Ordinal))
            {
                closed = true;
                break;
            }

            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            switch (key.ToLowerInvariant())
            {
                case "name":
                    name = value;
                    break;
                case "description":
                    description = value;
                    break;
                case "license":
                    license = string.IsNullOrWhiteSpace(value) ? null : value;
                    break;
            }
        }

        if (!closed)
        {
            throw new InvalidDataException("Skill front matter is missing its closing delimiter.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidDataException("Skill front matter requires a non-empty name.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidDataException("Skill front matter requires a non-empty description.");
        }

        return new SkillFrontMatter(name, description, license);
    }
}
