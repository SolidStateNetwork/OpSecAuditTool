using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Ermittelt aktive systemweite und benutzerspezifische Cron-Aufgaben.
/// </summary>
public sealed class CronJobChecker : IOpSecChecker
{
    public string Name => "Geplante Aufgaben & Cronjob-Persistence";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Audit der geplanten Tasks und Cronjobs...");

        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string userCronPath = $"/var/spool/cron/crontabs/{Environment.UserName}";
            string altUserCronPath = $"/var/spool/cron/{Environment.UserName}";

            List<string> activeCronSources = new();

            if (File.Exists(userCronPath) && HasActiveCronLines(userCronPath))
            {
                activeCronSources.Add($"User Crontab ({userCronPath})");
            }
            else if (File.Exists(altUserCronPath) && HasActiveCronLines(altUserCronPath))
            {
                activeCronSources.Add($"User Crontab ({altUserCronPath})");
            }

            string systemCronD = "/etc/cron.d";
            if (Directory.Exists(systemCronD))
            {
                var files = Directory.GetFiles(systemCronD);
                if (files.Length > 0)
                {
                    activeCronSources.Add($"/etc/cron.d ({files.Length} Skripte)");
                }
            }

            if (activeCronSources.Count > 0)
            {
                string sourcesText = string.Join(", ", activeCronSources);
                Logger.LogWarning($"Aktive Cronjobs / geplante Tasks gefunden: {sourcesText}");

                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Geplante Hintergrund-Tasks (Cron) sind aktiv!",
                    Details = $"Folgende Task-Quellen enthalten aktive Einträge:\n• {string.Join("\n• ", activeCronSources)}\n\n" +
                              "Hinweis: Überprüfe mit `crontab -l` und in `/etc/cron.d/`, ob alle automatischen Aufgaben von dir gewollt sind."
                });
            }

            Logger.LogTrace("Keine aktiven Benutzer-Cronjobs oder benutzerdefinierten Task-Scheduler gefunden.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine verdächtigen Cron-Persistence Einträge.",
                Details = "Es wurden keine aktiven Benutzer-Crontabs oder ungewöhnlichen Task-Scheduler-Skripte im System gefunden."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Cronjob-Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Cron Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }

    private static bool HasActiveCronLines(string filePath)
    {
        foreach (string line in File.ReadLines(filePath))
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith('#') && !string.IsNullOrWhiteSpace(trimmed))
            {
                return true;
            }
        }

        return false;
    }
}
