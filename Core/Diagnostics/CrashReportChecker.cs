using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Sucht plattformabhängig nach lokal gespeicherten Absturz- und Fehlerberichten.
/// </summary>
public sealed class CrashReportChecker : OpSecCheckerBase
{
    public override string Name => "Absturzberichte- & Coredump-Index-Prüfung";
    public override string Category => "Anti-Forensik / Hygiene";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der Absturzberichte...");

        if (OperatingSystem.IsWindows())
        {
            return CheckWindowsErrorReports();
        }

        var result = await ShellCommandService.ExecuteAsync("coredumpctl", "list --no-legend");
        if (!result.IsSuccess)
        {
            if ((result.StandardError + result.StandardOutput).Contains("No coredumps found", StringComparison.OrdinalIgnoreCase) ||
                (result.StandardError + result.StandardOutput).Contains("No match found", StringComparison.OrdinalIgnoreCase))
            {
                return Pass("Keine gelisteten Coredump-Absturzberichte.", "Der `coredumpctl`-Index enthält keine protokollierten Programmabstürze.");
            }
            return Warning(
                "Crash-Report-Prüfung eingeschränkt möglich.",
                "Das Befehlszeilen-Tool `coredumpctl` ist auf diesem System nicht verfügbar oder nicht abfragbar.");
        }

        string[] lines = result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length > 0)
        {
            Logger.LogWarning($"Coredumpctl hat {lines.Length} protokollierte Programmabstürze im Index gefunden!");
            return Warning(
                $"{lines.Length} Absturzberichte (Coredumps) im System-Index!",
                $"Der Befehl `coredumpctl` führt {lines.Length} historische Absturzberichte im Index.\n\n" +
                "Hinweis: Diese Berichte können RAM-Auszüge enthalten. Bereinige den Index via `coredumpctl vacuum` oder fahre das Logging herunter.");
        }

        Logger.LogTrace("Keine Einträge im coredumpctl Index vorhanden.");
        return Pass(
            "Keine gelisteten Coredump-Absturzberichte.",
            "Der `coredumpctl`-Index enthält keine protokollierten Programmabstürze.");
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

        return reportCount == 0
            ? Pass(
                "Keine lesbaren Windows-Absturzberichte gefunden.",
                "In den ohne Administratorrechte lesbaren WER-Verzeichnissen wurden keine Berichte gefunden.")
            : Warning(
                $"{reportCount} Windows-Fehlerbericht(e) gefunden.",
                "Windows Error Reporting kann Programmnamen, Pfade, Modulversionen und " +
                "unter Umständen Speicherabbilder für spätere Diagnose aufbewahren.");
    }
}
