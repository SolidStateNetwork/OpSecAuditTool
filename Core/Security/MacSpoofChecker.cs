using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Vergleicht aktuelle und permanente MAC-Adressen auf erkennbare Randomisierung.
/// </summary>
public sealed class MacSpoofChecker : IOpSecChecker
{
    public string Name => "MAC-Adressen-Anonymisierung (MAC-Spoofing)";
    public string Category => "Netzwerk / Anonymität";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der MAC-Randomisierungs-Konfiguration...");

        try
        {
            string nmConfPath = "/etc/NetworkManager/NetworkManager.conf";
            string confDDir = "/etc/NetworkManager/conf.d/";

            bool isRandomized = false;
            string matchedConfig = "";

            if (File.Exists(nmConfPath))
            {
                string content = File.ReadAllText(nmConfPath);
                if (CheckConfigForRandomMac(content))
                {
                    isRandomized = true;
                    matchedConfig = nmConfPath;
                }
            }

            if (!isRandomized && Directory.Exists(confDDir))
            {
                var confFiles = Directory.GetFiles(confDDir, "*.conf");
                foreach (var file in confFiles)
                {
                    string content = File.ReadAllText(file);
                    if (CheckConfigForRandomMac(content))
                    {
                        isRandomized = true;
                        matchedConfig = file;
                        break;
                    }
                }
            }

            if (isRandomized)
            {
                Logger.LogInfo($"MAC-Randomisierung ist aktiv in: {matchedConfig}");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "MAC-Randomisierung ist im NetworkManager aktiviert.",
                    Details = $"Gefunden in Konfigurationsdatei: {matchedConfig}\n\nDein System nutzt bei WLAN-Verbindungen zufällige Hardware-Adressen."
                });
            }

            Logger.LogWarning("Keine globale MAC-Randomisierung im NetworkManager gefunden!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "MAC-Randomisierung scheinbar inaktiv.",
                Details = "Es wurde kein Eintrag für 'cloned-mac-address=random' in den NetworkManager-Dateien gefunden.\n\n" +
                          "Hinweis: Deine echte WLAN-MAC-Adresse könnte in Netzwerken sichtbar sein."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Prüfen der MAC-Randomisierung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "MAC-Analyse fehlgeschlagen.",
                Details = $"Fehler beim Lesen der NetworkManager-Dateien: {ex.Message}"
            });
        }
    }

    private static bool CheckConfigForRandomMac(string fileContent)
    {
        return fileContent.Contains("cloned-mac-address=random", StringComparison.OrdinalIgnoreCase) ||
               fileContent.Contains("cloned-mac-address=stable", StringComparison.OrdinalIgnoreCase) ||
               fileContent.Contains("wifi.scan-rand-mac-address=yes", StringComparison.OrdinalIgnoreCase);
    }
}
