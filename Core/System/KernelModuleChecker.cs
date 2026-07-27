using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Prüft, ob das nachträgliche Laden von Linux-Kernelmodulen eingeschränkt ist.
/// Ein Standardzustand wird als solcher benannt und nicht als zusätzliche Härtung
/// ausgegeben.
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
            const string modulesDisabledPath = "/proc/sys/kernel/modules_disabled";

            if (!File.Exists(modulesDisabledPath))
            {
                Logger.LogWarning("Schnittstelle modules_disabled nicht vorhanden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Kernel-Modul-Ladesperre konnte nicht geprüft werden.",
                    Details = "Die Schnittstelle `/proc/sys/kernel/modules_disabled` ist nicht verfügbar. Eine aktive Modulsperre lässt sich deshalb nicht bestätigen."
                });
            }

            string content = File.ReadAllText(modulesDisabledPath).Trim();

            if (content == "1")
            {
                Logger.LogInfo("Kernel-Modul-Laden ist systemweit gesperrt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Kernel-Modul-Laden ist nach dem Booten gesperrt.",
                    Details = "`kernel.modules_disabled` steht auf 1. Neue Kernel-Module können bis zum Neustart nicht mehr geladen werden."
                });
            }

            if (content == "0")
            {
                Logger.LogInfo("Kernel-Modul-Laden ist im normalen Desktop-Standardzustand erlaubt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Kernel-Module können dynamisch geladen werden.",
                    Details = "`kernel.modules_disabled` steht auf 0. Das entspricht dem üblichen Desktop-Verhalten, stellt aber keine zusätzliche Härtung gegen nachgeladenen Kernel-Code dar."
                });
            }

            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Unerwarteter Wert für die Kernel-Modulsperre.",
                Details = $"Inhalt von `{modulesDisabledPath}`: '{content}'"
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
                Summary = "Kernel-Modul-Audit fehlgeschlagen.",
                Details = ex.Message
            });
        }
    }
}
