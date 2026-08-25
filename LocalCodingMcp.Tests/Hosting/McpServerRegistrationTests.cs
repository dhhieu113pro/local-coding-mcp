using LocalCodingMcp.Hosting;
using LocalCodingMcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalCodingMcp.Tests.Hosting;

public sealed class McpServerRegistrationTests
{
    [Fact]
    public void Shared_mcp_registration_type_is_available_for_both_hosts()
    {
        var assembly = typeof(LocalCodingMcp.Tools.WorkspaceTools).Assembly;
        var registrationType = assembly.GetType("LocalCodingMcp.Hosting.McpServerRegistration");
        Assert.NotNull(registrationType);
        Assert.NotNull(registrationType!.GetMethod("AddLocalCodingMcp", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static));
    }

    [Theory]
    [InlineData(LocalCodingMcpTransport.Http)]
    [InlineData(LocalCodingMcpTransport.Stdio)]
    public void AddLocalCodingMcp_RegistersRemoteSkillServices(LocalCodingMcpTransport transport)
    {
        var root = Path.Combine(Path.GetTempPath(), $"local-coding-registration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Skills:Directory"] = Path.Combine(root, "skills"),
                ["ExecutionHistory:FilePath"] = Path.Combine(root, "history.jsonl"),
                ["Skills:Remote:MaxBytes"] = "2048",
                ["Skills:Remote:TimeoutSeconds"] = "7",
                ["Skills:Remote:MaxRedirects"] = "2"
            }).Build();
            var services = new ServiceCollection();

            services.AddLocalCodingMcp(config, root, transport);
            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<RemoteSkillFetcher>());
            Assert.NotNull(provider.GetService<RemoteSkillService>());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
