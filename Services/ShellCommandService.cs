using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpSecAuditTool.Services;

/// <summary>
/// Ergebnis einer nicht-interaktiven Linux-Befehlsabfrage.
/// </summary>
public sealed record ShellCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut)
{
    public bool IsSuccess => !TimedOut && ExitCode == 0;
}

/// <summary>
/// Führt nicht-interaktive System- und CLI-Befehle sicher mit Timeout aus.
/// </summary>
public static class ShellCommandService
{
    public static async Task<ShellCommandResult> ExecuteAsync(
        string fileName,
        string arguments,
        TimeSpan? timeout = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                return new ShellCommandResult(
                    -1,
                    string.Empty,
                    $"Der Prozess '{fileName}' konnte nicht gestartet werden.",
                    false);
            }

            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();
            using var timeoutCancellation = new CancellationTokenSource(
                timeout ?? TimeSpan.FromSeconds(10));

            try
            {
                await process.WaitForExitAsync(timeoutCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Prozess kann bereits beendet sein.
                }

                return new ShellCommandResult(
                    -1,
                    standardOutput.IsCompletedSuccessfully
                        ? await standardOutput.ConfigureAwait(false)
                        : string.Empty,
                    standardError.IsCompletedSuccessfully
                        ? await standardError.ConfigureAwait(false)
                        : $"Der Befehl '{fileName}' wurde nach Ablauf des Zeitlimits beendet.",
                    true);
            }

            return new ShellCommandResult(
                process.ExitCode,
                await standardOutput.ConfigureAwait(false),
                await standardError.ConfigureAwait(false),
                false);
        }
        catch (Exception ex)
        {
            return new ShellCommandResult(
                -1,
                string.Empty,
                $"Fehler bei Ausführung von '{fileName}': {ex.Message}",
                false);
        }
    }
}
