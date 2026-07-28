using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft den Zustand verbreiteter Linux-Firewalls ohne Konfigurationen zu verändern.
/// </summary>
public sealed class FirewallChecker : OpSecCheckerBase
{
    public override string Name => "System-Firewall-Statusprüfung";
    public override string Category => "System / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung des Firewall-Status...");

        bool isUfwConfigEnabled = false;
        if (File.Exists("/etc/ufw/ufw.conf"))
        {
            string[] lines = await File.ReadAllLinesAsync("/etc/ufw/ufw.conf");
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                if (trimmed.Equals("ENABLED=yes", StringComparison.OrdinalIgnoreCase))
                {
                    isUfwConfigEnabled = true;
                    break;
                }
            }
        }

        bool isUfwServiceEnabled = await CheckSystemdStatusAsync("ufw", "enabled");
        bool isUfwActive = isUfwConfigEnabled && isUfwServiceEnabled;

        bool isFirewalldActive = await CheckSystemdStatusAsync("firewalld", "active");
        bool isNftablesActive = await CheckSystemdStatusAsync("nftables", "active");

        if (isUfwActive || isFirewalldActive || isNftablesActive)
        {
            string activeFirewall = isUfwActive ? "UFW" : (isFirewalldActive ? "Firewalld" : "Nftables");
            Logger.LogInfo($"Aktive Firewall erkannt: {activeFirewall}");
            return Pass(
                $"System-Firewall ({activeFirewall}) ist AKTIV.",
                $"Es wurde eine aktive und vom System freigegebene Firewall erkannt ({activeFirewall}).");
        }

        Logger.LogWarning("Keine aktive System-Firewall erkannt!");
        return Warning(
            "Keine aktive lokale Firewall gefunden.",
            "Weder UFW, Firewalld noch Nftables sind auf diesem System als aktiv gemeldet.\n\nHinweis: Aktiviere bei Bedarf die Firewall über deine Systemeinstellungen.");
    }

    private static async Task<bool> CheckSystemdStatusAsync(string serviceName, string statusType)
    {
        var result = await ShellCommandService.ExecuteAsync("systemctl", $"is-{statusType} {serviceName}");
        return result.StandardOutput.Trim().Equals(statusType, StringComparison.OrdinalIgnoreCase);
    }
}
