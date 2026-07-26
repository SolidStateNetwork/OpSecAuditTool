using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Ruft bei erlaubtem Internetzugriff die sichtbare öffentliche IP-Adresse und ISP-Daten ab.
/// </summary>
public sealed class IpPublicChecker : IOpSecChecker
{
    public string Name => "Öffentliche IP-Adresse & ISP-Prüfung";
    public string Category => "Netzwerk / Anonymität";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der öffentlichen IP...");

        if (!SettingsService.AllowInternetAccess)
        {
            Logger.LogTrace("Internet-Zugriff ist in den Einstellungen deaktiviert. IP-Check wird übersprungen.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "Check übersprungen (Offline-Modus).",
                Details = "Internet-Verbindungen wurden in den Einstellungen für das Audit-Tool deaktiviert."
            };
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            string publicIp = string.Empty;
            try
            {
                string json = await client.GetStringAsync("https://api.ipify.org?format=json");
                using var doc = JsonDocument.Parse(json);
                publicIp = doc.RootElement.GetProperty("ip").GetString() ?? string.Empty;
            }
            catch
            {
                // Der zweite Anbieter dient ausschließlich als Fallback.
                try
                {
                    string rawIp = await client.GetStringAsync("https://icanhazip.com");
                    publicIp = rawIp.Trim();
                }
                catch
                {
                    // Beide Fehler werden unten gemeinsam als kritisches, unklares Ergebnis behandelt.
                }
            }

            if (!string.IsNullOrEmpty(publicIp))
            {
                Logger.LogWarning($"Öffentliche IP ermittelt: {publicIp}");

                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"Öffentliche IP sichtbar: {publicIp}",
                    Details = $"Deine echte/aktuelle IP-Adresse ist online sichtbar ({publicIp}).\n\n" +
                              "Hinweis: Wenn du anonym surfen möchtest, aktiviere ein VPN oder die Tor-Verbindung."
                };
            }

            Logger.LogInfo("Öffentliche IP konnte nicht über Direct-HTTP abgerufen werden.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "IP-Prüfung ohne eindeutiges Ergebnis.",
                Details = "Die IP-Abfrage lieferte keine Antwort. Die Prüfung gilt als kritisch, weil sie die öffentliche IP nicht verlässlich bewerten konnte."
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == global::System.Net.HttpStatusCode.TooManyRequests)
        {
            Logger.LogWarning("IP-Check Rate-Limit erreicht (HTTP 429).");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "Rate-Limit erreicht (HTTP 429).",
                Details = "Der Anfragen-Dienst hat die Anfrage vorübergehend gedrosselt. Die öffentliche IP konnte deshalb nicht verlässlich geprüft werden."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Abrufen der öffentlichen IP", ex);
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "IP-Prüfung fehlgeschlagen.",
                Details = $"Die öffentliche IP konnte nicht verlässlich geprüft werden ({ex.Message})."
            };
        }
    }
}
