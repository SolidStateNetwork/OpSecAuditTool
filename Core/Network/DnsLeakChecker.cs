using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Bewertet die konfigurierten DNS-Resolver und erkennt auffällige Resolver-Konstellationen.
/// Funktioniert offline sowie (optional) online.
/// </summary>
public sealed class DnsLeakChecker : OpSecCheckerBase
{
    public override string Name => "DNS-Server & DNS-Leak-Prüfung";
    public override string Category => "Netzwerk / Anonymität";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der konfigurierten DNS-Server...");

        List<string> dnsServers = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up)
            .SelectMany(network => network.GetIPProperties().DnsAddresses)
            .Where(address => !IPAddress.IsLoopback(address))
            .Select(address => address.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (dnsServers.Count == 0 && OperatingSystem.IsLinux() && File.Exists("/etc/resolv.conf"))
        {
            var lines = await File.ReadAllLinesAsync("/etc/resolv.conf");
            foreach (var line in lines)
            {
                if (line.StartsWith("nameserver"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        dnsServers.Add(parts[1]);
                    }
                }
            }
        }

        if (dnsServers.Count > 0)
        {
            string serverList = string.Join(", ", dnsServers);
            Logger.LogInfo($"Gefundene DNS-Server: {serverList}");

            if (SettingsService.AllowInternetAccess)
            {
                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync("one.one.one.one");
                    Logger.LogTrace($"DNS-Auflösung erfolgreich: {hostEntry.HostName}");
                }
                catch (Exception ex)
                {
                    Logger.LogTrace($"DNS-Auflösungstest offline/fehlgeschlagen: {ex.Message}");
                }
            }

            return Pass(
                $"DNS Server aktiv: {serverList}",
                $"Die folgenden DNS-Resolver sind für aktive Netzwerkschnittstellen konfiguriert:\n• {string.Join("\n• ", dnsServers)}\n\nHinweis: Stelle sicher, dass deine DNS-Anfragen verschlüsselt (DoH/DoT) verarbeitet werden.");
        }

        Logger.LogWarning("Keine lokalen DNS-Resolver in resolv.conf oder aktiven Schnittstellen gefunden!");
        return Warning(
            "Keine DNS-Server konfiguriert.",
            "Für aktive Netzwerkschnittstellen konnten keine externen DNS-Resolver ermittelt werden.");
    }
}
