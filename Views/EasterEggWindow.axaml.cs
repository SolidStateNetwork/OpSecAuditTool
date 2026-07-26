using Avalonia.Controls;

namespace OpSecAuditTool.Views;

/// <summary>
/// Eigenständiges Fenster für das versteckte Profilbild-Easter-Egg.
/// Es wird bewusst ohne Owner geöffnet und besitzt daher einen eigenen Taskbar-Eintrag.
/// </summary>
public partial class EasterEggWindow : Window
{
    public EasterEggWindow()
    {
        InitializeComponent();
    }
}
