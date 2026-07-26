using System;
using System.Text.Json;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Liest Echtzeit-, Signatur- und Manipulationsschutz von Microsoft Defender aus.
/// </summary>
public sealed class WindowsDefenderChecker : IOpSecChecker
{
    public string Name => "Microsoft-Defender-Echtzeitschutz";
    public string Category => "Windows / Schutz";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe Microsoft Defender.");
        PowerShellResult query = await PowerShellService.ExecuteReadOnlyAsync(
            "Get-MpComputerStatus | Select-Object AntivirusEnabled," +
            "RealTimeProtectionEnabled,BehaviorMonitorEnabled,IoavProtectionEnabled," +
            "AntivirusSignatureLastUpdated | ConvertTo-Json -Compress");

        if (!query.IsSuccess || string.IsNullOrWhiteSpace(query.StandardOutput))
        {
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Defender-Status ist nicht verfügbar.",
                Details = "Microsoft Defender kann deaktiviert oder durch eine andere Sicherheitslösung ersetzt sein. " +
                          "Die Abfrage fordert bewusst keine Administratorrechte an.\n\n" +
                          query.StandardError.Trim()
            };
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(query.StandardOutput);
            JsonElement root = document.RootElement;
            bool antivirus = GetBoolean(root, "AntivirusEnabled");
            bool realtime = GetBoolean(root, "RealTimeProtectionEnabled");
            bool behavior = GetBoolean(root, "BehaviorMonitorEnabled");
            bool downloads = GetBoolean(root, "IoavProtectionEnabled");

            CheckStatus status = antivirus && realtime
                ? CheckStatus.Pass
                : antivirus || realtime
                    ? CheckStatus.Warning
                    : CheckStatus.Fail;
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = status,
                Summary = status == CheckStatus.Pass
                    ? "Defender und Echtzeitschutz sind aktiv."
                    : "Microsoft Defender ist nicht vollständig aktiv.",
                Details =
                    $"Antivirus: {OnOff(antivirus)}\n" +
                    $"Echtzeitschutz: {OnOff(realtime)}\n" +
                    $"Verhaltensüberwachung: {OnOff(behavior)}\n" +
                    $"Downloadprüfung: {OnOff(downloads)}"
            };
        }
        catch (Exception ex)
        {
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Defender-Antwort konnte nicht ausgewertet werden.",
                Details = ex.Message
            };
        }
    }

    private static bool GetBoolean(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out JsonElement property) &&
        property.ValueKind == JsonValueKind.True;

    private static string OnOff(bool enabled) => enabled ? "Aktiv" : "Inaktiv";
}
