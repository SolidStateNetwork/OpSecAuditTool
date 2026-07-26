using Avalonia.Media;

namespace OpSecAuditTool.Theme;

/// <summary>
/// Semantische UI-Farben für Werte, die zur Laufzeit im C#-Code gesetzt werden.
/// Die XAML-Oberflächen verwenden dieselben Hexwerte, damit Statusanzeigen,
/// Konsole und dynamisch erzeugte Elemente wie aus einem Guss wirken.
/// </summary>
public static class UiPalette
{
    public static readonly IBrush TextPrimary = Brush("#F4F4F4");
    public static readonly IBrush TextSecondary = Brush("#C2C5C3");
    public static readonly IBrush TextMuted = Brush("#89908C");

    // Neon-Grün kennzeichnet ausschließlich Fokus, Auswahl und Aktionen.
    // Sekundäre Hervorhebungen bleiben bewusst neutral, damit längere Texte
    // ruhig lesbar sind und der Akzent nicht die gesamte Oberfläche einfärbt.
    public static readonly IBrush Accent = Brush("#00FF66");
    public static readonly IBrush AccentSoft = Brush("#D7D9D8");
    public static readonly IBrush Info = Brush("#48CAE4");
    public static readonly IBrush Warning = Brush("#FF9D00");
    public static readonly IBrush Error = Brush("#FF6363");
    public static readonly IBrush Critical = Brush("#FF2020");

    // Netzwerkmodus: Rot signalisiert den bewusst abgeschalteten Zugang,
    // Neon-Grün den freigegebenen Online-Modus.
    public static readonly IBrush OfflineBackground = Brush("#260A0A");
    public static readonly IBrush OfflineBorder = Brush("#FF2020");
    public static readonly IBrush OnlineBackground = Brush("#082014");
    public static readonly IBrush OnlineBorder = Brush("#00FF66");

    private static IBrush Brush(string value) =>
        new SolidColorBrush(Color.Parse(value));
}
