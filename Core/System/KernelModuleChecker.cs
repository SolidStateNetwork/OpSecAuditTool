using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Prüft, ob das nachträgliche Laden von Linux-Kernelmodulen eingeschränkt ist.
/// </summary>
public sealed class KernelModuleChecker : OpSecCheckerBase
{
    public override string Name => "Kernel-Modul-Ladesperre";
    public override string Category => "Kernel / Speicher";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der Kernel-Modul-Ladesperre...");
        string modulesDisabledPath = "/proc/sys/kernel/modules_disabled";

        if (!File.Exists(modulesDisabledPath))
        {
            Logger.LogInfo("Schnittstelle modules_disabled nicht vorhanden.");
            return Pass(
                "Kernel-Modul-Status unauffällig.",
                "Schnittstelle zur Modulsperre ist nicht verfügbar.");
        }

        string content = (await File.ReadAllTextAsync(modulesDisabledPath)).Trim();

        if (content == "1")
        {
            Logger.LogInfo("Kernel-Modul-Laden ist systemweit gesperrt (modules_disabled = 1).");
            return Pass(
                "Kernel-Modul-Laden ist strikt gesperrt.",
                "Neue Kernel-Module können nach dem Booten nicht mehr geladen werden. Schutz vor dynamischem Nachladen von Rootkits ist aktiv.");
        }

        Logger.LogInfo("Kernel-Modul-Laden ist erlaubt (modules_disabled = 0).");
        return Warning(
            "Kernel-Modul-Laden ist erlaubt (Standard).",
            "Das dynamische Laden von Kernel-Modulen ist aktiv (`modules_disabled = 0`). Dies entspricht dem normalen Desktop-Verhalten, stellt aber in gehärteten Umgebungen einen potenziellen Angriffsvektor für Rootkits dar.");
    }
}
