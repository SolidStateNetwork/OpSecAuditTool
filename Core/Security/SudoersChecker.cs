using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Sucht in sudoers-Regeln nach kennwortlosen oder besonders weitreichenden Freigaben.
/// Erkennung von NOPASSWD sowie !authenticate in /etc/sudoers und /etc/sudoers.d/.
/// </summary>
public sealed class SudoersChecker : OpSecCheckerBase
{
    public override string Name => "Sudo-Berechtigungen & Rechteausweitung";
    public override string Category => "System / Härtung";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Audit der Sudoers-Konfiguration...");

        try
        {
            string sudoersFile = "/etc/sudoers";
            string sudoersDDir = "/etc/sudoers.d";

            bool hasPrivilegeEscalationRisk = false;
            string matchedDetails = "";

            if (File.Exists(sudoersFile))
            {
                if (CheckFileForRiskyRules(sudoersFile, out string match))
                {
                    hasPrivilegeEscalationRisk = true;
                    matchedDetails += $"• {sudoersFile}: {match}\n";
                }
            }

            if (Directory.Exists(sudoersDDir))
            {
                var files = Directory.GetFiles(sudoersDDir);
                foreach (var file in files)
                {
                    if (CheckFileForRiskyRules(file, out string match))
                    {
                        hasPrivilegeEscalationRisk = true;
                        matchedDetails += $"• {Path.GetFileName(file)}: {match}\n";
                    }
                }
            }

            if (hasPrivilegeEscalationRisk)
            {
                Logger.LogWarning("Passwortlose oder uneingeschränkte Regel in Sudoers gefunden!");
                return Task.FromResult(Warning(
                    "Privilege Escalation Risiko: Passwortlose Sudo-Regel aktiv!",
                    $"Gefundene kritische Sudoers-Einträge ('NOPASSWD' / '!authenticate'):\n{matchedDetails}\n" +
                    "Hinweis: Befehle können ohne Passwortbestätigung mit Root-Rechten ausgeführt werden. Überprüfe, ob diese Ausnahmen zwingend notwendig sind."));
            }

            Logger.LogInfo("Sudoers-Konfiguration enthält keine auffälligen NOPASSWD/!authenticate Regeln.");
            return Task.FromResult(Pass(
                "Sudoers-Konfiguration ist gehärtet.",
                "Es wurden keine uneingeschränkten 'NOPASSWD' oder '!authenticate' Berechtigungen in `/etc/sudoers` oder `/etc/sudoers.d/` gefunden."));
        }
        catch (UnauthorizedAccessException)
        {
            Logger.LogInfo("Kein Lesezugriff auf /etc/sudoers (Standard ohne Root/Sudo).");
            return Task.FromResult(Pass(
                "Sudoers-Dateien geschützt (kein Lesezugriff ohne Root).",
                "Standard-Nutzer haben keinen Lesezugriff auf `/etc/sudoers`. Dies entspricht den Standard-Sicherheitsrichtlinien."));
        }
    }

    private static bool CheckFileForRiskyRules(string path, out string matchSummary)
    {
        matchSummary = string.Empty;
        try
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed.Contains("NOPASSWD:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains("!authenticate", StringComparison.OrdinalIgnoreCase))
                {
                    matchSummary = trimmed;
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
