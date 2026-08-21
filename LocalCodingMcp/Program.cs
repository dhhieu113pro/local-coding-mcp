// Host entry point — covered by integration smoke, not unit tests.
using LocalCodingMcp.Services;
using LocalCodingMcp.Tools;

var builder = WebApplication.CreateBuilder(args);

// ── Config ──────────────────────────────────────────────
var allowedRoots = builder.Configuration.GetSection("AllowedRoots").Get<string[]>()
    ?? new[] { Path.GetTempPath() };

var blocked = builder.Configuration.GetSection("BlockedFileNames").Get<string[]>();
var cmdTimeout = builder.Configuration.GetValue("CommandTimeoutSeconds", 30);

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

// ── MCP Server (HTTP / Streamable HTTP) ─────────────────
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<WorkspaceTools>()
    .WithTools<FileTools>()
    .WithTools<GitTools>()
    .WithTools<ShellTools>();

var app = builder.Build();

app.MapMcp("/mcp");

app.MapGet("/", () => Results.Ok(new
{
    name = "LocalCodingMcp",
    endpoint = "/mcp",
    allowed_roots = allowedRoots
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

Console.WriteLine("LocalCodingMcp starting...");
Console.WriteLine($"Allowed roots: {string.Join(", ", allowedRoots)}");
Console.WriteLine("MCP endpoint: http://localhost:5000/mcp  (or the port Kestrel prints)");

app.Run();
