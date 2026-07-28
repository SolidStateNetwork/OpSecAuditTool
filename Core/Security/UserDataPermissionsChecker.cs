using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft unter Linux die Zugriffsrechte besonders sensibler Benutzerdateien.
/// </summary>
public sealed class UserDataPermissionsChecker : IOpSecChecker
{
    public string Name => "Berechtigungsprüfung des Benutzerverzeichnisses";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Zugriffsrechte für sensible User-Ordner...");

        try
        {
            if (!OperatingSystem.IsLinux())
            {
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Nicht-Linux System übersprungen.",
                    Details = "Zugriffsrechte-Audit ist aktuell für Linux-Systeme optimiert."
                });
            }

            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var sensitivePaths = new[]
            {
                Path.Combine(homeDir, ".ssh"),
                Path.Combine(homeDir, ".gnupg"),
                Path.Combine(homeDir, ".mozilla"),
                Path.Combine(homeDir, ".config", "BraveSoftware"),
                Path.Combine(homeDir, ".config", "google-chrome")
            };

            var insecureDirectories = new List<string>();

            foreach (var path in sensitivePaths)
            {
                if (!Directory.Exists(path)) continue;

                if (IsGroupOrOthersReadable(path))
                {
                    insecureDirectories.Add(Path.GetFileName(path));
                }
            }

            if (insecureDirectories.Count > 0)
            {
                string detailsList = string.Join(", ", insecureDirectories);
                Logger.LogWarning($"Zu offene Zugriffsrechte auf folgenden Ordnern gefunden: {detailsList}");

                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Offene Zugriffsrechte auf sensiblen Ordnern entdeckt!",
                    Details = $"Folgende Ordner sind für andere Systembenutzer lesbar:\n• {string.Join("\n• ", insecureDirectories)}\n\n" +
                              "Empfehlung: Korrigiere die Rechte im Terminal (z. B. `chmod 700 ~/.ssh ~/.gnupg ~/.config/google-chrome`)."
                });
            }

            Logger.LogInfo("Alle vorhandenen sensiblen User-Ordner sind strikt geschützt.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Sensible User-Ordner sind korrekt geschützt.",
                Details = "Die Zugriffsrechte von `~/.ssh`, `~/.gnupg` und den Browser-Profilen sind strikt auf deinen Benutzer beschränkt."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Prüfung der User-Ordner Rechte", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Rechte-Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }

    [SupportedOSPlatform("linux")]
    private static bool IsGroupOrOthersReadable(string path)
    {
        try
        {
            var mode = File.GetUnixFileMode(path);

            bool groupAccess = mode.HasFlag(UnixFileMode.GroupRead) ||
                              mode.HasFlag(UnixFileMode.GroupWrite) ||
                              mode.HasFlag(UnixFileMode.GroupExecute);

            bool otherAccess = mode.HasFlag(UnixFileMode.OtherRead) ||
                              mode.HasFlag(UnixFileMode.OtherWrite) ||
                              mode.HasFlag(UnixFileMode.OtherExecute);

            return groupAccess || otherAccess;
        }
        catch
        {
            return false;
        }
    }
}
