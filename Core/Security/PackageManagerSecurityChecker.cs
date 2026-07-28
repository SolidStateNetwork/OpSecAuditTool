using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft Konfigurationsdateien von Paketmanagern (~/.config/pip/pip.conf & ~/.npmrc)
/// auf Schutzmechanismen gegen Supply-Chain-Malware ('require-virtualenv' & 'ignore-scripts').
/// </summary>
public sealed class PackageManagerSecurityChecker : OpSecCheckerBase
{
    public override string Name => "Python- & Node.js Package-Manager Hygiene";
    public override string Category => "System / Härtung";
    public override bool CanFix => true;
    public override string FixDescription => "Setzt 'require-virtualenv = true' in ~/.config/pip/pip.conf und 'ignore-scripts = true' in ~/.npmrc.";

    public override async Task<FixResult> FixAsync()
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string pipDir = Path.Combine(homeDir, ".config", "pip");
            string pipConf = Path.Combine(pipDir, "pip.conf");
            string npmRc = Path.Combine(homeDir, ".npmrc");

            Directory.CreateDirectory(pipDir);
            if (!File.Exists(pipConf) || !(await File.ReadAllTextAsync(pipConf)).Contains("require-virtualenv"))
            {
                if (File.Exists(pipConf))
                {
                    BackupService.BackupFile(pipConf);
                }
                await File.AppendAllTextAsync(pipConf, "\n[global]\nrequire-virtualenv = true\n");
            }

            if (!File.Exists(npmRc) || !(await File.ReadAllTextAsync(npmRc)).Contains("ignore-scripts=true"))
            {
                if (File.Exists(npmRc))
                {
                    BackupService.BackupFile(npmRc);
                }
                await File.AppendAllTextAsync(npmRc, "\nignore-scripts=true\n");
            }

            return new FixResult
            {
                Success = true,
                Message = "Paketmanager gehärtet: pip erfordert nun virtuelle Umgebungen und npm ignoriert Installations-Skripte."
            };
        }
        catch (Exception ex)
        {
            return new FixResult { Success = false, Message = $"Fehler beim Härten: {ex.Message}" };
        }
    }

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der lokalen Paketmanager-Konfigurationen...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string pipConf = Path.Combine(homeDir, ".config", "pip", "pip.conf");
        string npmRc = Path.Combine(homeDir, ".npmrc");

        bool hasRequireVenv = false;
        bool hasIgnoreScripts = false;

        if (File.Exists(pipConf))
        {
            try
            {
                string content = await File.ReadAllTextAsync(pipConf);
                if (content.Contains("require-virtualenv", StringComparison.OrdinalIgnoreCase) &&
                    content.Contains("true", StringComparison.OrdinalIgnoreCase))
                {
                    hasRequireVenv = true;
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        if (File.Exists(npmRc))
        {
            try
            {
                string content = await File.ReadAllTextAsync(npmRc);
                if (content.Contains("ignore-scripts=true", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("ignore-scripts = true", StringComparison.OrdinalIgnoreCase))
                {
                    hasIgnoreScripts = true;
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        var missingHardenings = new List<string>();
        if (!hasRequireVenv)
        {
            missingHardenings.Add("Python pip: 'require-virtualenv = true' in '~/.config/pip/pip.conf' nicht konfiguriert (schützt vor unbedachten globalen Installs)");
        }
        if (!hasIgnoreScripts)
        {
            missingHardenings.Add("Node npm: 'ignore-scripts = true' in '~/.npmrc' nicht aktiv (schützt vor bösartigen post-install Malware-Skripten)");
        }

        if (missingHardenings.Count > 0)
        {
            return Warning(
                "Entwickler-Paketmanager sind nicht gegen Supply-Chain-Risiken gehärtet.",
                $"Folgende Schutzmaßnahmen in deinen Benutzerkonfigurationen sind noch offen:\n• {string.Join("\n• ", missingHardenings)}\n\n" +
                "Empfehlung: Aktiviere virtuelle Umgebungen ('require-virtualenv') und schließe die automatische Skriptausführung von Drittanbieter-Paketen aus.");
        }

        return Pass(
            "Paketmanager-Konfigurationen sind gegen Supply-Chain-Risiken gehärtet.",
            "Sowohl 'require-virtualenv' als auch 'ignore-scripts' sind in den jeweiligen Konfigurationsdateien aktiv.");
    }
}
