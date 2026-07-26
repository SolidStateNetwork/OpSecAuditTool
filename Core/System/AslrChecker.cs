using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Prüft, ob Address Space Layout Randomization im Linux-Kernel vollständig aktiv ist.
/// </summary>
public sealed class AslrChecker : IOpSecChecker
{
    public string Name => "ASLR-Speicheradressen-Randomisierung";
    public string Category => "Kernel / Speicher";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Address Space Layout Randomization (ASLR)...");

        try
        {
            string aslrPath = "/proc/sys/kernel/randomize_va_space";

            if (!File.Exists(aslrPath))
            {
                Logger.LogWarning("ASLR-Konfigurationsdatei im Kernel nicht gefunden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "ASLR-Status konnte nicht ermittelt werden.",
                    Details = "Die Datei '/proc/sys/kernel/randomize_va_space' ist auf diesem System nicht vorhanden."
                });
            }

            string content = File.ReadAllText(aslrPath).Trim();

            if (int.TryParse(content, out int value))
            {
                if (value == 2)
                {
                    Logger.LogInfo("ASLR ist vollständig aktiv (Level 2).");
                    return Task.FromResult(new CheckResult
                    {
                        Name = Name,
                        Category = Category,
                        Status = CheckStatus.Pass,
                        Summary = "ASLR ist vollständig aktiv (Level 2).",
                        Details = "Address Space Layout Randomization schützt Stack, VDSO, mmap und Heap vor Speicher-Exploits."
                    });
                }
                else if (value == 1)
                {
                    Logger.LogWarning("ASLR ist nur partiell aktiv (Level 1).");
                    return Task.FromResult(new CheckResult
                    {
                        Name = Name,
                        Category = Category,
                        Status = CheckStatus.Warning,
                        Summary = "ASLR nur partiell aktiv (Level 1).",
                        Details = "Die Randomisierung ist für mmap, Stack und VDSO aktiv, der Heap ist jedoch ausgenommen.\n\n" +
                                  "Empfehlung: Setze `kernel.randomize_va_space = 2` in `/etc/sysctl.d/50-aslr.conf`."
                    });
                }

                Logger.LogWarning("ASLR ist komplett deaktiviert (Level 0)!");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "KRITISCH: ASLR ist deaktiviert (Level 0)!",
                    Details = "Speicheradressen werden beim Programmstart nicht randomisiert. Das System ist hochgradig anfällig für Buffer Overflows und ROP-Attatcken.\n\n" +
                              "Empfehlung: Setze `kernel.randomize_va_space = 2` via sysctl."
                });
            }

            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Unerwarteter ASLR-Wert.",
                Details = $"Inhalt von randomize_va_space: '{content}'"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der ASLR-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "ASLR Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
