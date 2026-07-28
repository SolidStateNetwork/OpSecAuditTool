using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Bewertet sicherheitsrelevante Optionen der lokalen OpenSSH-Serverkonfiguration.
/// Unterstützt moderne Linux-Distributionen inklusive /etc/ssh/sshd_config.d/*.conf.
/// </summary>
public sealed class SshHardeningChecker : OpSecCheckerBase
{
    public override string Name => "SSH-Dienst-Härtungsprüfung";
    public override string Category => "System / Härtung";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der SSH-Server-Härtung...");

        bool isSshdRunning = ProcessInspectionService.IsAnyRunning("sshd");

        if (!isSshdRunning)
        {
            Logger.LogInfo("SSH Server (sshd) läuft nicht.");
            return Task.FromResult(Pass(
                "SSH Server ist inaktiv.",
                "Der SSH-Daemon ('sshd') läuft nicht im Hintergrund. Keine Fernwartungs-Schnittstelle geöffnet."));
        }

        var configFiles = new List<string>();
        string mainConfig = "/etc/ssh/sshd_config";
        string dropInDir = "/etc/ssh/sshd_config.d";

        if (File.Exists(mainConfig))
        {
            configFiles.Add(mainConfig);
        }

        if (Directory.Exists(dropInDir))
        {
            try
            {
                configFiles.AddRange(Directory.GetFiles(dropInDir, "*.conf"));
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Konnte {dropInDir} nicht auslesen: {ex.Message}");
            }
        }

        bool rootAllowed = true;
        bool passwordAuthAllowed = true;
        var inspectedFiles = new List<string>();

        foreach (var file in configFiles)
        {
            if (!File.Exists(file)) continue;

            try
            {
                var lines = File.ReadAllLines(file);
                inspectedFiles.Add(Path.GetFileName(file));

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                    if (trimmed.StartsWith("PermitRootLogin no", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("PermitRootLogin prohibit-password", StringComparison.OrdinalIgnoreCase))
                    {
                        rootAllowed = false;
                    }
                    else if (trimmed.StartsWith("PermitRootLogin yes", StringComparison.OrdinalIgnoreCase))
                    {
                        rootAllowed = true;
                    }

                    if (trimmed.StartsWith("PasswordAuthentication no", StringComparison.OrdinalIgnoreCase))
                    {
                        passwordAuthAllowed = false;
                    }
                    else if (trimmed.StartsWith("PasswordAuthentication yes", StringComparison.OrdinalIgnoreCase))
                    {
                        passwordAuthAllowed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Konfigurationsdatei {file} übersprungen: {ex.Message}");
            }
        }

        if (!rootAllowed && !passwordAuthAllowed)
        {
            Logger.LogInfo("SSH-Server ist vollständig gehärtet.");
            return Task.FromResult(Pass(
                "SSH Server ist streng gehärtet.",
                $"Geprüfte Konfigurationsdateien: {string.Join(", ", inspectedFiles)}\n\n• Root-Login ist deaktiviert.\n• Passwort-Authentifizierung ist deaktiviert (nur Key-Auth erlaubt)."));
        }

        string warningDetails = "Der SSH-Server ('sshd') ist aktiv, aber nicht vollständig gehärtet:\n";
        if (rootAllowed) warningDetails += "• Root-Login ist möglicherweise erlaubt ('PermitRootLogin yes/unconfigured').\n";
        if (passwordAuthAllowed) warningDetails += "• Passwort-Login ist erlaubt ('PasswordAuthentication yes/unconfigured').\n";

        Logger.LogWarning("SSH-Server läuft mit potenziell schwacher Konfiguration!");
        return Task.FromResult(Warning(
            "SSH Server ist aktiv & unvollständig gehärtet!",
            warningDetails + $"\nGeprüfte Konfigurationsdateien: {string.Join(", ", inspectedFiles)}\n" +
            "Empfehlung: Nutze ausschließlich SSH-Keys und setze 'PermitRootLogin no' sowie 'PasswordAuthentication no' in /etc/ssh/sshd_config oder in /etc/ssh/sshd_config.d/."));
    }
}
