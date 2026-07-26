using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Ermittelt lokal lauschende Netzwerkports und bewertet deren Angriffsfläche.
/// </summary>
public sealed class OpenPortsChecker : IOpSecChecker
{
    public string Name => "Offene Netzwerk-Ports & Dienste-Prüfung";
    public string Category => "Netzwerk / Anonymität";

    private readonly Dictionary<int, string> _criticalPorts = new()
    {
        { 21, "FTP (Unverschlüsselter Dateitransfer)" },
        { 22, "SSH (Remote Shell)" },
        { 23, "Telnet (Unverschlüsselte Remote Shell)" },
        { 80, "HTTP Webserver" },
        { 445, "SMB / NetBIOS (Dateifreigabe)" },
        { 3389, "RDP (Remote Desktop)" },
        { 5900, "VNC (Remote Desktop)" },
        { 8080, "HTTP Alternativ / Proxy" }
    };

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Scan auf lokale offene Ports...");

        var openPorts = new List<string>();

        foreach (var (port, description) in _criticalPorts)
        {
            bool isOpen = await IsPortOpenAsync("127.0.0.1", port);
            if (isOpen)
            {
                Logger.LogWarning($"Offener Port entdeckt: {port} ({description})");
                openPorts.Add($"Port {port}: {description}");
            }
        }

        if (openPorts.Count == 0)
        {
            Logger.LogInfo("Keine kritischen Standard-Ports lokal erreichbar.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine kritischen offenen Ports gefunden.",
                Details = "Getestete Standard-Ports (SSH, FTP, RDP, SMB, HTTP etc.) lauschen nicht oder sind lokal blockiert."
            };
        }

        string foundPortsList = string.Join("\n• ", openPorts);
        return new CheckResult
        {
            Name = Name,
            Category = Category,
            Status = CheckStatus.Warning,
            Summary = $"{openPorts.Count} offene(r) Dienst(e) auf dem System gefunden!",
            Details = $"Folgende Ports lauschen lokal:\n• {foundPortsList}\n\nHinweis: Prüfe, ob diese Dienste nach außen exponiert sind oder ob sie durch deine Firewall geblockt werden."
        };
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(300);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            if (completedTask == connectTask && client.Connected)
            {
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
