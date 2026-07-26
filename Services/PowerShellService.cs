using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpSecAuditTool.Services;

/// <summary>
/// Ergebnis einer nicht-interaktiven, zeitlich begrenzten PowerShell-Abfrage.
/// </summary>
public sealed record PowerShellResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut)
{
    public bool IsSuccess => !TimedOut && ExitCode == 0;
}

/// <summary>
/// Führt ausschließlich explizit übergebenen, nicht-interaktiven PowerShell-Code aus.
/// Die Windows-Checker verwenden dies nur für lesende Systemabfragen.
/// </summary>
public static class PowerShellService
{
    public static async Task<PowerShellResult> ExecuteReadOnlyAsync(
        string script,
        TimeSpan? timeout = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new PowerShellResult(
                -1,
                string.Empty,
                "PowerShell-Systemabfragen sind nur unter Windows verfügbar.",
                false);
        }

        string encodedScript = Convert.ToBase64String(
            Encoding.Unicode.GetBytes(
                "$ErrorActionPreference='Stop';" +
                "$OutputEncoding=[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new();" +
                script));
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoLogo -NoProfile -NonInteractive -EncodedCommand {encodedScript}",
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
                return new PowerShellResult(
                    -1,
                    string.Empty,
                    "powershell.exe konnte nicht gestartet werden.",
                    false);
            }

            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();
            using var timeoutCancellation = new CancellationTokenSource(
                timeout ?? TimeSpan.FromSeconds(12));

            try
            {
                await process.WaitForExitAsync(timeoutCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Der Prozess kann sich zwischen Timeout und Kill bereits beendet haben.
                }

                return new PowerShellResult(
                    -1,
                    standardOutput.IsCompletedSuccessfully
                        ? standardOutput.Result
                        : string.Empty,
                    standardError.IsCompletedSuccessfully
                        ? standardError.Result
                        : "Die PowerShell-Abfrage wurde nach dem Zeitlimit beendet.",
                    true);
            }

            return new PowerShellResult(
                process.ExitCode,
                await standardOutput,
                await standardError,
                false);
        }
        catch (Exception ex)
        {
            return new PowerShellResult(-1, string.Empty, ex.Message, false);
        }
    }
}
