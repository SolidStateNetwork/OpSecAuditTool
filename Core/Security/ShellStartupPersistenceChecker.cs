using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Sucht in den Benutzer-Shell-Konfigurationsdateien (~/.bashrc, ~/.zshrc, ~/.profile etc.)
/// nach verdächtigen Mustern wie LD_PRELOAD-Injektionen oder versteckten Aliases.
/// </summary>
public sealed class ShellStartupPersistenceChecker : OpSecCheckerBase
{
    public override string Name => "Shell-Profile & Alias-Persistence Prüfung";
    public override string Category => "System / Härtung";

    private static readonly string[] SuspiciousPatterns =
    {
        "LD_PRELOAD=",
        "curl -s http",
        "curl -s https",
        "wget -qO-",
        "eval $(echo",
        "alias sudo=",
        "alias ssh=",
        "alias su="
    };

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der Shell-Konfigurationsdateien auf Persistence...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] candidateFiles =
        {
            Path.Combine(homeDir, ".bashrc"),
            Path.Combine(homeDir, ".bash_profile"),
            Path.Combine(homeDir, ".zshrc"),
            Path.Combine(homeDir, ".profile"),
            Path.Combine(homeDir, ".config", "fish", "config.fish")
        };

        var findings = new List<string>();

        foreach (var file in candidateFiles)
        {
            if (!File.Exists(file)) continue;

            try
            {
                var lines = await File.ReadAllLinesAsync(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;

                    foreach (var pattern in SuspiciousPatterns)
                    {
                        if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            findings.Add($"{Path.GetFileName(file)} (Zeile {i + 1}): Muster '{pattern}' entdeckt");
                            break;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        if (findings.Count > 0)
        {
            return Warning(
                $"{findings.Count} verdächtige(s) Muster in Shell-Startdateien entdeckt!",
                $"Folgende Einträge enthalten potenziell kritische Befehle (z. B. LD_PRELOAD, Remote-Pipes oder Alias-Überschreibungen):\n• {string.Join("\n• ", findings)}\n\n" +
                "Empfehlung: Prüfe die jeweiligen Zeilen in deinen Shell-Profilen auf unerwünschte Modifikationen oder Malware-Reste.");
        }

        return Pass(
            "Keine verdächtigen Persistence-Muster in Shell-Startdateien gefunden.",
            "Die Shell-Profile (~/.bashrc, ~/.zshrc, ~/.profile etc.) sind frei von verdächtigen Injektionen und Alias-Überschreibungen.");
    }
}
