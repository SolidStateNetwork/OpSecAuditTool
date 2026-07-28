using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Untersucht bekannte Shell-History-Dateien auf persistente Befehlsverläufe.
/// Nutzt asynchrones Streaming (OOM-resistent) und moderne Developer/AI-Keywords.
/// </summary>
public sealed class ShellHistoryChecker : OpSecCheckerBase
{
    public override string Name => "Prüfung der Shell-Historie auf sensible Daten";
    public override string Category => "Anti-Forensik / Hygiene";
    public override bool CanFix => true;
    public override string FixDescription => "Sichert die aktuellen History-Dateien (.bash_history, .zsh_history) im Ordner 'Backups' und leert deren Inhalte, um sensible Befehle zu löschen.";

    public override async Task<FixResult> FixAsync()
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] historyFiles =
            {
                Path.Combine(homeDir, ".bash_history"),
                Path.Combine(homeDir, ".zsh_history")
            };

            int clearedCount = 0;
            foreach (var file in historyFiles)
            {
                if (File.Exists(file))
                {
                    BackupService.BackupFile(file);
                    await File.WriteAllTextAsync(file, string.Empty);
                    clearedCount++;
                }
            }

            return new FixResult
            {
                Success = true,
                Message = $"{clearedCount} Shell-History-Datei(en) wurden gesichert und geleert."
            };
        }
        catch (Exception ex)
        {
            return new FixResult
            {
                Success = false,
                Message = $"Fehler beim Leeren der Shell-History: {ex.Message}"
            };
        }
    }

    private static readonly string[] SensitiveKeywords =
    {
        "api_key", "apikey",
        "password=", "passwd=",
        "bearer ", "token=",
        "private_key", "-----BEGIN",
        "sudo -S", "curl -u",
        "export AWS_ACCESS_KEY_ID=", "export AWS_SECRET_ACCESS_KEY=",
        "export OPENAI_API_KEY=", "export GEMINI_API_KEY=",
        "mysql -u", "pg_dump", "sshpass", "docker login", "npm login"
    };

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte speichereffiziente Prüfung der Shell-Historien...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var historyFiles = new List<string>
        {
            Path.Combine(homeDir, ".bash_history"),
            Path.Combine(homeDir, ".zsh_history"),
            Path.Combine(homeDir, ".local", "share", "fish", "fish_history")
        };

        if (OperatingSystem.IsWindows())
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            historyFiles.Add(Path.Combine(
                appData, "Microsoft", "Windows", "PowerShell", "PSReadLine", "ConsoleHost_history.txt"));
            historyFiles.Add(Path.Combine(
                homeDir, "AppData", "Roaming", "Microsoft", "Windows", "PowerShell", "PSReadLine", "Visual Studio Code Host_history.txt"));
        }

        int totalFound = 0;
        var detectedDetails = new List<string>();

        foreach (var file in historyFiles)
        {
            if (!File.Exists(file)) continue;

            Logger.LogTrace($"Prüfe History-Datei: {Path.GetFileName(file)}");

            int matchCountForFile = 0;
            int linesRead = 0;
            const int maxLinesToScan = 15000;

            try
            {
                await foreach (string line in File.ReadLinesAsync(file))
                {
                    linesRead++;
                    if (linesRead > maxLinesToScan) break;

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    for (int i = 0; i < SensitiveKeywords.Length; i++)
                    {
                        if (line.Contains(SensitiveKeywords[i], StringComparison.OrdinalIgnoreCase))
                        {
                            matchCountForFile++;
                            break;
                        }
                    }

                    // Begrenze auf maximal 50 relevante Treffer pro Datei für performante Zählung
                    if (matchCountForFile >= 50) break;
                }

                if (matchCountForFile > 0)
                {
                    totalFound += matchCountForFile;
                    detectedDetails.Add($"• {Path.GetFileName(file)}: {matchCountForFile} verdächtige Eintrag/Einträge");
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Konnte {file} nicht vollständig parsen: {ex.Message}");
            }
        }

        if (totalFound > 0)
        {
            string summaryDetails = string.Join("\n", detectedDetails);
            Logger.LogWarning($"Verdächtige Einträge in der Shell-Historie gefunden ({totalFound} Treffer).");

            return Warning(
                $"{totalFound} potenzielle Zugangsdaten/Tokens in Shell-History!",
                $"In folgenden Dateien wurden sensible Muster (API-Keys, AWS/AI-Tokens, Passwörter) gefunden:\n{summaryDetails}\n\n" +
                "Hinweis: Bereinige deine Shell-History oder setze 'HISTSIZE=0' / 'HISTFILESIZE=0' in sensitiven Terminals für maximale Anonymität.");
        }

        Logger.LogInfo("Keine bekannten Passwörter oder API-Tokens in den Shell-Historien gefunden.");
        return Pass(
            "Shell-Historien sind frei von typischen Zugangsdaten.",
            "Die durchsuchten History-Dateien (bash/zsh/fish/PowerShell) wiesen keine verdächtigen Befehlsmuster mit Zugangsdaten auf.");
    }
}
