using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Ermittelt persistente Vorschaubild-Caches, die Dateiinhalte rekonstruierbar machen können.
/// </summary>
public sealed class ThumbnailCacheChecker : IOpSecChecker
{
    public string Name => "Vorschaubild-Cache-Prüfung (Thumbnails)";
    public string Category => "Anti-Forensik / Hygiene";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Thumbnail-Caches auf Bild-Artefakte...");

        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string thumbDir = OperatingSystem.IsWindows()
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft",
                    "Windows",
                    "Explorer")
                : Path.Combine(homeDir, ".cache", "thumbnails");
            string searchPattern = OperatingSystem.IsWindows() ? "thumbcache_*.db" : "*.png";

            if (!Directory.Exists(thumbDir))
            {
                Logger.LogInfo("Kein Thumbnail-Cache-Verzeichnis vorhanden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Kein Thumbnail-Cache vorhanden.",
                    Details = "Das Verzeichnis `~/.cache/thumbnails/` existiert nicht."
                });
            }

            var files = Directory.GetFiles(thumbDir, searchPattern, SearchOption.AllDirectories);
            int count = files.Length;

            if (count > 0)
            {
                long totalBytes = 0;
                foreach (var file in files)
                {
                    totalBytes += new FileInfo(file).Length;
                }

                double sizeMb = Math.Round((double)totalBytes / (1024 * 1024), 2);

                Logger.LogWarning($"Thumbnail-Cache enthält {count} gepufferte Vorschaubilder ({sizeMb} MB)!");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"{count} Vorschaubilder ({sizeMb} MB) im Cache gefunden!",
                    Details = $"Im systemeigenen Thumbnail-Cache liegen {count} Cache-Dateien bzw. Vorschaubilder.\n\n" +
                              "Hinweis: Auch gelöschte oder aus verschlüsselten Containern geöffnete Medien können als Vorschau erhalten bleiben."
                });
            }

            Logger.LogTrace("Thumbnail-Cache ist leer.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Thumbnail-Cache ist komplett leer.",
                Details = "Es befinden sich keine gespeicherten Vorschaubilder im plattformspezifischen Thumbnail-Cache."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Thumbnail-Cache-Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Thumbnail Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
