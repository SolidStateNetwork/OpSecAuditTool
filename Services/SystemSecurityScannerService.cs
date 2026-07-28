using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Media;
using OpSecAuditTool.Core;
using OpSecAuditTool.Core.Security;
using OpSecAuditTool.Core.Windows;
using OpSecAuditTool.Models;
using OpSecAuditTool.Theme;

namespace OpSecAuditTool.Services;

/// <summary>
/// Service für das Ausführen dynamischer System- und Netzwerkprüfungen.
/// </summary>
public static class SystemSecurityScannerService
{
    private static readonly Uri PublicIpEndpoint = new("https://api.ipify.org");
    private static readonly HttpClient PublicIpClient = new(
        new HttpClientHandler { CheckCertificateRevocationList = true })
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static async Task<SystemSecurityStatus> ScanAsync()
    {
        Logger.LogInfo("Manueller System- und Security-Scan gestartet.");
        var status = new SystemSecurityStatus
        {
            PublicIp = "Wird geprüft...",
            DnsServer = "Wird geprüft...",
            LuksEncryption = "Wird geprüft...",
            SwapStatus = "Wird geprüft...",
            FirewallStatus = "Wird geprüft...",
            TorRouting = "Wird geprüft...",
            PublicIpColor = UiPalette.TextPrimary,
            DnsColor = UiPalette.TextPrimary,
            DiskCryptColor = UiPalette.TextPrimary,
            SwapColor = UiPalette.TextPrimary,
            FirewallColor = UiPalette.TextPrimary,
            TorColor = UiPalette.TextPrimary
        };

        // 1. Öffentliche IP
        if (!SettingsService.AllowInternetAccess)
        {
            status.PublicIp = "Deaktiviert (Offline-Modus aktiv)";
            status.PublicIpColor = UiPalette.TextPrimary;
            Logger.LogInfo("Öffentlicher IP-Scan übersprungen (Offline-Modus aktiv).");
        }
        else
        {
            try
            {
                status.PublicIp = (await PublicIpClient.GetStringAsync(PublicIpEndpoint)).Trim();
                status.PublicIpColor = UiPalette.TextPrimary;
            }
            catch (Exception ex)
            {
                status.PublicIp = "Nicht erreichbar (Offline)";
                status.PublicIpColor = UiPalette.Warning;
                Logger.LogWarning($"Öffentliche IP konnte nicht abgefragt werden: {ex.Message}");
            }
        }

        // 2. DNS Server
        try
        {
            string[] dnsServers = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up)
                .SelectMany(network => network.GetIPProperties().DnsAddresses)
                .Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            status.DnsServer = dnsServers.Length > 0 ? string.Join(", ", dnsServers) : "-";
        }
        catch (Exception ex)
        {
            status.DnsServer = "Nicht verfügbar";
            Logger.LogWarning($"DNS-Konfiguration konnte nicht gelesen werden: {ex.Message}");
        }

        // 3. Verschlüsselungsstatus
        try
        {
            if (OperatingSystem.IsWindows())
            {
                CheckResult bitLockerResult = await new WindowsBitLockerChecker().ExecuteAsync();
                status.LuksEncryption = bitLockerResult.Status == CheckStatus.Pass ? "Aktiv (BitLocker)" : "Inaktiv / Unverschlüsselt";
                status.DiskCryptColor = bitLockerResult.Status == CheckStatus.Pass ? UiPalette.Accent : UiPalette.Critical;
            }
            else
            {
                CheckResult encryptionResult = await new DiskEncryptionChecker().ExecuteAsync();
                status.LuksEncryption = encryptionResult.Status == CheckStatus.Pass ? "Aktiv (LUKS/dm-crypt)" : "Inaktiv / Unverschlüsselt";
                status.DiskCryptColor = encryptionResult.Status == CheckStatus.Pass ? UiPalette.Accent : UiPalette.Critical;
            }
        }
        catch (Exception ex)
        {
            status.LuksEncryption = "Nicht verfügbar";
            status.DiskCryptColor = UiPalette.Warning;
            Logger.LogWarning($"Verschlüsselungsstatus konnte nicht gelesen werden: {ex.Message}");
        }

        // 4. Swap-Status
        try
        {
            if (OperatingSystem.IsWindows())
            {
                status.SwapStatus = "Windows-Auslagerungsdatei (Schutz folgt BitLocker)";
                status.SwapColor = status.DiskCryptColor;
            }
            else if (File.Exists("/proc/swaps"))
            {
                string[] swapLines = await File.ReadAllLinesAsync("/proc/swaps");
                bool isSwapActive = swapLines.Length > 1;
                status.SwapStatus = !isSwapActive ? "Deaktiviert" : "Aktiv";
                status.SwapColor = !isSwapActive ? UiPalette.Accent : UiPalette.Warning;
            }
        }
        catch (Exception ex)
        {
            status.SwapStatus = "Nicht verfügbar";
            status.SwapColor = UiPalette.Warning;
            Logger.LogWarning($"Swap-Status konnte nicht gelesen werden: {ex.Message}");
        }

        // 5. Tor-Routing Status
        try
        {
            bool isTorProcessRunning = ProcessInspectionService.IsAnyRunning("tor");
            bool isPort9050Open = await IsPortListeningAsync("127.0.0.1", 9050);
            bool isPort9150Open = await IsPortListeningAsync("127.0.0.1", 9150);

            if (isTorProcessRunning || isPort9050Open || isPort9150Open)
            {
                status.TorRouting = "Aktiv (SOCKS5/Daemon läuft)";
                status.TorColor = UiPalette.Accent;
            }
            else
            {
                status.TorRouting = "Inaktiv";
                status.TorColor = UiPalette.TextPrimary;
            }
        }
        catch (Exception ex)
        {
            status.TorRouting = "Nicht verfügbar";
            status.TorColor = UiPalette.Warning;
            Logger.LogWarning($"Tor-Status konnte nicht ermittelt werden: {ex.Message}");
        }

        // 6. Firewall-Status
        try
        {
            CheckResult fwResult = OperatingSystem.IsWindows()
                ? await new WindowsFirewallChecker().ExecuteAsync()
                : await new FirewallChecker().ExecuteAsync();

            if (fwResult.Status == CheckStatus.Pass)
            {
                status.FirewallStatus = "Aktiv";
                status.FirewallColor = UiPalette.Accent;
            }
            else
            {
                status.FirewallStatus = "Inaktiv / Nicht konfiguriert";
                status.FirewallColor = UiPalette.Critical;
            }
        }
        catch (Exception ex)
        {
            status.FirewallStatus = "Nicht verfügbar";
            status.FirewallColor = UiPalette.Warning;
            Logger.LogWarning($"Firewall-Status konnte nicht ermittelt werden: {ex.Message}");
        }

        Logger.LogInfo("System- und Security-Scan abgeschlossen.");
        return status;
    }

    private static async Task<bool> IsPortListeningAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            Task connectTask = client.ConnectAsync(host, port);
            Task completedTask = await Task.WhenAny(connectTask, Task.Delay(200));
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
