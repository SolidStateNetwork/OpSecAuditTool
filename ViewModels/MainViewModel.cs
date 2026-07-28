using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpSecAuditTool.Models;
using OpSecAuditTool.Services;
using OpSecAuditTool.Theme;
using OpSecAuditTool.Views;

namespace OpSecAuditTool.ViewModels;

/// <summary>
/// Zentraler Orchestrator des UI-Zustands. Koordiniert Sub-ViewModels,
/// Einstellungen und Fenstersteuerungen bei voller Binding-Kompatibilität.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private static readonly IBrush OfflineStatusBackground = UiPalette.OfflineBackground;
    private static readonly IBrush OfflineStatusBorder = UiPalette.OfflineBorder;
    private static readonly IBrush OfflineStatusForeground = UiPalette.Critical;
    private static readonly IBrush OnlineStatusBackground = UiPalette.OnlineBackground;
    private static readonly IBrush OnlineStatusBorder = UiPalette.OnlineBorder;
    private static readonly IBrush OnlineStatusForeground = UiPalette.Accent;

    [ObservableProperty] private int _selectedTabIndex = 0;

    public AuditRunnerViewModel AuditRunner { get; } = new();
    public SystemDashboardViewModel SystemDashboard { get; } = new();
    public ContactViewModel Contact { get; } = new();

    private bool _isDisposed;
    private int _profileClickCount;
    private EasterEggWindow? _easterEggWindow;

    public MainViewModel()
    {
        Logger.LogInfo("OpSec Audit Tool erfolgreich gestartet.");

        // Forward PropertyChanged Events von Sub-ViewModels an die UI-Bindings von MainViewModel
        AuditRunner.PropertyChanged += OnSubViewModelPropertyChanged;
        SystemDashboard.PropertyChanged += OnSubViewModelPropertyChanged;
        Contact.PropertyChanged += OnSubViewModelPropertyChanged;

        if (AutoStartAuditOnLaunch)
        {
            _ = StartAudit();
        }
    }

    private void OnSubViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null)
        {
            OnPropertyChanged(e.PropertyName);
        }
    }

    #region Audit Properties & Commands
    public string AuditScoreText => AuditRunner.AuditScoreText;
    public double WaterHeight => AuditRunner.WaterHeight;
    public string StatTotalText => AuditRunner.StatTotalText;
    public string StatPassedText => AuditRunner.StatPassedText;
    public string StatWarningText => AuditRunner.StatWarningText;
    public string StatFailText => AuditRunner.StatFailText;
    public string WelcomeRatingText => AuditRunner.WelcomeRatingText;
    public bool IsAuditRunning => AuditRunner.IsAuditRunning;
    public bool HasAuditFinished => AuditRunner.HasAuditFinished;
    public bool AreDetailsVisible => AuditRunner.AreDetailsVisible;
    public string AuditButtonText => AuditRunner.AuditButtonText;
    public string AuditButtonIcon => AuditRunner.AuditButtonIcon;
    public string ToggleDetailsButtonText => AuditRunner.ToggleDetailsButtonText;
    public ObservableCollection<AuditResultItem> AuditResults => AuditRunner.AuditResults;

    [RelayCommand]
    public Task StartAudit() => AuditRunner.StartAudit();

    [RelayCommand]
    public void ToggleDetails() => AuditRunner.ToggleDetails();

    [RelayCommand]
    public void ExportReport() => AuditRunner.ExportReport();

    [RelayCommand]
    public void ExportJsonReport() => AuditRunner.ExportJsonReport();

    [RelayCommand]
    public void ExportMarkdownReport() => AuditRunner.ExportMarkdownReport();
    #endregion


    #region System Dashboard Properties & Commands
    public string OsDistribution => SystemDashboard.OsDistribution;
    public string KernelVersion => SystemDashboard.KernelVersion;
    public string OsArchitecture => SystemDashboard.OsArchitecture;
    public string Hostname => SystemDashboard.Hostname;
    public string CpuCores => SystemDashboard.CpuCores;
    public string RamTotal => SystemDashboard.RamTotal;
    public string AppVersion => SystemDashboard.AppVersion;
    public string NetRuntime => SystemDashboard.NetRuntime;
    public string RuntimeRid => SystemDashboard.RuntimeRid;
    public string DisplayServer => SystemDashboard.DisplayServer;
    public string DesktopEnvironment => SystemDashboard.DesktopEnvironment;
    public string GlibcVersion => SystemDashboard.GlibcVersion;
    public string LocalIp => SystemDashboard.LocalIp;
    public string PublicIp => SystemDashboard.PublicIp;
    public string DnsServer => SystemDashboard.DnsServer;
    public string MacAddress => SystemDashboard.MacAddress;
    public string LuksEncryption => SystemDashboard.LuksEncryption;
    public string SwapStatus => SystemDashboard.SwapStatus;
    public string FirewallStatus => SystemDashboard.FirewallStatus;
    public string TorRouting => SystemDashboard.TorRouting;

    public IBrush PublicIpColor => SystemDashboard.PublicIpColor;
    public IBrush DnsColor => SystemDashboard.DnsColor;
    public IBrush DiskCryptColor => SystemDashboard.DiskCryptColor;
    public IBrush SwapColor => SystemDashboard.SwapColor;
    public IBrush FirewallColor => SystemDashboard.FirewallColor;
    public IBrush TorColor => SystemDashboard.TorColor;

    [RelayCommand]
    public Task RefreshSystem() => SystemDashboard.RefreshSystem();
    #endregion

    #region Contact Properties & Commands
    public string XmppAddress => Contact.XmppAddress;
    public string PgpKey => Contact.PgpKey;

    [RelayCommand]
    public Task CopyXmpp() => Contact.CopyXmpp();

    [RelayCommand]
    public Task CopyPgp() => Contact.CopyPgp();
    #endregion

    #region Settings & Status
    public bool AllowInternetAccess
    {
        get => SettingsService.AllowInternetAccess;
        set
        {
            if (SettingsService.AllowInternetAccess == value) return;

            SettingsService.AllowInternetAccess = value;
            Logger.LogInfo($"Einstellung 'Internet Access' geändert auf: {value}");

            OnPropertyChanged(nameof(AllowInternetAccess));
            OnPropertyChanged(nameof(InternetModeText));
            OnPropertyChanged(nameof(InternetModeBackground));
            OnPropertyChanged(nameof(InternetModeBorderBrush));
            OnPropertyChanged(nameof(InternetModeForeground));
        }
    }

    public string InternetModeText =>
        AllowInternetAccess ? "ONLINE AKTIV" : "OFFLINE-BEREIT";

    public IBrush InternetModeBackground =>
        AllowInternetAccess ? OnlineStatusBackground : OfflineStatusBackground;

    public IBrush InternetModeBorderBrush =>
        AllowInternetAccess ? OnlineStatusBorder : OfflineStatusBorder;

    public IBrush InternetModeForeground =>
        AllowInternetAccess ? OnlineStatusForeground : OfflineStatusForeground;

    public bool AutoStartAuditOnLaunch
    {
        get => SettingsService.Current.AutoStartAuditOnLaunch;
        set
        {
            if (SettingsService.Current.AutoStartAuditOnLaunch == value) return;

            SettingsService.Current.AutoStartAuditOnLaunch = value;
            SettingsService.SaveSettings();
            Logger.LogInfo($"Einstellung 'AutoStart Audit' geändert auf: {value}");
            OnPropertyChanged(nameof(AutoStartAuditOnLaunch));
        }
    }

    public bool ShowVerboseLogs
    {
        get => SettingsService.Current.ShowVerboseLogs;
        set
        {
            if (SettingsService.Current.ShowVerboseLogs == value) return;

            SettingsService.Current.ShowVerboseLogs = value;
            SettingsService.SaveSettings();

            Logger.LogInfo($"Einstellung 'Verbose Logs' geändert auf: {value}");
            OnPropertyChanged(nameof(ShowVerboseLogs));
        }
    }
    #endregion

    #region Window & Easter Egg Commands
    [RelayCommand]
    private void OpenOpSecBible()
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (lifetime?.MainWindow != null)
            {
                var bibleWindow = new OpSecAuditTool.Views.OpSecBibleWindow();
                bibleWindow.Show();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Öffnen der OpSec-Bibel", ex);
        }
    }

    [RelayCommand]
    private void RegisterProfileClick()
    {
        _profileClickCount++;
        Logger.LogTrace($"[EasterEgg] Profilbild angeklickt ({_profileClickCount}/10).");

        if (_profileClickCount < 10)
        {
            return;
        }

        _profileClickCount = 0;

        if (_easterEggWindow != null)
        {
            _easterEggWindow.Activate();
            return;
        }

        var window = new EasterEggWindow();
        _easterEggWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_easterEggWindow, window))
            {
                _easterEggWindow = null;
            }
        };

        window.Show();
        Logger.LogInfo("[EasterEgg] Easter-Egg-Fenster wurde freigeschaltet.");
    }
    #endregion

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        AuditRunner.PropertyChanged -= OnSubViewModelPropertyChanged;
        SystemDashboard.PropertyChanged -= OnSubViewModelPropertyChanged;
        Contact.PropertyChanged -= OnSubViewModelPropertyChanged;

        _easterEggWindow?.Close();
        _easterEggWindow = null;
        GC.SuppressFinalize(this);
    }
}
