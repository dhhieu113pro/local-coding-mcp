// Host entry point — covered by integration smoke, not unit tests.
using LocalCodingMcp.Hosting;

var builder = WebApplication.CreateBuilder(args);

var runtime = builder.Services.AddLocalCodingMcp(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    LocalCodingMcpTransport.Http);

var app = builder.Build();

app.MapMcp("/mcp");

app.MapGet("/", () => Results.Ok(new
{
    name = "LocalCodingMcp",
    endpoint = "/mcp",
    allowed_roots = runtime.AllowedRoots,
    execution_history = runtime.HistoryPath,
    skills_directory = runtime.SkillsPath
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

Console.WriteLine("LocalCodingMcp starting...");
Console.WriteLine($"Allowed roots: {string.Join(", ", runtime.AllowedRoots)}");
Console.WriteLine($"Skills directory: {runtime.SkillsPath}");
Console.WriteLine("MCP endpoint: http://localhost:5000/mcp  (or the port Kestrel prints)");

app.Run();
