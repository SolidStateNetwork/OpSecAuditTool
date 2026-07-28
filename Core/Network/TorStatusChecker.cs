using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Erkennt laufende Tor-Prozesse, Standard-Tor-Ports (9050/9150/9051/9151) sowie
/// BENUTZERDEFINIERTE Ports, die in lokalen torrc-Konfigurationsdateien geändert wurden.
/// </summary>
public sealed class TorStatusChecker : OpSecCheckerBase
{
    public override string Name => "Tor-Netzwerkverbindungs- & Proxy-Status (Verallgemeinert)";
    public override string Category => "Netzwerk / Anonymität";

    private static readonly Dictionary<int, string> DefaultTorPorts = new()
    {
        { 9050, "Tor SOCKS5 Proxy (Standard System-Daemon)" },
        { 9150, "Tor Browser SOCKS5 Proxy" },
        { 9051, "Tor Control Port (Standard System-Daemon)" },
        { 9151, "Tor Browser Control Port" },
        { 9040, "Tor Transparent Proxy (TransPort)" },
        { 5353, "Tor DNS Port (DNSPort)" },
        { 8118, "Privoxy / Tor HTTP Tunnel" }
    };

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte verallgemeinerte Prüfung des Tor-Netzwerks & dynamischer Ports...");

        bool isTorProcessRunning = ProcessInspectionService.IsAnyRunning("tor");

        // 1. Suche und analysiere alle Tor-Konfigurationsdateien (torrc) nach benutzerdefinierten Ports
        var customPorts = await DiscoverCustomTorPortsFromConfigAsync();

        // 2. Kombiniere Standard-Ports mit allen entkoppelten/angepassten Ports aus torrc
        var allPortsToCheck = new Dictionary<int, string>(DefaultTorPorts);
        foreach (var (port, desc) in customPorts)
        {
            allPortsToCheck[port] = desc;
        }

        var activePorts = new List<string>();

        foreach (var (port, description) in allPortsToCheck)
        {
            if (await IsPortListeningAsync("127.0.0.1", port))
            {
                activePorts.Add($"Port {port}: {description}");
            }
        }

        if (isTorProcessRunning || activePorts.Count > 0)
        {
            var detailsBuilder = new List<string>();

            if (activePorts.Count > 0)
            {
                detailsBuilder.Add("🟢 AKTIVE TOR- / PROXY-PORTS ERKANNT:");
                foreach (var p in activePorts)
                {
                    detailsBuilder.Add($"  • {p}");
                }
            }

            if (isTorProcessRunning)
            {
                if (detailsBuilder.Count > 0) detailsBuilder.Add("");
                detailsBuilder.Add("⚙️ PROZESS-STATUS:\n  • Der 'tor'-Daemon ist im System aktiv.");
            }

            if (customPorts.Count > 0)
            {
                detailsBuilder.Add("");
                detailsBuilder.Add($"📄 KONFIGURATION (torrc):\n  • {customPorts.Count} benutzerdefinierte(r) Port(s) aus torrc-Dateien eingelesen.");
            }

            string fullDetails = string.Join("\n", detailsBuilder) +
                "\n\nHinweis: Diese verallgemeinerte Prüfung erkennt Standard-Tor-Ports (9050, 9150, 9051) und liest zudem benutzerdefinierte SOCKS/Control/HTTP-Ports aus lokalen torrc-Dateien aus.";

            Logger.LogInfo("Tor-Dienst / Proxy ist auf dem System aktiv.");
            return Pass(
                "Tor-Netzwerk / SOCKS5-Proxy ist lokal verfügbar.",
                fullDetails);
        }

        Logger.LogInfo("Kein Tor-Dienst / Proxy aktiv.");
        return Warning(
            "Kein aktiver Tor-Dienst oder SOCKS5-Proxy gefunden.",
            "Weder auf den Standard-Ports (9050, 9150, 9051, 9151) noch auf benutzerdefinierten Ports aus torrc lauscht derzeit ein Tor-Proxy und der 'tor'-Prozess läuft nicht.\n\n" +
            "Hinweis: Starte bei Bedarf den Tor-Dienst oder den Tor Browser, um anonymen SOCKS5-Verkehr zu ermöglichen.");
    }

    private static async Task<Dictionary<int, string>> DiscoverCustomTorPortsFromConfigAsync()
    {
        var customPorts = new Dictionary<int, string>();
        var configFiles = new List<string>();

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string[] candidateFiles =
        {
            "/etc/tor/torrc",
            "/usr/local/etc/tor/torrc",
            Path.Combine(homeDir, ".tor", "torrc"),
            Path.Combine(homeDir, ".config", "tor", "torrc"),
            Path.Combine(homeDir, ".var", "app", "com.github.micahflee.torbrowser-launcher", "config", "tor", "torrc")
        };

        foreach (var f in candidateFiles)
        {
            if (File.Exists(f)) configFiles.Add(f);
        }

        if (Directory.Exists("/etc/tor/torrc.d"))
        {
            try
            {
                configFiles.AddRange(Directory.GetFiles("/etc/tor/torrc.d", "*.conf"));
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"/etc/tor/torrc.d nicht lesbar: {ex.Message}");
            }
        }

        foreach (var file in configFiles)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(file);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    string directive = parts[0];
                    string portValueStr = parts[1];

                    // Falls Angabe im Format IP:Port wie '127.0.0.1:9052'
                    int colonIdx = portValueStr.LastIndexOf(':');
                    if (colonIdx >= 0)
                    {
                        portValueStr = portValueStr.Substring(colonIdx + 1);
                    }

                    if (int.TryParse(portValueStr, out int port) && port > 0 && port <= 65535)
                    {
                        if (string.Equals(directive, "SocksPort", StringComparison.OrdinalIgnoreCase))
                        {
                            customPorts[port] = $"Tor SOCKS5 Proxy (Benutzerdefiniert in {Path.GetFileName(file)})";
                        }
                        else if (string.Equals(directive, "ControlPort", StringComparison.OrdinalIgnoreCase))
                        {
                            customPorts[port] = $"Tor Control Port (Benutzerdefiniert in {Path.GetFileName(file)})";
                        }
                        else if (string.Equals(directive, "HTTPTunnelPort", StringComparison.OrdinalIgnoreCase))
                        {
                            customPorts[port] = $"Tor HTTP Tunnel (Benutzerdefiniert in {Path.GetFileName(file)})";
                        }
                        else if (string.Equals(directive, "TransPort", StringComparison.OrdinalIgnoreCase))
                        {
                            customPorts[port] = $"Tor TransPort (Benutzerdefiniert in {Path.GetFileName(file)})";
                        }
                        else if (string.Equals(directive, "DNSPort", StringComparison.OrdinalIgnoreCase))
                        {
                            customPorts[port] = $"Tor DNSPort (Benutzerdefiniert in {Path.GetFileName(file)})";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Konfigurationsdatei {file} ignoriert: {ex.Message}");
            }
        }

        return customPorts;
    }

    private static async Task<bool> IsPortListeningAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(200));
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
