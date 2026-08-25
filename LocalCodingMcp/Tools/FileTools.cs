using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalCodingMcp.Services;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;

namespace LocalCodingMcp.Tools;

[McpServerToolType]
public sealed class FileTools
{
    private readonly WorkspaceManager _workspaces;
    private readonly PathSandbox _sandbox;
    private readonly SensitiveFileFilter _filter;
    private readonly int _maxSearchResults;

    /// <summary>Max decoded size for WriteBinaryFile (10 MiB).</summary>
    private const int MaxBinaryBytes = 10 * 1024 * 1024;

    public FileTools(
        WorkspaceManager workspaces,
        PathSandbox sandbox,
        SensitiveFileFilter filter,
        IConfiguration config)
    {
        _workspaces = workspaces;
        _sandbox = sandbox;
        _filter = filter;
        _maxSearchResults = config.GetValue("MaxSearchResults", 50);
    }

    [McpServerTool, Description("List files and directories in a path relative to the workspace")]
    public string ListDirectory(
        [Description("Relative path from workspace root (use '.' for root)")] string path,
        [Description("Workspace id from open_workspace")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);

        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        var entries = Directory.EnumerateFileSystemEntries(full)
            .Select(e =>
            {
                var name = Path.GetFileName(e);
                var isDir = Directory.Exists(e);
                return new
                {
                    name,
                    type = isDir ? "directory" : "file",
                    size = isDir ? (long?)null : new FileInfo(e).Length
                };
            })
            .OrderBy(e => e.type)
            .ThenBy(e => e.name);

        return JsonSerializer.Serialize(entries);
    }

    [McpServerTool, Description("Read a text file. Optionally limit to a line range.")]
    public string ReadFile(
        [Description("Relative path from workspace root")] string path,
        [Description("Workspace id")] string workspace_id,
        [Description("Start line (1-based, optional)")] int? start_line = null,
        [Description("End line (1-based, optional)")] int? end_line = null)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);
        _filter.EnsureNotBlocked(full);

        if (!File.Exists(full))
            throw new FileNotFoundException($"File not found: {path}");

        var content = File.ReadAllText(full, Encoding.UTF8);

        if (start_line is null && end_line is null)
            return content;

        var lines = content.Replace("\r\n", "\n").Split('\n');
        var start = Math.Max(1, start_line ?? 1);
        var end = Math.Min(lines.Length, end_line ?? lines.Length);

        if (start > end)
            return "";

        return string.Join("\n", lines[(start - 1)..end]);
    }

    [McpServerTool, Description("Create or overwrite a text file")]
    public string WriteFile(
        [Description("Relative path from workspace root")] string path,
        [Description("Full file content")] string content,
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);
        _filter.EnsureNotBlocked(full);

        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(full, content ?? "", Encoding.UTF8);
        return $"Wrote {path} ({(content ?? "").Length} chars)";
    }

    [McpServerTool, Description("Write a binary file (PNG/JPG/etc.) from base64. Strips optional data-URL prefix. Max 10 MiB decoded.")]
    public string WriteBinaryFile(
        [Description("Relative path from workspace root, e.g. images/photo.png")] string path,
        [Description("Base64 payload, or data:image/png;base64,...")] string base64_content,
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);
        _filter.EnsureNotBlocked(full);

        if (string.IsNullOrWhiteSpace(base64_content))
            throw new ArgumentException("base64_content is empty");

        var payload = base64_content.Trim();
        var comma = payload.IndexOf(',');
        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
            payload = payload[(comma + 1)..];

        payload = payload.Replace("\r", "").Replace("\n", "").Replace(" ", "");

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Invalid base64 content", ex);
        }

        if (bytes.Length == 0)
            throw new ArgumentException("Decoded content is empty");
        if (bytes.Length > MaxBinaryBytes)
            throw new ArgumentException($"Decoded size {bytes.Length} exceeds max {MaxBinaryBytes} bytes");

        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(full, bytes);
        return $"Wrote binary {path} ({bytes.Length} bytes)";
    }

    [McpServerTool, Description("Read a binary file as base64 (images, etc.). Max 10 MiB.")]
    public string ReadBinaryFile(
        [Description("Relative path from workspace root")] string path,
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);
        _filter.EnsureNotBlocked(full);

        if (!File.Exists(full))
            throw new FileNotFoundException($"File not found: {path}");

        var info = new FileInfo(full);
        if (info.Length > MaxBinaryBytes)
            throw new InvalidOperationException($"File size {info.Length} exceeds max {MaxBinaryBytes} bytes");

        var bytes = File.ReadAllBytes(full);
        return Convert.ToBase64String(bytes);
    }

    [McpServerTool, Description("Apply a unified diff patch to an existing file")]
    public string ApplyPatch(
        [Description("Relative path from workspace root")] string path,
        [Description("Unified diff content")] string patch,
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);
        _filter.EnsureNotBlocked(full);

        if (!File.Exists(full))
            throw new FileNotFoundException($"File not found: {path}");

        var original = File.ReadAllText(full, Encoding.UTF8);
        var updated = PatchApplier.Apply(original, patch);
        File.WriteAllText(full, updated, Encoding.UTF8);

        return $"Patch applied to {path}";
    }

    [McpServerTool, Description("Search for a text pattern across files in the workspace")]
    public string SearchFiles(
        [Description("Text or regex pattern to search")] string query,
        [Description("Workspace id")] string workspace_id,
        [Description("Relative path to search under (default '.')")] string path = ".",
        [Description("Max results")] int? max_results = null)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var searchRoot = _sandbox.Resolve(root, path);
        var limit = max_results ?? _maxSearchResults;

        if (!Directory.Exists(searchRoot) && !File.Exists(searchRoot))
            throw new DirectoryNotFoundException($"Path not found: {path}");

        var results = new List<object>();
        var regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var files = Directory.Exists(searchRoot)
            ? Directory.EnumerateFiles(searchRoot, "*", SearchOption.AllDirectories)
            : new[] { searchRoot };

        foreach (var file in files)
        {
            if (results.Count >= limit) break;

            try
            {
                _filter.EnsureNotBlocked(file);
            }
            catch
            {
                continue;
            }

            if (IsProbablyBinary(file)) continue;

            var text = TryReadAllText(file);
            if (text is null)
                continue;

            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (results.Count >= limit) break;
                if (!regex.IsMatch(lines[i])) continue;

                var rel = Path.GetRelativePath(root, file);
                results.Add(new
                {
                    file = rel.Replace('\\', '/'),
                    line = i + 1,
                    text = lines[i].TrimEnd()
                });
            }
        }

        return JsonSerializer.Serialize(results);
    }

    [McpServerTool, Description("Create a directory (and parents if needed)")]
    public string CreateDirectory(
        [Description("Relative path from workspace root")] string path,
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);
        Directory.CreateDirectory(full);
        return $"Created directory {path}";
    }

    [McpServerTool, Description("Move or rename a file or directory")]
    public string MoveFile(
        [Description("Source relative path")] string source,
        [Description("Destination relative path")] string destination,
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var src = _sandbox.Resolve(root, source);
        var dst = _sandbox.Resolve(root, destination);
        _filter.EnsureNotBlocked(src);
        _filter.EnsureNotBlocked(dst);

        var dstDir = Path.GetDirectoryName(dst);
        if (!string.IsNullOrEmpty(dstDir))
            Directory.CreateDirectory(dstDir);

        if (Directory.Exists(src))
            Directory.Move(src, dst);
        else
            File.Move(src, dst, overwrite: true);

        return $"Moved {source} → {destination}";
    }

    [McpServerTool, Description("Delete a file (use with care)")]
    public string DeleteFile(
        [Description("Relative path from workspace root")] string path,
        [Description("Workspace id")] string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);
        _filter.EnsureNotBlocked(full);

        if (File.Exists(full))
        {
            File.Delete(full);
            return $"Deleted file {path}";
        }

        if (Directory.Exists(full))
        {
            Directory.Delete(full, recursive: false);
            return $"Deleted directory {path}";
        }

        throw new FileNotFoundException($"Path not found: {path}");
    }

    internal static string? TryReadAllText(string path)
    {
        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsProbablyBinary(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".exe" or ".dll" or ".so" or ".dylib" or ".bin" or ".png" or ".jpg"
            or ".jpeg" or ".gif" or ".webp" or ".ico" or ".pdf" or ".zip" or ".gz" or ".tar";
    }
}
