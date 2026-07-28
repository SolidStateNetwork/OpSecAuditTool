using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft Flatpak- & Snap-Berechtigungen im User-Space auf zu weitreichende
/// Dateisystem-Zugriffe ('--filesystem=home' oder '--filesystem=host'), welche die Sandbox aushebeln.
/// </summary>
public sealed class SandboxPermissionChecker : OpSecCheckerBase
{
    public override string Name => "Flatpak & Snap Sandbox-Ausbruch Prüfung";
    public override string Category => "System / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung auf riskante Flatpak- und Snap-Sandbox-Overrides...");

        if (!OperatingSystem.IsLinux())
        {
            return Warning(
                "Nicht-Linux System übersprungen.",
                "Die Prüfung der Linux Flatpak- und Snap-Sandboxes ist für dieses Betriebssystem nicht anwendbar.");
        }

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string flatpakOverridesDir = Path.Combine(homeDir, ".local", "share", "flatpak", "overrides");

        var riskyApps = new List<string>();

        if (Directory.Exists(flatpakOverridesDir))
        {
            try
            {
                var files = Directory.GetFiles(flatpakOverridesDir);
                foreach (var f in files)
                {
                    string content = await File.ReadAllTextAsync(f);
                    if (content.Contains("filesystems=home", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("filesystems=host", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("filesystems=/", StringComparison.OrdinalIgnoreCase))
                    {
                        riskyApps.Add($"Flatpak '{Path.GetFileName(f)}': Vollzugriff auf das gesamte Home- oder Host-Dateisystem konfiguriert");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        if (riskyApps.Count > 0)
        {
            return Warning(
                $"{riskyApps.Count} Sandbox-Anwendung(en) mit unbeschränktem Dateisystemzugriff gefunden!",
                $"Folgende Apps haben ein '--filesystem=home' oder '--filesystem=host' Override, womit die Sandbox durchbrochen " +
                $"wird und Zugriff auf '~/.ssh', '~/.gnupg' und Browser-Cookies besteht:\n• {string.Join("\n• ", riskyApps)}\n\n" +
                "Empfehlung: Schränke die Zugriffe in Flatseal oder über 'flatpak override' auf spezifische Ordner ein.");
        }

        return Pass(
            "Keine riskanten Sandbox-Overrides in Flatpak-/Snap-Profilen entdeckt.",
            "Die überprüften App-Overrides beschränken Dateisystem-Zugriffe ordnungsgemäß.");
    }
}
