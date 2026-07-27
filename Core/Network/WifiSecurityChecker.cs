using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Ermittelt unter Linux, ob WLAN-Hardware vorhanden und per rfkill blockiert ist.
/// Verschlüsselung oder Authentifizierung einer Verbindung werden nicht bewertet.
/// </summary>
public sealed class WifiSecurityChecker : IOpSecChecker
{
    public string Name => "Linux-WLAN-Aktivitäts- und rfkill-Prüfung";
    public string Category => "Netzwerk / Funk";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Linux-WLAN-Schnittstellen...");

        try
        {
            if (CheckWifiRfkillBlocked())
            {
                Logger.LogInfo("WLAN ist per rfkill blockiert.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "WLAN ist per rfkill blockiert.",
                    Details = "Mindestens eine WLAN-rfkill-Schnittstelle meldet einen Hardware- oder Software-Block. Die Verschlüsselung gespeicherter WLAN-Profile wird nicht geprüft."
                });
            }

            bool hasWifiInterface = false;
            const string netSys = "/sys/class/net";
            if (Directory.Exists(netSys))
            {
                foreach (string directory in Directory.GetDirectories(netSys))
                {
                    if (Directory.Exists(Path.Combine(directory, "wireless")) ||
                        File.Exists(Path.Combine(directory, "phy80211")))
                    {
                        hasWifiInterface = true;
                        break;
                    }
                }
            }

            if (!hasWifiInterface)
            {
                Logger.LogInfo("Keine WLAN-Schnittstelle erkannt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Keine WLAN-Schnittstelle erkannt.",
                    Details = "Unter `/sys/class/net` wurde keine drahtlose Netzwerkschnittstelle gefunden."
                });
            }

            Logger.LogWarning("WLAN-Schnittstelle vorhanden und nicht per rfkill blockiert.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "WLAN-Hardware ist vorhanden und nicht per rfkill blockiert.",
                Details = "Die Prüfung bestätigt nur den lokalen Funkstatus. Sie ermittelt weder eine aktive Verbindung noch deren WPA-/WPA2-/WPA3-Authentifizierung. Deaktiviere WLAN bei sensiblen Offline-Arbeiten, wenn es nicht benötigt wird."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Linux-WLAN-Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "WLAN-Status konnte nicht vollständig geprüft werden.",
                Details = ex.Message
            });
        }
    }

    private static bool CheckWifiRfkillBlocked()
    {
        const string sysRfkill = "/sys/class/rfkill";
        if (!Directory.Exists(sysRfkill))
        {
            return false;
        }

        foreach (string directory in Directory.GetDirectories(sysRfkill))
        {
            string typePath = Path.Combine(directory, "type");
            if (!File.Exists(typePath) || File.ReadAllText(typePath).Trim() != "wlan")
            {
                continue;
            }

            string softPath = Path.Combine(directory, "soft");
            string hardPath = Path.Combine(directory, "hard");
            bool softBlocked = File.Exists(softPath) && File.ReadAllText(softPath).Trim() == "1";
            bool hardBlocked = File.Exists(hardPath) && File.ReadAllText(hardPath).Trim() == "1";
            if (softBlocked || hardBlocked)
            {
                return true;
            }
        }

        return false;
    }
}
