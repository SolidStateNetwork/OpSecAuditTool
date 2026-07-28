using Avalonia.Media;
using OpSecAuditTool.Theme;

namespace OpSecAuditTool.Models;

/// <summary>
/// Domain-Modell für den aktuellen Zustand dynamischer System- und Netzwerksicherheitsprüfungen.
/// </summary>
public sealed class SystemSecurityStatus
{
    public string PublicIp { get; set; } = "-";
    public string DnsServer { get; set; } = "-";
    public string LuksEncryption { get; set; } = "-";
    public string SwapStatus { get; set; } = "-";
    public string FirewallStatus { get; set; } = "-";
    public string TorRouting { get; set; } = "-";

    public IBrush PublicIpColor { get; set; } = UiPalette.TextPrimary;
    public IBrush DnsColor { get; set; } = UiPalette.TextPrimary;
    public IBrush DiskCryptColor { get; set; } = UiPalette.TextPrimary;
    public IBrush SwapColor { get; set; } = UiPalette.TextPrimary;
    public IBrush FirewallColor { get; set; } = UiPalette.TextPrimary;
    public IBrush TorColor { get; set; } = UiPalette.TextPrimary;
}
