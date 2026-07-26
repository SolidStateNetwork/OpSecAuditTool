using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Prüft Benutzer-Papierkörbe auf zurückgebliebene Dateien und Metadaten.
/// </summary>
public sealed class TrashChecker : IOpSecChecker
{
    public string Name => "Papierkorb-Inhalts- & Spurenprüfung";
    public string Category => "Anti-Forensik / Hygiene";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Papierkorbs auf verbleibende Dateien...");

        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string trashFilesDir = Path.Combine(homeDir, ".local", "share", "Trash", "files");

            if (!Directory.Exists(trashFilesDir))
            {
                Logger.LogInfo("Kein Papierkorb-Ordner gefunden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Papierkorb existiert nicht / ist leer.",
                    Details = "Das Verzeichnis `~/.local/share/Trash/files/` ist nicht vorhanden."
                });
            }

            var entries = Directory.GetFileSystemEntries(trashFilesDir);
            int count = entries.Length;

            if (count > 0)
            {
                long totalBytes = 0;
                foreach (var entry in entries)
                {
                    if (File.Exists(entry))
                    {
                        totalBytes += new FileInfo(entry).Length;
                    }
                    else if (Directory.Exists(entry))
                    {
                        totalBytes += GetDirectorySize(entry);
                    }
                }

                double sizeMb = Math.Round((double)totalBytes / (1024 * 1024), 2);

                Logger.LogWarning($"Papierkorb enthält {count} Objekt(e) ({sizeMb} MB)!");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"{count} Objekt(e) ({sizeMb} MB) im Papierkorb gefunden!",
                    Details = $"Im Ordner `~/.local/share/Trash/files/` befinden sich {count} gelöschte Dateien/Ordner.\n\n" +
                              "Hinweis: Objekte im Papierkorb verbleiben unverschlüsselt auf dem Datenträger. Leere den Papierkorb regelmäßig oder nutze `shred` / `rm` im Terminal."
                });
            }

            Logger.LogTrace("Papierkorb ist vollständig leer.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Papierkorb ist vollständig leer.",
                Details = "Es befinden sich keine verbleibenden Objekte in `~/.local/share/Trash/files/`."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Trash Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Trash Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }

    private static long GetDirectorySize(string path)
    {
        long size = 0;

        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            size += new FileInfo(file).Length;
        }

        return size;
    }
}
