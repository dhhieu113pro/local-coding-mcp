using System.Text.Json;
using ModelContextProtocol.Client;

namespace LocalCodingMcp.Services;

public sealed record CodebaseMemoryStatus(bool Configured, bool Available, string? Endpoint, string? Error);
public sealed record CodebaseMemoryToolInfo(string Name, string Description, string? InputSchemaJson);

public interface ICodebaseMemoryClient
{
    Task<CodebaseMemoryStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodebaseMemoryToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default);
    Task<string> CallToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default);
}

public sealed class CodebaseMemoryClient : ICodebaseMemoryClient, IAsyncDisposable
{
    private readonly bool _enabled;
    private readonly Uri? _endpoint;
    private readonly TimeSpan _connectionTimeout;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private McpClient? _client;

    public CodebaseMemoryClient(bool enabled, Uri? endpoint, TimeSpan connectionTimeout)
    {
        _enabled = enabled;
        _endpoint = endpoint;
        _connectionTimeout = connectionTimeout;
    }

    public async Task<CodebaseMemoryStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!_enabled || _endpoint is null)
        {
            return new CodebaseMemoryStatus(false, false, _endpoint?.ToString(), "Codebase Memory proxy is disabled.");
        }

        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
            await client.PingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return new CodebaseMemoryStatus(true, true, _endpoint.ToString(), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await ResetClientAsync().ConfigureAwait(false);
            return new CodebaseMemoryStatus(true, false, _endpoint.ToString(), ex.Message);
        }
    }

    public async Task<IReadOnlyList<CodebaseMemoryToolInfo>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return tools.Select(tool => new CodebaseMemoryToolInfo(
                tool.Name,
                tool.Description,
                tool.JsonSchema.ValueKind == JsonValueKind.Undefined ? null : tool.JsonSchema.GetRawText()))
                .ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await ResetClientAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Codebase Memory is unavailable: {ex.Message}", ex);
        }
    }

    public async Task<string> CallToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        EnsureConfigured();
        if (arguments.ValueKind is not JsonValueKind.Object)
            throw new ArgumentException("Codebase Memory tool arguments must be a JSON object.", nameof(arguments));

        var dictionary = arguments.EnumerateObject()
            .ToDictionary(property => property.Name, property => (object?)property.Value.Clone(), StringComparer.Ordinal);

        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
            var result = await client.CallToolAsync(toolName, dictionary, cancellationToken: cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await ResetClientAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Codebase Memory tool '{toolName}' failed: {ex.Message}", ex);
        }
    }

    private void EnsureConfigured()
    {
        if (!_enabled || _endpoint is null)
            throw new InvalidOperationException("Codebase Memory proxy is disabled. Configure CodebaseMemory:Enabled=true and CodebaseMemory:Endpoint.");
    }

    private async Task<McpClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null) return _client;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null) return _client;
            EnsureConfigured();

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = _endpoint!,
                TransportMode = HttpTransportMode.StreamableHttp,
                ConnectionTimeout = _connectionTimeout,
                EnableStandaloneGetStream = false,
                Name = "codebase-memory"
            });

            _client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
            return _client;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ResetClientAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_client is not null)
            {
                await _client.DisposeAsync().ConfigureAwait(false);
                _client = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ResetClientAsync().ConfigureAwait(false);
        _gate.Dispose();
    }
}
