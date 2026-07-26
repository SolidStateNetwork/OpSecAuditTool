using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Prüft, ob die Windows-Firewall in allen Netzwerkprofilen aktiv ist.
/// </summary>
public sealed class WindowsFirewallChecker : IOpSecChecker
{
    public string Name => "Windows-Firewall-Profilprüfung";
    public string Category => "Windows / Netzwerk";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe Firewall-Profile ohne Administratorrechte.");
        PowerShellResult result = await PowerShellService.ExecuteReadOnlyAsync(
            "Get-NetFirewallProfile -PolicyStore ActiveStore | " +
            "Select-Object Name,Enabled,DefaultInboundAction | ConvertTo-Json -Compress");

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return Unknown(
                "Firewall-Status konnte ohne erhöhte Rechte nicht vollständig gelesen werden.",
                result);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
            JsonElement[] profiles = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray().ToArray()
                : [document.RootElement];
            string[] disabledProfiles = profiles
                .Where(profile =>
                    profile.TryGetProperty("Enabled", out JsonElement enabled) &&
                    enabled.ValueKind == JsonValueKind.False)
                .Select(profile =>
                    profile.TryGetProperty("Name", out JsonElement name)
                        ? name.ToString()
                        : "Unbekannt")
                .ToArray();

            if (disabledProfiles.Length == 0 && profiles.Length > 0)
            {
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Alle Windows-Firewall-Profile sind aktiviert.",
                    Details = $"Aktive Profile: {string.Join(", ", profiles.Select(GetName))}."
                };
            }

            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = disabledProfiles.Length == profiles.Length
                    ? CheckStatus.Fail
                    : CheckStatus.Warning,
                Summary = $"{disabledProfiles.Length} Firewall-Profil(e) sind deaktiviert.",
                Details = $"Deaktiviert: {string.Join(", ", disabledProfiles)}.\n\n" +
                          "Aktiviere mindestens das öffentliche Profil, bevor du fremde Netzwerke verwendest."
            };
        }
        catch (Exception ex)
        {
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Firewall-Antwort konnte nicht ausgewertet werden.",
                Details = ex.Message
            };
        }
    }

    private string GetName(JsonElement profile) =>
        profile.TryGetProperty("Name", out JsonElement name)
            ? name.ToString()
            : "Unbekannt";

    private CheckResult Unknown(string summary, PowerShellResult result) => new()
    {
        Name = Name,
        Category = Category,
        Status = CheckStatus.Warning,
        Summary = summary,
        Details = result.TimedOut
            ? "Die lesende PowerShell-Abfrage hat das Zeitlimit überschritten."
            : result.StandardError.Trim()
    };
}
