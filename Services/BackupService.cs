using System;
using System.IO;

namespace OpSecAuditTool.Services;

/// <summary>
/// Service zur Erstellung von originalgetreuen Sicherungen im zentralen Backups-Unterordner der Anwendung.
/// </summary>
public static class BackupService
{
    /// <summary>
    /// Sichert eine Zieldatei im zentralen 'Backups'-Ordner ohne Änderung der Dateiendung (Originalzustand und Originalname).
    /// Falls dort bereits ein Backup unter gleichem Namen existiert, wird es überschrieben (overwrite = true).
    /// </summary>
    public static string? BackupFile(string sourceFilePath, string? customBackupName = null)
    {
        if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            return null;
        }

        Directory.CreateDirectory(AppPaths.BackupsDirectory);

        string fileName = customBackupName ?? Path.GetFileName(sourceFilePath);
        string destPath = Path.Combine(AppPaths.BackupsDirectory, fileName);

        File.Copy(sourceFilePath, destPath, true);
        return destPath;
    }
}
