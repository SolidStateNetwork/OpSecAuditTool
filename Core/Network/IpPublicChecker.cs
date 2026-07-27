using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Ruft bei ausdrücklich erlaubtem Internetzugriff die sichtbare öffentliche
/// IP-Adresse ab. Die Adresse wird in der Oberfläche angezeigt, aber nicht in das
/// dauerhafte Anwendungslog geschrieben.
/// </summary>
public sealed class IpPublicChecker : IOpSecChecker
{
    public string Name => "Sichtbare öffentliche IP-Adresse";
    public string Category => "Netzwerk / Anonymität";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der öffentlichen IP...");

        if (!SettingsService.AllowInternetAccess)
        {
            Logger.LogTrace("Internetzugriff ist deaktiviert. IP-Prüfung wird übersprungen.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "Prüfung übersprungen (Offline-Modus).",
                Details = "Internetverbindungen wurden in den Einstellungen des Audit-Tools deaktiviert."
            };
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            string publicIp = string.Empty;

            try
            {
                string json = await client.GetStringAsync("https://api.ipify.org?format=json");
                using JsonDocument document = JsonDocument.Parse(json);
                publicIp = document.RootElement.GetProperty("ip").GetString() ?? string.Empty;
            }
            catch
            {
                try
                {
                    publicIp = (await client.GetStringAsync("https://icanhazip.com")).Trim();
                }
                catch
                {
                    // Beide Fehler werden unten gemeinsam als unklares Ergebnis behandelt.
                }
            }

            if (!string.IsNullOrWhiteSpace(publicIp))
            {
                Logger.LogWarning("Eine öffentliche IP-Adresse wurde ermittelt; der Wert wird nicht protokolliert.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"Öffentliche IP sichtbar: {publicIp}",
                    Details = $"Der verwendete Internetzugang ist unter der Adresse {publicIp} sichtbar.\n\n" +
                              "Die Anzeige allein bewertet weder VPN- noch Tor-Nutzung. Vergleiche die Adresse mit der erwarteten Ausgangsverbindung."
                };
            }

            Logger.LogInfo("Öffentliche IP konnte nicht abgerufen werden.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "IP-Prüfung ohne eindeutiges Ergebnis.",
                Details = "Beide IP-Dienste lieferten keine verwertbare Antwort. Die öffentliche Adresse konnte deshalb nicht verlässlich bewertet werden."
            };
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode == global::System.Net.HttpStatusCode.TooManyRequests)
        {
            Logger.LogWarning("IP-Prüfung wurde durch HTTP 429 gedrosselt.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Fail,
                Summary = "Rate-Limit erreicht (HTTP 429).",
                Details = "Der Anfragedienst hat die Anfrage vorübergehend gedrosselt. Die öffentliche IP konnte nicht verlässlich geprüft werden."
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
                Details = ex.Message
            };
        }
    }
}
