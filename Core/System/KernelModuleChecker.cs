using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Prüft, ob das nachträgliche Laden von Linux-Kernelmodulen eingeschränkt ist.
/// </summary>
public sealed class KernelModuleChecker : IOpSecChecker
{
    public string Name => "Kernel-Modul-Ladesperre";
    public string Category => "Kernel / Speicher";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Kernel-Modul-Ladesperre...");

        try
        {
            string modulesDisabledPath = "/proc/sys/kernel/modules_disabled";

            if (!File.Exists(modulesDisabledPath))
            {
                Logger.LogInfo("Schnittstelle modules_disabled nicht vorhanden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Kernel-Modul-Status unauffällig.",
                    Details = "Schnittstelle zur Modulsperre ist nicht verfügbar."
                });
            }

            string content = File.ReadAllText(modulesDisabledPath).Trim();

            if (content == "1")
            {
                Logger.LogInfo("Kernel-Modul-Laden ist systemweit gesperrt (modules_disabled = 1).");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Kernel-Modul-Laden ist strikt gesperrt.",
                    Details = "Neue Kernel-Module können nach dem Booten nicht mehr geladen werden. Schutz vor dynamischem Nachladen von Rootkits ist aktiv."
                });
            }

            Logger.LogInfo("Kernel-Modul-Laden ist erlaubt (modules_disabled = 0).");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Kernel-Modul-Laden ist erlaubt (Standard).",
                Details = "Das dynamische Laden von Kernel-Modulen ist aktiv (`modules_disabled = 0`). Dies entspricht dem normalen Desktop-Verhalten."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Kernel-Modul-Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Kernel Module Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
