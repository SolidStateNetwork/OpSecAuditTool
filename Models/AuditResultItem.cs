using System;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpSecAuditTool.Core;
using OpSecAuditTool.Theme;

namespace OpSecAuditTool.Models;

/// <summary>
/// Für die Oberfläche aufbereitete Darstellung eines einzelnen Prüfergebnisses inkl. optionalem Quick-Fix.
/// Das fachliche Ergebnis selbst bleibt in <see cref="CheckResult"/>.
/// </summary>
public sealed partial class AuditResultItem : ObservableObject
{
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public IBrush BorderColor { get; init; } = UiPalette.TextMuted;
    public CheckStatus Status { get; init; }
    public bool CanFix { get; init; }
    public string FixDescription { get; init; } = string.Empty;
    public IOpSecChecker? Checker { get; init; }

    [ObservableProperty] private bool _isFixing;
    [ObservableProperty] private string _fixStatusText = string.Empty;
    [ObservableProperty] private bool _fixSuccess;

    [RelayCommand]
    public async Task ExecuteFixAsync()
    {
        if (Checker == null || !CanFix || IsFixing) return;
        IsFixing = true;
        FixStatusText = "Führe Sofort-Härtung aus...";
        try
        {
            var res = await Checker.FixAsync();
            FixSuccess = res.Success;
            FixStatusText = res.Message;
        }
        catch (Exception ex)
        {
            FixSuccess = false;
            FixStatusText = $"Fehler beim Fix: {ex.Message}";
        }
        finally
        {
            IsFixing = false;
        }
    }

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
