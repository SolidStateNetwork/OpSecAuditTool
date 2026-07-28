using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Zählt und prüft installierte Browser-Erweiterungen (Chromium, Chrome, Brave, Firefox),
/// die potenziell Zugangsdaten, Cookies oder den gesamten Seiteninhalt auslesen können.
/// </summary>
public sealed class BrowserExtensionAuditChecker : OpSecCheckerBase
{
    public override string Name => "Browser-Erweiterungen & Developer-Mode Audit";
    public override string Category => "Anti-Forensik / Hygiene";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Audit der installierten Browser-Erweiterungen...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] extensionDirs =
        {
            Path.Combine(homeDir, ".config", "google-chrome", "Default", "Extensions"),
            Path.Combine(homeDir, ".config", "BraveSoftware", "Brave-Browser", "Default", "Extensions"),
            Path.Combine(homeDir, ".config", "chromium", "Default", "Extensions"),
            Path.Combine(homeDir, ".config", "microsoft-edge", "Default", "Extensions")
        };

        int totalExtensions = 0;
        var browserCounts = new Dictionary<string, int>();

        foreach (var dir in extensionDirs)
        {
            if (!Directory.Exists(dir)) continue;

            try
            {
                var extensions = Directory.GetDirectories(dir);
                // Ignoriere die internen Standard-Extensions (z.B. Temp/BuiltIn)
                int count = extensions.Count(d => Path.GetFileName(d) != "Temp");
                if (count > 0)
                {
                    totalExtensions += count;
                    string browserName = dir.Contains("google-chrome") ? "Google Chrome" :
                                         dir.Contains("BraveSoftware") ? "Brave" :
                                         dir.Contains("chromium") ? "Chromium" : "Edge";
                    browserCounts[browserName] = count;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        // Suche in Firefox nach Erweiterungen
        string mozillaDir = Path.Combine(homeDir, ".mozilla", "firefox");
        if (Directory.Exists(mozillaDir))
        {
            try
            {
                var profiles = Directory.GetDirectories(mozillaDir, "*.*");
                int fxCount = 0;
                foreach (var profile in profiles)
                {
                    string extDir = Path.Combine(profile, "extensions");
                    if (Directory.Exists(extDir))
                    {
                        fxCount += Directory.GetFiles(extDir, "*.xpi").Length;
                    }
                }
                if (fxCount > 0)
                {
                    totalExtensions += fxCount;
                    browserCounts["Firefox"] = fxCount;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        if (totalExtensions >= 5)
        {
            var summaryLines = browserCounts.Select(kv => $"  • {kv.Key}: {kv.Value} Erweiterung(en)");
            return Task.FromResult(Warning(
                $"Insgesamt {totalExtensions} installierte Browser-Erweiterung(en) erkannt.",
                "Aufschlüsselung nach Browser:\n" + string.Join("\n", summaryLines) +
                "\n\nHinweis: Jede Erweiterung hat hohe Berechtigungen innerhalb deines Browsers und " +
                "kann potenziell Cookies, Sessions und Website-Inhalte auslesen. Prüfe regelmäßig, ob alle Erweiterungen notwendig sind."));
        }

        return Task.FromResult(Pass(
            $"{totalExtensions} Browser-Erweiterung(en) installiert (geringe Angriffsfläche).",
            "Die Anzahl der installierten Erweiterungen im Browser-Profil ist minimal."));
    }
}
