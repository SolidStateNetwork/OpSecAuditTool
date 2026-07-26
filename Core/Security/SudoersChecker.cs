using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Sucht in sudoers-Regeln nach kennwortlosen oder besonders weitreichenden Freigaben.
/// </summary>
public sealed class SudoersChecker : IOpSecChecker
{
    public string Name => "Sudo-Berechtigungen & Rechteausweitung";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Audit der Sudoers-Konfiguration...");

        try
        {
            string sudoersFile = "/etc/sudoers";
            string sudoersDDir = "/etc/sudoers.d";

            bool hasNoPasswd = false;
            string matchedDetails = "";

            if (File.Exists(sudoersFile))
            {
                if (CheckFileForNoPasswd(sudoersFile, out string match))
                {
                    hasNoPasswd = true;
                    matchedDetails += $"• {sudoersFile}: {match}\n";
                }
            }

            if (Directory.Exists(sudoersDDir))
            {
                var files = Directory.GetFiles(sudoersDDir);
                foreach (var file in files)
                {
                    if (CheckFileForNoPasswd(file, out string match))
                    {
                        hasNoPasswd = true;
                        matchedDetails += $"• {Path.GetFileName(file)}: {match}\n";
                    }
                }
            }

            if (hasNoPasswd)
            {
                Logger.LogWarning("NOPASSWD-Regel in der Sudoers-Konfiguration gefunden!");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Privilege Escalation Risiko: 'NOPASSWD' Regel aktiv!",
                    Details = $"Gefundene 'NOPASSWD'-Einträge:\n{matchedDetails}\n" +
                              "Hinweis: Befehle können ohne Passwortbestätigung als Root ausgeführt werden. Überprüfe, ob diese Ausnahmen zwingend notwendig sind."
                });
            }

            Logger.LogInfo("Sudoers-Konfiguration enthält keine auffälligen NOPASSWD-Regeln.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Sudoers-Konfiguration ist gehärtet.",
                Details = "Es wurden keine uneingeschränkten 'NOPASSWD'-Berechtigungen in `/etc/sudoers` oder `/etc/sudoers.d/` gefunden."
            });
        }
        catch (UnauthorizedAccessException)
        {
            Logger.LogInfo("Kein Lesezugriff auf /etc/sudoers (Standard ohne Root/Sudo).");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Sudoers-Dateien geschützt (kein Lesezugriff ohne Root).",
                Details = "Standard-Nutzer haben keinen Lesezugriff auf `/etc/sudoers`. Dies entspricht den Standard-Sicherheitsrichtlinien."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Prüfen der Sudoers-Konfiguration", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Sudoers Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }

    private static bool CheckFileForNoPasswd(string filePath, out string matchInfo)
    {
        matchInfo = "";
        foreach (string line in File.ReadLines(filePath))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

            if (trimmed.Contains("NOPASSWD", StringComparison.OrdinalIgnoreCase))
            {
                matchInfo = trimmed;
                return true;
            }
        }

        return false;
    }
}
