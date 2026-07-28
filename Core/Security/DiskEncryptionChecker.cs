using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Erkennt Linux-Datenträger, die über LUKS, dm-crypt, systemd-cryptsetup oder eCryptfs eingebunden sind.
/// </summary>
public sealed class DiskEncryptionChecker : OpSecCheckerBase
{
    public override string Name => "Festplattenverschlüsselungs-Prüfung (LUKS)";
    public override string Category => "System / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der Festplattenverschlüsselung (LUKS/dm-crypt/ecryptfs)...");

        if (!File.Exists("/proc/mounts"))
        {
            Logger.LogWarning("/proc/mounts nicht gefunden.");
            return Warning(
                "Mount-Informationen nicht verfügbar.",
                "Das System stellt '/proc/mounts' nicht bereit.");
        }

        var mounts = await File.ReadAllLinesAsync("/proc/mounts");

        bool isRootEncrypted = false;
        bool isHomeEncrypted = false;

        foreach (var line in mounts)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;

            string device = parts[0];
            string mountPoint = parts[1];
            string fsType = parts[2];
            string options = parts[3];

            if (mountPoint == "/")
            {
                if (IsEncryptedMount(device, fsType, options)) isRootEncrypted = true;
            }
            else if (mountPoint == "/home")
            {
                if (IsEncryptedMount(device, fsType, options)) isHomeEncrypted = true;
            }
        }

        if (isRootEncrypted || isHomeEncrypted)
        {
            string encryptedScope = (isRootEncrypted && isHomeEncrypted)
                ? "Root- und Home-Partition"
                : isRootEncrypted ? "Root-Partition (/)" : "Home-Partition (/home)";

            Logger.LogInfo($"Festplattenverschlüsselung aktiv für: {encryptedScope}");
            return Pass(
                "Voll- oder Home-Verschlüsselung ist aktiv.",
                $"Folgende System-Bereiche laufen über eine verschlüsselte Partition (LUKS/dm-crypt/ecryptfs): {encryptedScope}.\n\nDaten sind bei physikalischem Verlust oder im ausgeschalteten Zustand vor unbefugtem Zugriff geschützt.");
        }

        Logger.LogWarning("Keine Verschlüsselung für / oder /home erkannt!");
        return Warning(
            "KRITISCH: Datenträger ist unverschlüsselt!",
            "Weder Root (/) noch /home liegen auf einem verschlüsselten Device (LUKS, dm-crypt oder eCryptfs).\n\n" +
            "Hinweis: Bei Diebstahl oder physikalischem Zugriff können alle Dateien unverschlüsselt von externen Boot-Medien ausgelesen werden.");
    }

    private static bool IsEncryptedMount(string device, string fsType, string options)
    {
        if (device.StartsWith("/dev/mapper/", StringComparison.Ordinal) ||
            device.StartsWith("/dev/dm-", StringComparison.Ordinal) ||
            device.StartsWith("/dev/disk/by-id/dm-", StringComparison.Ordinal) ||
            device.Contains("luks", StringComparison.OrdinalIgnoreCase) ||
            device.Contains("crypt", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(fsType, "ecryptfs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(fsType, "zfs", StringComparison.OrdinalIgnoreCase) &&
            options.Contains("encryption=on", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
