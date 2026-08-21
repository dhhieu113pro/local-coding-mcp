using LocalCodingMcp.Services;

namespace LocalCodingMcp.Tests;

public class SensitiveFileFilterTests
{
    private readonly SensitiveFileFilter _filter = new();

    [Theory]
    [InlineData("/proj/.env")]
    [InlineData("/proj/.env.local")]
    [InlineData("/proj/.env.production")]
    [InlineData("/proj/id_rsa")]
    [InlineData("/proj/id_ed25519")]
    [InlineData("/proj/secret.pem")]
    [InlineData("/proj/cert.pfx")]
    [InlineData("/proj/store.p12")]
    [InlineData("/proj/server.key")]
    [InlineData("/proj/credentials.json")]
    [InlineData("/proj/secrets.json")]
    [InlineData("/proj/appsettings.Production.json")]
    public void EnsureNotBlocked_Sensitive_Throws(string path)
    {
        Assert.Throws<UnauthorizedAccessException>(() => _filter.EnsureNotBlocked(path));
    }

    [Theory]
    [InlineData("/proj/Program.cs")]
    [InlineData("/proj/README.md")]
    [InlineData("/proj/appsettings.json")]
    [InlineData("/proj/data.txt")]
    public void EnsureNotBlocked_Normal_Succeeds(string path)
    {
        _filter.EnsureNotBlocked(path);
    }

    [Fact]
    public void CustomPatterns_Work()
    {
        var filter = new SensitiveFileFilter(new[] { "secret.txt", "*.tok" });
        Assert.Throws<UnauthorizedAccessException>(() => filter.EnsureNotBlocked("/a/secret.txt"));
        Assert.Throws<UnauthorizedAccessException>(() => filter.EnsureNotBlocked("/a/x.tok"));
        filter.EnsureNotBlocked("/a/ok.txt");
    }
}
