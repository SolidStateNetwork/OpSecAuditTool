using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Sucht in lesbaren sudoers-Regeln nach kennwortlosen Freigaben. Nicht lesbare
/// Konfigurationen werden als unbekannt und nicht als bestanden gewertet.
/// </summary>
public sealed class SudoersChecker : IOpSecChecker
{
    public string Name => "Sudo-Berechtigungen und NOPASSWD-Regeln";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Audit der Sudoers-Konfiguration...");

        try
        {
            const string sudoersFile = "/etc/sudoers";
            const string sudoersDDir = "/etc/sudoers.d";
            var matchedFiles = new List<string>();

            if (File.Exists(sudoersFile) && ContainsNoPasswdRule(sudoersFile))
            {
                matchedFiles.Add(sudoersFile);
            }

            if (Directory.Exists(sudoersDDir))
            {
                foreach (string file in Directory.GetFiles(sudoersDDir))
                {
                    if (ContainsNoPasswdRule(file))
                    {
                        matchedFiles.Add(Path.Combine(sudoersDDir, Path.GetFileName(file)));
                    }
                }
            }

            if (matchedFiles.Count > 0)
            {
                Logger.LogWarning("NOPASSWD-Regel in der Sudoers-Konfiguration gefunden.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = $"NOPASSWD-Regeln in {matchedFiles.Count} Sudoers-Datei(en) erkannt.",
                    Details = $"Betroffene Dateien:\n• {string.Join("\n• ", matchedFiles)}\n\n" +
                              "Die konkreten Regeln werden aus Datenschutzgründen nicht in Bericht oder Log übernommen. Prüfe, ob jede kennwortlose Ausnahme zwingend erforderlich und auf einzelne Befehle begrenzt ist."
                });
            }

            Logger.LogInfo("In den lesbaren Sudoers-Dateien wurden keine NOPASSWD-Regeln erkannt.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Keine NOPASSWD-Regeln in den lesbaren Sudoers-Dateien gefunden.",
                Details = "Die lesbaren Dateien `/etc/sudoers` und `/etc/sudoers.d/` enthalten keine aktiven NOPASSWD-Einträge."
            });
        }
        catch (UnauthorizedAccessException)
        {
            Logger.LogWarning("Sudoers-Konfiguration ist ohne erhöhte Rechte nicht vollständig lesbar.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Sudoers-Konfiguration konnte nicht vollständig geprüft werden.",
                Details = "Mindestens eine Sudoers-Datei ist für den aktuellen Benutzer nicht lesbar. Ihr Schutz ist sinnvoll, erlaubt aber keine Aussage darüber, ob NOPASSWD-Regeln enthalten sind."
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
                Summary = "Sudoers-Audit fehlgeschlagen.",
                Details = ex.Message
            });
        }
    }

    private static bool ContainsNoPasswdRule(string filePath)
    {
        foreach (string line in File.ReadLines(filePath))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.Contains("NOPASSWD", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
