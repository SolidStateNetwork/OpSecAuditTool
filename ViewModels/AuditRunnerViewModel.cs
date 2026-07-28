using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpSecAuditTool.Core;
using OpSecAuditTool.Models;
using OpSecAuditTool.Services;
using OpSecAuditTool.Theme;

namespace OpSecAuditTool.ViewModels;

/// <summary>
/// Sub-ViewModel zur Koordination von parallelen Echtzeit-Audit-Läufen, Testergebnissen, Scoring und Multi-Format-Export.
/// </summary>
public sealed partial class AuditRunnerViewModel : ObservableObject
{
    [ObservableProperty] private string _auditScoreText = "0%";
    [ObservableProperty] private double _waterHeight = 0.0;

    [ObservableProperty] private string _statTotalText = "Total : 0";
    [ObservableProperty] private string _statPassedText = "0 bestanden";
    [ObservableProperty] private string _statWarningText = "0 Warnungen";
    [ObservableProperty] private string _statFailText = "0 Kritisch";

    [ObservableProperty] private string _welcomeRatingText = "Noch kein Audit durchgeführt.";
    [ObservableProperty] private bool _isAuditRunning = false;
    [ObservableProperty] private bool _hasAuditFinished = false;
    [ObservableProperty] private bool _areDetailsVisible = false;

    [ObservableProperty] private string _auditButtonText = "Audit Starten";
    [ObservableProperty] private string _auditButtonIcon = "M5 3l14 9-14 9V3z";

    [ObservableProperty] private string _toggleDetailsButtonText = "▼ Detailergebnisse anzeigen";

    public ObservableCollection<AuditResultItem> AuditResults { get; } = new();
    private readonly List<CheckResult> _rawResultsForExport = [];
    private CancellationTokenSource? _auditCts;

    [RelayCommand]
    public async Task StartAudit()
    {
        if (IsAuditRunning)
        {
            _auditCts?.Cancel();
            Logger.LogWarning("Abbruch des OpSec-Audits angefordert...");
            return;
        }

        IsAuditRunning = true;
        HasAuditFinished = false;
        AuditButtonText = "Audit Abbrechen";
        AuditButtonIcon = "M6 6l12 12 M6 18L18 6";
        Logger.LogInfo("Volles OpSec-Systemaudit gestartet (Parallele Echtzeit-Ausführung).");

        _auditCts = new CancellationTokenSource();
        CancellationToken token = _auditCts.Token;

        WaterHeight = 0;
        AuditScoreText = "0%";
        AuditResults.Clear();
        _rawResultsForExport.Clear();

        IOpSecChecker[] checkers = AuditCheckerCatalog.CreateAll();
        int total = checkers.Length;
        int passedCount = 0;
        int warningCount = 0;
        int failCount = 0;

        StatTotalText = $"Total : {total}";
        StatPassedText = "0 bestanden";
        StatWarningText = "0 Warnungen";
        StatFailText = "0 Kritisch";

        using var semaphore = new SemaphoreSlim(4);
        var tasks = checkers.Select(async checker =>
        {
            await semaphore.WaitAsync(token);
            try
            {
                if (token.IsCancellationRequested) return;

                CheckResult result;
                try
                {
                    result = await checker.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Fehler beim Ausführen des Checkers '{checker.GetType().Name}'", ex);
                    result = new CheckResult
                    {
                        Name = checker.Name,
                        Category = checker.Category,
                        Status = CheckStatus.Fail,
                        Summary = "Prüfung konnte nicht ausgeführt werden.",
                        Details = $"Interner Fehler: {ex.Message}"
                    };
                }

                if (token.IsCancellationRequested) return;

                lock (_rawResultsForExport)
                {
                    _rawResultsForExport.Add(result);
                    if (result.Status == CheckStatus.Pass) passedCount++;
                    else if (result.Status == CheckStatus.Warning) warningCount++;
                    else if (result.Status == CheckStatus.Fail) failCount++;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    AuditResults.Add(CreateAuditResultItem(result));
                    StatPassedText = $"{passedCount} bestanden";
                    StatWarningText = $"{warningCount} Warnungen";
                    StatFailText = $"{failCount} Kritisch";

                    int percent = total > 0 ? (passedCount * 100) / total : 0;
                    AuditScoreText = $"{percent}%";
                    WaterHeight = (150.0 * percent) / 100.0;
                });
            }
            finally
            {
                semaphore.Release();
            }
        });

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("OpSec-Audit wurde vom Benutzer abgebrochen.");
        }

        var sorted = AuditResults.OrderByDescending(item => item.Status.SortOrder()).ToList();
        AuditResults.Clear();
        foreach (var item in sorted)
        {
            AuditResults.Add(item);
        }

        int finalPercent = total > 0 ? (passedCount * 100) / total : 0;
        WelcomeRatingText = $"Letzter Audit-Status: {passedCount} von {total} Checks bestanden ({finalPercent}%)";

        HasAuditFinished = true;
        IsAuditRunning = false;

        AuditButtonText = "Audit wiederholen";
        AuditButtonIcon = "M23 4v6h-6 M1 20v-6h6 M3.51 9a9 9 0 0 1 14.85-3.36L23 10 M1 14l4.64 4.36A9 9 0 0 0 20.49 15";

        Logger.LogInfo($"Audit abgeschlossen: {finalPercent}% Sicherheits-Rating erreicht.");
    }

    [RelayCommand]
    public void ToggleDetails()
    {
        AreDetailsVisible = !AreDetailsVisible;
        ToggleDetailsButtonText = AreDetailsVisible ? "▲ Detailergebnisse ausblenden" : "▼ Detailergebnisse anzeigen";
    }

    [RelayCommand]
    public void ExportReport()
    {
        try
        {
            string filePath = AuditReportService.Save(_rawResultsForExport);
            Logger.LogInfo($"Audit-Bericht (TXT) gespeichert unter: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Speichern des TXT-Berichts", ex);
        }
    }

    [RelayCommand]
    public void ExportJsonReport()
    {
        try
        {
            string filePath = AuditReportService.SaveJson(_rawResultsForExport);
            Logger.LogInfo($"Audit-Bericht (JSON) gespeichert unter: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Speichern des JSON-Berichts", ex);
        }
    }

    [RelayCommand]
    public void ExportMarkdownReport()
    {
        try
        {
            string filePath = AuditReportService.SaveMarkdown(_rawResultsForExport);
            Logger.LogInfo($"Audit-Bericht (Markdown) gespeichert unter: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Speichern des Markdown-Berichts", ex);
        }
    }

    private static AuditResultItem CreateAuditResultItem(CheckResult result)
    {
        IBrush borderColor = result.Status switch
        {
            CheckStatus.Pass => UiPalette.Accent,
            CheckStatus.Warning => UiPalette.Warning,
            CheckStatus.Fail => UiPalette.Critical,
            _ => UiPalette.TextMuted
        };

        return new AuditResultItem
        {
            Category = result.Category,
            Name = result.Name,
            Summary = result.Summary,
            Details = result.Details,
            BorderColor = borderColor,
            Status = result.Status
        };
    }
}
