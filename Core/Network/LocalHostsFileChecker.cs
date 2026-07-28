using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Prüft die lokale '/etc/hosts' Datei auf Manipulationen oder Hijacking-Einträge,
/// die wichtige Sicherheits-, Paket- oder Entwickler-Domains umleiten.
/// </summary>
public sealed class LocalHostsFileChecker : OpSecCheckerBase
{
    public override string Name => "Lokale /etc/hosts & DNS-Hijacking Prüfung";
    public override string Category => "Netzwerk / Anonymität";

    private static readonly string[] MonitoredSecurityDomains =
    {
        "github.com",
        "google.com",
        "deb.debian.org",
        "security.ubuntu.com",
        "archive.ubuntu.com",
        "packages.microsoft.com",
        "raw.githubusercontent.com",
        "api.github.com"
    };

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung von '/etc/hosts' auf Manipulationen...");

        if (!File.Exists("/etc/hosts"))
        {
            return Pass(
                "Keine '/etc/hosts' Datei vorhanden oder zugänglich.",
                "Es wurde keine Standard-Hosts-Datei gefunden.");
        }

        var suspiciousRedirects = new List<string>();

        try
        {
            var lines = await File.ReadAllLinesAsync("/etc/hosts");
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                foreach (var domain in MonitoredSecurityDomains)
                {
                    if (trimmed.Contains(domain, StringComparison.OrdinalIgnoreCase))
                    {
                        suspiciousRedirects.Add(trimmed);
                        break;
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Warning(
                "Kein Lesezugriff auf '/etc/hosts'.",
                "Die Prüfung der lokalen Hosts-Datei wurde wegen unzureichender Berechtigungen übersprungen.");
        }

        if (suspiciousRedirects.Count > 0)
        {
            return Warning(
                $"{suspiciousRedirects.Count} verdächtige Domain-Umleitung(en) in '/etc/hosts' entdeckt!",
                $"Folgende Einträge in '/etc/hosts' überschreiben das DNS für kritische Entwickler- und Sicherheits-Server:\n• {string.Join("\n• ", suspiciousRedirects)}\n\n" +
                "Empfehlung: Prüfe, ob diese Umleitungen absichtlich gesetzt wurden (z. B. lokaler Proxy/Mirror) oder von einer Schadsoftware stammen.");
        }

        return Pass(
            "Keine verdächtigen Umleitungen in '/etc/hosts' gefunden.",
            "Weder GitHub- noch Linux-Paketquellen oder Sicherheitsdomains werden über die lokale Hosts-Datei manipuliert.");
    }
}
