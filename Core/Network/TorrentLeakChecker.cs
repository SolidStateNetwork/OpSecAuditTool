using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Prüft laufende Prozesse auf bekannte BitTorrent-Clients mit möglichem IP-Leak-Risiko.
/// Arbeitet rein lokal im Speicher und benötigt keinen Internetzugriff.
/// </summary>
public sealed class TorrentLeakChecker : OpSecCheckerBase
{
    public override string Name => "BitTorrent-Client & IP-Leak-Prüfung";
    public override string Category => "Netzwerk / Anonymität";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung auf aktive Torrent-Clients...");

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
            return Task.FromResult(Warning(
                $"Torrent-Client läuft: {processName}",
                $"Der Prozess '{processName}' wird derzeit ausgeführt.\n\n" +
                "Hinweis: BitTorrent-Verbindungen können deine öffentliche IP an Schwärme leaken, wenn kein Kill-Switch oder dediziertes VPN-Bindung eingerichtet ist."));
        }

        Logger.LogInfo("Keine aktiven BitTorrent-Prozesse lokalisiert.");
        return Task.FromResult(Pass(
            "Keine Torrent-Clients aktiv.",
            "Es wurden keine bekannten BitTorrent-Dienste im Speicher des Systems gefunden."));
    }
}
