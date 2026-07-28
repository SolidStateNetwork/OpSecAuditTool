using System;
using System.IO;

namespace OpSecAuditTool.Services;

/// <summary>
/// Liefert die Speicherorte für den portablen Betrieb. Sämtliche veränderlichen
/// Anwendungsdaten bleiben unabhängig vom Betriebssystem neben der ausführbaren Datei.
/// </summary>
public static class AppPaths
{
    public static string DataDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory;

    public static string SettingsDirectory => Path.Combine(DataDirectory, "Settings");
    public static string LogsDirectory => Path.Combine(DataDirectory, "Logs");
    public static string ReportsDirectory => Path.Combine(DataDirectory, "Reports");
    public static string BackupsDirectory => Path.Combine(DataDirectory, "Backups");
}
