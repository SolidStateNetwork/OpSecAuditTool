using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Erkennt laufende Tor-Prozesse und typische lokale SOCKS-Ports.
/// </summary>
public sealed class TorStatusChecker : IOpSecChecker
{
    public string Name => "Tor-Netzwerkverbindungs-Status";
    public string Category => "Netzwerk / Anonymität";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Tor-Netzwerk-Status...");

        try
        {
            bool isTorProcessRunning = ProcessInspectionService.IsAnyRunning("tor");

            bool isPort9050Open = await IsPortListeningAsync("127.0.0.1", 9050);
            bool isPort9150Open = await IsPortListeningAsync("127.0.0.1", 9150);

            if (isTorProcessRunning || isPort9050Open || isPort9150Open)
            {
                string details = "";
                if (isPort9050Open) details += "• Tor SOCKS5 Proxy lauscht auf Port 9050 (Standard System-Daemon)\n";
                if (isPort9150Open) details += "• Tor Browser SOCKS5 Proxy lauscht auf Port 9150\n";
                if (isTorProcessRunning) details += "• Tor-Prozess ist im System aktiv\n";

                Logger.LogInfo("Tor-Dienst / Proxy lokal aktiv.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Tor-Netzwerk / SOCKS5-Proxy ist lokal verfügbar.",
                    Details = $"Gefundene Tor-Komponenten:\n{details}\nHinweis: Anwendungen können für maximale Anonymität über SOCKS5 (127.0.0.1:9050) geroutet werden."
                };
            }

            Logger.LogInfo("Kein Tor-Dienst / Proxy auf dem System aktiv.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Kein aktiver Tor-Dienst oder SOCKS5-Proxy gefunden.",
                Details = "Auf Ports 9050 / 9150 lauscht derzeit kein Tor-Proxy und der 'tor'-Prozess läuft nicht.\n\n" +
                          (OperatingSystem.IsWindows()
                              ? "Hinweis: Starte bei Bedarf den Tor Browser oder einen lokal installierten Tor-Dienst."
                              : "Hinweis: Starte bei Bedarf den Tor-Dienst oder benutze den Tor Browser.")
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Tor-Status-Prüfung", ex);
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Tor-Analyse fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            };
        }
    }

    private static async Task<bool> IsPortListeningAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(200);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
