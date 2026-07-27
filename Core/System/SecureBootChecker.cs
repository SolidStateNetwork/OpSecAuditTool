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
    public string Name => "UEFI-Secure-Boot-Status";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Secure-Boot-Status...");

        try
        {
            const string efiPath = "/sys/firmware/efi";

            if (!Directory.Exists(efiPath))
            {
                Logger.LogWarning("System läuft ohne erkennbare UEFI-Schnittstelle.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Kein UEFI-Secure-Boot-Status verfügbar.",
                    Details = "Das System wurde möglicherweise im Legacy-BIOS-/CSM-Modus gestartet. Secure Boot steht in dieser Konfiguration nicht zur Verfügung."
                });
            }

            const string efivarsPath = "/sys/firmware/efi/efivars";
            bool isSecureBootEnabled = false;

            if (Directory.Exists(efivarsPath))
            {
                string[] files = Directory.GetFiles(efivarsPath, "SecureBoot-*");
                if (files.Length > 0)
                {
                    byte[] data = File.ReadAllBytes(files[0]);
                    isSecureBootEnabled = data.Length >= 5 && data[4] == 1;
                }
            }

            if (isSecureBootEnabled)
            {
                Logger.LogInfo("Secure Boot ist im UEFI aktiv.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Secure Boot ist aktiviert.",
                    Details = "Der UEFI-Secure-Boot-Status ist aktiv. Das erschwert das Starten nicht vertrauenswürdiger Boot-Komponenten."
                });
            }

            Logger.LogWarning("Secure Boot ist im UEFI deaktiviert oder nicht lesbar.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Secure Boot ist deaktiviert oder nicht eindeutig lesbar.",
                Details = "Das System läuft im UEFI-Modus, aber die Secure-Boot-Variable bestätigt keinen aktiven Schutz."
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
                Summary = "Secure-Boot-Audit fehlgeschlagen.",
                Details = ex.Message
            });
        }
    }
}
