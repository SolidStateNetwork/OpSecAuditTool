using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft die Linux-Yama-Einstellung, die Prozessinspektion über ptrace beschränkt.
/// </summary>
public sealed class PtraceScopeChecker : IOpSecChecker
{
    public string Name => "Prozess-Sprechverbot-Prüfung (ptrace scope)";
    public string Category => "Kernel / Speicher";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der YAMA ptrace_scope Einstellungen...");

        try
        {
            string ptracePath = "/proc/sys/kernel/yama/ptrace_scope";

            if (!File.Exists(ptracePath))
            {
                Logger.LogInfo("YAMA ptrace_scope ist auf diesem Kernel nicht verfügbar.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Ptrace-Schutz (YAMA) nicht gefunden.",
                    Details = "Die Datei '/proc/sys/kernel/yama/ptrace_scope' existiert nicht auf diesem System."
                });
            }

            string content = File.ReadAllText(ptracePath).Trim();

            if (int.TryParse(content, out int level))
            {
                if (level >= 1)
                {
                    string scopeDescription = level switch
                    {
                        1 => "Eingeschränkt (Prozesse dürfen nur ihre eigenen Kindprozesse analysieren).",
                        2 => "Admin-only (Nur Benutzer mit CAP_SYS_PTRACE dürfen ptrace nutzen).",
                        3 => "Deaktiviert (Ptrace ist systemweit bis zum Neustart komplett gesperrt).",
                        _ => $"Stufe {level} aktiv."
                    };

                    Logger.LogInfo($"ptrace_scope ist sicher konfiguriert (Level {level}).");
                    return Task.FromResult(new CheckResult
                    {
                        Name = Name,
                        Category = Category,
                        Status = CheckStatus.Pass,
                        Summary = $"Ptrace-Schutz ist aktiv (Level {level}).",
                        Details = $"Aktueller Schutz-Status: {scopeDescription}\n\nUnbefugtes Auslesen des Arbeitsspeichers durch fremde Prozesse wird verhindert."
                    });
                }

                Logger.LogWarning("ptrace_scope steht auf 0 (Klassische/Umfassende Rechte)!");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "KRITISCH: Ptrace-Schutz ist deaktiviert (Level 0)!",
                    Details = "Jeder Prozess deines Benutzers kann den RAM anderer laufender Programme (z. B. Browser, Passwort-Manager) auslesen.\n\n" +
                              "Empfehlung: Setze `kernel.yama.ptrace_scope = 1` in `/etc/sysctl.d/10-ptrace.conf`."
                });
            }

            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "ptrace_scope Wert nicht lesbar.",
                Details = $"Unerwarteter Inhalt in ptrace_scope: '{content}'"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Ptrace-Schutz-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Ptrace Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
