using System;
using System.Collections.Concurrent;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using OpSecAuditTool.Services;
using OpSecAuditTool.ViewModels;

namespace OpSecAuditTool.Views;

/// <summary>
/// Hauptfenster und ausschließlich UI-nahe Koordination für Animationen,
/// Scroll-Effekte und die farbige Live-Konsole.
/// </summary>
public sealed partial class MainWindow : Window
{
    private static readonly IBrush TimestampBrush = CreateBrush("#68737D");
    private static readonly IBrush TraceBrush = CreateBrush("#87929C");
    private static readonly IBrush InfoBrush = CreateBrush("#48CAE4");
    private static readonly IBrush WarningBrush = CreateBrush("#FFB020");
    private static readonly IBrush ErrorBrush = CreateBrush("#FF6363");
    private static readonly IBrush CriticalBrush = CreateBrush("#FF2D55");
    private static readonly IBrush ComponentBrush = CreateBrush("#B79CFF");
    private static readonly IBrush TraceMessageBrush = CreateBrush("#A5AFB8");
    private static readonly IBrush MessageBrush = CreateBrush("#D7E0D9");
    private static readonly IBrush ExceptionBrush = CreateBrush("#FF8A8A");

    private DispatcherTimer? _waveTimer;
    private DispatcherTimer? _radarTimer;
    private DispatcherTimer? _starTimer;
    private DispatcherTimer? _logFlushTimer;
    private DispatcherTimer? _profileTiltTimer;
    private readonly ConcurrentQueue<LogEntry> _pendingLogEntries = new();
    private readonly RotateTransform _profileRotateTransform = new();
    private readonly TranslateTransform _profileTranslateTransform = new();
    private readonly RotateTransform _radarRotateTransform = new();
    private readonly ScaleTransform _radarDotOneScale = new();
    private readonly ScaleTransform _radarDotTwoScale = new();
    private readonly ScaleTransform _radarDotThreeScale = new();
    private double _waveOffset;
    private double _radarAngle;
    private TranslateTransform? _waveTranslate;
    private Action<LogEntry>? _logEntryHandler;
    private bool _followConsoleOutput;
    private double _profileRotation;
    private double _profileOffsetX;
    private double _profileOffsetY;
    private double _targetProfileRotation;
    private double _targetProfileOffsetX;
    private double _targetProfileOffsetY;
    public MainWindow()
    {
        InitializeComponent();

        var profileTransforms = new TransformGroup();
        profileTransforms.Children.Add(_profileRotateTransform);
        profileTransforms.Children.Add(_profileTranslateTransform);
        ProfileImageFrameControl.RenderTransform = profileTransforms;

        InitializeRadarAnimation();

        // Das Fenster erstellt sein ViewModel einmalig und verwendet es für alle Tabs.
        var vm = new MainViewModel();
        DataContext = vm;

        // UI-Aktionen werden zentral protokolliert, statt jeden Button einzeln zu verdrahten.
        this.AddHandler(Button.ClickEvent, (s, e) =>
        {
            if (e.Source is Button btn)
            {
                string btnName = btn.Name ?? "Button";

                // Extrahiert den reinen Text-Namen, selbst wenn der Button ein Icon (StackPanel) enthält
                if (btn.Content is string strContent) btnName = strContent;
                else if (btn.Content is Panel panel)
                {
                    var tb = panel.Children.OfType<TextBlock>().FirstOrDefault();
                    if (tb != null && !string.IsNullOrEmpty(tb.Text)) btnName = tb.Text;
                }

                Logger.LogTrace($"UI-Interaktion: Button [{btnName}] geklickt.");
            }
        }, RoutingStrategies.Bubble);

        // Dasselbe gilt für Änderungen an Einstellungen.
        this.AddHandler(Avalonia.Controls.Primitives.ToggleButton.IsCheckedChangedEvent, (s, e) =>
        {
            if (e.Source is CheckBox cb)
            {
                string cbName = cb.Content?.ToString() ?? "CheckBox";
                string state = cb.IsChecked == true ? "Aktiviert" : "Deaktiviert";
                Logger.LogTrace($"UI-Interaktion: Checkbox [{cbName}] -> {state}.");
            }
        }, RoutingStrategies.Bubble);

        // Referenzen auf die Scrollbereiche mit dynamischen Fade-Effekten.
        var resourceScroll = this.FindControl<ScrollViewer>("ResourceScrollViewer");
        var resTopFade = this.FindControl<Rectangle>("ResourceTopFade");
        var resBottomFade = this.FindControl<Rectangle>("ResourceBottomFade");

        var consoleScroll = this.FindControl<ScrollViewer>("ConsoleScrollViewer");
        var consoleTopFade = this.FindControl<Rectangle>("ConsoleTopFade");
        var consoleBottomFade = this.FindControl<Rectangle>("ConsoleBottomFade");

        var auditScroll = this.FindControl<ScrollViewer>("AuditScrollViewer");
        var auditTopFade = this.FindControl<Rectangle>("AuditTopFade");
        var auditBottomFade = this.FindControl<Rectangle>("AuditBottomFade");

        var aboutScroll = this.FindControl<ScrollViewer>("AboutScrollViewer");
        var aboutTopFade = this.FindControl<Rectangle>("AboutTopFade");
        var aboutBottomFade = this.FindControl<Rectangle>("AboutBottomFade");

        _logFlushTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(80)
        };
        _logFlushTimer.Tick += (_, _) =>
            FlushPendingLogEntries(consoleScroll, consoleTopFade, consoleBottomFade, vm);
        _logFlushTimer.Start();

        // Bereits vorhandene Sitzungseinträge beim Öffnen des Fensters anzeigen.
        if (ConsoleOutput != null)
        {
            RenderVisibleLogs(vm.ShowVerboseLogs);

            // Sofortige Neuberechnung der Fade-Effekte für die Konsole beim Start
            Dispatcher.UIThread.Post(() => UpdateFadeEffects(consoleScroll, consoleTopFade, consoleBottomFade), DispatcherPriority.Loaded);
        }

        // Rohe Diagnoseblöcke gehören nur in die Datei; Trace-Einträge sind optional.
        _logEntryHandler = entry =>
        {
            // Vor dem Dispatch filtern, damit ausgeblendete Trace-Einträge die UI-Queue
            // während umfangreicher Hintergrundarbeiten nicht unnötig belasten.
            if (entry.IsRaw || (entry.Level == LogLevel.Trace && !vm.ShowVerboseLogs))
            {
                return;
            }

            _pendingLogEntries.Enqueue(entry);
        };
        Logger.OnLogAdded += _logEntryHandler;

        // Tab-Wechsel aktualisieren nur Animationen, Logs und echte Scroll-Fades.
        // Die Fenstergröße bleibt davon bewusst unberührt.
        if (MainTabs != null)
        {
            MainTabs.SelectionChanged += (s, e) =>
            {
                // Nur auf echte Tab-Wechsel reagieren (verhindert Trigger durch interne Listen)
                if (e.Source == MainTabs && MainTabs.SelectedItem is TabItem activeTab)
                {
                    UpdateTabAnimations(MainTabs.SelectedIndex);

                    // Tab-Wechsel als Trace loggen.
                    Logger.LogTrace($"UI-Interaktion: Tab gewechselt zu [{activeTab.Header}].");
                    Dispatcher.UIThread.Post(() =>
                    {
                        // Fade-Effekte beim Tab-Wechsel sicherheitshalber auch triggern
                        UpdateFadeEffects(resourceScroll, resTopFade, resBottomFade);
                        UpdateFadeEffects(consoleScroll, consoleTopFade, consoleBottomFade);
                        UpdateFadeEffects(auditScroll, auditTopFade, auditBottomFade);
                        UpdateFadeEffects(aboutScroll, aboutTopFade, aboutBottomFade);
                    }, DispatcherPriority.Loaded);
                }
            };
        }

        // Horcht auf ViewModel-Änderungen
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.AuditScoreText))
            {
                Dispatcher.UIThread.Post(() => UpdateBlackWaterMask(vm.AuditScoreText));
            }

            // Wenn Verbose-Logs im Betrieb ein/ausgeschaltet werden, bauen wir das Log-Fenster retroaktiv aus dem RAM neu auf!
            if (e.PropertyName == nameof(MainViewModel.ShowVerboseLogs))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (ConsoleOutput != null)
                    {
                        RenderVisibleLogs(vm.ShowVerboseLogs);
                        UpdateFadeEffects(consoleScroll, consoleTopFade, consoleBottomFade);
                    }
                });
            }
        };

        UpdateBlackWaterMask("0%");

        // Wellenfarbe auf den hellsten Punkt des Rechteck-Farbverlaufs setzen
        try
        {
            var grid = ScoreFillBorder?.Child as Grid;
            var rect = grid?.Children.OfType<Rectangle>().FirstOrDefault();
            if (rect?.Fill is LinearGradientBrush gradient && gradient.GradientStops.Count > 0 && WavePath != null)
            {
                WavePath.Fill = new SolidColorBrush(gradient.GradientStops[0].Color);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Wellenfarbe konnte nicht aus dem Verlauf gelesen werden: {ex.Message}");
            if (WavePath != null)
            {
                WavePath.Fill = Brushes.LimeGreen;
            }
        }

        // Der Timer für die flüssige Wasser-Animation
        _waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _waveTimer.Tick += (s, e) =>
        {
            if (_waveTranslate == null && WavePath != null)
            {
                var parentCanvas = WavePath.Parent as Canvas;
                _waveTranslate = parentCanvas?.RenderTransform as TranslateTransform;
            }
            if (_waveTranslate != null)
            {
                _waveOffset = (_waveOffset + 1.5) % 150;
                _waveTranslate.X = -_waveOffset;
            }
        };

        // Dekorative Partikelanimation der Startansicht initialisieren.
        InitStarrySky();

        // Nur echte Scrollbereiche erhalten Laufzeit-Tracking. Die Systemseite
        // passt vollständig in die deklarierte Mindestgröße und scrollt nicht.
        this.Loaded += (sender, args) =>
        {
            UpdateTabAnimations(MainTabs?.SelectedIndex ?? 0);

            ConfigureFadeTracking(resourceScroll, resTopFade, resBottomFade);
            ConfigureFadeTracking(consoleScroll, consoleTopFade, consoleBottomFade);
            ConfigureFadeTracking(auditScroll, auditTopFade, auditBottomFade);
            ConfigureFadeTracking(aboutScroll, aboutTopFade, aboutBottomFade);
            ConfigureConsoleAutoFollow(consoleScroll);
        };
    }

    private void FlushPendingLogEntries(
        ScrollViewer? consoleScroll,
        Rectangle? consoleTopFade,
        Rectangle? consoleBottomFade,
        MainViewModel viewModel)
    {
        if (ConsoleOutput == null || MainTabs?.SelectedIndex != 3)
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

        // Nur ein Layout- und Scrollupdate pro Paket statt pro einzelner Logzeile.
        UpdateFadeEffects(consoleScroll, consoleTopFade, consoleBottomFade);
        if (_followConsoleOutput && consoleScroll != null)
        {
            Dispatcher.UIThread.Post(consoleScroll.ScrollToEnd, DispatcherPriority.Loaded);
        }
    }

    private void UpdateTabAnimations(int selectedTabIndex)
    {
        SetTimerState(_waveTimer, selectedTabIndex == 1);
        SetTimerState(_radarTimer, selectedTabIndex == 0);
        SetTimerState(_starTimer, selectedTabIndex == 6);
    }

    /// <summary>
    /// Dreht den Radarstrahl und lässt eine Markierung kurz aufleuchten, sobald
    /// die Vorderkante des Strahls ihre Winkelposition erreicht. Die Animation
    /// pausiert automatisch, sobald die Übersichtsseite nicht sichtbar ist.
    /// </summary>
    private void InitializeRadarAnimation()
    {
        RadarSweepLayer.RenderTransform = _radarRotateTransform;
        RadarDotOne.RenderTransform = _radarDotOneScale;
        RadarDotTwo.RenderTransform = _radarDotTwoScale;
        RadarDotThree.RenderTransform = _radarDotThreeScale;

        _radarTimer = new DispatcherTimer
        {
            // Rund 30 FPS reichen für den ruhigen Radar-Effekt und halten die
            // Belastung des UI-Threads bewusst niedrig.
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _radarTimer.Tick += (_, _) =>
        {
            _radarAngle = (_radarAngle + 1.35) % 360;
            _radarRotateTransform.Angle = _radarAngle;

            UpdateRadarDot(RadarDotOne, _radarDotOneScale, 212.6);
            UpdateRadarDot(RadarDotTwo, _radarDotTwoScale, 350.5);
            UpdateRadarDot(RadarDotThree, _radarDotThreeScale, 63.2);
        };
    }

    private void UpdateRadarDot(Ellipse dot, ScaleTransform scale, double dotAngle)
    {
        double distance = Math.Abs((_radarAngle - dotAngle + 540) % 360 - 180);
        double pulse = Math.Max(0, 1 - distance / 18);
        double renderedScale = 1 + pulse * 1.15;

        scale.ScaleX = renderedScale;
        scale.ScaleY = renderedScale;
        dot.Opacity = 0.68 + pulse * 0.32;
    }

    private static void SetTimerState(DispatcherTimer? timer, bool shouldRun)
    {
        if (timer == null)
        {
            return;
        }

        if (shouldRun)
        {
            timer.Start();
        }
        else
        {
            timer.Stop();
        }
    }

    /// <summary>
    /// Die Schnellaktionen der Übersicht führen weiterhin ihren bisherigen
    /// Befehl aus und öffnen zusätzlich den Bereich mit den zugehörigen Details.
    /// </summary>
    private void OverviewAuditButton_Click(object? sender, RoutedEventArgs e)
    {
        if (MainTabs != null)
        {
            MainTabs.SelectedIndex = 1;
        }
    }

    private void OverviewSystemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (MainTabs != null)
        {
            MainTabs.SelectedIndex = 2;
        }
    }

    /// <summary>
    /// Aktiviert die automatische Nachführung nur, solange sich die Konsole am
    /// unteren Ende befindet. Eine Scrollbewegung nach oben gibt die Position frei.
    /// </summary>
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
                // Ohne Überlauf ist der Anfang zugleich das Ende.
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

    /// <summary>
    /// Verknüpft einen Scrollbereich mit seinen beiden Fade-Overlays.
    /// Neben Scrollbewegungen werden Größenänderungen beobachtet, weil dynamische Inhalte
    /// (z. B. Audit-Ergebnisse) die Scrollbarkeit nachträglich ändern können.
    /// </summary>
    private void ConfigureFadeTracking(
        ScrollViewer? scrollViewer,
        Rectangle? topFade,
        Rectangle? bottomFade)
    {
        if (scrollViewer == null || topFade == null || bottomFade == null)
        {
            return;
        }

        scrollViewer.PropertyChanged += (_, args) =>
        {
            if (args.Property == ScrollViewer.OffsetProperty ||
                args.Property == ScrollViewer.ExtentProperty ||
                args.Property == ScrollViewer.ViewportProperty)
            {
                UpdateFadeEffects(scrollViewer, topFade, bottomFade);
            }
        };

        UpdateFadeEffects(scrollViewer, topFade, bottomFade);
    }

    private void UpdateFadeEffects(ScrollViewer? scrollViewer, Rectangle? topFade, Rectangle? bottomFade)
    {
        if (scrollViewer == null || topFade == null || bottomFade == null)
            return;

        double verticalOffset = scrollViewer.Offset.Y;
        double maxScrollable = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;

        if (maxScrollable <= 5)
        {
            topFade.Opacity = 0.0;
            bottomFade.Opacity = 0.0;
            return;
        }

        topFade.Opacity = verticalOffset > 5.0 ? 1.0 : 0.0;
        bottomFade.Opacity = verticalOffset < (maxScrollable - 5.0) ? 1.0 : 0.0;
    }

    /// <summary>
    /// Erzeugt die Textmaske für den zweifarbigen Prozentwert im animierten Wasserstand.
    /// </summary>
    private void UpdateBlackWaterMask(string text)
    {
        if (BlackWaterContainer == null) return;
        var txtMask = new TextBlock { Text = text, FontSize = 24, FontWeight = Avalonia.Media.FontWeight.Bold, Foreground = Brushes.Black, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        var gridMask = new Grid { Width = 150, Height = 150, Background = Brushes.Transparent };
        gridMask.Children.Add(txtMask);
        var visualBrush = new VisualBrush(gridMask) { Stretch = Stretch.None, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Bottom };
        BlackWaterContainer.OpacityMask = visualBrush;
    }

    private sealed class StarParticle
    {
        public Border Shape { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double SpeedX { get; set; }
        public double SpeedY { get; set; }
        public double BaseSize { get; set; }
        public double SparklePhase { get; set; }
        public double SparkleSpeed { get; set; }
        public double SparkleStrength { get; set; }
    }

    private void InitStarrySky()
    {
        if (StarCanvas == null) return;

        var random = new Random();
        var stars = new System.Collections.Generic.List<StarParticle>();
        int starCount = 20;
        double sectorSize = (Math.PI * 2) / starCount;

        for (int i = 0; i < starCount; i++)
        {
            double size = 1.8 + random.NextDouble() * 1.8;
            int colorVariant = random.Next(0, 4);
            Color starColor = colorVariant switch
            {
                0 => Color.FromRgb(210, 255, 225),
                1 or 2 => Color.FromRgb(0, 255, 102),
                _ => Color.FromRgb(0, 190, 75)
            };

            var starBorder = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(starColor),
                BoxShadow = BoxShadows.Parse("0 0 8 2 #00FF66")
            };

            // Sektoren-basierte Verteilung: Garantiert keine leeren Ecken oder Anhäufungen
            double minAngle = i * sectorSize;
            double maxAngle = (i + 1) * sectorSize;
            double angle = minAngle + random.NextDouble() * (maxAngle - minAngle);

            // Variabler Abstand sorgt für organische Tiefenwirkung bis in die Ecken
            double radius = random.Next(38, 64);

            double x = 65 + Math.Cos(angle) * radius;
            double y = 65 + Math.Sin(angle) * radius;

            Canvas.SetLeft(starBorder, x - size / 2);
            Canvas.SetTop(starBorder, y - size / 2);
            StarCanvas.Children.Add(starBorder);

            stars.Add(new StarParticle
            {
                Shape = starBorder,
                X = x,
                Y = y,
                SpeedX = (random.NextDouble() - 0.5) * 0.22,
                SpeedY = (random.NextDouble() - 0.5) * 0.22,
                BaseSize = size,
                SparklePhase = random.NextDouble() * Math.PI * 2,
                SparkleSpeed = 0.045 + random.NextDouble() * 0.075,
                SparkleStrength = 0.7 + random.NextDouble() * 0.6
            });
        }

        _starTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _starTimer.Tick += (s, e) =>
        {
            foreach (var star in stars)
            {
                star.X += star.SpeedX;
                star.Y += star.SpeedY;

                double dx = star.X - 65;
                double dy = star.Y - 65;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                // Sanfte Begrenzung für das Bewegungsfeld
                if (dist < 36 || dist > 66)
                {
                    star.SpeedX *= -1;
                    star.SpeedY *= -1;
                }

                // Eine schmale Sinusspitze erzeugt kurze, klar erkennbare Lichtblitze.
                star.SparklePhase += star.SparkleSpeed;
                double wave = (Math.Sin(star.SparklePhase) + 1) / 2;
                double flash = Math.Pow(wave, 4) * star.SparkleStrength;
                double renderedSize = star.BaseSize * (0.72 + 0.28 * wave + 0.32 * flash);

                star.Shape.Width = renderedSize;
                star.Shape.Height = renderedSize;
                star.Shape.Opacity = Math.Clamp(0.25 + 0.45 * wave + 0.35 * flash, 0.25, 1.0);

                Canvas.SetLeft(star.Shape, star.X - renderedSize / 2);
                Canvas.SetTop(star.Shape, star.Y - renderedSize / 2);
            }
        };
    }

    /// <summary>
    /// Übersetzt die Mausposition in eine kleine Rotation und Verschiebung. Der
    /// begrenzte Bewegungsradius hält Bild und Glow sicher innerhalb ihrer Renderfläche.
    /// </summary>
    private void ProfileButton_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control profileButton ||
            profileButton.Bounds.Width <= 0 ||
            profileButton.Bounds.Height <= 0)
        {
            return;
        }

        Point pointer = e.GetPosition(profileButton);
        double normalizedX = Math.Clamp(
            (pointer.X / profileButton.Bounds.Width * 2) - 1,
            -1,
            1);
        double normalizedY = Math.Clamp(
            (pointer.Y / profileButton.Bounds.Height * 2) - 1,
            -1,
            1);

        _targetProfileRotation = normalizedX * 5;
        _targetProfileOffsetX = normalizedX * 4;
        _targetProfileOffsetY = normalizedY * 4;
        EnsureProfileTiltAnimation();
    }

    private void ProfileButton_PointerExited(object? sender, PointerEventArgs e)
    {
        _targetProfileRotation = 0;
        _targetProfileOffsetX = 0;
        _targetProfileOffsetY = 0;
        EnsureProfileTiltAnimation();
    }

    private void EnsureProfileTiltAnimation()
    {
        if (_profileTiltTimer == null)
        {
            _profileTiltTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _profileTiltTimer.Tick += UpdateProfileTilt;
        }

        _profileTiltTimer.Start();
    }

    private void UpdateProfileTilt(object? sender, EventArgs e)
    {
        const double easing = 0.22;
        _profileRotation += (_targetProfileRotation - _profileRotation) * easing;
        _profileOffsetX += (_targetProfileOffsetX - _profileOffsetX) * easing;
        _profileOffsetY += (_targetProfileOffsetY - _profileOffsetY) * easing;

        _profileRotateTransform.Angle = _profileRotation;
        _profileTranslateTransform.X = _profileOffsetX;
        _profileTranslateTransform.Y = _profileOffsetY;

        bool isAtRest =
            Math.Abs(_targetProfileRotation - _profileRotation) < 0.01 &&
            Math.Abs(_targetProfileOffsetX - _profileOffsetX) < 0.01 &&
            Math.Abs(_targetProfileOffsetY - _profileOffsetY) < 0.01;
        if (isAtRest)
        {
            _profileRotation = _targetProfileRotation;
            _profileOffsetX = _targetProfileOffsetX;
            _profileOffsetY = _targetProfileOffsetY;
            _profileRotateTransform.Angle = _profileRotation;
            _profileTranslateTransform.X = _profileOffsetX;
            _profileTranslateTransform.Y = _profileOffsetY;
            _profileTiltTimer?.Stop();
        }
    }

    /// <summary>
    /// Baut die sichtbare Sitzungshistorie neu auf, beispielsweise nach Änderung
    /// des Trace-Filters. Alle farbigen Segmente bleiben gemeinsam auswählbar.
    /// </summary>
    private void RenderVisibleLogs(bool showVerboseLogs)
    {
        if (ConsoleOutput == null)
        {
            return;
        }

        InlineCollection inlines = ConsoleOutput.Inlines ??= new InlineCollection();
        inlines.Clear();

        foreach (LogEntry entry in Logger.GetSessionLogs())
        {
            if (entry.IsRaw || (entry.Level == LogLevel.Trace && !showVerboseLogs))
            {
                continue;
            }

            AppendLogEntry(entry);
        }
    }

    private void AppendLogEntry(LogEntry entry)
    {
        if (ConsoleOutput == null)
        {
            return;
        }

        InlineCollection inlines = ConsoleOutput.Inlines ??= new InlineCollection();
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

    protected override void OnClosed(EventArgs e)
    {
        // DispatcherTimer laufen andernfalls weiter und halten das geschlossene Fenster am Leben.
        _waveTimer?.Stop();
        _radarTimer?.Stop();
        _starTimer?.Stop();
        _logFlushTimer?.Stop();
        _profileTiltTimer?.Stop();
        if (_logEntryHandler != null)
        {
            Logger.OnLogAdded -= _logEntryHandler;
        }

        if (DataContext is IDisposable disposableViewModel)
        {
            disposableViewModel.Dispose();
        }

        base.OnClosed(e);
    }
}
