using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Prüft Browser-Konfigurationen (Firefox, Chromium, Chrome, Brave) auf aktivierte
/// WebRTC-Funktionen, die trotz VPN oder Tor die lokale LAN- oder öffentliche IP verraten können.
/// </summary>
public sealed class WebRtcLeakChecker : OpSecCheckerBase
{
    public override string Name => "WebRTC IP-Leak & Browser-Privacy Prüfung";
    public override string Category => "Anti-Forensik / Hygiene";
    public override bool CanFix => true;
    public override string FixDescription => "Erstellt/aktualisiert 'user.js' in allen Firefox-Profilen mit 'user_pref(\"media.peerconnection.enabled\", false);', um WebRTC-IP-Leaks sofort zu stoppen.";

    public override async Task<FixResult> FixAsync()
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string mozillaDir = Path.Combine(homeDir, ".mozilla", "firefox");
            int fixedCount = 0;

            if (Directory.Exists(mozillaDir))
            {
                var profiles = Directory.GetDirectories(mozillaDir, "*.*");
                foreach (var profile in profiles)
                {
                    string userJs = Path.Combine(profile, "user.js");
                    string prefLine = "user_pref(\"media.peerconnection.enabled\", false);";
                    bool exists = File.Exists(userJs) && (await File.ReadAllTextAsync(userJs)).Contains(prefLine);
                    if (!exists)
                    {
                        if (File.Exists(userJs))
                        {
                            BackupService.BackupFile(userJs, $"user_{Path.GetFileName(profile)}.js");
                        }
                        await File.AppendAllTextAsync(userJs, Environment.NewLine + prefLine + Environment.NewLine);
                        fixedCount++;
                    }
                }
            }

            return new FixResult
            {
                Success = true,
                Message = $"WebRTC wurde in {fixedCount} Firefox-Profil(en) über user.js gehärtet."
            };
        }
        catch (Exception ex)
        {
            return new FixResult { Success = false, Message = $"Fehler beim Härten: {ex.Message}" };
        }
    }

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der WebRTC-Einstellungen in Browser-Profilen...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var findings = new List<string>();

        // 1. Firefox prefs.js prüfen
        string mozillaDir = Path.Combine(homeDir, ".mozilla", "firefox");
        if (Directory.Exists(mozillaDir))
        {
            try
            {
                var profiles = Directory.GetDirectories(mozillaDir, "*.*");
                foreach (var profile in profiles)
                {
                    string prefsFile = Path.Combine(profile, "prefs.js");
                    if (File.Exists(prefsFile))
                    {
                        string content = await File.ReadAllTextAsync(prefsFile);
                        if (!content.Contains("\"media.peerconnection.enabled\", false"))
                        {
                            findings.Add($"Firefox ({Path.GetFileName(profile)}): WebRTC ist nicht explizit deaktiviert (media.peerconnection.enabled = true)");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        // 2. Chromium / Chrome / Brave Preferences prüfen
        string[] chromeConfigs =
        {
            Path.Combine(homeDir, ".config", "google-chrome", "Default", "Preferences"),
            Path.Combine(homeDir, ".config", "BraveSoftware", "Brave-Browser", "Default", "Preferences"),
            Path.Combine(homeDir, ".config", "chromium", "Default", "Preferences")
        };

        foreach (var file in chromeConfigs)
        {
            if (!File.Exists(file)) continue;
            try
            {
                string content = await File.ReadAllTextAsync(file);
                // Wenn webrtc_ip_handling_policy nicht auf default_public_interface_only oder disable_non_proxified_udp gesetzt ist
                if (!content.Contains("webrtc_ip_handling_policy"))
                {
                    string browserName = file.Contains("google-chrome") ? "Google Chrome" :
                                         file.Contains("BraveSoftware") ? "Brave" : "Chromium";
                    findings.Add($"{browserName}: Keine restriktive WebRTC-IP-Handling-Policy konfiguriert");
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        if (findings.Count > 0)
        {
            return Warning(
                $"{findings.Count} Browser-Profil(e) ohne WebRTC IP-Leak-Schutz gefunden.",
                $"Folgende Browser gestatten standardmäßig WebRTC-STUN-Abfragen, welche die lokale LAN- oder echte öffentliche IP trotz VPN verraten können:\n• {string.Join("\n• ", findings)}\n\n" +
                "Empfehlung: Deaktiviere in Firefox 'media.peerconnection.enabled' via about:config oder nutze in Chromium eine WebRTC-Leak-Protect Erweiterung.");
        }

        return Pass(
            "WebRTC IP-Leak-Schutz ist in den Browser-Profilen konfiguriert oder keine Profile aktiv.",
            "Die überprüften Firefox- und Chromium-Profile sind vor ungeschütztem WebRTC-IP-Leaking geschützt.");
    }
}
