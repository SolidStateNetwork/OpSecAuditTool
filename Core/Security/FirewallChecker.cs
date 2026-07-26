using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft den Zustand verbreiteter Linux-Firewalls ohne Konfigurationen zu verändern.
/// </summary>
public sealed class FirewallChecker : IOpSecChecker
{
    public string Name => "System-Firewall-Statusprüfung";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Firewall-Status...");

        try
        {
            bool isUfwConfigEnabled = false;
            if (File.Exists("/etc/ufw/ufw.conf"))
            {
                string[] lines = File.ReadAllLines("/etc/ufw/ufw.conf");
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    if (trimmed.Equals("ENABLED=yes", StringComparison.OrdinalIgnoreCase))
                    {
                        isUfwConfigEnabled = true;
                        break;
                    }
                }
            }

            bool isUfwServiceEnabled = CheckSystemdStatus("ufw", "enabled");
            bool isUfwActive = isUfwConfigEnabled && isUfwServiceEnabled;

            bool isFirewalldActive = CheckSystemdStatus("firewalld", "active");
            bool isNftablesActive = CheckSystemdStatus("nftables", "active");

            if (isUfwActive || isFirewalldActive || isNftablesActive)
            {
                string activeFirewall = isUfwActive ? "UFW" : (isFirewalldActive ? "Firewalld" : "Nftables");
                Logger.LogInfo($"Aktive Firewall erkannt: {activeFirewall}");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = $"System-Firewall ({activeFirewall}) ist AKTIV.",
                    Details = $"Es wurde eine aktive und vom System freigegebene Firewall erkannt ({activeFirewall})."
                });
            }

            Logger.LogWarning("Keine aktive System-Firewall erkannt!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "Mögliches Risiko! Keine aktive Firewall gefunden.",
                Details = "Weder UFW, Firewalld noch Nftables sind auf diesem System aktiv.\n\nHinweis: Aktiviere die Firewall über deine KDE-Systemeinstellungen."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Firewall-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Firewall-Status konnte nicht ermittelt werden.",
                Details = $"Fehler beim Zugriff auf Systemdaten: {ex.Message}"
            });
        }
    }

    private static bool CheckSystemdStatus(string serviceName, string statusType)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = $"is-{statusType} {serviceName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return output.Equals(statusType, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
