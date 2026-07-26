using System.Collections.Generic;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Erkennt aktivierte Windows-Funktionen für Remotedesktop und Fernzugriff.
/// </summary>
public sealed class WindowsRemoteAccessChecker : IOpSecChecker
{
    public string Name => "Windows-Fernzugriffs- & Remote-Dienstprüfung";
    public string Category => "Windows / Netzwerk";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe RDP, WinRM und Remote Registry.");
        int? denyRdp = WindowsRegistryReader.ReadLocalMachineInt32(
            @"SYSTEM\CurrentControlSet\Control\Terminal Server",
            "fDenyTSConnections");
        int? winRmStart = WindowsRegistryReader.ReadLocalMachineInt32(
            @"SYSTEM\CurrentControlSet\Services\WinRM",
            "Start");
        int? remoteRegistryStart = WindowsRegistryReader.ReadLocalMachineInt32(
            @"SYSTEM\CurrentControlSet\Services\RemoteRegistry",
            "Start");

        var activeSurfaces = new List<string>();
        if (denyRdp == 0) activeSurfaces.Add("Remote Desktop (RDP) ist erlaubt.");
        if (winRmStart is 2 or 3) activeSurfaces.Add("Windows Remote Management ist startfähig.");
        if (remoteRegistryStart is 2 or 3) activeSurfaces.Add("Remote Registry ist startfähig.");

        if (activeSurfaces.Count == 0)
        {
            return Task.FromResult(Result(
                CheckStatus.Pass,
                "Keine aktivierte Standard-Fernverwaltung erkannt.",
                "RDP ist deaktiviert; WinRM und Remote Registry sind deaktiviert oder nicht automatisch aktiv."));
        }

        return Task.FromResult(Result(
            CheckStatus.Warning,
            $"{activeSurfaces.Count} Fernzugriffsfläche(n) sind aktiviert.",
            string.Join("\n", activeSurfaces) +
            "\n\nDeaktiviere nicht benötigte Fernverwaltung und begrenze benötigte Zugänge per Firewall."));
    }

    private CheckResult Result(CheckStatus status, string summary, string details) => new()
    {
        Name = Name,
        Category = Category,
        Status = status,
        Summary = summary,
        Details = details
    };
}
