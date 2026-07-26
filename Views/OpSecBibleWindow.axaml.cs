using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using OpSecAuditTool.ViewModels;

namespace OpSecAuditTool.Views;

/// <summary>
/// Eigenständiges Lesefenster für die integrierte OpSec-Bibel.
/// </summary>
public sealed partial class OpSecBibleWindow : Window
{
    public OpSecBibleWindow()
    {
        InitializeComponent();

        DataContext = new OpSecBibleViewModel();

        Loaded += (_, _) =>
        {
            ConfigureFadeTracking(ContentScrollViewer, TopFadeRectangle, BottomFadeRectangle);
            ConfigureFadeTracking(TocScrollViewer, TocTopFade, TocBottomFade);
        };
    }

    /// <summary>
    /// Hält die Fade-Overlays bei Scroll- und Größenänderungen synchron.
    /// </summary>
    private static void ConfigureFadeTracking(
        ScrollViewer scrollViewer,
        Rectangle topFade,
        Rectangle bottomFade)
    {
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

    private static void UpdateFadeEffects(
        ScrollViewer scrollViewer,
        Rectangle topFade,
        Rectangle bottomFade)
    {
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

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
