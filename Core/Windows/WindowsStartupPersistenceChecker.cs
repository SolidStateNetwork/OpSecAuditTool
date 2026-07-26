using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Sucht nach Autostart-Einträgen, die Programme dauerhaft mit der Anmeldung starten.
/// </summary>
public sealed class WindowsStartupPersistenceChecker : IOpSecChecker
{
    private const string RunKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public string Name => "Windows-Autostart-Persistenzprüfung";
    public string Category => "Windows / Forensik";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe Run-Keys und Benutzer-Autostart.");
        var entries = new List<string>();
        entries.AddRange(
            WindowsRegistryReader.ReadCurrentUserValueNames(RunKey)
                .Select(name => $"Benutzer-Run: {name}"));
        entries.AddRange(
            WindowsRegistryReader.ReadLocalMachineValueNames(RunKey)
                .Select(name => $"System-Run: {name}"));

        string startupDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.Startup);
        if (Directory.Exists(startupDirectory))
        {
            entries.AddRange(
                Directory.GetFileSystemEntries(startupDirectory)
                    .Select(path => $"Startup-Ordner: {Path.GetFileName(path)}"));
        }

        entries = entries
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(entries.Count == 0
            ? Result(
                CheckStatus.Pass,
                "Keine klassische Autostart-Persistenz gefunden.",
                "Benutzer-/System-Run-Keys und der persönliche Startup-Ordner sind leer.")
            : Result(
                CheckStatus.Warning,
                $"{entries.Count} Autostart-Eintrag/Einträge sollten geprüft werden.",
                $"Gefundene Namen:\n• {string.Join("\n• ", entries)}\n\n" +
                "Autostarts können legitim sein, werden aber häufig für Persistenz missbraucht. " +
                "Befehlszeilen werden aus Datenschutzgründen nicht in den Bericht übernommen."));
    }

    private CheckResult Result(CheckStatus status, string summary, string details) => new()
    {
        Name = Name,
        Category = Category,
        Status = status,
        Summary = summary,
        Details = details
    };
}
