using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Prüft laufende Prozesse auf bekannte BitTorrent-Clients mit möglichem
/// IP-Expositionsrisiko. Die Prüfung arbeitet ausschließlich lokal.
/// </summary>
public sealed class TorrentLeakChecker : IOpSecChecker
{
    public string Name => "Aktive BitTorrent-Clients";
    public string Category => "Netzwerk / Anonymität";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte lokale Prüfung auf aktive Torrent-Clients...");

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
                              "BitTorrent-Verbindungen können die öffentliche IP gegenüber anderen Schwarmteilnehmern offenlegen. Diese lokale Prozessprüfung bestätigt weder VPN-Bindung noch Kill-Switch."
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
            Logger.LogError("Fehler beim lokalen Torrent-Client-Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Torrent-Client-Prüfung fehlgeschlagen.",
                Details = ex.Message
            });
        }
    }
}
