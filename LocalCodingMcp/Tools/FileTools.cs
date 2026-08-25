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
        var entries = Directory.EnumerateFileSystemEntries(full)
            .Select(p => new
            {
                name = Path.GetFileName(p),
                type = Directory.Exists(p) ? "directory" : "file",
                size = File.Exists(p) ? new FileInfo(p).Length : 0
            });
        return JsonSerializer.Serialize(entries);
    }

    [McpServerTool, Description("Read a UTF-8 text file")]
    public string ReadFile(
        [Description("Relative path from workspace root")] string path,
        [Description("Workspace id")] string workspace_id)
    {
        var full = ResolveSafe(workspace_id, path);
        return File.ReadAllText(full, Encoding.UTF8);
    }

    [McpServerTool, Description("Write or overwrite a UTF-8 text file")]
    public string WriteFile(
        [Description("Relative path from workspace root")] string path,
        [Description("Text content")]
        string content,
        [Description("Workspace id")]
        string workspace_id)
    {
        var full = ResolveSafe(workspace_id, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, Encoding.UTF8);
        return JsonSerializer.Serialize(new { path, bytes = Encoding.UTF8.GetByteCount(content) });
    }

    [McpServerTool, Description("Read a binary file and return base64")]
    public string ReadBinaryFile(
        [Description("Relative path from workspace root")]
        string path,
        [Description("Workspace id")]
        string workspace_id)
    {
        var full = ResolveSafe(workspace_id, path);
        var bytes = File.ReadAllBytes(full);
        return JsonSerializer.Serialize(new
        {
            path,
            bytes = bytes.Length,
            base64 = Convert.ToBase64String(bytes)
        });
    }

    [McpServerTool, Description("Write a binary file from base64 data")]
    public string WriteBinaryFile(
        [Description("Relative path from workspace root")]
        string path,
        [Description("Base64 encoded file contents")]
        string base64,
        [Description("Workspace id")]
        string workspace_id)
    {
        var full = ResolveSafe(workspace_id, path);
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid base64 data", nameof(base64));
        }

        if (bytes.Length > MaxBinaryBytes)
        {
            throw new InvalidOperationException($"Binary file exceeds the {MaxBinaryBytes / (1024 * 1024)} MiB limit");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
        return JsonSerializer.Serialize(new { path, bytes = bytes.Length });
    }

    [McpServerTool, Description("Apply a unified diff patch to a text file")]
    public string ApplyPatch(
        [Description("Relative path from workspace root")]
        string path,
        [Description("Unified diff patch")]
        string patch,
        [Description("Workspace id")]
        string workspace_id)
    {
        var full = ResolveSafe(workspace_id, path);
        var original = File.Exists(full) ? File.ReadAllText(full, Encoding.UTF8) : string.Empty;
        var updated = PatchApplier.Apply(original, patch);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, updated, Encoding.UTF8);
        return JsonSerializer.Serialize(new { path, applied = true });
    }

    [McpServerTool, Description("Search text files with a regex pattern")]
    public string SearchFiles(
        [Description("Regex pattern")]
        string pattern,
        [Description("Relative directory from workspace root")]
        string path,
        [Description("Workspace id")]
        string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var directory = _sandbox.Resolve(root, path);
        var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
        var results = new List<object>();

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (results.Count >= _maxSearchResults)
            {
                break;
            }

            var relative = Path.GetRelativePath(root, file);
            if (_filter.IsBlocked(relative))
            {
                continue;
            }

            try
            {
                var lineNumber = 0;
                foreach (var line in File.ReadLines(file))
                {
                    lineNumber++;
                    if (regex.IsMatch(line))
                    {
                        results.Add(new { path = relative, line = lineNumber, text = line });
                        if (results.Count >= _maxSearchResults)
                        {
                            break;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Skip unreadable files.
            }
            catch (UnauthorizedAccessException)
            {
                // Skip inaccessible files.
            }
        }

        return JsonSerializer.Serialize(results);
    }

    [McpServerTool, Description("Create a directory")]
    public string CreateDirectory(
        [Description("Relative path from workspace root")]
        string path,
        [Description("Workspace id")]
        string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var full = _sandbox.Resolve(root, path);
        Directory.CreateDirectory(full);
        return JsonSerializer.Serialize(new { path, created = true });
    }

    [McpServerTool, Description("Move or rename a file or directory")]
    public string MoveFile(
        [Description("Source relative path")]
        string source,
        [Description("Destination relative path")]
        string destination,
        [Description("Workspace id")]
        string workspace_id)
    {
        var root = _workspaces.GetRoot(workspace_id);
        var sourceFull = _sandbox.Resolve(root, source);
        var destinationFull = _sandbox.Resolve(root, destination);
        _filter.EnsureAllowed(source);
        _filter.EnsureAllowed(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFull)!);

        if (File.Exists(sourceFull))
        {
            File.Move(sourceFull, destinationFull);
        }
        else if (Directory.Exists(sourceFull))
        {
            Directory.Move(sourceFull, destinationFull);
        }
        else
        {
            throw new FileNotFoundException("Source does not exist", source);
        }

        return JsonSerializer.Serialize(new { source, destination });
    }

    [McpServerTool, Description("Delete a file or empty directory")]
    public string DeleteFile(
        [Description("Relative path from workspace root")]
        string path,
        [Description("Workspace id")]
        string workspace_id)
    {
        var full = ResolveSafe(workspace_id, path);
        if (File.Exists(full))
        {
            File.Delete(full);
        }
        else if (Directory.Exists(full))
        {
            Directory.Delete(full);
        }
        else
        {
            throw new FileNotFoundException("Path does not exist", path);
        }

        return JsonSerializer.Serialize(new { path, deleted = true });
    }

    private string ResolveSafe(string workspaceId, string path)
    {
        var root = _workspaces.GetRoot(workspaceId);
        _filter.EnsureAllowed(path);
        return _sandbox.Resolve(root, path);
    }
}
