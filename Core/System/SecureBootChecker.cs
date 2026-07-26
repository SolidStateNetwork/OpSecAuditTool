using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Ermittelt den UEFI-Secure-Boot-Zustand unter Linux.
/// </summary>
public sealed class SecureBootChecker : IOpSecChecker
{
    public string Name => "UEFI Secure Boot Statusprüfung";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Secure-Boot-Status...");

        try
        {
            string efiPath = "/sys/firmware/efi";

            if (!Directory.Exists(efiPath))
            {
                Logger.LogWarning("System läuft im Legacy/CSM-Modus (kein UEFI).");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Legacy BIOS Modus aktiv (Kein UEFI).",
                    Details = "Das System wurde im Legacy-BIOS/CSM-Modus gebootet. Secure Boot steht in dieser Konfiguration nicht zur Verfügung."
                });
            }

            string efivarsPath = "/sys/firmware/efi/efivars";
            bool isSecureBootEnabled = false;

            if (Directory.Exists(efivarsPath))
            {
                var files = Directory.GetFiles(efivarsPath, "SecureBoot-*");
                if (files.Length > 0)
                {
                    byte[] data = File.ReadAllBytes(files[0]);
                    if (data.Length >= 5 && data[4] == 1)
                    {
                        isSecureBootEnabled = true;
                    }
                }
            }

            if (isSecureBootEnabled)
            {
                Logger.LogInfo("Secure Boot ist im UEFI aktiv und schützt den Boot-Prozess.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Secure Boot ist aktiv.",
                    Details = "Der UEFI-Bootloader und der Kernel sind vor Manipulationen (Evil Maid / Bootkit-Angriffe) geschützt."
                });
            }

            Logger.LogWarning("Secure Boot ist im UEFI deaktiviert.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Secure Boot is deaktiviert.",
                Details = "Das System läuft im UEFI-Modus, jedoch ist Secure Boot nicht aktiviert.\n\n" +
                          "Hinweis: Aktiviere Secure Boot im Mainboard-UEFI, um die Bootloader-Integrität zu sichern."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Secure-Boot-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Secure Boot Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
