using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Prüft Browserprofile auf persistente Cookie- und Sitzungsdatenbanken.
/// Unterstützt native, Flatpak- und Snap-Installationen unter Linux.
/// </summary>
public sealed class BrowserStorageChecker : OpSecCheckerBase
{
    public override string Name => "Browser-Speicher- & Cookie-Spurenprüfung";
    public override string Category => "Anti-Forensik / Hygiene";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung auf verbleibende Browser-Sessions & Cookie-Datenbanken (Native/Flatpak/Snap)...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var chromiumPaths = new List<string>();
        var firefoxDirs = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            chromiumPaths.AddRange(new[]
            {
                Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Network", "Cookies"),
                Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Network", "Cookies"),
                Path.Combine(localAppData, "Chromium", "User Data", "Default", "Network", "Cookies"),
                Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Network", "Cookies")
            });
            firefoxDirs.Add(Path.Combine(appData, "Mozilla", "Firefox", "Profiles"));
        }
        else
        {
            chromiumPaths.AddRange(new[]
            {
                // Native
                Path.Combine(homeDir, ".config", "BraveSoftware", "Brave-Browser", "Default", "Cookies"),
                Path.Combine(homeDir, ".config", "google-chrome", "Default", "Cookies"),
                Path.Combine(homeDir, ".config", "chromium", "Default", "Cookies"),
                // Flatpak
                Path.Combine(homeDir, ".var", "app", "com.brave.Browser", "config", "BraveSoftware", "Brave-Browser", "Default", "Cookies"),
                Path.Combine(homeDir, ".var", "app", "com.google.Chrome", "config", "google-chrome", "Default", "Cookies"),
                Path.Combine(homeDir, ".var", "app", "org.chromium.Chromium", "config", "chromium", "Default", "Cookies"),
                // Snap
                Path.Combine(homeDir, "snap", "chromium", "common", "chromium", "Default", "Cookies")
            });

            firefoxDirs.AddRange(new[]
            {
                Path.Combine(homeDir, ".mozilla", "firefox"),
                Path.Combine(homeDir, ".var", "app", "org.mozilla.firefox", ".mozilla", "firefox"),
                Path.Combine(homeDir, "snap", "firefox", "common", ".mozilla", "firefox")
            });
        }

        List<string> foundArtifacts = new();

        foreach (var path in chromiumPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Length > 0)
                    {
                        string browserName = path.Contains("Brave") ? "Brave" : path.Contains("chrome") ? "Chrome" : "Chromium/Edge";
                        string packageType = path.Contains(".var/app") ? " (Flatpak)" : path.Contains("snap") ? " (Snap)" : "";
                        foundArtifacts.Add($"{browserName}{packageType} Cookie-DB ({fileInfo.Length / 1024} KB)");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogTrace($"Konfrontierte Berechtigungsfehler bei {path}: {ex.Message}");
                }
            }
        }

        foreach (var dir in firefoxDirs)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    var cookieFiles = Directory.GetFiles(dir, "cookies.sqlite", SearchOption.AllDirectories);
                    foreach (var file in cookieFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length > 0)
                        {
                            string packageType = file.Contains(".var/app") ? " (Flatpak)" : file.Contains("snap") ? " (Snap)" : "";
                            foundArtifacts.Add($"Firefox{packageType} Cookie-DB ({fileInfo.Length / 1024} KB)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogTrace($"Firefox Profilsuche verwehrt in {dir}: {ex.Message}");
                }
            }
        }

        if (foundArtifacts.Count > 0)
        {
            Logger.LogWarning($"Persistente Browser-Session-Datenbanken gefunden: {string.Join(", ", foundArtifacts)}");
            return Task.FromResult(Warning(
                "Browser-Session-Daten auf der Festplatte gefunden!",
                $"Folgende unverschlüsselte/aktive Cookie-Datenbanken wurden entdeckt:\n• {string.Join("\n• ", foundArtifacts)}\n\n" +
                "Hinweis: Bei forensischer Analyse oder Malware-Zugriff können Session-Tokens abgegriffen werden. Nutze empfindliche Profile im Incognito-Modus oder aktiviere 'Cookies beim Beenden löschen'."));
        }

        Logger.LogInfo("Keine persistenten Browser-Cookie-Datenbanken gefunden.");
        return Task.FromResult(Pass(
            "Keine verbleibenden Browser-Cookies / Sessions gefunden.",
            "In gängigen Browser-Profilpfaden (Native/Flatpak/Snap) wurden keine Cookie-Datenbanken lokalisiert."));
    }
}
