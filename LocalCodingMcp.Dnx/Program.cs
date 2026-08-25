using LocalCodingMcp.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// MCP stdio requires stdout to contain protocol messages only.
builder.Logging.ClearProviders();

builder.Services.AddLocalCodingMcp(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    LocalCodingMcpTransport.Stdio);

await builder.Build().RunAsync();
