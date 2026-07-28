using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Ermittelt ALLE lokal und öffentlich lauschenden Netzwerkports (0 - 65535) auf dem System.
/// Nutzt dynamisches Socket-Tabelle-Parsing ('ss -tln' / '/proc/net/tcp') und ordnet
/// bekannte Dienste (Remote, DB, AI, Web, Dev) automatisch zu.
/// </summary>
public sealed class OpenPortsChecker : OpSecCheckerBase
{
    public override string Name => "Offene Netzwerk-Ports & Dienste-Prüfung (Alle Ports 0-65535)";
    public override string Category => "Netzwerk / Anonymität";

    private static readonly Dictionary<int, string> KnownServices = new()
    {
        { 21, "FTP (Unverschlüsselter Dateitransfer)" },
        { 22, "SSH (Remote Shell)" },
        { 23, "Telnet (Unverschlüsselte Remote Shell)" },
        { 25, "SMTP (Mail Transfer)" },
        { 53, "DNS (Domain Name Service)" },
        { 80, "HTTP (Webserver)" },
        { 88, "Kerberos (Authentifizierung)" },
        { 111, "RPCBind / Portmapper" },
        { 389, "LDAP (Verzeichnisdienst)" },
        { 443, "HTTPS (Sicherer Webserver)" },
        { 445, "SMB / NetBIOS (Dateifreigabe)" },
        { 636, "LDAPS (Sicherer Verzeichnisdienst)" },
        { 1080, "SOCKS Proxy" },
        { 2375, "Docker Daemon (Unverschlüsselte API)" },
        { 3000, "Web Dev / Grafana / Node.js" },
        { 3306, "MySQL / MariaDB (Datenbank)" },
        { 3389, "RDP (Remote Desktop)" },
        { 4000, "Web Dev Server / GraphQL" },
        { 5000, "Flask / ASP.NET / UPnP" },
        { 5432, "PostgreSQL (Datenbank)" },
        { 5900, "VNC (Remote Desktop)" },
        { 6379, "Redis (In-Memory Key-Value Store)" },
        { 8000, "HTTP Dev Server / Django / FastAPI" },
        { 8080, "HTTP Alternativ / Proxy / Tomcat" },
        { 8443, "HTTPS Alternativ" },
        { 9000, "PHP-FPM / MinIO / SonarQube" },
        { 9090, "Prometheus / Cockpit Web UI" },
        { 9200, "Elasticsearch (Such- und Log-DB)" },
        { 11434, "Ollama (Lokale LLM / AI API)" },
        { 27017, "MongoDB (NoSQL Datenbank)" }
    };

    private sealed record DiscoveredPort(int Port, bool IsPublicBinding, string ServiceName);

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte verallgemeinerten Scan auf ALLE lauschenden Ports (0-65535)...");

        var discoveredPorts = new Dictionary<int, DiscoveredPort>();

        // 1. Primärer dynamischer Scan via 'ss -tln'
        await DiscoverPortsViaSsAsync(discoveredPorts);

        // 2. Fallback / Ergänzung via /proc/net/tcp & /proc/net/tcp6
        await DiscoverPortsViaProcAsync(discoveredPorts);

        // 3. Falls beide Systemtabellen auf nicht-Linux fehlgeschlagen sind: Fallback auf Active-Probe
        if (discoveredPorts.Count == 0)
        {
            await DiscoverPortsViaTcpConnectAsync(discoveredPorts);
        }

        if (discoveredPorts.Count == 0)
        {
            Logger.LogInfo("Keine offenen TCP-Ports auf dem System erkannt.");
            return Pass(
                "Keine offenen TCP-Ports gefunden.",
                "Auf dem System wurden weder lokal noch öffentlich lauschende Dienste entdeckt.");
        }

        var sortedPorts = discoveredPorts.Values.OrderBy(p => p.Port).ToList();
        var publicPorts = sortedPorts.Where(p => p.IsPublicBinding).ToList();
        var localPorts = sortedPorts.Where(p => !p.IsPublicBinding).ToList();

        var detailsBuilder = new List<string>();

        if (publicPorts.Count > 0)
        {
            detailsBuilder.Add("🌐 ÖFFENTLICH ERREICHBARE PORTS (0.0.0.0 / [::]):");
            foreach (var p in publicPorts)
            {
                detailsBuilder.Add($"  • Port {p.Port}: {p.ServiceName} [EXPONIERT]");
            }
        }

        if (localPorts.Count > 0)
        {
            if (publicPorts.Count > 0) detailsBuilder.Add("");
            detailsBuilder.Add("🔒 NUR LOKAL LAUSCHENDE PORTS (127.0.0.1 / [::1]):");
            foreach (var p in localPorts)
            {
                detailsBuilder.Add($"  • Port {p.Port}: {p.ServiceName} [Lokal]");
            }
        }

        string fullDetails = string.Join("\n", detailsBuilder) +
            "\n\nHinweis: Diese verallgemeinerte Prüfung analysiert die Kernel-Socket-Tabellen auf sämtliche lauschende Ports (0-65535). " +
            "Stelle sicher, dass öffentlich erreichbare Ports durch eine Firewall abgesichert sind und dass lokale Datenbank- oder AI-Dienste nicht unauthentifiziert zugänglich sind.";

        if (publicPorts.Count > 0)
        {
            Logger.LogWarning($"{publicPorts.Count} öffentlich erreichbare Port(s) und {localPorts.Count} lokale(r) Port(s) gefunden!");
            return Warning(
                $"{sortedPorts.Count} offene Port(s) gefunden ({publicPorts.Count} öffentlich erreichbar)!",
                fullDetails);
        }

        Logger.LogInfo($"{localPorts.Count} ausschließlich lokal lauschende Port(s) erkannt.");
        return Warning(
            $"{sortedPorts.Count} offene(r) Port(s) (alle lokal auf 127.0.0.1 / [::1]) gefunden.",
            fullDetails);
    }

    private static async Task DiscoverPortsViaSsAsync(Dictionary<int, DiscoveredPort> target)
    {
        try
        {
            var res = await ShellCommandService.ExecuteAsync("ss", "-tln");
            if (!res.IsSuccess || string.IsNullOrWhiteSpace(res.StandardOutput)) return;

            var lines = res.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("State") || line.Contains("Recv-Q")) continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;

                // Spalte 3 oder 4 ist Local Address:Port
                string localAddrPort = parts.FirstOrDefault(p => p.Contains(':') && !p.StartsWith("LISTEN", StringComparison.OrdinalIgnoreCase)) ?? "";
                if (string.IsNullOrEmpty(localAddrPort))
                {
                    localAddrPort = parts.Length >= 4 ? parts[3] : parts[2];
                }

                ParseAndAddAddressPort(localAddrPort, target);
            }
        }
        catch (Exception ex)
        {
            Logger.LogTrace($"ss -tln Analyse übersprungen: {ex.Message}");
        }
    }

    private static async Task DiscoverPortsViaProcAsync(Dictionary<int, DiscoveredPort> target)
    {
        string[] procFiles = { "/proc/net/tcp", "/proc/net/tcp6" };
        foreach (var file in procFiles)
        {
            if (!File.Exists(file)) continue;
            try
            {
                var lines = await File.ReadAllLinesAsync(file);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 4) continue;

                    string stateHex = parts[3];
                    if (!string.Equals(stateHex, "0A", StringComparison.OrdinalIgnoreCase)) continue; // 0A = TCP_LISTEN

                    string localHex = parts[1];
                    int colonIdx = localHex.IndexOf(':');
                    if (colonIdx <= 0) continue;

                    string ipHex = localHex.Substring(0, colonIdx);
                    string portHex = localHex.Substring(colonIdx + 1);

                    if (int.TryParse(portHex, NumberStyles.HexNumber, null, out int port))
                    {
                        bool isLocalOnly =
                            string.Equals(ipHex, "0100007F", StringComparison.OrdinalIgnoreCase) || // 127.0.0.1
                            ipHex.EndsWith("00000001", StringComparison.OrdinalIgnoreCase);          // ::1

                        AddOrUpdatePort(target, port, !isLocalOnly);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Konnte {file} nicht auslesen: {ex.Message}");
            }
        }
    }

    private static async Task DiscoverPortsViaTcpConnectAsync(Dictionary<int, DiscoveredPort> target)
    {
        foreach (var (port, _) in KnownServices)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync("127.0.0.1", port);
                var completed = await Task.WhenAny(connectTask, Task.Delay(200));
                if (completed == connectTask && client.Connected)
                {
                    AddOrUpdatePort(target, port, isPublicBinding: false);
                }
            }
            catch
            {
                // Port geschlossen
            }
        }
    }

    private static void ParseAndAddAddressPort(string addrPort, Dictionary<int, DiscoveredPort> target)
    {
        int lastColon = addrPort.LastIndexOf(':');
        if (lastColon <= 0) return;

        string portStr = addrPort.Substring(lastColon + 1);
        string ipStr = addrPort.Substring(0, lastColon).Trim();

        if (int.TryParse(portStr, out int port) && port > 0 && port <= 65535)
        {
            bool isLocalOnly =
                ipStr == "127.0.0.1" ||
                ipStr == "::1" ||
                ipStr == "[::1]" ||
                string.Equals(ipStr, "localhost", StringComparison.OrdinalIgnoreCase);

            AddOrUpdatePort(target, port, !isLocalOnly);
        }
    }

    private static void AddOrUpdatePort(Dictionary<int, DiscoveredPort> target, int port, bool isPublicBinding)
    {
        if (!target.TryGetValue(port, out var existing))
        {
            string serviceName = KnownServices.TryGetValue(port, out var name)
                ? name
                : "Benutzerdefinierter / Unbekannter Dienst";

            target[port] = new DiscoveredPort(port, isPublicBinding, serviceName);
        }
        else if (isPublicBinding && !existing.IsPublicBinding)
        {
            target[port] = existing with { IsPublicBinding = true };
        }
    }
}
