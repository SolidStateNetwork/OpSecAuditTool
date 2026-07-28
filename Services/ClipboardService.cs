using System;
using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace OpSecAuditTool.Services;

/// <summary>
/// Dienst für Interaktionen mit der System-Zwischenablage.
/// </summary>
public static class ClipboardService
{
    public static async Task CopyToClipboardAsync(string text, string context)
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            IClipboard? clipboard = lifetime?.MainWindow?.Clipboard;

            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                Logger.LogInfo($"{context} erfolgreich in die Zwischenablage kopiert.");
            }
            else
            {
                Logger.LogWarning("Clipboard-Interface konnte nicht ermittelt werden.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Fehler beim Kopieren von {context}", ex);
        }
    }
}
