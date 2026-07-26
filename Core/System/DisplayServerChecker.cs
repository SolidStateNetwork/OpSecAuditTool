using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Erkennt die Linux-Grafiksitzung und bewertet X11 gegenüber Wayland.
/// </summary>
public sealed class DisplayServerChecker : IOpSecChecker
{
    public string Name => "Display-Server-Isolierung (X11 vs. Wayland)";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des aktiven Display-Servers...");

        try
        {
            string? sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
            string? waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

            bool isWayland = (sessionType != null && sessionType.Equals("wayland", StringComparison.OrdinalIgnoreCase)) ||
                             !string.IsNullOrEmpty(waylandDisplay);

            if (isWayland)
            {
                Logger.LogInfo("Wayland Session erkannt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Wayland-Sitzung aktiv (Fenster-Isolierung gewahrt).",
                    Details = "Das System nutzt Wayland als Display-Server. Prozesse sind gegeneinander isoliert; globales Keylogging und unbefugte Screenshots durch Hintergrund-Apps werden unterbunden."
                });
            }

            Logger.LogWarning("X11 Session erkannt!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "X11-Sitzung aktiv (Eingeschränkte Fenster-Isolierung)!",
                Details = $"Aktueller Sitzungstyp: '{sessionType ?? "X11"}'.\n\n" +
                          "Hinweis: Unter X11 können alle Programme desselben Benutzers Tastenschläge anderer Fenster mitlesen oder Bildschirmfotos anfertigen. Wechsel bei vertraulichen Aufgaben zu Wayland."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Display-Server-Audit", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Display Server Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
