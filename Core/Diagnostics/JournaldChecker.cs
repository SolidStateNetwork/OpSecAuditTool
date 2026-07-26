using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Prüft, ob systemd-journald Systemereignisse dauerhaft auf dem Datenträger speichert.
/// </summary>
public sealed class JournaldChecker : IOpSecChecker
{
    public string Name => "Systemd-Journal-Protokollspeicherung";
    public string Category => "Anti-Forensik / Hygiene";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der systemd-journald Log-Konfiguration...");

        try
        {
            string journalConf = "/etc/systemd/journald.conf";
            bool isVolatile = false;

            if (File.Exists(journalConf))
            {
                var lines = File.ReadAllLines(journalConf);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                    if (trimmed.StartsWith("Storage=volatile", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("Storage=none", StringComparison.OrdinalIgnoreCase))
                    {
                        isVolatile = true;
                        break;
                    }
                }
            }

            string journalLogDir = "/var/log/journal";
            bool hasPersistentLogs = Directory.Exists(journalLogDir) && Directory.GetFileSystemEntries(journalLogDir).Length > 0;

            if (isVolatile)
            {
                Logger.LogInfo("journald ist auf 'volatile' gesetzt. Logs liegen flüchtig im RAM.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "System-Logs sind flüchtig (RAM-only).",
                    Details = "In `/etc/systemd/journald.conf` ist `Storage=volatile` konfiguriert. Alle System-Logs werden beim Ausschalten spurlos gelöscht."
                });
            }

            if (hasPersistentLogs)
            {
                Logger.LogWarning("Persistente System-Logs in /var/log/journal/ gefunden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Persistentes Logging aktiv (Spuren auf Festplatte).",
                    Details = "Das System speichert Journal-Logs dauerhaft unter `/var/log/journal/` auf der Festplatte.\n\n" +
                              "Hinweis: Forensiker oder lokale Angreifer können historische Aktivitäten nachvollziehen. Setze `Storage=volatile` in `/etc/systemd/journald.conf` für reine RAM-Speicherung."
                });
            }

            Logger.LogTrace("Keine auffälligen persistenten Journal-Logs entdeckt.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Journal-Logging ist unauffällig.",
                Details = "Es wurden keine persistenten Journald-Ordner auf der Festplatte identifiziert."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Prüfen der Journald-Konfiguration", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Journald Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
