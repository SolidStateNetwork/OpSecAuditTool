using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Bewertet den Hostnamen auf eine direkte Übereinstimmung mit dem Benutzernamen
/// und zeigt die lokale Zeitzone zur manuellen Einordnung an.
/// </summary>
public sealed class HostnameTimezoneChecker : IOpSecChecker
{
    public string Name => "Hostname- und Zeitzonen-Hinweise";
    public string Category => "System / Datenschutz";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte lokale Prüfung von Hostname und Zeitzone; konkrete Werte werden nicht protokolliert.");

        try
        {
            string hostname = Environment.MachineName;
            string username = Environment.UserName;
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            bool containsUserName = !string.IsNullOrWhiteSpace(username) &&
                                    username.Length > 2 &&
                                    hostname.Contains(username, StringComparison.OrdinalIgnoreCase);

            if (containsUserName)
            {
                Logger.LogWarning("Der Hostname enthält den lokalen Benutzernamen.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Hostname enthält den lokalen Benutzernamen.",
                    Details = $"Hostname: '{hostname}'\nZeitzone: '{localTimeZone.Id}' ({localTimeZone.DisplayName})\n\n" +
                              "Ein persönlicher Hostname kann im lokalen Netzwerk Identitätshinweise offenlegen. Die Zeitzone wird nur angezeigt und nicht automatisch gegen einen VPN-Standort bewertet."
                });
            }

            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine direkte Benutzername-Übereinstimmung im Hostnamen erkannt.",
                Details = $"Hostname: '{hostname}'\nZeitzone: '{localTimeZone.Id}' ({localTimeZone.DisplayName})\n\n" +
                          "Die Prüfung erkennt nur eine einfache Zeichenübereinstimmung. Andere persönliche Hostnamen oder Standortinformationen können unentdeckt bleiben."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Hostname-/Zeitzonen-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Hostname und Zeitzone konnten nicht geprüft werden.",
                Details = ex.Message
            });
        }
    }
}
