using Avalonia.Media;
using OpSecAuditTool.Core;

namespace OpSecAuditTool.Models;

/// <summary>
/// Für die Oberfläche aufbereitete Darstellung eines einzelnen Prüfergebnisses.
/// Das fachliche Ergebnis selbst bleibt in <see cref="CheckResult"/>.
/// </summary>
public sealed class AuditResultItem
{
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public IBrush BorderColor { get; init; } = Brushes.Gray;
    public CheckStatus Status { get; init; }

    /// <summary>
    /// Kurzes, sprachlich einheitliches Label für die kompakte Ergebniskarte.
    /// </summary>
    public string StatusLabel => Status switch
    {
        CheckStatus.Pass => "BESTANDEN",
        CheckStatus.Warning => "WARNUNG",
        CheckStatus.Fail => "KRITISCH",
        _ => "UNBEKANNT"
    };
}
