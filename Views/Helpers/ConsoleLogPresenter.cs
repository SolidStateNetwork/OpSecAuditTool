using System;
using System.Collections.Concurrent;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using OpSecAuditTool.Models;
using OpSecAuditTool.Services;
using OpSecAuditTool.Theme;
using OpSecAuditTool.ViewModels;

namespace OpSecAuditTool.Views.Helpers;

/// <summary>
/// Kapselt die Präsentations- und Auto-Scroll-Logik für die farbige Live-Konsole,
/// um die Haupt-Code-Behind-Datei (MainWindow.axaml.cs) zu entlasten.
/// </summary>
public sealed class ConsoleLogPresenter : IDisposable
{
    private static readonly IBrush TimestampBrush = UiPalette.TextMuted;
    private static readonly IBrush TraceBrush = CreateBrush("#9CA29F");
    private static readonly IBrush InfoBrush = UiPalette.Info;
    private static readonly IBrush WarningBrush = UiPalette.Warning;
    private static readonly IBrush ErrorBrush = UiPalette.Error;
    private static readonly IBrush CriticalBrush = UiPalette.Critical;
    private static readonly IBrush ComponentBrush = CreateBrush("#B79CFF");
    private static readonly IBrush TraceMessageBrush = CreateBrush("#A8ADAA");
    private static readonly IBrush MessageBrush = CreateBrush("#E1E3E2");
    private static readonly IBrush ExceptionBrush = CreateBrush("#FF8A8A");

    private readonly SelectableTextBlock _consoleOutput;
    private readonly ScrollViewer? _consoleScroll;
    private readonly Rectangle? _consoleTopFade;
    private readonly Rectangle? _consoleBottomFade;
    private readonly Func<bool> _isConsoleTabActive;
    private readonly Action<ScrollViewer?, Rectangle?, Rectangle?> _updateFadeEffects;
    private readonly DispatcherTimer _logFlushTimer;
    private readonly ConcurrentQueue<LogEntry> _pendingLogEntries = new();
    private Action<LogEntry>? _logEntryHandler;
    private bool _followConsoleOutput;

    public ConsoleLogPresenter(
        SelectableTextBlock consoleOutput,
        ScrollViewer? consoleScroll,
        Rectangle? consoleTopFade,
        Rectangle? consoleBottomFade,
        MainViewModel viewModel,
        Func<bool> isConsoleTabActive,
        Action<ScrollViewer?, Rectangle?, Rectangle?> updateFadeEffects)
    {
        _consoleOutput = consoleOutput;
        _consoleScroll = consoleScroll;
        _consoleTopFade = consoleTopFade;
        _consoleBottomFade = consoleBottomFade;
        _isConsoleTabActive = isConsoleTabActive;
        _updateFadeEffects = updateFadeEffects;

        ConfigureConsoleAutoFollow(_consoleScroll);

        _logFlushTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(80)
        };
        _logFlushTimer.Tick += (_, _) => FlushPendingLogEntries(viewModel);
        _logFlushTimer.Start();

        RenderVisibleLogs(viewModel.ShowVerboseLogs);

        // Sofortige Neuberechnung der Fade-Effekte für die Konsole beim Start
        Dispatcher.UIThread.Post(() => _updateFadeEffects(_consoleScroll, _consoleTopFade, _consoleBottomFade), DispatcherPriority.Loaded);

        _logEntryHandler = entry =>
        {
            if (entry.IsRaw || (entry.Level == LogLevel.Trace && !viewModel.ShowVerboseLogs))
            {
                return;
            }

            _pendingLogEntries.Enqueue(entry);
        };
        Logger.OnLogAdded += _logEntryHandler;
    }

    /// <summary>
    /// Baut die sichtbare Sitzungshistorie neu auf, beispielsweise nach Änderung
    /// des Trace-Filters. Alle farbigen Segmente bleiben gemeinsam auswählbar.
    /// </summary>
    public void RenderVisibleLogs(bool showVerboseLogs)
    {
        InlineCollection inlines = _consoleOutput.Inlines ??= new InlineCollection();
        inlines.Clear();

        foreach (LogEntry entry in Logger.GetSessionLogs())
        {
            if (entry.IsRaw || (entry.Level == LogLevel.Trace && !showVerboseLogs))
            {
                continue;
            }

            AppendLogEntry(entry);
        }

        _updateFadeEffects(_consoleScroll, _consoleTopFade, _consoleBottomFade);
    }

    private void FlushPendingLogEntries(MainViewModel viewModel)
    {
        if (!_isConsoleTabActive())
        {
            return;
        }

        bool entryWasAppended = false;
        int processedEntries = 0;
        const int maximumEntriesPerTick = 40;

        while (processedEntries < maximumEntriesPerTick &&
               _pendingLogEntries.TryDequeue(out LogEntry? entry))
        {
            processedEntries++;
            if (entry.Level == LogLevel.Trace && !viewModel.ShowVerboseLogs)
            {
                continue;
            }

            AppendLogEntry(entry);
            entryWasAppended = true;
        }

        if (!entryWasAppended)
        {
            return;
        }

        _updateFadeEffects(_consoleScroll, _consoleTopFade, _consoleBottomFade);
        if (_followConsoleOutput && _consoleScroll != null)
        {
            Dispatcher.UIThread.Post(_consoleScroll.ScrollToEnd, DispatcherPriority.Loaded);
        }
    }

    private void AppendLogEntry(LogEntry entry)
    {
        InlineCollection inlines = _consoleOutput.Inlines ??= new InlineCollection();
        inlines.Add(new Run($"[{entry.Timestamp:HH:mm:ss}] ")
        {
            Foreground = TimestampBrush
        });
        inlines.Add(new Run($"[{entry.Level}] ")
        {
            Foreground = GetLevelBrush(entry.Level),
            FontWeight = FontWeight.SemiBold
        });
        string message = entry.Message;
        int componentEnd = message.StartsWith('[') ? message.IndexOf(']') : -1;
        if (componentEnd is > 1 and < 40)
        {
            inlines.Add(new Run($"{message[..(componentEnd + 1)]} ")
            {
                Foreground = ComponentBrush,
                FontWeight = FontWeight.SemiBold
            });
            message = message[(componentEnd + 1)..].TrimStart();
        }

        inlines.Add(new Run(message)
        {
            Foreground = entry.Level == LogLevel.Trace ? TraceMessageBrush : MessageBrush
        });

        if (!string.IsNullOrWhiteSpace(entry.ExceptionDetails))
        {
            inlines.Add(new Run($"  |  {entry.ExceptionDetails}")
            {
                Foreground = ExceptionBrush
            });
        }

        inlines.Add(new LineBreak());
    }

    private static IBrush GetLevelBrush(LogLevel level) => level switch
    {
        LogLevel.Trace => TraceBrush,
        LogLevel.Info => InfoBrush,
        LogLevel.Warning => WarningBrush,
        LogLevel.Error => ErrorBrush,
        LogLevel.Critical => CriticalBrush,
        _ => MessageBrush
    };

    private static IBrush CreateBrush(string hexColor) =>
        new SolidColorBrush(Color.Parse(hexColor));

    private void ConfigureConsoleAutoFollow(ScrollViewer? scrollViewer)
    {
        if (scrollViewer == null)
        {
            return;
        }

        scrollViewer.PropertyChanged += (_, args) =>
        {
            if (args.Property == ScrollViewer.OffsetProperty)
            {
                _followConsoleOutput = IsScrolledToBottom(scrollViewer);
            }
            else if ((args.Property == ScrollViewer.ExtentProperty ||
                      args.Property == ScrollViewer.ViewportProperty) &&
                     scrollViewer.Extent.Height <= scrollViewer.Viewport.Height)
            {
                _followConsoleOutput = true;
            }
        };

        _followConsoleOutput = IsScrolledToBottom(scrollViewer);
    }

    private static bool IsScrolledToBottom(ScrollViewer scrollViewer)
    {
        double maxOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        const double tolerance = 2;
        return scrollViewer.Offset.Y >= maxOffset - tolerance;
    }

    public void Dispose()
    {
        _logFlushTimer.Stop();
        if (_logEntryHandler != null)
        {
            Logger.OnLogAdded -= _logEntryHandler;
            _logEntryHandler = null;
        }
    }
}
