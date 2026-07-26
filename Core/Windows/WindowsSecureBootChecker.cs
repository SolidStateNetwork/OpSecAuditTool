using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Ermittelt den UEFI-Secure-Boot-Zustand über eine lesende PowerShell-Abfrage.
/// </summary>
public sealed class WindowsSecureBootChecker : IOpSecChecker
{
    private const string SecureBootStateKey =
        @"SYSTEM\CurrentControlSet\Control\SecureBoot\State";

    public string Name => "Windows-Secure-Boot-Prüfung";
    public string Category => "Windows / Boot";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe Secure Boot über den lesbaren Systemstatus.");
        int? enabled = WindowsRegistryReader.ReadLocalMachineInt32(
            SecureBootStateKey,
            "UEFISecureBootEnabled");

        return Task.FromResult(enabled switch
        {
            1 => Result(
                CheckStatus.Pass,
                "Secure Boot ist aktiviert.",
                "Der UEFI-Startpfad akzeptiert nur vertrauenswürdige Boot-Komponenten."),
            0 => Result(
                CheckStatus.Warning,
                "Secure Boot ist deaktiviert.",
                "Manipulierte Bootloader oder Bootkits werden nicht durch Secure Boot blockiert."),
            _ => Result(
                CheckStatus.Warning,
                "Secure-Boot-Status ist ohne erhöhte Rechte nicht eindeutig.",
                "Der Statuswert war nicht lesbar oder das Gerät verwendet kein unterstütztes UEFI Secure Boot.")
        });
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
