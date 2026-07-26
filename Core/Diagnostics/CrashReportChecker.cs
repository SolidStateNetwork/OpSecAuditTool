using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Sucht plattformabhängig nach lokal gespeicherten Absturz- und Fehlerberichten.
/// </summary>
public sealed class CrashReportChecker : IOpSecChecker
{
    public string Name => "Absturzberichte- & Coredump-Index-Prüfung";
    public string Category => "Anti-Forensik / Hygiene";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der coredumpctl-Absturzberichte...");

        try
        {
            if (OperatingSystem.IsWindows())
            {
                return CheckWindowsErrorReports();
            }

            var psi = new ProcessStartInfo
            {
                FileName = "coredumpctl",
                Arguments = "list --no-legend",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Logger.LogInfo("coredumpctl Utility steht nicht zur Verfügung.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Fail,
                    Summary = "Crash-Report-Prüfung nicht möglich.",
                    Details = "Das Befehlszeilen-Tool `coredumpctl` ist nicht verfügbar. Vorhandene Absturzberichte lassen sich deshalb nicht sicher ausschließen."
                };
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length > 0)
            {
                Logger.LogWarning($"Coredumpctl hat {lines.Length} protokollierte Programmabstürze im Index gefunden!");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"{lines.Length} Absturzberichte (Coredumps) im System-Index!",
                    Details = $"Der Befehl `coredumpctl` führt {lines.Length} historische Absturzberichte im Index.\n\n" +
                              "Hinweis: Diese Berichte können RAM-Auszüge enthalten. Bereinige den Index via `coredumpctl vacuum` oder fahre das Logging herunter."
                };
            }

            Logger.LogTrace("Keine Einträge im coredumpctl Index vorhanden.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine gelisteten Coredump-Absturzberichte.",
                Details = "Der `coredumpctl`-Index enthält keine protokollierten Programmabstürze."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Coredumpctl-Audit", ex);
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Crash Report Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            };
        }
    }

    private CheckResult CheckWindowsErrorReports()
    {
        string localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        string programData = Environment.GetFolderPath(
            Environment.SpecialFolder.CommonApplicationData);
        string[] reportDirectories =
        {
            Path.Combine(localAppData, "Microsoft", "Windows", "WER"),
            Path.Combine(programData, "Microsoft", "Windows", "WER")
        };

        int reportCount = 0;
        foreach (string directory in reportDirectories.Where(Directory.Exists))
        {
            try
            {
                reportCount += Directory
                    .EnumerateFiles(directory, "*.wer", SearchOption.AllDirectories)
                    .Take(501 - reportCount)
                    .Count();
            }
            catch (UnauthorizedAccessException)
            {
                // Systemweite WER-Ordner können ohne Admin teilweise geschützt sein.
            }
        }

        return new CheckResult
        {
            Name = Name,
            Category = Category,
            Status = reportCount == 0 ? CheckStatus.Pass : CheckStatus.Warning,
            Summary = reportCount == 0
                ? "Keine lesbaren Windows-Absturzberichte gefunden."
                : $"{reportCount} Windows-Fehlerbericht(e) gefunden.",
            Details = reportCount == 0
                ? "In den ohne Administratorrechte lesbaren WER-Verzeichnissen wurden keine Berichte gefunden."
                : "Windows Error Reporting kann Programmnamen, Pfade, Modulversionen und " +
                  "unter Umständen Speicherabbilder für spätere Diagnose aufbewahren."
        };
    }
}
