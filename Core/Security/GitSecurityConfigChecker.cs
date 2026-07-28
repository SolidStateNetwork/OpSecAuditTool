using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft, ob Git-Passwörter oder Access-Tokens unverschlüsselt in '~/.git-credentials'
/// abgelegt sind oder der unsichere Credential-Helper 'store' konfiguriert wurde.
/// </summary>
public sealed class GitSecurityConfigChecker : OpSecCheckerBase
{
    public override string Name => "Git-Credentials & Klartext-Passwort-Speicher Prüfung";
    public override string Category => "System / Härtung";
    public override bool CanFix => true;
    public override string FixDescription => "Sichert die Dateien ~/.gitconfig und ~/.git-credentials im Ordner 'Backups' und löscht anschließend unverschlüsselt gespeicherte Git-Credentials (~/.git-credentials).";

    public override Task<FixResult> FixAsync()
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string gitConfigPath = Path.Combine(homeDir, ".gitconfig");
            string gitCredentialsPath = Path.Combine(homeDir, ".git-credentials");

            bool fixedSomething = false;

            if (File.Exists(gitConfigPath))
            {
                BackupService.BackupFile(gitConfigPath);
            }

            if (File.Exists(gitCredentialsPath))
            {
                BackupService.BackupFile(gitCredentialsPath);
                File.Delete(gitCredentialsPath);
                fixedSomething = true;
            }

            return Task.FromResult(new FixResult
            {
                Success = true,
                Message = fixedSomething
                    ? "Klartext Git-Credentials (~/.git-credentials) wurden nach Backup gelöscht."
                    : "Keine unverschlüsselten Credentials zum Löschen gefunden."
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new FixResult
            {
                Success = false,
                Message = $"Fehler beim Härten der Git-Credentials: {ex.Message}"
            });
        }
    }

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der globalen Git-Konfiguration und Credential-Helper...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string gitConfigPath = Path.Combine(homeDir, ".gitconfig");
        string gitCredentialsPath = Path.Combine(homeDir, ".git-credentials");

        bool hasStoreHelper = false;
        bool hasPlaintextCredentialsFile = false;

        if (File.Exists(gitConfigPath))
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(gitConfigPath);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("#")) continue;

                    if (trimmed.Contains("credential.helper", StringComparison.OrdinalIgnoreCase) &&
                        trimmed.Contains("store", StringComparison.OrdinalIgnoreCase))
                    {
                        hasStoreHelper = true;
                        break;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        if (File.Exists(gitCredentialsPath))
        {
            try
            {
                string content = await File.ReadAllTextAsync(gitCredentialsPath);
                if (content.Contains("http://") || content.Contains("https://"))
                {
                    hasPlaintextCredentialsFile = true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignorieren, falls unlesbar
            }
        }

        if (hasStoreHelper || hasPlaintextCredentialsFile)
        {
            return Warning(
                "Git-Zugangsdaten im Klartext gefunden!",
                $"• 'credential.helper = store' konfiguriert: {(hasStoreHelper ? "JA" : "NEIN")}\n" +
                $"• Klartext-Datei '~/.git-credentials' vorhanden: {(hasPlaintextCredentialsFile ? "JA" : "NEIN")}\n\n" +
                "Empfehlung: Nutze einen verschlüsselten Credential-Manager (z. B. 'libsecret', 'gnome-keyring', 'keepassxc' oder SSH-Keys statt HTTP-Basic-Auth).");
        }

        return Pass(
            "Git-Credentials sind sicher konfiguriert.",
            "Es wurde weder eine unverschlüsselte '~/.git-credentials' Datei noch der unsichere 'store'-Helper gefunden.");
    }
}
