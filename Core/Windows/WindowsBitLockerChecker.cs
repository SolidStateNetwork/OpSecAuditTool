using System;
using System.Text.Json;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Ermittelt den BitLocker-Schutzstatus lokaler Windows-Laufwerke.
/// </summary>
public sealed class WindowsBitLockerChecker : IOpSecChecker
{
    public string Name => "BitLocker-Systemlaufwerkverschlüsselung";
    public string Category => "Windows / Datenträger";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe BitLocker für das Systemlaufwerk.");
        PowerShellResult query = await PowerShellService.ExecuteReadOnlyAsync(
            "$v=Get-BitLockerVolume -MountPoint $env:SystemDrive;" +
            "[pscustomobject]@{" +
            "MountPoint=$v.MountPoint;" +
            "ProtectionOn=($v.ProtectionStatus -eq 'On');" +
            "FullyEncrypted=($v.VolumeStatus -eq 'FullyEncrypted');" +
            "EncryptionPercentage=$v.EncryptionPercentage;" +
            "EncryptionMethod=$v.EncryptionMethod.ToString()" +
            "}|ConvertTo-Json -Compress");

        if (!query.IsSuccess || string.IsNullOrWhiteSpace(query.StandardOutput))
        {
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "BitLocker konnte nicht vollständig geprüft werden.",
                Details = "Die Abfrage bleibt absichtlich ohne UAC-Erhöhung. Auf manchen Windows-Editionen " +
                          "oder Richtlinien ist der BitLocker-Status für Standardbenutzer nicht lesbar.\n\n" +
                          query.StandardError.Trim()
            };
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(query.StandardOutput);
            JsonElement root = document.RootElement;
            bool protectionOn = root.GetProperty("ProtectionOn").GetBoolean();
            bool fullyEncrypted = root.GetProperty("FullyEncrypted").GetBoolean();
            int percentage = root.TryGetProperty(
                "EncryptionPercentage",
                out JsonElement percentageElement)
                ? percentageElement.GetInt32()
                : 0;
            string method = root.TryGetProperty("EncryptionMethod", out JsonElement methodElement)
                ? methodElement.ToString()
                : "Unbekannt";

            CheckStatus status = protectionOn && fullyEncrypted
                ? CheckStatus.Pass
                : percentage == 0
                    ? CheckStatus.Fail
                    : CheckStatus.Warning;
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = status,
                Summary = status == CheckStatus.Pass
                    ? "Das Windows-Systemlaufwerk ist vollständig geschützt."
                    : "Das Windows-Systemlaufwerk ist nicht vollständig geschützt.",
                Details =
                    $"BitLocker-Schutz: {(protectionOn ? "Aktiv" : "Inaktiv")}\n" +
                    $"Verschlüsselungsgrad: {percentage}%\n" +
                    $"Methode: {method}"
            };
        }
        catch (Exception ex)
        {
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "BitLocker-Antwort konnte nicht ausgewertet werden.",
                Details = ex.Message
            };
        }
    }
}
