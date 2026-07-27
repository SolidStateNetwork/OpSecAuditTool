using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Erkennt bekannte Linux-Dienste für Diagnosedaten sowie lokale Datei-Indexer.
/// Indexer werden ausdrücklich nicht mit externer Telemetrie gleichgesetzt.
/// </summary>
public sealed class TelemetryChecker : IOpSecChecker
{
    public string Name => "Linux-Diagnosedienste und Datei-Indexer";
    public string Category => "System / Datenschutz";

    private readonly string[] _diagnosticProcesses =
    {
        "ubuntu-report",
        "popularity-contest",
        "apport"
    };

    private readonly string[] _indexerProcesses =
    {
        "tracker-miner-fs",
        "tracker-miner-3",
        "localsearch",
        "baloo_file"
    };

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung auf Diagnosedienste und lokale Datei-Indexer...");

        try
        {
            var diagnostics = ProcessInspectionService.FindRunning(_diagnosticProcesses);
            var indexers = ProcessInspectionService.FindRunning(_indexerProcesses);

            if (diagnostics.Count == 0 && indexers.Count == 0)
            {
                return Task.FromResult(Result(
                    CheckStatus.Pass,
                    "Keine bekannten Diagnosedienste oder Datei-Indexer aktiv.",
                    "In der begrenzten Prozessliste wurden keine passenden Hintergrunddienste erkannt."));
            }

            string details = string.Empty;
            if (diagnostics.Count > 0)
            {
                details += $"Diagnose-/Berichtsdienste:\n• {string.Join("\n• ", diagnostics)}\n";
            }
            if (indexers.Count > 0)
            {
                details += $"Lokale Datei-Indexer:\n• {string.Join("\n• ", indexers)}\n";
            }

            Logger.LogWarning($"{diagnostics.Count} Diagnosedienst(e) und {indexers.Count} Datei-Indexer erkannt.");
            return Task.FromResult(Result(
                CheckStatus.Warning,
                $"{diagnostics.Count} Diagnosedienst(e) und {indexers.Count} lokale Datei-Indexer aktiv.",
                details +
                "\nDatei-Indexer wie Baloo oder Tracker arbeiten normalerweise lokal und sind nicht automatisch Telemetrie. Sie können jedoch Dateinamen und Inhalte katalogisieren. Prüfe jeden Dienst nach deinem Bedrohungsmodell."));
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Diagnose-/Indexer-Prüfung", ex);
            return Task.FromResult(Result(
                CheckStatus.Warning,
                "Diagnosedienste und Datei-Indexer konnten nicht geprüft werden.",
                ex.Message));
        }
    }

    private CheckResult Result(CheckStatus status, string summary, string details) => new()
    {
        Name = Name,
        Category = Category,
        Status = status,
        Summary = summary,
        Details = details
    };
}
