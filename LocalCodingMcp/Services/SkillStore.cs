using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalCodingMcp.Services;

public sealed record SkillInfo(
    string Name,
    string Path,
    DateTimeOffset ModifiedAt,
    bool Enabled,
    bool BuiltIn,
    string? SourceUrl,
    string? License);

public sealed record SkillDocument(
    string Name,
    string Path,
    string Content,
    DateTimeOffset ModifiedAt,
    bool Enabled,
    bool BuiltIn,
    string? SourceUrl,
    string? License);

public sealed partial class SkillStore
{
    private const string SkillFileName = "SKILL.md";
    private const string MetadataFileName = ".skill.json";
    private readonly string _rootPath;

    public SkillStore(string rootPath, bool seedBuiltIns = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);

        if (seedBuiltIns)
        {
            EnsureBuiltIns();
        }
    }

    public string RootPath => _rootPath;

    public IReadOnlyList<SkillInfo> List()
    {
        if (!Directory.Exists(_rootPath))
        {
            return Array.Empty<SkillInfo>();
        }

        return Directory.EnumerateDirectories(_rootPath)
            .Select(directory => new
            {
                Directory = directory,
                File = Path.Combine(directory, SkillFileName)
            })
            .Where(item => File.Exists(item.File))
            .Select(item =>
            {
                var metadata = ReadMetadata(item.Directory);
                return new SkillInfo(
                    Path.GetFileName(item.Directory),
                    item.File,
                    File.GetLastWriteTimeUtc(item.File),
                    metadata.Enabled,
                    metadata.BuiltIn,
                    metadata.SourceUrl,
                    metadata.License);
            })
            .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<SkillDocument> ListEnabled()
    {
        return List()
            .Where(skill => skill.Enabled)
            .Select(skill => Get(skill.Name))
            .ToArray();
    }

    public SkillDocument Get(string name)
    {
        var directory = GetSkillDirectory(name);
        var filePath = Path.Combine(directory, SkillFileName);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Skill '{name}' was not found.");
        }

        var metadata = ReadMetadata(directory);
        return new SkillDocument(
            name,
            filePath,
            File.ReadAllText(filePath),
            File.GetLastWriteTimeUtc(filePath),
            metadata.Enabled,
            metadata.BuiltIn,
            metadata.SourceUrl,
            metadata.License);
    }

    public SkillDocument Create(string name, string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var directory = GetSkillDirectory(name);
        var filePath = Path.Combine(directory, SkillFileName);

        if (Directory.Exists(directory) || File.Exists(filePath))
        {
            throw new InvalidOperationException($"Skill '{name}' already exists.");
        }

        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(filePath, content);
            WriteMetadata(directory, new SkillMetadata(true, false, null, null));
        }
        catch
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
            throw;
        }

        return Get(name);
    }

    public SkillDocument Update(string name, string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var filePath = GetSkillFilePath(name);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Skill '{name}' was not found.");
        }

        File.WriteAllText(filePath, content);
        return Get(name);
    }

    public SkillDocument SetEnabled(string name, bool enabled)
    {
        var directory = GetSkillDirectory(name);
        var filePath = Path.Combine(directory, SkillFileName);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Skill '{name}' was not found.");
        }

        var current = ReadMetadata(directory);
        WriteMetadata(directory, current with { Enabled = enabled });
        return Get(name);
    }

    public bool Delete(string name)
    {
        var directory = GetSkillDirectory(name);
        var filePath = Path.Combine(directory, SkillFileName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        if (ReadMetadata(directory).BuiltIn)
        {
            throw new InvalidOperationException(
                $"Built-in skill '{name}' cannot be deleted. Disable it instead.");
        }

        Directory.Delete(directory, recursive: true);
        return true;
    }

    private void EnsureBuiltIns()
    {
        foreach (var skill in BuiltInSkillCatalog.All)
        {
            var directory = GetSkillDirectory(skill.Name);
            var filePath = Path.Combine(directory, SkillFileName);
            if (File.Exists(filePath))
            {
                continue;
            }

            Directory.CreateDirectory(directory);
            File.WriteAllText(filePath, skill.Content);
            WriteMetadata(
                directory,
                new SkillMetadata(false, true, skill.SourceUrl, skill.License));
        }
    }

    private SkillMetadata ReadMetadata(string directory)
    {
        var metadataPath = Path.Combine(directory, MetadataFileName);
        if (!File.Exists(metadataPath))
        {
            return new SkillMetadata(true, false, null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<SkillMetadata>(File.ReadAllText(metadataPath))
                ?? new SkillMetadata(true, false, null, null);
        }
        catch (JsonException)
        {
            return new SkillMetadata(true, false, null, null);
        }
    }

    private static void WriteMetadata(string directory, SkillMetadata metadata)
    {
        var metadataPath = Path.Combine(directory, MetadataFileName);
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata));
    }

    private string GetSkillFilePath(string name) => Path.Combine(GetSkillDirectory(name), SkillFileName);

    private string GetSkillDirectory(string name)
    {
        ValidateName(name);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, name));
        var relative = Path.GetRelativePath(_rootPath, fullPath);

        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new ArgumentException("Skill name resolves outside the configured skills directory.", nameof(name));
        }

        return fullPath;
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!SkillNameRegex().IsMatch(name))
        {
            throw new ArgumentException(
                "Skill name must be 1-64 characters and contain only letters, numbers, '.', '_' or '-'.",
                nameof(name));
        }
    }

    private sealed record SkillMetadata(
        bool Enabled,
        bool BuiltIn,
        string? SourceUrl,
        string? License);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SkillNameRegex();
}
