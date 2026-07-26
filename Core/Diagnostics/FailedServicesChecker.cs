using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Meldet fehlgeschlagene systemd-Dienste, die auf Stabilitäts- oder Sicherheitsprobleme hindeuten.
/// </summary>
public sealed class FailedServicesChecker : IOpSecChecker
{
    public string Name => "Fehlgeschlagene Systemd-Dienste";
    public string Category => "System / Härtung";

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung auf fehlerhafte oder fehlgeschlagene Systemd-Dienste...");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = "--failed --plain --no-legend",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Logger.LogInfo("systemctl steht nicht zur Verfügung.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Fail,
                    Summary = "Systemd Status nicht abrufbar.",
                    Details = "Der Befehl `systemctl` konnte nicht ausgeführt werden. Fehlerhafte Dienste lassen sich deshalb nicht sicher ausschließen."
                };
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> failedServices = new();

            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    failedServices.Add(parts[0]);
                }
            }

            if (failedServices.Count > 0)
            {
                string serviceList = string.Join(", ", failedServices);
                Logger.LogWarning($"Fehlgeschlagene Systemd-Dienste gefunden: {serviceList}");

                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"{failedServices.Count} fehlgeschlagene(r) Systemd-Dienst(e) entdeckt!",
                    Details = $"Folgende Systemd-Units befinden sich im Status 'failed':\n• {string.Join("\n• ", failedServices)}\n\n" +
                              "Hinweis: Prüfe mit `systemctl status <unit>` die Fehlerursache. Unbekannte fehlgeschlagene Dienste können auf ungewollte Persistence-Versuche hindeuten."
                };
            }

            Logger.LogTrace("Keine fehlerhaften Systemd-Services vorhanden.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Alle Systemd-Dienste laufen sauber.",
                Details = "Es wurden keine Einheiten im Status 'failed' identifiziert."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Systemd Failed Services Audit", ex);
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Systemd Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            };
        }
    }
}
