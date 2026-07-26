using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Untersucht bekannte Shell-History-Dateien auf persistente Befehlsverläufe.
/// </summary>
public sealed class ShellHistoryChecker : IOpSecChecker
{
    public string Name => "Prüfung der Shell-Historie auf sensible Daten";
    public string Category => "Anti-Forensik / Hygiene";

    private readonly string[] _sensitiveKeywords = new[]
    {
        "api_key", "apikey",
        "password=", "passwd=",
        "bearer ", "token=",
        "private_key", "-----BEGIN",
        "sudo -S", "curl -u"
    };

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Shell-Historien...");

        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var historyFiles = new List<string>
            {
                Path.Combine(homeDir, ".bash_history"),
                Path.Combine(homeDir, ".zsh_history"),
                Path.Combine(homeDir, ".local", "share", "fish", "fish_history")
            };
            if (OperatingSystem.IsWindows())
            {
                string appData = Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);
                historyFiles.Add(Path.Combine(
                    appData,
                    "Microsoft",
                    "Windows",
                    "PowerShell",
                    "PSReadLine",
                    "ConsoleHost_history.txt"));
                historyFiles.Add(Path.Combine(
                    homeDir,
                    "AppData",
                    "Roaming",
                    "Microsoft",
                    "Windows",
                    "PowerShell",
                    "PSReadLine",
                    "Visual Studio Code Host_history.txt"));
            }

            int totalFound = 0;
            var detectedDetails = new List<string>();

            foreach (var file in historyFiles)
            {
                if (!File.Exists(file)) continue;

                Logger.LogTrace($"Prüfe History-Datei: {Path.GetFileName(file)}");
                var lines = File.ReadAllLines(file);

                var matches = lines
                    .Where(line => _sensitiveKeywords.Any(kw => line.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                    .Distinct()
                    .Take(5)
                    .ToList();

                if (matches.Count > 0)
                {
                    totalFound += matches.Count;
                    detectedDetails.Add($"• {Path.GetFileName(file)}: {matches.Count} verdächtige Eintrag/Einträge");
                }
            }

            if (totalFound > 0)
            {
                string summaryDetails = string.Join("\n", detectedDetails);
                Logger.LogWarning($"Verdächtige Einträge in der Shell-Historie gefunden ({totalFound} Treffer).");

                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"{totalFound} potenzielle Zugangsdaten/Tokens in Shell-History!",
                    Details = $"In folgenden Dateien wurden sensible Muster gefunden:\n{summaryDetails}\n\n" +
                              "Hinweis: Bereinige deine Shell-History oder setze 'HISTSIZE=0' / 'HISTFILESIZE=0' für maximale Anonymität."
                });
            }

            Logger.LogTrace("Keine sensiblen Klartext-Muster in den Shell-Historien gefunden.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Shell-Historien sind unauffällig / sauber.",
                Details = "In den vorhandenen Bash-, Zsh-, Fish- und PowerShell-Historien wurden keine typischen Klartext-Passwörter oder API-Keys entdeckt."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Prüfen der Shell-Historie", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Shell-History Check fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
