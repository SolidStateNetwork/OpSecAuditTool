using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Prüft laufende Prozesse auf bekannte BitTorrent-Clients mit möglichem IP-Leak-Risiko.
/// </summary>
public sealed class TorrentLeakChecker : IOpSecChecker
{
    public string Name => "BitTorrent-Client & IP-Leak-Prüfung";
    public string Category => "Netzwerk / Anonymität";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung auf aktive Torrent-Clients...");

        if (!SettingsService.AllowInternetAccess)
        {
            Logger.LogTrace("Internet-Zugriff ist in den Einstellungen deaktiviert. Torrent-Leak-Check wird übersprungen.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "Check übersprungen (Offline-Modus).",
                Details = "Internet-Verbindungen wurden in den Einstellungen für das Audit-Tool deaktiviert."
            });
        }

        try
        {
            string[] torrentProcesses =
            {
                "qbittorrent", "transmission-gtk", "transmission-daemon",
                "deluge", "aria2c", "rtorrent", "utorrent", "bittorrent"
            };
            var activeProcesses = ProcessInspectionService.FindRunning(torrentProcesses);

            if (activeProcesses.Count > 0)
            {
                string processName = activeProcesses[0];
                Logger.LogWarning($"Aktiver Torrent-Client entdeckt: {processName}");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"Torrent-Client läuft: {processName}",
                    Details = $"Der Prozess '{processName}' wird derzeit ausgeführt.\n\n" +
                              "Hinweis: BitTorrent-Verbindungen können deine öffentliche IP an Schwärme leaken, wenn kein Kill-Switch oder dediziertes VPN-Bindung eingerichtet ist."
                });
            }

            Logger.LogInfo("Keine aktiven BitTorrent-Prozesse lokalisiert.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine aktiven Torrent-Clients gefunden.",
                Details = "Es wurden keine gängigen BitTorrent-Anwendungen im Prozessbaum identifiziert."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Torrent-Leak Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Torrent Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
