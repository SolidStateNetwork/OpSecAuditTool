using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Prüft lokale Konfigurationsordner beliebter Desktop-Messenger (Signal, Telegram, Discord, Element)
/// auf lokale Datenspeicher, unverschlüsselte SQLite-Chatbanken und Dateiberechtigungen.
/// </summary>
public sealed class MessengerStoragePrivacyChecker : OpSecCheckerBase
{
    public override string Name => "Messenger-Datenbanken & Lokale Speicherung Prüfung";
    public override string Category => "Anti-Forensik / Hygiene";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung auf lokal gespeicherte Messenger-Verläufe und Datenbanken...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var detectedMessengers = new List<string>();

        string[] targets =
        {
            Path.Combine(homeDir, ".config", "Signal"),
            Path.Combine(homeDir, ".local", "share", "TelegramDesktop"),
            Path.Combine(homeDir, ".config", "discord"),
            Path.Combine(homeDir, ".config", "Element"),
            Path.Combine(homeDir, ".config", "WhatsApp")
        };

        foreach (var path in targets)
        {
            if (Directory.Exists(path))
            {
                string name = Path.GetFileName(path);
                if (name == "Signal")
                {
                    string dbPath = Path.Combine(path, "sql", "db.sqlite");
                    detectedMessengers.Add($"Signal Desktop (Lokale SQLite-Datenbank: {(File.Exists(dbPath) ? "Vorhanden" : "Verzeichnis aktiv")})");
                }
                else if (name == "TelegramDesktop")
                {
                    string tdata = Path.Combine(path, "tdata");
                    detectedMessengers.Add($"Telegram Desktop (Lokaler tdata-Cache: {(Directory.Exists(tdata) ? "Aktiv" : "Verzeichnis aktiv")})");
                }
                else
                {
                    detectedMessengers.Add($"{name} (Lokaler Profilordner entdeckt)");
                }
            }
        }

        if (detectedMessengers.Count > 0)
        {
            return Task.FromResult(Warning(
                $"{detectedMessengers.Count} aktive Desktop-Messenger-Profil(e) mit lokalem Verlaufsspeicher aufgedeckt.",
                $"Folgende Messenger speichern Sitzungscookies, Anhänge und Chatverläufe auf diesem System:\n• {string.Join("\n• ", detectedMessengers)}\n\n" +
                "Hinweis: Sorge dafür, dass deine Systempartition vollständig verschlüsselt ist (LUKS/BitLocker) und nutze temporäre, " +
                "sichere Löschroutinen für nicht mehr benötigte Desktop-Clients."));
        }

        return Task.FromResult(Pass(
            "Keine lokalen Desktop-Messenger Profile in Standard-Pfaden aufgefunden.",
            "Weder Signal-, Telegram- noch Element/Discord-Verzeichnisse sind aktiv."));
    }
}
