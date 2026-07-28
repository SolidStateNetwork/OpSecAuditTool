using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Sucht in sensiblen Verzeichnissen (~, ~/Downloads, /var/tmp) nach verwaisten
/// Coredump- oder Crash-Dump-Dateien (*.core, *.dmp), die Klartext-Passwörter aus dem RAM enthalten können.
/// </summary>
public sealed class LocalCrashDumpFileChecker : OpSecCheckerBase
{
    public override string Name => "Lokale Coredump- / Crash-Dateien Prüfung";
    public override string Category => "Anti-Forensik / Hygiene";
    public override bool CanFix => true;
    public override string FixDescription => "Sichert gefundene Coredump- und Crash-Dateien (*.core, *.dmp) im Ordner 'Backups' und löscht sie anschließend aus den Benutzerverzeichnissen.";

    public override Task<FixResult> FixAsync()
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloadsDir = Path.Combine(homeDir, "Downloads");
            string[] searchDirs = { homeDir, downloadsDir, "/var/tmp" };

            int deletedCount = 0;
            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;
                try
                {
                    var files = Directory.GetFiles(dir);
                    foreach (var f in files)
                    {
                        string name = Path.GetFileName(f);
                        if (name.EndsWith(".core", StringComparison.OrdinalIgnoreCase) ||
                            name.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("core.", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("dump.", StringComparison.OrdinalIgnoreCase))
                        {
                            BackupService.BackupFile(f);
                            File.Delete(f);
                            deletedCount++;
                        }
                    }
                }
                catch { }
            }

            return Task.FromResult(new FixResult
            {
                Success = true,
                Message = $"{deletedCount} Coredump-/Crash-Datei(en) wurden nach Backup entfernt."
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new FixResult
            {
                Success = false,
                Message = $"Fehler beim Bereinigen von Crash-Dumps: {ex.Message}"
            });
        }
    }

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Suche nach verwaisten Coredump- und Memory-Dump-Dateien...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloadsDir = Path.Combine(homeDir, "Downloads");

        string[] searchDirs =
        {
            homeDir,
            downloadsDir,
            "/var/tmp"
        };

        var foundDumps = new List<string>();

        foreach (var dir in searchDirs)
        {
            if (!Directory.Exists(dir)) continue;

            try
            {
                // Nur flach im Verzeichnis suchen, um keine tiefen Scans zu starten
                var files = Directory.GetFiles(dir);
                foreach (var f in files)
                {
                    string name = Path.GetFileName(f);
                    if (name.StartsWith("core.") || name.EndsWith(".core") || name.EndsWith(".dmp") || name.EndsWith(".mdmp"))
                    {
                        var info = new FileInfo(f);
                        if (info.Length > 1_000_000) // Mindestens ~1 MB, um echte Dumps von Mini-Dateien zu trennen
                        {
                            foundDumps.Add($"{name} ({info.Length / (1024 * 1024)} MB in {dir})");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        if (foundDumps.Count > 0)
        {
            return Task.FromResult(Warning(
                $"{foundDumps.Count} verwaiste Memory-/Coredump-Datei(en) im Dateisystem aufgespürt!",
                $"Folgende große Abbilddateien aus Arbeitsspeicher-Abstürzen wurden entdeckt:\n• {string.Join("\n• ", foundDumps)}\n\n" +
                "Empfehlung: Lösche nicht mehr benötigte Dump-Dateien sicher, da sie unverschlüsselte Sitzungsdaten und Passwörter aus dem RAM beinhalten können."));
        }

        return Task.FromResult(Pass(
            "Keine verwaisten Coredump- oder Crash-Dateien in User-Verzeichnissen aufgefunden.",
            "Weder im Home-Verzeichnis noch in ~/Downloads oder /var/tmp liegen forensisch kritische Memory-Dumps."));
    }
}
