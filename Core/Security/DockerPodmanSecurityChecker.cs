using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft Container-Sicherheit (Docker & Podman): Zugehörigkeit zur privilegierten
/// 'docker'-Gruppe und unverschlüsselt gespeicherte Registry-Tokens.
/// </summary>
public sealed class DockerPodmanSecurityChecker : OpSecCheckerBase
{
    public override string Name => "Container-Sicherheit (Docker / Podman Privilegien & Tokens)";
    public override string Category => "System / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der Docker- und Podman-Sicherheitskonfiguration...");

        if (!OperatingSystem.IsLinux())
        {
            return Warning(
                "Nicht-Linux System übersprungen.",
                "Die Prüfung der Linux-spezifischen Docker-Gruppenrechte ist für dieses Betriebssystem nicht anwendbar.");
        }

        bool inDockerGroup = false;
        var groupRes = await ShellCommandService.ExecuteAsync("id", "-Gn");
        if (groupRes.IsSuccess)
        {
            var groups = groupRes.StandardOutput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var g in groups)
            {
                if (string.Equals(g.Trim(), "docker", StringComparison.OrdinalIgnoreCase))
                {
                    inDockerGroup = true;
                    break;
                }
            }
        }

        bool hasPlaintextDockerAuth = false;
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] authFiles =
        {
            Path.Combine(homeDir, ".docker", "config.json"),
            Path.Combine(homeDir, ".config", "containers", "auth.json")
        };

        string foundAuthFile = string.Empty;
        foreach (var file in authFiles)
        {
            if (!File.Exists(file)) continue;
            try
            {
                string content = await File.ReadAllTextAsync(file);
                if (content.Contains("\"auths\"") && content.Contains("\"auth\":") && !content.Contains("\"credsStore\""))
                {
                    hasPlaintextDockerAuth = true;
                    foundAuthFile = Path.GetFileName(file);
                    break;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignorieren, falls kein Lesezugriff
            }
        }

        if (inDockerGroup && hasPlaintextDockerAuth)
        {
            return Warning(
                "Kritisches Risiko: Docker-Gruppenrechte & Klartext-Registry-Tokens entdeckt!",
                $"• Dein Benutzer ist Mitglied der Gruppe 'docker' (ermöglicht Root-Eskalation ohne Passwort!).\n" +
                $"• In der Konfiguration '{foundAuthFile}' wurden Registry-Tokens ohne Credential-Store entdeckt.\n\n" +
                "Empfehlung: Nutze Rootless-Docker/Podman und konfiguriere einen sicheren credsStore (z. B. 'secretservice' oder 'pass').");
        }

        if (inDockerGroup)
        {
            return Warning(
                "Benutzer ist Mitglied der privilegierten 'docker'-Gruppe.",
                "Mitgliedschaft in der 'docker'-Gruppe gewährt auf Linux-Systemen im Wesentlichen passwortlosen Root-Zugriff " +
                "durch das Einbinden des Root-Dateisystems in Container.\n\n" +
                "Empfehlung: Setze nach Möglichkeit auf Rootless-Docker oder Podman im User-Space.");
        }

        if (hasPlaintextDockerAuth)
        {
            return Warning(
                $"Unverschlüsselte Container-Registry-Tokens in {foundAuthFile} gefunden.",
                $"Die Datei '{foundAuthFile}' enthält Base64-codierte Authentifizierungstokens ohne Credential-Store.\n\n" +
                "Empfehlung: Konfiguriere einen 'credsStore' in deiner Docker-/Podman-Config.");
        }

        return Pass(
            "Container-Sicherheitsprüfung bestanden.",
            "Keine privilegierte Mitgliedschaft in der 'docker'-Gruppe und keine ungeschützten Registry-Tokens gefunden.");
    }
}
