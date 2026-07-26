using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Liest den aktiven Linux-Kernel-Lockdown-Modus aus.
/// </summary>
public sealed class KernelLockdownChecker : IOpSecChecker
{
    public string Name => "Kernel-Lockdown-Sicherheitsmodus";
    public string Category => "Kernel / Speicher";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Linux Kernel Lockdown Modus...");

        try
        {
            string lockdownPath = "/sys/kernel/security/lockdown";

            if (!File.Exists(lockdownPath))
            {
                Logger.LogInfo("Kernel Lockdown Interface ist auf diesem System nicht verfügbar.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Kernel Lockdown wird nicht unterstützt / inaktiv.",
                    Details = "Die Schnittstelle `/sys/kernel/security/lockdown` ist nicht vorhanden (z. B. wegen inaktivem EFI Secure Boot oder fehlendem Security-Modul)."
                });
            }

            string content = File.ReadAllText(lockdownPath).Trim();

            if (content.Contains("[integrity]") || content.Contains("[confidentiality]"))
            {
                string activeMode = content.Contains("[confidentiality]") ? "confidentiality" : "integrity";
                Logger.LogInfo($"Kernel Lockdown ist im Modus '{activeMode}' aktiv.");

                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = $"Kernel Lockdown ist aktiv ({activeMode}).",
                    Details = $"Der Kernel ist gegen direkte Manipulationen durch Root-Prozesse geschützt.\n\nAktivierter Modus: '{activeMode}'."
                });
            }

            Logger.LogWarning("Kernel Lockdown ist deaktiviert (none)!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Kernel Lockdown ist deaktiviert (none).",
                Details = "Inhalt von /sys/kernel/security/lockdown steht auf 'none'. Root-Prozesse können potenziell unsignierten Kernel-Code nachladen oder Kernel-RAM ansprechen.\n\n" +
                          "Hinweis: Aktiviere Secure Boot oder setze die Kernel-Boot-Option `lockdown=integrity`."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Kernel-Lockdown-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Lockdown Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
