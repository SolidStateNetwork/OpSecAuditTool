using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Sucht nach plattformspezifischen Listen zuletzt verwendeter Dateien.
/// </summary>
public sealed class RecentFilesChecker : IOpSecChecker
{
    public string Name => "Verlauf kürzlich geöffneter Dateien";
    public string Category => "Anti-Forensik / Hygiene";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Dateizugriffs-Historie (recently-used.xbel)...");

        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (OperatingSystem.IsWindows())
            {
                return Task.FromResult(CheckWindowsRecentFiles());
            }

            string recentXbelPath = Path.Combine(homeDir, ".local", "share", "recently-used.xbel");

            if (!File.Exists(recentXbelPath))
            {
                Logger.LogInfo("Keine recently-used.xbel Datei gefunden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Keine Dateihistorie vorhanden.",
                    Details = "Die Datei `~/.local/share/recently-used.xbel` existiert nicht."
                });
            }

            var fileInfo = new FileInfo(recentXbelPath);
            if (fileInfo.Length == 0)
            {
                Logger.LogInfo("recently-used.xbel ist leer.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Dateihistorie ist leer.",
                    Details = "Die Protokolldatei für zuletzt geöffnete Dateien ist vollständig leer."
                });
            }

            int itemCount = 0;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(recentXbelPath);
                var bookmarkNodes = doc.GetElementsByTagName("bookmark");
                itemCount = bookmarkNodes.Count;
            }
            catch
            {
                itemCount = 1;
            }

            if (itemCount > 0)
            {
                Logger.LogWarning($"Dateihistorie enthält {itemCount} protokollierte Dateizugriffe!");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"{itemCount} geöffnete Dateien in der Desktop-Historie protokolliert!",
                    Details = $"In `~/.local/share/recently-used.xbel` wurden {itemCount} historische Dateizugriffe gefunden.\n\n" +
                              "Hinweis: Diese Datei speichert Pfade und Metadaten zu allen geöffneten Medien und Dokumenten. Lösche die Datei oder verlinke sie auf `/dev/null`."
                });
            }

            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine Einträge in der Dateihistorie.",
                Details = "Die Liste zuletzt verwendeter Dateien enthält keine protokollierten Pfade."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Recent Files Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Recent Files Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }

    private CheckResult CheckWindowsRecentFiles()
    {
        string recentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
        if (!Directory.Exists(recentDirectory))
        {
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine Windows-Dateihistorie vorhanden.",
                Details = "Der benutzerspezifische Recent-Ordner existiert nicht."
            };
        }

        int itemCount = Directory.GetFiles(
            recentDirectory,
            "*",
            SearchOption.TopDirectoryOnly).Length;
        return new CheckResult
        {
            Name = Name,
            Category = Category,
            Status = itemCount == 0 ? CheckStatus.Pass : CheckStatus.Warning,
            Summary = itemCount == 0
                ? "Windows-Dateihistorie ist leer."
                : $"{itemCount} Verknüpfungen im Windows-Dateiverlauf gefunden.",
            Details = itemCount == 0
                ? "Der benutzerspezifische Recent-Ordner enthält keine Einträge."
                : "Windows speichert Verknüpfungen zu kürzlich verwendeten Dateien. " +
                  "Diese können Dokumentnamen und ursprüngliche Pfade offenlegen."
        };
    }
}
