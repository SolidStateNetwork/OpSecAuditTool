using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Prüft Browserprofile auf persistente Cookie- und Sitzungsdatenbanken.
/// </summary>
public sealed class BrowserStorageChecker : IOpSecChecker
{
    public string Name => "Browser-Speicher- & Cookie-Spurenprüfung";
    public string Category => "Anti-Forensik / Hygiene";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung auf verbleibende Browser-Sessions & Cookie-Datenbanken...");

        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            string appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

            string[] chromiumPaths = OperatingSystem.IsWindows()
                ?
                [
                    Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Network", "Cookies"),
                    Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Network", "Cookies"),
                    Path.Combine(localAppData, "Chromium", "User Data", "Default", "Network", "Cookies"),
                    Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Network", "Cookies")
                ]
                :
                [
                    Path.Combine(homeDir, ".config", "BraveSoftware", "Brave-Browser", "Default", "Cookies"),
                    Path.Combine(homeDir, ".config", "google-chrome", "Default", "Cookies"),
                    Path.Combine(homeDir, ".config", "chromium", "Default", "Cookies")
                ];
            string firefoxDir = OperatingSystem.IsWindows()
                ? Path.Combine(appData, "Mozilla", "Firefox", "Profiles")
                : Path.Combine(homeDir, ".mozilla", "firefox");

            var browserPaths = new Dictionary<string, string[]>
            {
                { "Chromium / Chrome / Brave / Edge", chromiumPaths },
                { "Firefox", [firefoxDir] }
            };

            List<string> foundArtifacts = new();

            foreach (var path in browserPaths["Chromium / Chrome / Brave / Edge"])
            {
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Length > 0)
                    {
                        foundArtifacts.Add($"{Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path)))} Cookie-DB ({fileInfo.Length / 1024} KB)");
                    }
                }
            }

            firefoxDir = browserPaths["Firefox"][0];
            if (Directory.Exists(firefoxDir))
            {
                var cookieFiles = Directory.GetFiles(firefoxDir, "cookies.sqlite", SearchOption.AllDirectories);
                foreach (var file in cookieFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length > 0)
                    {
                        foundArtifacts.Add($"Firefox Cookie-DB ({fileInfo.Length / 1024} KB)");
                    }
                }
            }

            if (foundArtifacts.Count > 0)
            {
                Logger.LogWarning($"Persistente Browser-Session-Datenbanken gefunden: {string.Join(", ", foundArtifacts)}");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Browser-Session-Daten auf der Festplatte gefunden!",
                    Details = $"Folgende unverschlüsselte/aktive Cookie-Datenbanken wurden entdeckt:\n• {string.Join("\n• ", foundArtifacts)}\n\n" +
                              "Hinweis: Bei forensischer Analyse oder Malware-Zugriff können Session-Tokens abgegriffen werden. Nutze empfindliche Profile im Incognito-Modus oder aktiviere 'Cookies beim Beenden löschen'."
                });
            }

            Logger.LogInfo("Keine persistenten Browser-Cookie-Datenbanken im Home-Verzeichnis gefunden.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine verbleibenden Browser-Cookie-Datenbanken.",
                Details = "Es wurden keine aktiven Cookie-Speicheratefakte in den Standard-Browserpfaden lokalisiert."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Browser-Storage-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Browser Storage Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
