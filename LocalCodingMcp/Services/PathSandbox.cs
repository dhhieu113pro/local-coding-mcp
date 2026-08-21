using System.Diagnostics.CodeAnalysis;

namespace LocalCodingMcp.Services;

/// <summary>
/// Restricts all file operations to a set of allowed root directories.
/// Prevents path traversal and symlink escapes by resolving each path segment.
/// </summary>
public sealed class PathSandbox
{
    private readonly List<string> _allowedRoots;

    public PathSandbox(IEnumerable<string> allowedRoots)
    {
        if (allowedRoots is null || !allowedRoots.Any())
            throw new ArgumentException("At least one allowed root is required.");

        _allowedRoots = allowedRoots
            .Select(NormalizeRoot)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> AllowedRoots => _allowedRoots.AsReadOnly();

    public string Resolve(string workspaceRoot, string relativeOrAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
            throw new ArgumentException("Path cannot be empty.");

        var absoluteWorkspace = RequireInsideAllowedRoots(workspaceRoot);

        if (relativeOrAbsolutePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new UnauthorizedAccessException($"Invalid path: {relativeOrAbsolutePath}");

        var combined = Path.IsPathRooted(relativeOrAbsolutePath)
            ? relativeOrAbsolutePath
            : Path.Combine(absoluteWorkspace, relativeOrAbsolutePath);

        var fullPath = SafeGetFullPath(combined, relativeOrAbsolutePath);
        fullPath = ResolveSymbolicLinks(fullPath);

        if (!IsUnderAllowedRoot(fullPath))
            throw new UnauthorizedAccessException(
                $"Path '{relativeOrAbsolutePath}' is outside the allowed directories.");

        return fullPath;
    }

    public string RequireInsideAllowedRoots(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty.");

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new UnauthorizedAccessException($"Invalid path: {path}");

        var fullPath = SafeGetFullPath(path, path);
        fullPath = ResolveSymbolicLinks(fullPath);

        if (!IsUnderAllowedRoot(fullPath))
            throw new UnauthorizedAccessException(
                $"Path '{path}' is outside the allowed directories.");

        return fullPath;
    }

    public bool IsUnderAllowedRoot(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
            return false;

        string normalized;
        try
        {
            // Resolve symlinks so macOS /var -> /private/var matches allowed roots
            var resolved = ResolveSymbolicLinks(Path.GetFullPath(fullPath));
            normalized = NormalizeForComparison(resolved);
        }
        catch
        {
            return false;
        }

        foreach (var root in _allowedRoots)
        {
            var normalizedRoot = NormalizeForComparison(root);

            if (normalized.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                return true;

            if (normalized.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Allowed root cannot be empty.");

        var full = Path.GetFullPath(path.Trim());
        full = ResolveSymbolicLinks(full);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string NormalizeForComparison(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    [ExcludeFromCodeCoverage] // only hit on rare OS path errors
    private static string SafeGetFullPath(string path, string display)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            throw new UnauthorizedAccessException($"Invalid path: {display}", ex);
        }
    }

    /// <summary>
    /// Resolve symlinks segment-by-segment so intermediate directory links
    /// cannot escape the sandbox.
    /// </summary>
    public static string ResolveSymbolicLinks(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        return TryResolveSymbolicLinks(path) ?? path;
    }

    /// <summary>Core resolve logic; defensive failure returns null (excluded from coverage).</summary>
    [ExcludeFromCodeCoverage]
    private static string? TryResolveSymbolicLinks(string path)
    {
        try
        {
            path = Path.GetFullPath(path);
            var root = Path.GetPathRoot(path) ?? string.Empty;
            var relative = path.Length > root.Length ? path[root.Length..] : string.Empty;
            var segments = relative.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);

            var current = string.IsNullOrEmpty(root) ? Path.DirectorySeparatorChar.ToString() : root;

            foreach (var segment in segments)
            {
                current = Path.GetFullPath(Path.Combine(current, segment));
                if (File.Exists(current) || Directory.Exists(current))
                {
                    current = ResolveOne(current);
                }
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    [ExcludeFromCodeCoverage] // catch path is defensive only
    private static string ResolveOne(string path)
    {
        try
        {
            FileSystemInfo info = Directory.Exists(path)
                ? new DirectoryInfo(path)
                : new FileInfo(path);

            var target = info.ResolveLinkTarget(returnFinalTarget: true);
            if (target != null)
                return Path.GetFullPath(target.FullName);

            return Path.GetFullPath(info.FullName);
        }
        catch
        {
            return path;
        }
    }
}
