using LocalCodingMcp.Hosting;
using LocalCodingMcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalCodingMcp.Tests.Hosting;

public sealed class CodebaseMemorySkillRegistrationTests
{
    [Fact]
    public void Registration_EnablesCodebaseMemorySkill_WhenProxyEnabled()
    {
        WithRegistration(proxyEnabled: true, (services, _) =>
        {
            using var provider = services.BuildServiceProvider();
            Assert.True(provider.GetRequiredService<SkillStore>().Get("codebase-memory").Enabled);
        });
    }

    [Fact]
    public void Registration_LeavesCodebaseMemorySkillDisabled_WhenProxyDisabled()
    {
        WithRegistration(proxyEnabled: false, (services, _) =>
        {
            using var provider = services.BuildServiceProvider();
            Assert.False(provider.GetRequiredService<SkillStore>().Get("codebase-memory").Enabled);
        });
    }

    [Fact]
    public void Registration_PreservesExplicitDisable_AfterProxyWasEnabled()
    {
        WithRegistration(proxyEnabled: true, (services, root) =>
        {
            using (var provider = services.BuildServiceProvider())
                provider.GetRequiredService<SkillStore>().SetEnabledFromUser("codebase-memory", false);

            var config = BuildConfig(root, proxyEnabled: true);
            var reopenedServices = new ServiceCollection();
            reopenedServices.AddLocalCodingMcp(config, root, LocalCodingMcpTransport.Http);
            using var reopened = reopenedServices.BuildServiceProvider();

            Assert.False(reopened.GetRequiredService<SkillStore>().Get("codebase-memory").Enabled);
        });
    }

    private static void WithRegistration(bool proxyEnabled, Action<ServiceCollection, string> assertion)
    {
        var root = Path.Combine(Path.GetTempPath(), $"local-coding-cbm-skill-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var services = new ServiceCollection();
            services.AddLocalCodingMcp(BuildConfig(root, proxyEnabled), root, LocalCodingMcpTransport.Http);
            assertion(services, root);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static IConfiguration BuildConfig(string root, bool proxyEnabled)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Skills:Directory"] = Path.Combine(root, "skills"),
            ["ExecutionHistory:FilePath"] = Path.Combine(root, "history.jsonl"),
            ["CodebaseMemory:Enabled"] = proxyEnabled.ToString(),
            ["CodebaseMemory:Endpoint"] = "http://codebase-memory:9750/mcp"
        }).Build();
}
