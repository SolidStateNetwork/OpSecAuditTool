using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Bewertet sicherheitsrelevante Optionen der lokalen OpenSSH-Serverkonfiguration.
/// </summary>
public sealed class SshHardeningChecker : IOpSecChecker
{
    public string Name => "SSH-Dienst-Härtungsprüfung";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der SSH-Server-Härtung...");

        try
        {
            bool isSshdRunning = ProcessInspectionService.IsAnyRunning("sshd");

            if (!isSshdRunning)
            {
                Logger.LogInfo("SSH Server (sshd) läuft nicht.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "SSH Server ist inaktiv.",
                    Details = "Der SSH-Daemon ('sshd') läuft nicht im Hintergrund. Keine Fernwartungs-Schnittstelle geöffnet."
                });
            }

            string mainConfig = "/etc/ssh/sshd_config";
            bool rootAllowed = true;
            bool passwordAuthAllowed = true;

            if (File.Exists(mainConfig))
            {
                var lines = File.ReadAllLines(mainConfig);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                    if (trimmed.StartsWith("PermitRootLogin no", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("PermitRootLogin prohibit-password", StringComparison.OrdinalIgnoreCase))
                    {
                        rootAllowed = false;
                    }

                    if (trimmed.StartsWith("PasswordAuthentication no", StringComparison.OrdinalIgnoreCase))
                    {
                        passwordAuthAllowed = false;
                    }
                }
            }

            if (!rootAllowed && !passwordAuthAllowed)
            {
                Logger.LogInfo("SSH-Server ist vollständig gehärtet.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "SSH Server ist streng gehärtet.",
                    Details = "• Root-Login ist deaktiviert.\n• Passwort-Authentifizierung ist deaktiviert (nur Key-Auth erlaubt)."
                });
            }

            string warningDetails = "Der SSH-Server ('sshd') ist aktiv, aber nicht vollständig gehärtet:\n";
            if (rootAllowed) warningDetails += "• Root-Login ist möglicherweise erlaubt ('PermitRootLogin yes').\n";
            if (passwordAuthAllowed) warningDetails += "• Passwort-Login ist erlaubt ('PasswordAuthentication yes').\n";

            Logger.LogWarning("SSH-Server läuft mit potenziell schwacher Konfiguration!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "SSH Server ist aktiv & unvollständig gehärtet!",
                Details = warningDetails + "\nEmpfehlung: Nutze ausschließlich SSH-Keys und setze 'PermitRootLogin no' sowie 'PasswordAuthentication no' in /etc/ssh/sshd_config."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der SSH-Härtungsprüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "SSH Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
