using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Ruft bei erlaubtem Internetzugriff die sichtbare öffentliche IP-Adresse ab.
/// Respektiert den Offline-Modus ohne fehlerhaftes Kritisch-Scoring.
/// </summary>
public sealed class IpPublicChecker : OpSecCheckerBase
{
    public override string Name => "Öffentliche IP-Adresse & ISP-Prüfung";
    public override string Category => "Netzwerk / Anonymität";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der öffentlichen IP...");

        if (!SettingsService.AllowInternetAccess)
        {
            Logger.LogInfo("Internet-Zugriff ist in den Einstellungen deaktiviert. IP-Check wird im Offline-Modus als sicher eingestuft.");
            return Pass(
                "Online-IP-Abfrage deaktiviert (Offline-Modus aktiv).",
                "Der Internetzugriff für das Audit-Tool wurde in den Einstellungen deaktiviert. Es findet kein Verbindungsaufbau zu externen Servern statt – voller Datenschutz offline.");
        }

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
            try
            {
                string rawIp = await client.GetStringAsync("https://icanhazip.com");
                publicIp = rawIp.Trim();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == global::System.Net.HttpStatusCode.TooManyRequests)
            {
                Logger.LogWarning("IP-Check Rate-Limit erreicht (HTTP 429).");
                return Warning(
                    "Rate-Limit beim IP-Check erreicht (HTTP 429).",
                    "Der externe IP-Dienst hat die Anfrage vorübergehend gedrosselt. Die IP-Adresse konnte im aktuellen Lauf nicht ermittelt werden.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Öffentliche IP konnte nicht abgerufen werden: {ex.Message}");
                return Warning(
                    "Öffentliche IP konnte nicht extern ermittelt werden.",
                    $"Keine Verbindung zu IP-Testservern möglich ({ex.Message}). Bitte überprüfe bei Bedarf deine Internetverbindung oder Firewall-Regeln.");
            }
        }

        if (!string.IsNullOrEmpty(publicIp))
        {
            Logger.LogWarning($"Öffentliche IP ermittelt: {publicIp}");
            return Warning(
                $"Öffentliche IP sichtbar: {publicIp}",
                $"Deine echte/aktuelle IP-Adresse ist online sichtbar ({publicIp}).\n\n" +
                "Hinweis: Wenn du anonym surfen möchtest, aktiviere ein VPN oder route den Traffic über Tor.");
        }

        return Warning(
            "IP-Prüfung ohne eindeutiges Ergebnis.",
            "Die IP-Abfrage lieferte keine Antwort von den abgefragten Servern.");
    }
}
