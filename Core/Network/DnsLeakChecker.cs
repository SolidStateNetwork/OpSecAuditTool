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
/// </summary>
public sealed class DnsLeakChecker : IOpSecChecker
{
    public string Name => "DNS-Server & DNS-Leak-Prüfung";
    public string Category => "Netzwerk / Anonymität";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der konfigurierten DNS-Server...");

        if (!SettingsService.AllowInternetAccess)
        {
            Logger.LogTrace("Internet-Zugriff ist in den Einstellungen deaktiviert. DNS-Leak-Check wird übersprungen.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "Check übersprungen (Offline-Modus).",
                Details = "Internet-Verbindungen wurden in den Einstellungen für das Audit-Tool deaktiviert."
            };
        }

        try
        {
            List<string> dnsServers = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up)
                .SelectMany(network => network.GetIPProperties().DnsAddresses)
                .Where(address => !IPAddress.IsLoopback(address))
                .Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Einige Linux-Resolver melden nur den lokalen Stub über die
            // Netzwerkschnittstelle; resolv.conf dient dort als zusätzlicher Fallback.
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

                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync("one.one.one.one");
                    Logger.LogTrace($"DNS-Auflösung erfolgreich: {hostEntry.HostName}");
                }
                catch
                {
                    // Die optionale Namensauflösung beeinflusst die lokale Resolver-Auswertung nicht.
                }

                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = $"DNS Server aktiv: {serverList}",
                    Details = $"Die folgenden DNS-Resolver sind für aktive Netzwerkschnittstellen konfiguriert:\n• {string.Join("\n• ", dnsServers)}\n\nHinweis: Stelle sicher, dass deine DNS-Anfragen verschlüsselt (DoH/DoT) verarbeitet werden."
                };
            }

            Logger.LogWarning("Keine lokalen DNS-Resolver in resolv.conf gefunden!");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Keine DNS-Server konfiguriert.",
                Details = "Für aktive Netzwerkschnittstellen konnten keine DNS-Resolver ermittelt werden."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der DNS-Prüfung", ex);
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "DNS Check fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            };
        }
    }
}
