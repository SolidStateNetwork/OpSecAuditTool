using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Ermittelt, ob ein USBGuard-Prozess läuft. Die tatsächlich geladene Richtlinie
/// und deren Wirksamkeit werden ohne zusätzliche Abfragen nicht bestätigt.
/// </summary>
public sealed class UsbGuardChecker : IOpSecChecker
{
    public string Name => "USBGuard-Daemonstatus";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des USBGuard-Daemonstatus...");

        try
        {
            bool isUsbGuardRunning = ProcessInspectionService.IsAnyRunning(
                "usbguard",
                "usbguard-daemon");

            if (isUsbGuardRunning)
            {
                Logger.LogInfo("USBGuard-Prozess ist aktiv.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Ein USBGuard-Prozess läuft; Richtlinie nicht verifiziert.",
                    Details = "Der Prozessstatus zeigt einen laufenden USBGuard-Dienst. Ob eine restriktive und aktuelle Geräte-Richtlinie geladen ist, wurde nicht geprüft."
                });
            }

            Logger.LogWarning("Kein USBGuard-Prozess erkannt.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "USBGuard ist nicht aktiv oder nicht installiert.",
                Details = "Es läuft kein bekannter USBGuard-Prozess. Das bedeutet nicht automatisch, dass neue USB-Geräte ohne Kontrolle zugelassen werden, bestätigt aber keinen USBGuard-basierten Schutz."
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
                Summary = "USBGuard-Daemonstatus konnte nicht geprüft werden.",
                Details = ex.Message
            });
        }
    }
}
