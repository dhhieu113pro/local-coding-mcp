using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

namespace LocalCodingMcp.Hosting;

public enum LocalCodingMcpTransport
{
    Http,
    Stdio
}

public sealed record LocalCodingMcpRuntime(
    string[] AllowedRoots,
    string HistoryPath,
    string SkillsPath);

public static class McpServerRegistration
{
    public static LocalCodingMcpRuntime AddLocalCodingMcp(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath,
        LocalCodingMcpTransport transport)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        var allowedRoots = configuration.GetSection("AllowedRoots").Get<string[]>()
            ?? new[] { Path.GetTempPath() };
        var blocked = configuration.GetSection("BlockedFileNames").Get<string[]>();
        var commandTimeout = configuration.GetValue("CommandTimeoutSeconds", 30);

        var configuredHistoryPath = configuration.GetValue<string>("ExecutionHistory:FilePath")
            ?? Path.Combine("data", "execution-history.jsonl");
        var historyPath = Path.IsPathRooted(configuredHistoryPath)
            ? configuredHistoryPath
            : Path.Combine(contentRootPath, configuredHistoryPath);
        var historyArgumentLimit = configuration.GetValue("ExecutionHistory:MaxArgumentLength", 2_000);
        var historyMaxFileMb = configuration.GetValue("ExecutionHistory:MaxFileSizeMb", 10);
        var historyStore = new ExecutionHistoryStore(
            historyPath,
            historyArgumentLimit,
            Math.Clamp(historyMaxFileMb, 1, 1024) * 1024L * 1024);

        var configuredSkillsPath = configuration.GetValue<string>("Skills:Directory")
            ?? Path.Combine("data", "skills");
        var skillsPath = Path.IsPathRooted(configuredSkillsPath)
            ? configuredSkillsPath
            : Path.Combine(contentRootPath, configuredSkillsPath);
        var skillStore = new SkillStore(skillsPath);

        var remoteMaxBytes = Math.Clamp(configuration.GetValue("Skills:Remote:MaxBytes", 1_048_576), 1_024, 10_485_760);
        var remoteTimeoutSeconds = Math.Clamp(configuration.GetValue("Skills:Remote:TimeoutSeconds", 15), 1, 120);
        var remoteMaxRedirects = Math.Clamp(configuration.GetValue("Skills:Remote:MaxRedirects", 3), 0, 10);
        var remoteHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(remoteTimeoutSeconds)
        };
        var remoteFetcher = new RemoteSkillFetcher(remoteHttpClient, remoteMaxBytes, remoteMaxRedirects);
        var remoteSkillService = new RemoteSkillService(skillStore, remoteFetcher);

        foreach (var root in allowedRoots)
        {
            try { Directory.CreateDirectory(root); } catch { /* ignore */ }
        }

        services.AddSingleton(new PathSandbox(allowedRoots));
        services.AddSingleton(new SensitiveFileFilter(blocked));
        services.AddSingleton<WorkspaceManager>();
        services.AddSingleton(new CommandRunner(commandTimeout));
        services.AddSingleton(historyStore);
        services.AddSingleton(skillStore);
        services.AddSingleton(remoteFetcher);
        services.AddSingleton(remoteSkillService);

        var informationalVersion = typeof(McpServerRegistration).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .Split('+', 2)[0]
            ?? "0.0.0";

        var mcp = services
            .AddMcpServer(options =>
            {
                McpServerInstructions.Apply(options);
                options.ServerInfo = new()
                {
                    Name = "LocalCodingMcp",
                    Version = informationalVersion
                };
            })
            .WithTools<WorkspaceTools>()
            .WithTools<FileTools>()
            .WithTools<GitTools>()
            .WithTools<ShellTools>()
            .WithTools<HistoryTools>()
            .WithTools<SkillTools>()
            .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                var stopwatch = Stopwatch.StartNew();
                var tool = context.Params?.Name ?? "unknown";

                try
                {
                    var result = await next(context, cancellationToken);
                    stopwatch.Stop();
                    await historyStore.RecordAsync(
                        tool,
                        context.Params?.Arguments,
                        result.IsError != true,
                        stopwatch.ElapsedMilliseconds,
                        result.IsError == true ? "Tool returned an error result" : null,
                        CancellationToken.None);
                    return result;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    await historyStore.RecordAsync(
                        tool,
                        context.Params?.Arguments,
                        false,
                        stopwatch.ElapsedMilliseconds,
                        ex.Message,
                        CancellationToken.None);
                    throw;
                }
            }));

        if (transport == LocalCodingMcpTransport.Stdio) mcp.WithStdioServerTransport();
        else mcp.WithHttpTransport();

        return new LocalCodingMcpRuntime(allowedRoots, historyPath, skillsPath);
    }
}
