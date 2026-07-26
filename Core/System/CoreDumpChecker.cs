using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Ermittelt, ob Speicherabbilder nach Prozessabstürzen sensible Daten hinterlassen können.
/// </summary>
public sealed class CoreDumpChecker : IOpSecChecker
{
    public string Name => "Speicherabbild-Sicherheitsprüfung (Core Dumps)";
    public string Category => "Kernel / Speicher";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Core-Dump-Konfiguration...");

        try
        {
            string coredumpConf = "/etc/systemd/coredump.conf";
            bool isDisabledInSystemd = false;

            if (File.Exists(coredumpConf))
            {
                var lines = File.ReadAllLines(coredumpConf);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                    if (trimmed.StartsWith("Storage=none", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("ProcessSizeMax=0", StringComparison.OrdinalIgnoreCase))
                    {
                        isDisabledInSystemd = true;
                        break;
                    }
                }
            }

            string coredumpDir = "/var/lib/systemd/coredump";
            int dumpCount = 0;
            if (Directory.Exists(coredumpDir))
            {
                dumpCount = Directory.GetFiles(coredumpDir).Length;
            }

            if (isDisabledInSystemd)
            {
                Logger.LogInfo("Core Dumps sind in coredump.conf deaktiviert (Storage=none).");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Core Dumps sind sicher deaktiviert.",
                    Details = "In `/etc/systemd/coredump.conf` ist das Speichern von Speicherabbildern deaktiviert (`Storage=none`). Beim Absturz von Apps gelangen keine RAM-Inhalte auf die Festplatte."
                });
            }

            if (dumpCount > 0)
            {
                Logger.LogWarning($"Core Dumps sind aktiv und es wurden bereits {dumpCount} Speicherabbilder gefunden!");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"Mögliches RAM-Leak: {dumpCount} Core Dumps auf der Festplatte gefunden!",
                    Details = $"Im Ordner `{coredumpDir}` liegen {dumpCount} Abbilder abgestürzter Programme.\n\n" +
                              "Hinweis: Diese Dateien können sensible Klartext-Daten (Passwörter, Keys) enthalten. Setze `Storage=none` in `/etc/systemd/coredump.conf`."
                });
            }

            Logger.LogWarning("Core Dumps sind nicht explizit deaktiviert.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Core Dumps sind systemweit erlaubt.",
                Details = "In `/etc/systemd/coredump.conf` ist `Storage=none` nicht gesetzt.\n\n" +
                          "Hinweis: Wenn eine Anwendung abstürzt, kann ihr Arbeitsspeicher unverschlüsselt auf die Festplatte geschrieben werden."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Core-Dump-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Core Dump Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
