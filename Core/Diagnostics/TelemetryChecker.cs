using System;
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
                              "Hinweis: Diese Dienste lesen/indizieren Dateien oder erfassen Diagnosedaten. Deaktiviere sie bei Bedarf (z. B. `balooctl disable` oder via systemctl)."
                });
            }

            Logger.LogTrace("Keine bekannten Telemetrie- oder Indizierungsdienste im Hintergrund aktiv.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine aktiven Telemetrie- oder Indexer-Dienste.",
                Details = "Gängige Tracker-Dienste (GNOME Tracker, KDE Baloo, Apport, Telemetrie) sind inaktiv."
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
