using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Prüft, ob DNS-over-TLS (DoT) oder ein verschlüsselter DNS-Proxy aktiv konfiguriert
/// ist, um das Mitlesen von DNS-Abfragen durch Router oder ISP zu verhindern.
/// </summary>
public sealed class EncryptedDnsChecker : OpSecCheckerBase
{
    public override string Name => "DNS-over-TLS / DNS-over-HTTPS (DoT / DoH) Härtung";
    public override string Category => "Netzwerk / Anonymität";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung auf verschlüsselte DNS-Auflösung (DoT / DoH)...");

        if (!OperatingSystem.IsLinux())
        {
            return Warning(
                "Nicht-Linux System übersprungen.",
                "Die Prüfung der Linux systemd-resolved DNSOverTLS-Konfiguration ist hier nicht anwendbar.");
        }

        bool hasDoTConfigured = false;
        string detailInfo = string.Empty;

        // 1. Prüfe /etc/systemd/resolved.conf
        if (File.Exists("/etc/systemd/resolved.conf"))
        {
            try
            {
                var lines = await File.ReadAllLinesAsync("/etc/systemd/resolved.conf");
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#")) continue;

                    if (trimmed.Contains("DNSOverTLS=", StringComparison.OrdinalIgnoreCase) &&
                        (trimmed.Contains("yes", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("opportunistic", StringComparison.OrdinalIgnoreCase)))
                    {
                        hasDoTConfigured = true;
                        detailInfo = "In '/etc/systemd/resolved.conf' ist DNSOverTLS konfiguriert.";
                        break;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        // 2. Prüfe resolvectl status
        if (!hasDoTConfigured)
        {
            var res = await ShellCommandService.ExecuteAsync("resolvectl", "status");
            if (res.IsSuccess && res.StandardOutput.Contains("+DoT", StringComparison.Ordinal))
            {
                hasDoTConfigured = true;
                detailInfo = "'resolvectl status' meldet aktive +DoT (DNS over TLS) Unterstützung auf mindestens einem Interface.";
            }
        }

        // 3. Prüfe ob dnscrypt-proxy oder stubby läuft
        if (!hasDoTConfigured)
        {
            if (ProcessInspectionService.IsAnyRunning("dnscrypt-proxy") || ProcessInspectionService.IsAnyRunning("stubby"))
            {
                hasDoTConfigured = true;
                detailInfo = "Lokaler verschlüsselter DNS-Daemon ('dnscrypt-proxy' oder 'stubby') ist aktiv.";
            }
        }

        if (hasDoTConfigured)
        {
            return Pass(
                "Verschlüsselte DNS-Auflösung (DoT / DoH) ist konfiguriert.",
                detailInfo);
        }

        return Warning(
            "Keine explizite DNS-over-TLS (DoT) Konfiguration erkannt.",
            "Klassisches DNS (Port 53) wird unverschlüsselt übertragen, wodurch ISPs oder lokales WLAN deine Domainaufrufe mitlesen können.\n\n" +
            "Empfehlung: Aktiviere 'DNSOverTLS=yes' in '/etc/systemd/resolved.conf' oder nutze einen verschlüsselten DNS-Resolver.");
    }
}
