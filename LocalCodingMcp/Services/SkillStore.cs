using System.Text.RegularExpressions;

namespace LocalCodingMcp.Services;

public sealed record SkillInfo(string Name, string Path, DateTimeOffset ModifiedAt);
public sealed record SkillDocument(string Name, string Path, string Content, DateTimeOffset ModifiedAt);

public sealed partial class SkillStore
{
    private const string SkillFileName = "SKILL.md";
    private readonly string _rootPath;

    public SkillStore(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
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
            .Select(item => new SkillInfo(
                Path.GetFileName(item.Directory),
                item.File,
                File.GetLastWriteTimeUtc(item.File)))
            .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public SkillDocument Get(string name)
    {
        var filePath = GetSkillFilePath(name);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Skill '{name}' was not found.");
        }

        return new SkillDocument(
            name,
            filePath,
            File.ReadAllText(filePath),
            File.GetLastWriteTimeUtc(filePath));
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
        }
        catch
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
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

    public bool Delete(string name)
    {
        var directory = GetSkillDirectory(name);
        var filePath = Path.Combine(directory, SkillFileName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        Directory.Delete(directory, recursive: true);
        return true;
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

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SkillNameRegex();
}
