using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Erkennt einen lokalen Tor-Prozess und typische SOCKS-Ports. Die Prüfung kann
/// nicht bestätigen, dass andere Anwendungen ihren Verkehr tatsächlich darüber
/// leiten.
/// </summary>
public sealed class TorStatusChecker : IOpSecChecker
{
    public string Name => "Lokaler Tor-Prozess- und SOCKS-Port-Status";
    public string Category => "Netzwerk / Anonymität";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte lokale Prüfung auf Tor-Prozess und SOCKS-Ports...");

        try
        {
            bool isTorProcessRunning = ProcessInspectionService.IsAnyRunning("tor");
            bool isPort9050Open = await IsPortListeningAsync("127.0.0.1", 9050);
            bool isPort9150Open = await IsPortListeningAsync("127.0.0.1", 9150);

            if (isTorProcessRunning || isPort9050Open || isPort9150Open)
            {
                string details = string.Empty;
                if (isPort9050Open) details += "• Lokaler TCP-Port 9050 ist erreichbar.\n";
                if (isPort9150Open) details += "• Lokaler TCP-Port 9150 ist erreichbar.\n";
                if (isTorProcessRunning) details += "• Ein Prozess mit dem Namen `tor` läuft.\n";

                Logger.LogInfo("Lokale Tor-Indikatoren erkannt.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Lokale Tor-/SOCKS-Indikatoren wurden erkannt.",
                    Details = $"Gefundene Indikatoren:\n{details}\n" +
                              "Ein Prozess oder offener lokaler Port beweist nicht, dass Browser oder andere Anwendungen ihren Datenverkehr über Tor routen. Prüfe deren Proxy-Konfiguration separat."
                };
            }

            Logger.LogInfo("Keine lokalen Tor-Indikatoren erkannt.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Kein lokaler Tor-Prozess oder typischer SOCKS-Port erkannt.",
                Details = "Auf den lokalen Ports 9050 und 9150 wurde kein erreichbarer Dienst gefunden und kein Prozess mit dem Namen `tor` erkannt. Tor ist optional und nicht für jedes Bedrohungsmodell erforderlich."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der lokalen Tor-Prüfung", ex);
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Lokale Tor-Indikatoren konnten nicht geprüft werden.",
                Details = ex.Message
            };
        }
    }

    private static async Task<bool> IsPortListeningAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            Task connectTask = client.ConnectAsync(host, port);
            Task timeoutTask = Task.Delay(200);
            Task completedTask = await Task.WhenAny(connectTask, timeoutTask);
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
