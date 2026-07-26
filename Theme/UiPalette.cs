using Avalonia.Media;

namespace OpSecAuditTool.Theme;

/// <summary>
/// Semantische UI-Farben für Werte, die zur Laufzeit im C#-Code gesetzt werden.
/// Die XAML-Oberflächen verwenden dieselben Hexwerte, damit Statusanzeigen,
/// Konsole und dynamisch erzeugte Elemente wie aus einem Guss wirken.
/// </summary>
public static class UiPalette
{
    public static readonly IBrush TextPrimary = Brush("#F1F5F2");
    public static readonly IBrush TextSecondary = Brush("#BEC8C1");
    public static readonly IBrush TextMuted = Brush("#8D9991");

    public static readonly IBrush Accent = Brush("#35E878");
    public static readonly IBrush AccentSoft = Brush("#7FE5A1");
    public static readonly IBrush Info = Brush("#53BDEA");
    public static readonly IBrush Warning = Brush("#F2B84B");
    public static readonly IBrush Error = Brush("#FF7781");
    public static readonly IBrush Critical = Brush("#FF5F6D");

    public static readonly IBrush OfflineBackground = Brush("#192A20");
    public static readonly IBrush OfflineBorder = Brush("#3D5C48");
    public static readonly IBrush OnlineBackground = Brush("#2B2418");
    public static readonly IBrush OnlineBorder = Brush("#705426");

    private static IBrush Brush(string value) =>
        new SolidColorBrush(Color.Parse(value));
}
