using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft, ob auf Linux-Systemen das moderne, isolierte Wayland-Protokoll oder das
/// ältere X11-Protokoll läuft, welches globales Keylogging & Screen-Capture erlaubt.
/// </summary>
public sealed class DisplayServerSecurityChecker : OpSecCheckerBase
{
    public override string Name => "X11 vs. Wayland Display-Server Sicherheit";
    public override string Category => "System / Härtung";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung des grafischen Display-Servers (X11 / Wayland)...");

        if (!OperatingSystem.IsLinux())
        {
            return Task.FromResult(Warning(
                "Nicht-Linux System übersprungen.",
                "Die Prüfung der X11/Wayland-Protokollisolation ist nur unter Linux relevant."));
        }

        string? sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.Trim();
        string? waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")?.Trim();
        string? xDisplay = Environment.GetEnvironmentVariable("DISPLAY")?.Trim();

        bool isWayland = string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase) ||
                         !string.IsNullOrEmpty(waylandDisplay);

        bool isX11 = string.Equals(sessionType, "x11", StringComparison.OrdinalIgnoreCase) ||
                     (!isWayland && !string.IsNullOrEmpty(xDisplay));

        if (isWayland)
        {
            return Task.FromResult(Pass(
                "Sicherer Wayland Display-Server ist aktiv.",
                "Unter Wayland sind grafische Anwendungen stark voneinander isoliert (kein unbeaufsichtigtes globales Keylogging oder Screen-Scraping möglich)."));
        }

        if (isX11)
        {
            return Task.FromResult(Warning(
                "Grafischer X11 Display-Server aktiv (keine Fenster-Isolation).",
                "Das X11-Protokoll gestattet standardmäßig jeder grafischen Anwendung, Tastatureingaben " +
                "anderer Programme abzufangen (Keylogging) und den gesamten Bildschirm mitzuschneiden.\n\n" +
                "Empfehlung: Nutze nach Möglichkeit Wayland als Standard-Sitzung in deinem Display-Manager."));
        }

        return Task.FromResult(Pass(
            "Kein grafischer X11-Server erkannt (CLI / Headless System).",
            "Weder ein X11- noch ein Wayland-Sitzungstyp ist aktiv. Headless-/Terminal-Umgebungen sind vor grafischem Keylogging geschützt."));
    }
}
