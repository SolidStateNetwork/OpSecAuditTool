using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Erkennt Linux-Datenträger, die über dm-crypt beziehungsweise LUKS eingebunden sind.
/// </summary>
public sealed class DiskEncryptionChecker : IOpSecChecker
{
    public string Name => "Festplattenverschlüsselungs-Prüfung (LUKS)";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Festplattenverschlüsselung (LUKS/dm-crypt)...");

        try
        {
            if (!File.Exists("/proc/mounts"))
            {
                Logger.LogInfo("/proc/mounts nicht gefunden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Mount-Informationen nicht verfügbar.",
                    Details = "Das System stellt '/proc/mounts' nicht bereit."
                });
            }

            var mounts = File.ReadAllLines("/proc/mounts");

            bool isRootEncrypted = mounts.Any(line =>
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2 && parts[1] == "/" && parts[0].StartsWith("/dev/mapper/");
            });

            bool isHomeEncrypted = mounts.Any(line =>
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2 && parts[1] == "/home" && parts[0].StartsWith("/dev/mapper/");
            });

            if (isRootEncrypted || isHomeEncrypted)
            {
                string encryptedScope = (isRootEncrypted && isHomeEncrypted)
                    ? "Root- und Home-Partition"
                    : isRootEncrypted ? "Root-Partition (/)" : "Home-Partition (/home)";

                Logger.LogInfo($"Festplattenverschlüsselung (LUKS) aktiv für: {encryptedScope}");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Vollverschlüsselung (LUKS) ist aktiv.",
                    Details = $"Folgende System-Bereiche laufen über ein verschlüsseltes Device-Mapper Device (dm-crypt): {encryptedScope}.\n\nDaten sind bei physikalischem Verlust oder im ausgeschalteten Zustand vor unbefugtem Zugriff geschützt."
                });
            }

            Logger.LogWarning("Keine LUKS-Verschlüsselung für / oder /home erkannt!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "KRITISCH: Datenträger ist unverschlüsselt!",
                Details = "Weder Root (/) noch /home liegen auf einem verschlüsselten `mapper`-Device (LUKS/dm-crypt).\n\n" +
                          "Hinweis: Bei Diebstahl oder physikalischem Zugriff können alle Dateien unverschlüsselt von externen Boot-Medien ausgelesen werden."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Verschlüsselungs-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Encryption Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
