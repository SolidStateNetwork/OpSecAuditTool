using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Erkennt bekannte Telemetrie- und Fehlerberichtsdienste im Linux-Hintergrund.
/// </summary>
public sealed class TelemetryChecker : IOpSecChecker
{
    public string Name => "System-Telemetrie & Diagnosedienste";
    public string Category => "System / Härtung";
    public bool CanFix => true;
    public string FixDescription => "Fügt Anti-Telemetrie Umgebungsvariablen (DOTNET_CLI_TELEMETRY_OPTOUT=1, etc.) zu ~/.bashrc hinzu.";

    public async Task<FixResult> FixAsync()
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string bashrc = Path.Combine(homeDir, ".bashrc");
            string lines = "\n# OpSec Anti-Telemetry\nexport DOTNET_CLI_TELEMETRY_OPTOUT=1\nexport POWERSHELL_TELEMETRY_OPTOUT=1\nexport SAM_CLI_TELEMETRY=0\n";

            if (File.Exists(bashrc) && !(await File.ReadAllTextAsync(bashrc)).Contains("DOTNET_CLI_TELEMETRY_OPTOUT"))
            {
                BackupService.BackupFile(bashrc);
                await File.AppendAllTextAsync(bashrc, lines);
                return new FixResult { Success = true, Message = "Anti-Telemetrie-Variablen wurden zu ~/.bashrc hinzugefügt (Sicherung im 'Backups'-Ordner)." };
            }

            return new FixResult { Success = true, Message = "Anti-Telemetrie-Variablen waren in ~/.bashrc bereits vorhanden." };
        }
        catch (Exception ex)
        {
            return new FixResult { Success = false, Message = $"Fehler beim Härten: {ex.Message}" };
        }
    }

    private readonly string[] _telemetryProcesses = new[]
    {
        "tracker-miner-fs",
        "tracker-miner-3",
        "localsearch",
        "baloo_file",
        "ubuntu-report",
        "popularity-contest",
        "apport"
    };

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung auf aktive System-Telemetrie und Datei-Indexer...");

        try
        {
            var activeTrackers = ProcessInspectionService.FindRunning(_telemetryProcesses);

            if (activeTrackers.Count > 0)
            {
                string trackerList = string.Join(", ", activeTrackers);
                Logger.LogWarning($"Aktive Telemetrie-/Indexer-Dienste gefunden: {trackerList}");

                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"{activeTrackers.Count} Telemetrie- / Indizierungsdienste aktiv!",
                    Details = $"Folgende Hintergrunddienste wurden erkannt:\n• {string.Join("\n• ", activeTrackers)}\n\n" +
                              "Hinweis: Diese Dienste lesen/indizieren Dateien oder erfassen Diagnosedaten. Deaktiviere sie bei Bedarf (z. B. `balooctl disable` oder via systemctl).",
                    CanFix = CanFix,
                    FixDescription = FixDescription,
                    Checker = this
                });
            }

            Logger.LogTrace("Keine bekannten Telemetrie- oder Indizierungsdienste im Hintergrund aktiv.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine aktiven Telemetrie- oder Indexer-Dienste.",
                Details = "Gängige Tracker-Dienste (GNOME Tracker, KDE Baloo, Apport, Telemetrie) sind inaktiv.",
                CanFix = CanFix,
                FixDescription = FixDescription,
                Checker = this
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Telemetrie-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Telemetrie Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
