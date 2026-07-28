using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Analysiert benutzerdefinierte Autostart-Programme (~/.config/autostart/) sowie
/// User-Space Systemd-Dienste (~/.config/systemd/user/), die für Persistence genutzt werden können.
/// </summary>
public sealed class UserAutostartChecker : OpSecCheckerBase
{
    public override string Name => "User-Space Autostart & Persistence-Analyse";
    public override string Category => "System / Härtung";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der User-Space Autostart-Verzeichnisse...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string autostartDir = Path.Combine(homeDir, ".config", "autostart");
        string userSystemdDir = Path.Combine(homeDir, ".config", "systemd", "user");

        var autostartItems = new List<string>();
        var userSystemdItems = new List<string>();

        if (Directory.Exists(autostartDir))
        {
            try
            {
                var desktopFiles = Directory.GetFiles(autostartDir, "*.desktop");
                foreach (var f in desktopFiles)
                {
                    autostartItems.Add(Path.GetFileName(f));
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        if (Directory.Exists(userSystemdDir))
        {
            try
            {
                var systemdFiles = Directory.GetFiles(userSystemdDir, "*.*")
                    .Where(f => f.EndsWith(".service") || f.EndsWith(".timer") || f.EndsWith(".target"))
                    .ToList();
                foreach (var f in systemdFiles)
                {
                    userSystemdItems.Add(Path.GetFileName(f));
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        int totalCount = autostartItems.Count + userSystemdItems.Count;

        if (totalCount > 0)
        {
            var details = new List<string>();
            if (autostartItems.Count > 0)
            {
                details.Add($"⚡ Desktop Autostarts (~/.config/autostart/):\n  • {string.Join("\n  • ", autostartItems)}");
            }
            if (userSystemdItems.Count > 0)
            {
                if (details.Count > 0) details.Add("");
                details.Add($"⚙️ User Systemd-Dienste (~/.config/systemd/user/):\n  • {string.Join("\n  • ", userSystemdItems)}");
            }

            return Task.FromResult(Warning(
                $"{totalCount} User-Space Autostart-Eintrag/Einträge aktiv.",
                string.Join("\n", details) +
                "\n\nHinweis: Auch ohne Sudo/Root-Rechte können Programme über diese User-Verzeichnisse " +
                "automatisch beim Login gestartet werden. Prüfe, ob alle gelisteten Einträge von dir erwünscht sind."));
        }

        return Task.FromResult(Pass(
            "Keine benutzerdefinierten Autostart-Einträge oder User-Systemd-Units gefunden.",
            "In '~/.config/autostart/' und '~/.config/systemd/user/' befinden sich keine aktiven Persistence-Skripte oder Dienste."));
    }
}
