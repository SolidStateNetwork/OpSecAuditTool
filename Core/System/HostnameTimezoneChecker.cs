using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Bewertet Hostname und Zeitzone auf vermeidbare Standort- oder Identitätshinweise.
/// </summary>
public sealed class HostnameTimezoneChecker : IOpSecChecker
{
    public string Name => "Hostname- & Zeitzonen-Anonymitätsprüfung";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung von Hostname und Zeitzone...");

        try
        {
            string hostname = Environment.MachineName;
            string username = Environment.UserName;
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

            Logger.LogTrace($"System-Hostname: '{hostname}', Benutzer: '{username}', Zeitzone: '{localTimeZone.DisplayName}'");

            bool isHostnamePersonal = false;

            if (!string.IsNullOrEmpty(username) && username.Length > 2 &&
                hostname.Contains(username, StringComparison.OrdinalIgnoreCase))
            {
                isHostnamePersonal = true;
            }

            bool isGenericHostname = hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                                     hostname.StartsWith("arch", StringComparison.OrdinalIgnoreCase) ||
                                     hostname.StartsWith("linux", StringComparison.OrdinalIgnoreCase) ||
                                     hostname.StartsWith("desktop", StringComparison.OrdinalIgnoreCase);

            if (isHostnamePersonal)
            {
                Logger.LogWarning($"Hostname '{hostname}' enthält den Benutzernamen '{username}'!");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"Persönlicher Hostname erkannt ('{hostname}')",
                    Details = $"Der Computercode/Hostname enthält deinen Benutzernamen ({username}).\n\nHinweis: Das kann im lokalen Netzwerk deine Identität verraten. Nutze lieber einen anonymen Hostnamen."
                });
            }

            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Hostname & Systemzeit-Konfiguration unauffällig.",
                Details = $"Hostname: '{hostname}'\nZeitzone: '{localTimeZone.Id}' ({localTimeZone.DisplayName})\n\nHinweis: Achte darauf, dass deine Zeitzone zum Standort deines VPNs passt."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Hostname/Zeitzonen-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Hostname-Analyse fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
