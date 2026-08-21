namespace LocalCodingMcp.Services;

/// <summary>
/// Blocks access to common sensitive files (secrets, keys, env files, etc.).
/// </summary>
public sealed class SensitiveFileFilter
{
    private readonly List<string> _blockedPatterns;

    public SensitiveFileFilter(IEnumerable<string>? blockedPatterns = null)
    {
        _blockedPatterns = (blockedPatterns ?? DefaultBlocked)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
    }

    public static IReadOnlyList<string> DefaultBlocked { get; } = new[]
    {
        ".env",
        ".env.local",
        ".env.production",
        ".env.development",
        "id_rsa",
        "id_rsa.pub",
        "id_ed25519",
        "id_ed25519.pub",
        "id_ecdsa",
        "id_ecdsa.pub",
        "*.pem",
        "*.pfx",
        "*.p12",
        "*.key",
        "credentials.json",
        "secrets.json",
        "appsettings.Production.json",
        ".npmrc",
        ".netrc",
        "authorized_keys",
        "known_hosts"
    };

    public void EnsureNotBlocked(string fullPath)
    {
        var fileName = Path.GetFileName(fullPath);

        foreach (var pattern in _blockedPatterns)
        {
            if (IsMatch(fileName, pattern))
            {
                throw new UnauthorizedAccessException(
                    $"Access to sensitive file '{fileName}' is blocked.");
            }
        }
    }

    private static bool IsMatch(string fileName, string pattern)
    {
        if (pattern.StartsWith("*."))
        {
            var ext = pattern[1..]; // ".pem"
            return fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
        }

        return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
