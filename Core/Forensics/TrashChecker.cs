using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Prüft Benutzer-Papierkörbe auf zurückgebliebene Dateien und Metadaten.
/// Inklusive fehlerresistentem Scan von ~ und externen Datenträgern (.Trash-1000).
/// </summary>
public sealed class TrashChecker : OpSecCheckerBase
{
    public override string Name => "Papierkorb-Inhalts- & Spurenprüfung";
    public override string Category => "Anti-Forensik / Hygiene";
    public override bool CanFix => true;
    public override string FixDescription => "Leert den lokalen Papierkorb (~/.local/share/Trash/files und info) vollständig und löscht alle verbleibenden gelöschten Dateien.";

    public override Task<FixResult> FixAsync()
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] trashFolders =
            {
                Path.Combine(homeDir, ".local", "share", "Trash", "files"),
                Path.Combine(homeDir, ".local", "share", "Trash", "info")
            };

            int deletedCount = 0;
            foreach (var folder in trashFolders)
            {
                if (Directory.Exists(folder))
                {
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    foreach (var subDir in Directory.GetDirectories(folder))
                    {
                        Directory.Delete(subDir, true);
                        deletedCount++;
                    }
                }
            }

            return Task.FromResult(new FixResult
            {
                Success = true,
                Message = $"Der Papierkorb wurde geleert ({deletedCount} Element(e) entfernt)."
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new FixResult
            {
                Success = false,
                Message = $"Fehler beim Leeren des Papierkorbs: {ex.Message}"
            });
        }
    }

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der Papierkörbe auf verbleibende Dateien...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var trashDirs = new List<string>
        {
            Path.Combine(homeDir, ".local", "share", "Trash", "files")
        };

        // Überprüfe auch nach FreeDesktop-Standard gemountete Volumes auf .Trash-1000
        try
        {
            string mediaDir = "/run/media";
            if (!Directory.Exists(mediaDir)) mediaDir = "/media";

            if (Directory.Exists(mediaDir))
            {
                foreach (var userDir in Directory.GetDirectories(mediaDir))
                {
                    foreach (var mountDir in Directory.GetDirectories(userDir))
                    {
                        string trash1000 = Path.Combine(mountDir, ".Trash-1000", "files");
                        if (Directory.Exists(trash1000))
                        {
                            trashDirs.Add(trash1000);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogTrace($"Externe Medienprüfung auf Trash übersprungen: {ex.Message}");
        }

        int totalCount = 0;
        long totalBytes = 0;
        var activeTrashLocations = new List<string>();

        foreach (var trashDir in trashDirs)
        {
            if (!Directory.Exists(trashDir)) continue;

            try
            {
                var entries = Directory.GetFileSystemEntries(trashDir);
                if (entries.Length > 0)
                {
                    totalCount += entries.Length;
                    long dirBytes = GetDirectorySizeSafe(trashDir);
                    totalBytes += dirBytes;
                    activeTrashLocations.Add($"{trashDir} ({entries.Length} Objekte)");
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Zugriff auf Trash-Ordner {trashDir} eingeschränkt: {ex.Message}");
            }
        }

        if (totalCount > 0)
        {
            double sizeMb = Math.Round((double)totalBytes / (1024 * 1024), 2);
            string locationList = string.Join("\n• ", activeTrashLocations);

            Logger.LogWarning($"Papierkorb enthält {totalCount} Objekt(e) (~{sizeMb} MB)!");
            return Task.FromResult(Warning(
                $"{totalCount} Objekt(e) (~{sizeMb} MB) in Papierkörben gefunden!",
                $"In folgenden Papierkorb-Verzeichnissen befinden sich gelöschte Dateien/Ordner:\n• {locationList}\n\n" +
                "Hinweis: Objekte im Papierkorb verbleiben unverschlüsselt auf dem Datenträger. Leere den Papierkorb regelmäßig oder nutze `shred` / `rm` im Terminal."));
        }

        Logger.LogTrace("Alle überprüften Papierkörbe sind vollständig leer.");
        return Task.FromResult(Pass(
            "Papierkorb ist vollständig leer.",
            "Es befinden sich keine verbleibenden Objekte in `~/.local/share/Trash/files/` oder auf externen Medien."));
    }

    private static long GetDirectorySizeSafe(string rootPath)
    {
        long size = 0;
        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            string currentDir = stack.Pop();
            try
            {
                foreach (string file in Directory.GetFiles(currentDir))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch
                    {
                        // Einzelne unlesbare Dateien überspringen
                    }
                }

                foreach (string subDir in Directory.GetDirectories(currentDir))
                {
                    stack.Push(subDir);
                }
            }
            catch
            {
                // Unlesbare Unterordner sicher überspringen (kein Crash bei Permission Denied)
            }
        }

        return size;
    }
}
