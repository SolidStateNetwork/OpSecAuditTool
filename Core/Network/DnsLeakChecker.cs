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
/// Ermittelt die lokal konfigurierten DNS-Resolver. Die Prüfung behauptet bewusst
/// weder einen extern gemessenen DNS-Leak noch eine nicht lokal verifizierbare
/// DoH-/DoT-Verschlüsselung.
/// </summary>
public sealed class DnsLeakChecker : IOpSecChecker
{
    public string Name => "Lokale DNS-Resolver-Konfiguration";
    public string Category => "Netzwerk / Anonymität";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der lokal konfigurierten DNS-Resolver...");

        try
        {
            var dnsServers = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up)
                .SelectMany(network => network.GetIPProperties().DnsAddresses)
                .Where(address => !IPAddress.IsLoopback(address))
                .Select(address => address.ToString())
                .ToList();

            // Einige Linux-Resolver melden über die Schnittstelle nur einen lokalen
            // Stub. resolv.conf ergänzt die lokal sichtbare Konfiguration, ohne eine
            // Internetverbindung aufzubauen.
            if (OperatingSystem.IsLinux() && File.Exists("/etc/resolv.conf"))
            {
                string[] lines = await File.ReadAllLinesAsync("/etc/resolv.conf");
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!trimmed.StartsWith("nameserver", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        dnsServers.Add(parts[1]);
                    }
                }
            }

            string[] distinctServers = dnsServers
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(server => server, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (distinctServers.Length == 0)
            {
                Logger.LogWarning("Keine lokal konfigurierten DNS-Resolver ermittelt.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Keine DNS-Resolver konnten ermittelt werden.",
                    Details = "Für aktive Netzwerkschnittstellen und die lokale Resolver-Konfiguration wurden keine DNS-Server gefunden. Der tatsächliche Auflösungsweg bleibt deshalb unbekannt."
                };
            }

            Logger.LogInfo($"{distinctServers.Length} lokale DNS-Resolver-Konfiguration(en) erkannt.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = $"{distinctServers.Length} DNS-Resolver lokal konfiguriert; Verschlüsselung nicht verifiziert.",
                Details = $"Lokal sichtbare Resolver:\n• {string.Join("\n• ", distinctServers)}\n\n" +
                          "Diese lokale Bestandsaufnahme misst keinen externen DNS-Leak und kann nicht zuverlässig bestätigen, ob DNS-over-HTTPS oder DNS-over-TLS verwendet wird."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der DNS-Resolver-Prüfung", ex);
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "DNS-Resolver konnten nicht vollständig geprüft werden.",
                Details = ex.Message
            };
        }
    }
}
