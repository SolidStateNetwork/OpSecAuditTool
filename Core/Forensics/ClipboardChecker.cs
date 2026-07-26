using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Forensics;

/// <summary>
/// Bewertet den aktuellen Inhalt der Zwischenablage auf sensible Datenmuster.
/// </summary>
public sealed class ClipboardChecker : IOpSecChecker
{
    public string Name => "Prüfung der Zwischenablage auf sensible Daten";
    public string Category => "Anti-Forensik / Hygiene";

    private readonly string[] _sensitivePatterns = new[]
    {
        "-----BEGIN PGP PRIVATE KEY BLOCK-----",
        "-----BEGIN OPENSSH PRIVATE KEY-----",
        "-----BEGIN RSA PRIVATE KEY-----",
        "eyJ",
        "xprv",
        "nsec1"
    };

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der System-Zwischenablage...");

        try
        {
            Window? mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (mainWindow == null)
            {
                Logger.LogWarning("Hauptfenster für Clipboard-Zugriff nicht gefunden.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Clipboard nicht erreichbar.",
                    Details = "Das Hauptfenster konnte nicht für den Clipboard-Zugriff referenziert werden."
                };
            }

            IClipboard? clipboard = TopLevel.GetTopLevel(mainWindow)?.Clipboard;

            if (clipboard == null)
            {
                Logger.LogInfo("Kein Zugriff auf das Clipboard-Interface möglich.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Clipboard nicht erreichbar.",
                    Details = "Das System-Clipboard konnte über Avalonia nicht abgefragt werden."
                };
            }

            string? clipboardText = await clipboard.TryGetTextAsync();

            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                Logger.LogInfo("Zwischenablage ist leer.");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Zwischenablage ist leer.",
                    Details = "Es befinden sich aktuell keine Textdaten im Clipboard."
                };
            }

            bool containsSensitiveData = _sensitivePatterns.Any(pattern =>
                clipboardText.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            if (containsSensitiveData)
            {
                Logger.LogWarning("Sensible Daten (z. B. PGP/SSH Private Key oder Token) im Clipboard entdeckt!");
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Fail,
                    Summary = "KRITISCH: Privater Schlüssel / Token liegt im Clipboard!",
                    Details = "In deiner Zwischenablage befindet sich aktuell ein unverschlüsselter Private Key oder Session-Token!\n\n" +
                              "Hinweis: Leere dein Clipboard sofort (oder nutze einen Passwort-Manager mit Auto-Clear Funktion)."
                };
            }

            if (clipboardText.Length > 500)
            {
                return new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Großer Textblock im Clipboard gespeichert.",
                    Details = $"Derzeit liegen {clipboardText.Length} Zeichen unverschlüsselt in der Zwischenablage.\n\n" +
                              "Hinweis: Leere das Clipboard nach sensiblen Kopiervorgängen."
                };
            }

            Logger.LogTrace("Zwischenablage enthält keinen erkannten Private Key.");
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Pass,
                Summary = "Zwischenablage enthält keine offensichtlichen Secrets.",
                Details = "Es wurden keine SSH/PGP Private Keys oder bekannten Token-Muster im Clipboard gefunden."
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Clipboard-Prüfung", ex);
            return new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Clipboard Check fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            };
        }
    }
}
