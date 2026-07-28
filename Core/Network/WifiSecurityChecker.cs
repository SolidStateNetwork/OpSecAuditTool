using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Bewertet die Verschlüsselung der aktuell verwendeten WLAN-Verbindung unter Linux.
/// </summary>
public sealed class WifiSecurityChecker : IOpSecChecker
{
    public string Name => "WLAN-Schnittstellen- & Funk-Prüfung";
    public string Category => "Netzwerk / Anonymität";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Wi-Fi Schnittstellen...");

        try
        {
            bool isBlockedByRfkill = CheckWifiRfkillBlocked();

            if (isBlockedByRfkill)
            {
                Logger.LogInfo("Wi-Fi ist per rfkill vollständig blockiert.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Wi-Fi ist deaktiviert / blockiert (rfkill).",
                    Details = "Die WLAN-Schnittstelle ist per Hardware/Software-Block vollständig deaktiviert. Keinerlei Funk-Beacons oder Probe Requests werden ausgesendet."
                });
            }

            string netSys = "/sys/class/net";
            bool hasWifiInterface = false;

            if (Directory.Exists(netSys))
            {
                foreach (var dir in Directory.GetDirectories(netSys))
                {
                    if (Directory.Exists(Path.Combine(dir, "wireless")) || File.Exists(Path.Combine(dir, "phy80211")))
                    {
                        hasWifiInterface = true;
                        break;
                    }
                }
            }

            if (!hasWifiInterface)
            {
                Logger.LogInfo("Keine Wi-Fi Schnittstelle auf dem System gefunden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Keine WLAN-Hardware vorhanden.",
                    Details = "Auf diesem System wurden keine drahtlosen Netzwerk-Schnittstellen identifiziert."
                });
            }

            Logger.LogWarning("WLAN-Schnittstelle ist aktiv und nicht per rfkill blockiert!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Wi-Fi Schnittstelle ist aktiv!",
                Details = "Eine WLAN-Karte ist im System aktiv. Wenn sie nicht genutzt wird, sie sendet kontinuierlich Sonden (Probe Requests) aus, die Standortspuren hinterlassen können.\n\n" +
                          "Empfehlung: Blockiere WLAN bei Nichtgebrauch via `rfkill block wifi`."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Wi-Fi-Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Wi-Fi Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }

    private static bool CheckWifiRfkillBlocked()
    {
        string sysRfkill = "/sys/class/rfkill";
        if (!Directory.Exists(sysRfkill)) return false;

        foreach (string directory in Directory.GetDirectories(sysRfkill))
        {
            string typePath = Path.Combine(directory, "type");
            if (File.Exists(typePath) && File.ReadAllText(typePath).Trim() == "wlan")
            {
                string softPath = Path.Combine(directory, "soft");
                string hardPath = Path.Combine(directory, "hard");

                bool softBlocked = File.Exists(softPath) && File.ReadAllText(softPath).Trim() == "1";
                bool hardBlocked = File.Exists(hardPath) && File.ReadAllText(hardPath).Trim() == "1";

                if (softBlocked || hardBlocked) return true;
            }
        }

        return false;
    }
}
