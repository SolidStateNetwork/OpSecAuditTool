using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft die clientseitige SSH-Konfiguration (~/.ssh/config) auf gefährliche Parameter
/// wie deaktiviertes HostKeyChecking oder riskantes Agent-Forwarding.
/// </summary>
public sealed class SshClientConfigChecker : OpSecCheckerBase
{
    public override string Name => "SSH-Client Härtung & Konfigurationsprüfung";
    public override string Category => "System / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der clientseitigen SSH-Konfiguration...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string sshConfigPath = Path.Combine(homeDir, ".ssh", "config");

        if (!File.Exists(sshConfigPath))
        {
            return Pass(
                "Keine clientseitige SSH-Konfigurationsdatei (~/.ssh/config) vorhanden.",
                "Es existiert keine spezielle SSH-Client-Config. Standardmäßig greifen die sicheren OpenSSH-Systemdefaults.");
        }

        var riskyDirectives = new List<string>();

        try
        {
            var lines = await File.ReadAllLinesAsync(sshConfigPath);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed.Contains("StrictHostKeyChecking", StringComparison.OrdinalIgnoreCase) &&
                    (trimmed.Contains("no", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("off", StringComparison.OrdinalIgnoreCase)))
                {
                    riskyDirectives.Add("StrictHostKeyChecking no (Deaktivierte Host-Verifikation)");
                }
                else if (trimmed.Contains("UserKnownHostsFile", StringComparison.OrdinalIgnoreCase) &&
                         trimmed.Contains("/dev/null", StringComparison.OrdinalIgnoreCase))
                {
                    riskyDirectives.Add("UserKnownHostsFile /dev/null (Ignoriert bekannte Host-Schlüssel)");
                }
                else if (trimmed.Contains("ForwardAgent", StringComparison.OrdinalIgnoreCase) &&
                         (trimmed.Contains("yes", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("on", StringComparison.OrdinalIgnoreCase)))
                {
                    riskyDirectives.Add("ForwardAgent yes (Gefahr von SSH-Agent Hijacking auf Remote-Systemen)");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Warning(
                "SSH-Konfiguration konnte wegen unzureichender Berechtigungen nicht ausgelesen werden.",
                $"Kein Lesezugriff auf '{sshConfigPath}'.");
        }

        if (riskyDirectives.Count > 0)
        {
            return Warning(
                $"{riskyDirectives.Count} riskante Einstellung(en) in ~/.ssh/config entdeckt!",
                $"Folgende Parameter weichen von sicheren SSH-Standards ab:\n• {string.Join("\n• ", riskyDirectives)}\n\n" +
                "Empfehlung: Aktiviere stets 'StrictHostKeyChecking yes' und vermeide pauschales 'ForwardAgent yes'.");
        }

        return Pass(
            "SSH-Client Konfiguration ist sicher konfiguriert.",
            "In '~/.ssh/config' wurden keine unsicheren Host-Key-Prüfungen oder globalen Agent-Forwardings entdeckt.");
    }
}
