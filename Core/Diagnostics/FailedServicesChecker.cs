using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Meldet fehlgeschlagene systemd-Dienste, die auf Stabilitäts- oder Sicherheitsprobleme hindeuten.
/// </summary>
public sealed class FailedServicesChecker : OpSecCheckerBase
{
    public override string Name => "Fehlgeschlagene Systemd-Dienste";
    public override string Category => "System / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        var result = await ShellCommandService.ExecuteAsync("systemctl", "--failed --plain --no-legend");
        if (!result.IsSuccess)
        {
            return Warning(
                "Systemd Status nicht abrufbar.",
                "Der Befehl `systemctl` konnte nicht ausgeführt werden (z.B. in Containern oder Nicht-systemd Systemen).");
        }

        string[] lines = result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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

            return Warning(
                $"{failedServices.Count} fehlgeschlagene(r) Systemd-Dienst(e) entdeckt!",
                $"Folgende Systemd-Units befinden sich im Status 'failed':\n• {string.Join("\n• ", failedServices)}\n\n" +
                "Hinweis: Prüfe mit `systemctl status <unit>` die Fehlerursache. Unbekannte fehlgeschlagene Dienste können auf ungewollte Persistence-Versuche hindeuten.");
        }

        Logger.LogTrace("Keine fehlerhaften Systemd-Services vorhanden.");
        return Pass(
            "Alle Systemd-Dienste laufen sauber.",
            "Es wurden keine Einheiten im Status 'failed' identifiziert.");
    }
}
