using System;
using System.IO;
using System.Text.Json;

namespace OpSecAuditTool.Services;

/// <summary>
/// Repräsentiert die persistenten Anwendungseinstellungen.
/// </summary>
public sealed class AppSettings
{
    public bool AllowInternetAccess { get; set; }
    public bool AutoStartAuditOnLaunch { get; set; }
    public bool ShowVerboseLogs { get; set; }
}

/// <summary>
/// Verwaltet das Laden, Speichern und Bereitstellen der Anwendungseinstellungen via JSON.
/// </summary>
public static class SettingsService
{
    private static readonly string SettingsDir = AppPaths.SettingsDirectory;
    private static readonly string SettingsFilePath = Path.Combine(SettingsDir, "Settings.json");
    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static AppSettings Current { get; private set; } = new();

    /// <summary>
    /// Shortcut-Property, die beim Ändern automatisch die Einstellungen speichert.
    /// </summary>
    public static bool AllowInternetAccess
    {
        get => Current.AllowInternetAccess;
        set
        {
            Current.AllowInternetAccess = value;
            SaveSettings();
        }
    }

    static SettingsService()
    {
        LoadSettings();
    }

    /// <summary>
    /// Lädt die Einstellungen aus der JSON-Datei, falls vorhanden.
    /// </summary>
    public static void LoadSettings()
    {
        lock (SyncRoot)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);

                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        Current = loaded;
                    }
                }
                else
                {
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Fehler beim Laden der Einstellungen", ex);
            }
        }
    }

    /// <summary>
    /// Speichert die aktuellen Einstellungen atomar. Dadurch bleibt die zuletzt
    /// gültige Datei auch bei einem Abbruch während des Schreibens erhalten.
    /// </summary>
    public static void SaveSettings()
    {
        lock (SyncRoot)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(Current, SerializerOptions);
                string temporaryPath = SettingsFilePath + ".new";
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, SettingsFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                Logger.LogError("Fehler beim Speichern der Einstellungen", ex);
            }
        }
    }
}
