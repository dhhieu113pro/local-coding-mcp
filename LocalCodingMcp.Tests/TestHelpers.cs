using LocalCodingMcp.Services;
using Microsoft.Extensions.Configuration;

namespace LocalCodingMcp.Tests;

internal static class TestHelpers
{
    public static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcp-t-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(root);
        return root;
    }

    public static void SafeDelete(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { Directory.Delete(path, true); } catch { /* ignore */ }
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }

    public static IConfiguration Config(int maxSearch = 50)
    {
        var path = Path.Combine(Path.GetTempPath(), "mcp-cfg-" + Guid.NewGuid().ToString("N")[..8] + ".json");
        File.WriteAllText(path, $"{{\"MaxSearchResults\": {maxSearch}}}");
        return new ConfigurationBuilder().AddJsonFile(path, optional: false).Build();
    }

    /// <summary>
    /// Returns (root, sandbox, workspaces, filter, runner)
    /// </summary>
    public static (string root, PathSandbox sandbox, WorkspaceManager workspaces, SensitiveFileFilter filter, CommandRunner runner)
        CreateEnv(string? root = null)
    {
        root ??= CreateTempRoot();
        var sandbox = new PathSandbox(new[] { root });
        var workspaces = new WorkspaceManager(sandbox);
        var filter = new SensitiveFileFilter();
        var runner = new CommandRunner(15);
        return (root, sandbox, workspaces, filter, runner);
    }

    public static (PathSandbox sandbox, WorkspaceManager workspaces, SensitiveFileFilter filter, CommandRunner runner, string root)
        CreateStack(string? root = null)
    {
        var env = CreateEnv(root);
        return (env.sandbox, env.workspaces, env.filter, env.runner, env.root);
    }
}
