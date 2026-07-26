using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Prüft, ob temporäre Linux-Verzeichnisse flüchtig im Arbeitsspeicher liegen.
/// </summary>
public sealed class TmpFsChecker : IOpSecChecker
{
    public string Name => "Prüfung des temporären Speichers (/tmp im RAM)";
    public string Category => "Anti-Forensik / Hygiene";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der /tmp Mount-Konfiguration...");

        try
        {
            if (!File.Exists("/proc/mounts"))
            {
                Logger.LogInfo("/proc/mounts nicht gefunden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Mount-Informationen nicht verfügbar.",
                    Details = "Das System stellt '/proc/mounts' nicht bereit."
                });
            }

            var mounts = File.ReadAllLines("/proc/mounts");

            var tmpMount = mounts.FirstOrDefault(line =>
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 3 && parts[1] == "/tmp" && parts[2] == "tmpfs";
            });

            if (tmpMount != null)
            {
                Logger.LogInfo("`/tmp` ist als `tmpfs` im RAM gemountet.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "`/tmp` liegt sicher im RAM (tmpfs).",
                    Details = "Der Ordner `/tmp` ist als RAM-Disk eingebunden. Alle temporären Dateien werden beim Herunterfahren spurlos gelöscht."
                });
            }

            Logger.LogWarning("`/tmp` ist nicht als `tmpfs` eingebunden!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Mögliches Forensik-Risiko: `/tmp` liegt auf der Festplatte.",
                Details = "Temporäre Dateien werden auf dem persistenten Datenträger gespeichert und nach dem Herunterfahren nicht automatisch aus dem Speicher gelöscht.\n\n" +
                          "Hinweis: Aktiviere `tmp.mount` via systemctl (`sudo systemctl enable --now tmp.mount`)."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der /tmp Mount-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "/tmp Analyse fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
