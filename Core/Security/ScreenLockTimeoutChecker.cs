using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft, ob die automatische Bildschirmsperre (Lock-Screen) aktiviert und das Idle-Timeout
/// auf eine sichere Zeitspanne (max. 15 Minuten) eingestellt ist, um physischen Zugriff zu erschweren.
/// </summary>
public sealed class ScreenLockTimeoutChecker : OpSecCheckerBase
{
    public override string Name => "Bildschirm-Sperre & Idle-Timeout Prüfung";
    public override string Category => "System / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der Bildschirm-Sperre und Idle-Timeouts...");

        if (!OperatingSystem.IsLinux())
        {
            return Warning(
                "Nicht-Linux System übersprungen.",
                "Die Prüfung der gsettings/KDE Bildschirmsperre ist für dieses System nicht anwendbar.");
        }

        // 1. Prüfe via gsettings (GNOME / Cinnamon / MATE / Budgie)
        var lockRes = await ShellCommandService.ExecuteAsync("gsettings", "get org.gnome.desktop.screensaver lock-enabled");
        var delayRes = await ShellCommandService.ExecuteAsync("gsettings", "get org.gnome.desktop.session idle-delay");

        if (lockRes.IsSuccess && delayRes.IsSuccess)
        {
            bool lockEnabled = lockRes.StandardOutput.Trim().Contains("true", StringComparison.OrdinalIgnoreCase);
            string delayStr = delayRes.StandardOutput.Trim().Replace("uint32", "").Replace("'", "").Trim();
            int.TryParse(delayStr, out int delaySeconds);

            if (!lockEnabled || delaySeconds == 0 || delaySeconds > 900)
            {
                return Warning(
                    "Automatische Bildschirmsperre ist deaktiviert oder das Timeout ist zu hoch konfiguriert.",
                    $"• Bildschirmsperre aktiv: {(lockEnabled ? "JA" : "NEIN")}\n" +
                    $"• Idle-Timeout bis Sperre: {(delaySeconds == 0 ? "Nie (0 Sekunden)" : $"{delaySeconds} Sekunden")}\n\n" +
                    "Empfehlung: Aktiviere die automatische Bildschirmsperre und wähle ein Timeout von maximal 10–15 Minuten (600–900 Sekunden).");
            }

            return Pass(
                "Automatische Bildschirmsperre ist sicher und aktiv konfiguriert.",
                $"Sperre: AKTIV, Idle-Timeout: {delaySeconds} Sekunden (unter 15 Minuten).");
        }

        // 2. Fallback: Prüfe KDE kscreenlockerrc
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string kdeConfig = Path.Combine(homeDir, ".config", "kscreenlockerrc");
        if (File.Exists(kdeConfig))
        {
            try
            {
                string content = await File.ReadAllTextAsync(kdeConfig);
                if (content.Contains("Autolock=false", StringComparison.OrdinalIgnoreCase))
                {
                    return Warning(
                        "KDE Bildschirmsperre (Autolock) ist in kscreenlockerrc deaktiviert.",
                        "Die automatische Sitzungssperre wurde für diese KDE-Umgebung abgeschaltet.");
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        return Pass(
            "Kein unsicheres Bildschirmsperren-Timeout aufgedeckt.",
            "Weder in 'gsettings' noch in den KDE-Konfigurationen wurde eine deaktivierte Bildschirmsperre erkannt.");
    }
}
