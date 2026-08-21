using System.Diagnostics;
using System.Text;

namespace LocalCodingMcp.Services;

public sealed class CommandResult
{
    public required int ExitCode { get; init; }
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public required double DurationMs { get; init; }
}

/// <summary>
/// Runs shell commands inside a workspace with timeout.
/// </summary>
public sealed class CommandRunner
{
    private readonly int _timeoutSeconds;
    private readonly bool _isWindows;

    public CommandRunner(int timeoutSeconds = 30, bool? isWindows = null)
    {
        _timeoutSeconds = Math.Clamp(timeoutSeconds, 5, 300);
        _isWindows = isWindows ?? OperatingSystem.IsWindows();
    }

    public async Task<CommandResult> RunAsync(string command, string workingDirectory, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be empty.");

        var sw = Stopwatch.StartNew();

        var psi = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        ConfigureShell(psi, command, _isWindows);

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new TimeoutException($"Command timed out after {_timeoutSeconds} seconds.");
        }

        sw.Stop();

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            Stdout = stdout.ToString(),
            Stderr = stderr.ToString(),
            DurationMs = sw.Elapsed.TotalMilliseconds
        };
    }

    /// <summary>Visible for tests — configures FileName/Arguments for the host OS.</summary>
    public static void ConfigureShell(ProcessStartInfo psi, string command, bool isWindows)
    {
        if (isWindows)
        {
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c " + command;
        }
        else
        {
            psi.FileName = "/bin/bash";
            psi.Arguments = "-c " + Quote(command);
        }
    }

    private static string Quote(string value) =>
        "\"" + value.Replace("\"", "\\\"") + "\"";
}
