using System.Text.Json;
using System.Text;

namespace LocalCodingMcp.Services;

public sealed record ExecutionHistoryEntry(
    string Id,
    DateTimeOffset Timestamp,
    string Tool,
    IReadOnlyDictionary<string, JsonElement> Arguments,
    bool Success,
    long DurationMs,
    string? Error);

public sealed class ExecutionHistoryStore
{
    private static readonly string[] SensitiveNames =
    [
        "password", "passwd", "secret", "token", "api_key", "apikey",
        "authorization", "credential", "private_key", "content", "base64"
    ];

    private readonly string _filePath;
    private readonly int _maxArgumentLength;
    private readonly long _maxFileBytes;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ExecutionHistoryStore(string filePath, int maxArgumentLength = 2_000, long maxFileBytes = 10 * 1024 * 1024)
    {
        _filePath = Path.GetFullPath(filePath);
        _maxArgumentLength = Math.Clamp(maxArgumentLength, 128, 20_000);
        _maxFileBytes = Math.Clamp(maxFileBytes, 1_024, 1024L * 1024 * 1024);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
    }

    public string FilePath => _filePath;

    public async Task RecordAsync(
        string tool,
        IDictionary<string, JsonElement>? arguments,
        bool success,
        long durationMs,
        string? error,
        CancellationToken cancellationToken = default)
    {
        var entry = new ExecutionHistoryEntry(
            Guid.NewGuid().ToString("n"),
            DateTimeOffset.UtcNow,
            tool,
            Sanitize(arguments),
            success,
            durationMs,
            Truncate(error, 1_000));

        var line = JsonSerializer.Serialize(entry, _jsonOptions) + Environment.NewLine;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_filePath) && new FileInfo(_filePath).Length + Encoding.UTF8.GetByteCount(line) > _maxFileBytes)
            {
                File.Move(_filePath, _filePath + ".1", true);
            }
            await File.AppendAllTextAsync(_filePath, line, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<ExecutionHistoryEntry>> GetRecentAsync(
        int count = 50,
        string? tool = null,
        bool? success = null,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 500);
        if (!File.Exists(_filePath)) return [];

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = new List<ExecutionHistoryEntry>();
            var paths = new[] { _filePath + ".1", _filePath }.Where(File.Exists);
            foreach (var path in paths)
            {
                foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<ExecutionHistoryEntry>(line, _jsonOptions);
                        if (entry is not null) entries.Add(entry);
                    }
                    catch (JsonException)
                    {
                        // Preserve availability if a partial line remains after an interrupted write.
                    }
                }
            }

            return entries
                .Where(e => tool is null || e.Tool.Equals(tool, StringComparison.OrdinalIgnoreCase))
                .Where(e => success is null || e.Success == success)
                .TakeLast(count)
                .Reverse()
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private IReadOnlyDictionary<string, JsonElement> Sanitize(IDictionary<string, JsonElement>? arguments)
    {
        if (arguments is null) return new Dictionary<string, JsonElement>();

        return arguments.ToDictionary(
            pair => pair.Key,
            pair => IsSensitive(pair.Key)
                ? JsonSerializer.SerializeToElement("[REDACTED]")
                : SanitizeValue(pair.Value));
    }

    private JsonElement SanitizeValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => JsonSerializer.SerializeToElement(Truncate(value.GetString(), _maxArgumentLength)),
        JsonValueKind.Object or JsonValueKind.Array when value.GetRawText().Length > _maxArgumentLength =>
            JsonSerializer.SerializeToElement(Truncate(value.GetRawText(), _maxArgumentLength)),
        _ => value.Clone()
    };

    private static bool IsSensitive(string name) =>
        SensitiveNames.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength] + "…[truncated]";
}
