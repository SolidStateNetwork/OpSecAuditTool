using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Sucht in NetworkManager-Konfigurationen nach Einstellungen für zufällige oder
/// stabile MAC-Adressen. Die aktuelle Hardwareadresse wird dabei nicht mit einer
/// permanenten Adresse verglichen.
/// </summary>
public sealed class MacSpoofChecker : IOpSecChecker
{
    public string Name => "NetworkManager-MAC-Randomisierung";
    public string Category => "Netzwerk / Anonymität";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der NetworkManager-MAC-Randomisierung...");

        try
        {
            const string nmConfPath = "/etc/NetworkManager/NetworkManager.conf";
            const string confDDir = "/etc/NetworkManager/conf.d/";
            string? matchedConfig = null;

            if (File.Exists(nmConfPath) && ContainsRandomizationSetting(File.ReadAllText(nmConfPath)))
            {
                matchedConfig = nmConfPath;
            }

            if (matchedConfig == null && Directory.Exists(confDDir))
            {
                foreach (string file in Directory.GetFiles(confDDir, "*.conf"))
                {
                    if (ContainsRandomizationSetting(File.ReadAllText(file)))
                    {
                        matchedConfig = file;
                        break;
                    }
                }
            }

            if (matchedConfig != null)
            {
                Logger.LogInfo("NetworkManager-Konfiguration für MAC-Randomisierung erkannt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Eine NetworkManager-Einstellung zur MAC-Randomisierung ist vorhanden.",
                    Details = $"Gefunden in: {matchedConfig}\n\n" +
                              "Die Konfiguration spricht für zufällige oder stabile MAC-Adressen. Der tatsächlich verwendete Wert einer aktuellen Verbindung wurde nicht gemessen."
                });
            }

            Logger.LogWarning("Keine globale NetworkManager-MAC-Randomisierung gefunden.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Keine globale MAC-Randomisierung in NetworkManager erkannt.",
                Details = "Es wurde keine passende Einstellung wie `cloned-mac-address=random`, `cloned-mac-address=stable` oder `wifi.scan-rand-mac-address=yes` gefunden. Verbindungsprofile können dennoch eigene Werte enthalten."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Prüfen der NetworkManager-MAC-Randomisierung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "MAC-Randomisierungs-Konfiguration konnte nicht geprüft werden.",
                Details = ex.Message
            });
        }
    }

    private static bool ContainsRandomizationSetting(string fileContent) =>
        fileContent.Contains("cloned-mac-address=random", StringComparison.OrdinalIgnoreCase) ||
        fileContent.Contains("cloned-mac-address=stable", StringComparison.OrdinalIgnoreCase) ||
        fileContent.Contains("wifi.scan-rand-mac-address=yes", StringComparison.OrdinalIgnoreCase);
}
