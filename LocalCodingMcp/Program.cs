// Host entry point — covered by integration smoke, not unit tests.
using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ── Config ──────────────────────────────────────────────
var allowedRoots = builder.Configuration.GetSection("AllowedRoots").Get<string[]>()
    ?? new[] { Path.GetTempPath() };

var blocked = builder.Configuration.GetSection("BlockedFileNames").Get<string[]>();
var cmdTimeout = builder.Configuration.GetValue("CommandTimeoutSeconds", 30);
var configuredHistoryPath = builder.Configuration.GetValue<string>("ExecutionHistory:FilePath")
    ?? Path.Combine("data", "execution-history.jsonl");
var historyPath = Path.IsPathRooted(configuredHistoryPath)
    ? configuredHistoryPath
    : Path.Combine(builder.Environment.ContentRootPath, configuredHistoryPath);
var historyArgumentLimit = builder.Configuration.GetValue("ExecutionHistory:MaxArgumentLength", 2_000);
var historyMaxFileMb = builder.Configuration.GetValue("ExecutionHistory:MaxFileSizeMb", 10);
var historyStore = new ExecutionHistoryStore(
    historyPath,
    historyArgumentLimit,
    Math.Clamp(historyMaxFileMb, 1, 1024) * 1024L * 1024);
var configuredSkillsPath = builder.Configuration.GetValue<string>("Skills:Directory")
    ?? Path.Combine("data", "skills");
var skillsPath = Path.IsPathRooted(configuredSkillsPath)
    ? configuredSkillsPath
    : Path.Combine(builder.Environment.ContentRootPath, configuredSkillsPath);
var skillStore = new SkillStore(skillsPath);

// Ensure at least one root exists for demo
foreach (var root in allowedRoots)
{
    try { Directory.CreateDirectory(root); } catch { /* ignore */ }
}

// ── Services ────────────────────────────────────────────
builder.Services.AddSingleton(new PathSandbox(allowedRoots));
builder.Services.AddSingleton(new SensitiveFileFilter(blocked));
builder.Services.AddSingleton<WorkspaceManager>();
builder.Services.AddSingleton(new CommandRunner(cmdTimeout));
builder.Services.AddSingleton(historyStore);
builder.Services.AddSingleton(skillStore);

// ── MCP Server (HTTP / Streamable HTTP) ─────────────────
builder.Services
    .AddMcpServer(McpServerInstructions.Apply)
    .WithHttpTransport()
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

var app = builder.Build();

app.MapMcp("/mcp");

app.MapGet("/", () => Results.Ok(new
{
    name = "LocalCodingMcp",
    endpoint = "/mcp",
    allowed_roots = allowedRoots,
    execution_history = historyPath,
    skills_directory = skillsPath
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

Console.WriteLine("LocalCodingMcp starting...");
Console.WriteLine($"Allowed roots: {string.Join(", ", allowedRoots)}");
Console.WriteLine($"Skills directory: {skillsPath}");
Console.WriteLine("MCP endpoint: http://localhost:5000/mcp  (or the port Kestrel prints)");

app.Run();
