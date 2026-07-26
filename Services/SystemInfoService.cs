using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace OpSecAuditTool.Services;

/// <summary>
/// Unveränderliche Momentaufnahme der Systemdaten, die Dashboard und Berichte gemeinsam nutzen.
/// </summary>
public sealed record SystemInfoSnapshot(
    string OperatingSystem,
    string KernelVersion,
    string OsArchitecture,
    int LogicalProcessorCount,
    string TotalMemory,
    string Hostname,
    string ApplicationVersion,
    string DotnetRuntime,
    string RuntimeIdentifier,
    string GlibcVersion,
    string DisplayServer,
    string DesktopEnvironment,
    string LocalIpAddress,
    string MacAddress);

/// <summary>
/// Verantwortlich für das Sammeln von System-, Hardware- und Laufzeitinformationen.
/// </summary>
public static class SystemInfoService
{
    /// <summary>
    /// Liest alle statischen Dashboardwerte in einer typisierten Momentaufnahme ein.
    /// </summary>
    public static SystemInfoSnapshot GetSnapshot()
    {
        return new SystemInfoSnapshot(
            OperatingSystem: GetOSDistroName(),
            KernelVersion: RuntimeInformation.OSDescription,
            OsArchitecture: RuntimeInformation.OSArchitecture.ToString(),
            LogicalProcessorCount: Environment.ProcessorCount,
            TotalMemory: GetTotalRamInfo(),
            Hostname: Environment.MachineName,
            ApplicationVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unbekannt",
            DotnetRuntime: Environment.Version.ToString(),
            RuntimeIdentifier: RuntimeInformation.RuntimeIdentifier,
            GlibcVersion: GetGlibcVersion(),
            DisplayServer: OperatingSystem.IsWindows()
                ? "Windows Desktop (DWM)"
                : Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "Unbekannt",
            DesktopEnvironment: OperatingSystem.IsWindows()
                ? "Windows Explorer"
                : Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "Unbekannt",
            LocalIpAddress: GetLocalIpAddress(),
            MacAddress: GetMacAddress());
    }

    /// <summary>
    /// Generiert einen strukturierten Header mit Hardware-, OS- und Laufzeitdaten für Logs und Berichte.
    /// </summary>
    public static string GetSystemReportHeader()
    {
        SystemInfoSnapshot snapshot = GetSnapshot();
        var sb = new StringBuilder();
        sb.AppendLine("==========================================");
        sb.AppendLine(" SYSTEM- & UMGEBUNGS-DIAGNOSE");
        sb.AppendLine("==========================================");

        // Betriebssystem und Distribution
        sb.AppendLine($"Betriebssystem:      {snapshot.OperatingSystem}");
        sb.AppendLine($"Kernel-Version:      {snapshot.KernelVersion}");
        sb.AppendLine($"OS-Architektur:      {snapshot.OsArchitecture}");

        // Hardware-Eigenschaften
        sb.AppendLine($"CPU-Kerne (Logisch): {snapshot.LogicalProcessorCount}");
        sb.AppendLine($"Arbeitsspeicher:     {snapshot.TotalMemory}");
        sb.AppendLine($"Hostname:            {snapshot.Hostname}");

        // Laufzeitumgebung und .NET
        sb.AppendLine("------------------------------------------");
        sb.AppendLine($"Anwendungs-Version:  {snapshot.ApplicationVersion}");
        sb.AppendLine($".NET Runtime:        {snapshot.DotnetRuntime}");
        sb.AppendLine($"Prozess-Architektur: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"Laufzeit-RID:        {snapshot.RuntimeIdentifier}");

        // Systemabhängigkeiten und Desktop-Sitzung
        sb.AppendLine("------------------------------------------");
        sb.AppendLine($"C-Laufzeitbibliothek:{snapshot.GlibcVersion}");
        sb.AppendLine($"Display-Server:      {snapshot.DisplayServer}");
        sb.AppendLine($"Desktop-Umgebung:    {snapshot.DesktopEnvironment}");

        // Netzwerk und Routing
        sb.AppendLine("------------------------------------------");
        sb.AppendLine($"Lokale IP:           {snapshot.LocalIpAddress}");
        sb.AppendLine($"Öffentliche IP:      Wird beim Audit geprüft...");
        sb.AppendLine($"DNS-Server:          Ausstehend...");
        sb.AppendLine($"MAC-Adresse:         {snapshot.MacAddress}");

        // OpSec und Sicherheit
        sb.AppendLine("------------------------------------------");
        sb.AppendLine($"Festplattenverschlüsselung: Unbekannt (Bitte Audit starten)");
        sb.AppendLine($"Swap-Status:         Unbekannt");
        sb.AppendLine($"Firewall:            Unbekannt");
        sb.AppendLine($"Tor-Routing:         Unbekannt");

        sb.AppendLine("==========================================");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Ermittelt die aktive lokale IPv4 Adresse.
    /// </summary>
    private static string GetLocalIpAddress()
    {
        try
        {
            var firstUpInterface = NetworkInterface.GetAllNetworkInterfaces()
                .OrderByDescending(c => c.Speed)
                .FirstOrDefault(c => c.NetworkInterfaceType != NetworkInterfaceType.Loopback && c.OperationalStatus == OperationalStatus.Up);

            if (firstUpInterface != null)
            {
                var props = firstUpInterface.GetIPProperties();
                var ipv4 = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4 != null) return ipv4.Address.ToString();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Konnte lokale IP nicht ermitteln: {ex.Message}");
        }
        return "Nicht verbunden";
    }

    /// <summary>
    /// Ermittelt die MAC-Adresse der aktiven Netzwerkkarte.
    /// </summary>
    private static string GetMacAddress()
    {
        try
        {
            var firstUpInterface = NetworkInterface.GetAllNetworkInterfaces()
                .OrderByDescending(c => c.Speed)
                .FirstOrDefault(c => c.NetworkInterfaceType != NetworkInterfaceType.Loopback && c.OperationalStatus == OperationalStatus.Up);

            if (firstUpInterface != null)
            {
                var macBytes = firstUpInterface.GetPhysicalAddress().GetAddressBytes();
                return string.Join(":", macBytes.Select(b => b.ToString("X2")));
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Konnte MAC-Adresse nicht ermitteln: {ex.Message}");
        }
        return "Unbekannt";
    }

    /// <summary>
    /// Ermittelt den formatierten Namen der Linux-Distribution aus /etc/os-release.
    /// </summary>
    private static string GetOSDistroName()
    {
        try
        {
            if (File.Exists("/etc/os-release"))
            {
                var lines = File.ReadAllLines("/etc/os-release");
                foreach (var line in lines)
                {
                    if (line.StartsWith("PRETTY_NAME="))
                    {
                        return line.Replace("PRETTY_NAME=", "").Trim('"', ' ');
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Konnte OS-Distribution nicht auslesen: {ex.Message}");
        }

        return RuntimeInformation.OSDescription;
    }

    /// <summary>
    /// Liest den Arbeitsspeicher aus /proc/meminfo aus.
    /// </summary>
    private static string GetTotalRamInfo()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsTotalRamInfo();
            }

            if (File.Exists("/proc/meminfo"))
            {
                var lines = File.ReadAllLines("/proc/meminfo");
                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:"))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out long kb))
                        {
                            double gb = Math.Round((double)kb / (1024 * 1024), 2);
                            return $"{gb} GB ({kb} kB)";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Konnte RAM-Informationen nicht auslesen: {ex.Message}");
        }

        return "Unbekannt";
    }

    [SupportedOSPlatform("windows")]
    private static string GetWindowsTotalRamInfo()
    {
        var memoryStatus = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(memoryStatus))
        {
            return "Unbekannt";
        }

        double gibibytes = Math.Round(
            memoryStatus.TotalPhysical / (1024d * 1024d * 1024d),
            2);
        return $"{gibibytes} GB ({memoryStatus.TotalPhysical:N0} Bytes)";
    }

    /// <summary>
    /// Ermittelt die GLIBC-Version unter Linux per P/Invoke.
    /// </summary>
    private static string GetGlibcVersion()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                IntPtr ptr = gnu_get_libc_version();
                if (ptr != IntPtr.Zero)
                {
                    return Marshal.PtrToStringAnsi(ptr) ?? "Unbekannt";
                }
            }
        }
        catch
        {
            // Fallback, falls die libc auf bestimmten Minimal-Systemen nicht direkt erreichbar ist
        }

        return OperatingSystem.IsWindows() ? "Windows Runtime (N/A)" : "N/A";
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(
        [In, Out] MemoryStatusEx buffer);

    [DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr gnu_get_libc_version();
}
