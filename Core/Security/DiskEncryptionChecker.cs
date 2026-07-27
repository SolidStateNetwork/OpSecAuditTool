using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft, ob das Linux-Root-Dateisystem und eine gegebenenfalls separat
/// eingehängte Home-Partition tatsächlich über einen dm-crypt-/LUKS-Mapper laufen.
/// Ein beliebiger /dev/mapper-Pfad wird nicht mehr pauschal als Verschlüsselung
/// gewertet, da dort auch unverschlüsseltes LVM liegen kann.
/// </summary>
public sealed class DiskEncryptionChecker : IOpSecChecker
{
    public string Name => "Linux-Datenträgerverschlüsselung (dm-crypt/LUKS)";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Datenträgerverschlüsselung...");

        try
        {
            if (!File.Exists("/proc/mounts"))
            {
                return Task.FromResult(Result(
                    CheckStatus.Warning,
                    "Mount-Informationen sind nicht verfügbar.",
                    "Das System stellt `/proc/mounts` nicht bereit. Eine Datenträgerverschlüsselung kann deshalb nicht bestätigt werden."));
            }

            string[] mounts = File.ReadAllLines("/proc/mounts");
            string? rootSource = FindMountSource(mounts, "/");
            string? homeSource = FindMountSource(mounts, "/home");

            if (string.IsNullOrWhiteSpace(rootSource))
            {
                return Task.FromResult(Result(
                    CheckStatus.Warning,
                    "Quelle des Root-Dateisystems konnte nicht ermittelt werden.",
                    "Der Eintrag für `/` fehlt in den gelesenen Mount-Informationen."));
            }

            bool rootEncrypted = IsDmCryptMapper(rootSource);
            bool separateHome = !string.IsNullOrWhiteSpace(homeSource);
            bool homeEncrypted = !separateHome || IsDmCryptMapper(homeSource!);

            if (rootEncrypted && homeEncrypted)
            {
                string scope = separateHome
                    ? "Root- und separate Home-Partition verwenden dm-crypt/LUKS."
                    : "Das Root-Dateisystem verwendet dm-crypt/LUKS; `/home` liegt darin.";
                return Task.FromResult(Result(
                    CheckStatus.Pass,
                    "Systemdaten liegen auf einem bestätigten dm-crypt-/LUKS-Mapper.",
                    scope));
            }

            if (rootEncrypted || (separateHome && homeEncrypted))
            {
                return Task.FromResult(Result(
                    CheckStatus.Warning,
                    "Datenträgerverschlüsselung ist nur teilweise bestätigt.",
                    $"Root-Quelle: {rootSource} ({EncryptedLabel(rootEncrypted)})\n" +
                    $"Home-Quelle: {homeSource ?? "Teil des Root-Dateisystems"} ({EncryptedLabel(homeEncrypted)})\n\n" +
                    "Für vollständigen Schutz sollten Root-Dateisystem und jede separat eingehängte Home-Partition verschlüsselt sein."));
            }

            bool usesMapper = IsMapperPath(rootSource) ||
                              (separateHome && IsMapperPath(homeSource!));
            return Task.FromResult(Result(
                CheckStatus.Warning,
                usesMapper
                    ? "Device Mapper erkannt, aber dm-crypt/LUKS nicht bestätigt."
                    : "Keine dm-crypt-/LUKS-Verschlüsselung bestätigt.",
                $"Root-Quelle: {rootSource}\nHome-Quelle: {homeSource ?? "Teil des Root-Dateisystems"}\n\n" +
                (usesMapper
                    ? "Ein `/dev/mapper`-Pfad kann auch unverschlüsseltes LVM sein. Die Prüfung wertet ihn nur dann als verschlüsselt, wenn die zugehörige Device-Mapper-UUID auf dm-crypt/LUKS hinweist."
                    : "Bei physischem Zugriff können unverschlüsselte Systemdaten ausgelesen werden.")));
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Verschlüsselungsprüfung", ex);
            return Task.FromResult(Result(
                CheckStatus.Warning,
                "Datenträgerverschlüsselung konnte nicht vollständig geprüft werden.",
                ex.Message));
        }
    }

    private static string? FindMountSource(string[] mounts, string mountPoint) => mounts
        .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Where(parts => parts.Length >= 2 && parts[1] == mountPoint)
        .Select(parts => parts[0])
        .FirstOrDefault();

    private static bool IsDmCryptMapper(string source)
    {
        string? blockName = ResolveMapperBlockName(source);
        if (string.IsNullOrWhiteSpace(blockName))
        {
            return false;
        }

        string uuidPath = Path.Combine("/sys/class/block", blockName, "dm", "uuid");
        if (!File.Exists(uuidPath))
        {
            return false;
        }

        string uuid = File.ReadAllText(uuidPath).Trim();
        return uuid.StartsWith("CRYPT-LUKS", StringComparison.OrdinalIgnoreCase) ||
               uuid.StartsWith("CRYPT-PLAIN", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveMapperBlockName(string source)
    {
        if (source.StartsWith("/dev/dm-", StringComparison.Ordinal))
        {
            return Path.GetFileName(source);
        }

        if (!source.StartsWith("/dev/mapper/", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            FileSystemInfo? target = new FileInfo(source).ResolveLinkTarget(returnFinalTarget: true);
            if (target != null && Path.GetFileName(target.FullName).StartsWith("dm-", StringComparison.Ordinal))
            {
                return Path.GetFileName(target.FullName);
            }
        }
        catch
        {
            // Fallback über die in sysfs veröffentlichten Mapper-Namen.
        }

        string mapperName = Path.GetFileName(source);
        const string sysBlock = "/sys/class/block";
        if (!Directory.Exists(sysBlock))
        {
            return null;
        }

        foreach (string directory in Directory.GetDirectories(sysBlock, "dm-*"))
        {
            string namePath = Path.Combine(directory, "dm", "name");
            if (File.Exists(namePath) &&
                File.ReadAllText(namePath).Trim().Equals(mapperName, StringComparison.Ordinal))
            {
                return Path.GetFileName(directory);
            }
        }

        return null;
    }

    private static bool IsMapperPath(string source) =>
        source.StartsWith("/dev/mapper/", StringComparison.Ordinal) ||
        source.StartsWith("/dev/dm-", StringComparison.Ordinal);

    private static string EncryptedLabel(bool encrypted) =>
        encrypted ? "dm-crypt/LUKS bestätigt" : "nicht bestätigt";

    private CheckResult Result(CheckStatus status, string summary, string details) => new()
    {
        Name = Name,
        Category = Category,
        Status = status,
        Summary = summary,
        Details = details
    };
}
