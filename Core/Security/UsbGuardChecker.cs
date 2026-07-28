using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Ermittelt, ob USBGuard zur Kontrolle neu angeschlossener USB-Geräte aktiv ist.
/// </summary>
public sealed class UsbGuardChecker : IOpSecChecker
{
    public string Name => "USBGuard-Hardware-Schutzprüfung";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des USBGuard-Schutzes...");

        try
        {
            bool isUsbGuardRunning = ProcessInspectionService.IsAnyRunning(
                "usbguard",
                "usbguard-daemon");

            if (isUsbGuardRunning)
            {
                Logger.LogInfo("USBGuard Daemon ist im Hintergrund aktiv.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "USBGuard Protection ist aktiv.",
                    Details = "Der 'usbguard-daemon' läuft. Das automatische Einbinden unbekannter oder schädlicher USB-Geräte wird blockiert."
                });
            }

            Logger.LogWarning("USBGuard ist nicht aktiv.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "USBGuard ist inaktiv / nicht installiert.",
                Details = "Es läuft kein 'usbguard-daemon'. Neue USB-Geräte werden vom System ohne Vorabprüfung eingebunden.\n\n" +
                          "Hinweis: Für Schutz vor BadUSB/Rubber Ducky Empfehlung: 'usbguard' installieren und aktivieren."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der USBGuard-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "USBGuard Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
