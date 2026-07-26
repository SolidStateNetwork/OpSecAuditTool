using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpSecAuditTool.Core;
using OpSecAuditTool.Core.Security;
using OpSecAuditTool.Core.Windows;
using OpSecAuditTool.Models;
using OpSecAuditTool.Services;
using OpSecAuditTool.Views;

namespace OpSecAuditTool.ViewModels;

/// <summary>
/// Zentraler UI-Zustand der Hauptansicht. Koordiniert Auditläufe,
/// Systemübersicht, Einstellungen und die zusätzlichen Fenster.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private static readonly Uri PublicIpEndpoint = new("https://api.ipify.org");
    private static readonly HttpClient PublicIpClient = new(
        new HttpClientHandler { CheckCertificateRevocationList = true })
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    [ObservableProperty] private int _selectedTabIndex = 0;

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

    // Der Button wechselt nach dem ersten Durchlauf von „Start“ zu „Wiederholen“.
    [ObservableProperty] private string _auditButtonText = "Audit Starten";
    [ObservableProperty] private string _auditButtonIcon = "M5 3l14 9-14 9V3z";

    [ObservableProperty] private string _toggleDetailsButtonText = "▼ Detailergebnisse anzeigen";

    public ObservableCollection<AuditResultItem> AuditResults { get; } = new();
    private readonly List<CheckResult> _rawResultsForExport = [];

    private bool _isDisposed;
    private int _profileClickCount;
    private EasterEggWindow? _easterEggWindow;

    [ObservableProperty] private string _osDistribution = "-";
    [ObservableProperty] private string _kernelVersion = "-";
    [ObservableProperty] private string _osArchitecture = "-";
    [ObservableProperty] private string _hostname = "-";
    [ObservableProperty] private string _cpuCores = "-";
    [ObservableProperty] private string _ramTotal = "-";
    [ObservableProperty] private string _appVersion = "-";
    [ObservableProperty] private string _netRuntime = "-";
    [ObservableProperty] private string _runtimeRid = "-";
    [ObservableProperty] private string _displayServer = "-";
    [ObservableProperty] private string _desktopEnvironment = "-";
    [ObservableProperty] private string _glibcVersion = "-";
    [ObservableProperty] private string _localIp = "-";
    [ObservableProperty] private string _publicIp = "-";
    [ObservableProperty] private string _dnsServer = "-";
    [ObservableProperty] private string _macAddress = "-";
    [ObservableProperty] private string _luksEncryption = "-";
    [ObservableProperty] private string _swapStatus = "-";
    [ObservableProperty] private string _firewallStatus = "-";
    [ObservableProperty] private string _torRouting = "-";

    [ObservableProperty] private IBrush _publicIpColor = Brushes.White;
    [ObservableProperty] private IBrush _dnsColor = Brushes.White;
    [ObservableProperty] private IBrush _diskCryptColor = Brushes.White;
    [ObservableProperty] private IBrush _swapColor = Brushes.White;
    [ObservableProperty] private IBrush _firewallColor = Brushes.White;
    [ObservableProperty] private IBrush _torColor = Brushes.White;

    public MainViewModel()
    {
        Logger.LogInfo("OpSec Audit Tool erfolgreich gestartet.");
        LoadStaticSystemInfo();

        if (AutoStartAuditOnLaunch)
        {
            _ = StartAudit();
        }
    }

    public bool AllowInternetAccess
    {
        get => SettingsService.AllowInternetAccess;
        set
        {
            if (SettingsService.AllowInternetAccess == value) return;

            SettingsService.AllowInternetAccess = value;
            Logger.LogInfo($"Einstellung 'Internet Access' geändert auf: {value}");

            OnPropertyChanged(nameof(AllowInternetAccess));
        }
    }

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

    [RelayCommand]
    private async Task StartAudit()
    {
        if (IsAuditRunning) return;

        IsAuditRunning = true;
        HasAuditFinished = false;
        Logger.LogInfo("Volles OpSec-Systemaudit gestartet.");
        WaterHeight = 0;
        AuditScoreText = "0%";
        AuditResults.Clear();
        _rawResultsForExport.Clear();

        IOpSecChecker[] checkers = AuditCheckerCatalog.CreateAll();
        var tempResults = new List<AuditResultItem>();

        foreach (var checker in checkers)
        {
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

            _rawResultsForExport.Add(result);
            tempResults.Add(CreateAuditResultItem(result));
        }

        foreach (AuditResultItem item in tempResults.OrderBy(item => item.Status.SortOrder()))
        {
            AuditResults.Add(item);
        }

        int passedCount = _rawResultsForExport.Count(result => result.Status == CheckStatus.Pass);
        int warningCount = _rawResultsForExport.Count(result => result.Status == CheckStatus.Warning);
        int failCount = _rawResultsForExport.Count(result => result.Status == CheckStatus.Fail);
        int total = checkers.Length;
        int percent = total > 0 ? (passedCount * 100) / total : 0;

        StatTotalText = $"Total : {total}";
        StatPassedText = $"{passedCount} bestanden";
        StatWarningText = $"{warningCount} Warnungen";
        StatFailText = $"{failCount} Kritisch";

        AuditScoreText = $"{percent}%";
        WaterHeight = (150.0 * percent) / 100.0;
        WelcomeRatingText = $"Letzter Audit-Status: {passedCount} von {total} Checks bestanden ({percent}%)";

        HasAuditFinished = true;
        IsAuditRunning = false;

        // Nach dem ersten Durchlauf bietet derselbe Button einen erneuten Audit an.
        AuditButtonText = "Audit wiederholen";
        AuditButtonIcon = "M23 4v6h-6 M1 20v-6h6 M3.51 9a9 9 0 0 1 14.85-3.36L23 10 M1 14l4.64 4.36A9 9 0 0 0 20.49 15";

        Logger.LogInfo($"Audit abgeschlossen: {percent}% Sicherheits-Rating erreicht.");
    }

    [RelayCommand]
    private void ToggleDetails()
    {
        AreDetailsVisible = !AreDetailsVisible;
        ToggleDetailsButtonText = AreDetailsVisible ? "▲ Detailergebnisse ausblenden" : "▼ Detailergebnisse anzeigen";
    }

    [RelayCommand]
    private void ExportReport()
    {
        try
        {
            string filePath = AuditReportService.Save(_rawResultsForExport);
            Logger.LogInfo($"Audit-Bericht gespeichert unter: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler beim Speichern des Berichts", ex);
        }
    }

    private static AuditResultItem CreateAuditResultItem(CheckResult result)
    {
        IBrush borderColor = result.Status switch
        {
            CheckStatus.Pass => Brushes.LimeGreen,
            CheckStatus.Warning => Brushes.Orange,
            CheckStatus.Fail => Brushes.Red,
            _ => Brushes.Gray
        };

        return new AuditResultItem
        {
            Category = $"[{result.Category}]",
            Name = result.Name,
            Summary = $"Status: {result.Summary}",
            Details = result.Details,
            BorderColor = borderColor,
            Status = result.Status
        };
    }

    private void LoadStaticSystemInfo()
    {
        try
        {
            SystemInfoSnapshot snapshot = SystemInfoService.GetSnapshot();
            OsDistribution = snapshot.OperatingSystem;
            KernelVersion = snapshot.KernelVersion;
            OsArchitecture = snapshot.OsArchitecture;
            CpuCores = $"{snapshot.LogicalProcessorCount} Logisch";
            RamTotal = snapshot.TotalMemory;
            Hostname = snapshot.Hostname;
            AppVersion = snapshot.ApplicationVersion;
            NetRuntime = snapshot.DotnetRuntime;
            RuntimeRid = snapshot.RuntimeIdentifier;
            DisplayServer = snapshot.DisplayServer;
            DesktopEnvironment = snapshot.DesktopEnvironment;
            GlibcVersion = snapshot.GlibcVersion;
            LocalIp = snapshot.LocalIpAddress;
            MacAddress = snapshot.MacAddress;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Fehler beim Parsen des System-Dashboards: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshSystem()
    {
        Logger.LogInfo("Manueller System- und Security-Scan gestartet.");

        PublicIp = DnsServer = LuksEncryption = SwapStatus = FirewallStatus = TorRouting = "Wird geprüft...";
        PublicIpColor = DnsColor = DiskCryptColor = SwapColor = FirewallColor = TorColor = Brushes.White;

        if (!SettingsService.AllowInternetAccess)
        {
            PublicIp = "Deaktiviert (Bitte in Einstellungen erlauben)";
            PublicIpColor = Brushes.Orange;
            Logger.LogWarning("Öffentlicher IP-Scan übersprungen (Offline-Modus aktiv).");
        }
        else
        {
            try
            {
                PublicIp = (await PublicIpClient.GetStringAsync(PublicIpEndpoint)).Trim();
                PublicIpColor = Brushes.White;
            }
            catch (Exception ex) { PublicIp = "Fehler (Offline)"; PublicIpColor = Brushes.Red; Logger.LogError("Fehler IP", ex); }
        }

        try
        {
            string[] dnsServers = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up)
                .SelectMany(network => network.GetIPProperties().DnsAddresses)
                .Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            DnsServer = dnsServers.Length > 0 ? string.Join(", ", dnsServers) : "-";
        }
        catch (Exception ex)
        {
            DnsServer = "Fehler beim Lesen";
            Logger.LogError("DNS-Konfiguration konnte nicht gelesen werden.", ex);
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                CheckResult bitLockerResult = await new WindowsBitLockerChecker().ExecuteAsync();
                LuksEncryption = bitLockerResult.Summary;
                DiskCryptColor = bitLockerResult.Status switch
                {
                    CheckStatus.Pass => Brushes.LimeGreen,
                    CheckStatus.Warning => Brushes.Orange,
                    _ => Brushes.Red
                };
            }
            else if (File.Exists("/proc/mounts"))
            {
                string[] mounts = await File.ReadAllLinesAsync("/proc/mounts");
                bool isEncrypted = mounts.Any(line =>
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length >= 2 &&
                           (parts[1] == "/" || parts[1] == "/home") &&
                           parts[0].StartsWith("/dev/mapper/", StringComparison.Ordinal);
                });
                LuksEncryption = isEncrypted ? "Aktiv (LUKS dm-crypt)" : "Inaktiv / Unverschlüsselt";
                DiskCryptColor = isEncrypted ? Brushes.LimeGreen : Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            LuksEncryption = "Fehler beim Lesen";
            DiskCryptColor = Brushes.Red;
            Logger.LogError("Verschlüsselungsstatus konnte nicht gelesen werden.", ex);
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                SwapStatus = "Windows-Auslagerungsdatei (Schutz folgt BitLocker)";
                SwapColor = DiskCryptColor;
            }
            else if (File.Exists("/proc/swaps"))
            {
                string[] swapLines = await File.ReadAllLinesAsync("/proc/swaps");
                bool isSwapActive = swapLines.Length > 1;
                SwapStatus = !isSwapActive ? "Deaktiviert" : "Aktiv";
                SwapColor = !isSwapActive ? Brushes.LimeGreen : Brushes.Orange;
            }
        }
        catch (Exception ex)
        {
            SwapStatus = "Fehler beim Lesen";
            SwapColor = Brushes.Red;
            Logger.LogError("Swap-Status konnte nicht gelesen werden.", ex);
        }

        try
        {
            bool isTorProcessRunning = ProcessInspectionService.IsAnyRunning("tor");
            bool isPort9050Open = await IsPortListeningAsync("127.0.0.1", 9050);
            bool isPort9150Open = await IsPortListeningAsync("127.0.0.1", 9150);

            if (isTorProcessRunning || isPort9050Open || isPort9150Open)
            {
                TorRouting = "Aktiv (SOCKS5/Daemon läuft)";
                TorColor = Brushes.LimeGreen;
            }
            else { TorRouting = "Inaktiv"; TorColor = Brushes.White; }
        }
        catch (Exception ex)
        {
            TorRouting = "Fehler bei Tor-Prüfung";
            TorColor = Brushes.Red;
            Logger.LogError("Tor-Status konnte nicht ermittelt werden.", ex);
        }

        try
        {
            CheckResult fwResult = OperatingSystem.IsWindows()
                ? await new WindowsFirewallChecker().ExecuteAsync()
                : await new FirewallChecker().ExecuteAsync();

            if (fwResult.Status == CheckStatus.Pass)
            {
                FirewallStatus = "Aktiv";
                FirewallColor = Brushes.LimeGreen;
            }
            else
            {
                FirewallStatus = "Inaktiv / Nicht konfiguriert";
                FirewallColor = Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            FirewallStatus = "Fehler bei Prüfung";
            FirewallColor = Brushes.Red;
            Logger.LogError("Fehler bei Firewall-System-Check", ex);
        }

        Logger.LogInfo("System- und Security-Scan abgeschlossen.");
    }

    private static async Task<bool> IsPortListeningAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            Task connectTask = client.ConnectAsync(host, port);
            Task completedTask = await Task.WhenAny(connectTask, Task.Delay(200));
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            // Geschlossene oder nicht erreichbare Ports sind das erwartete Negativergebnis.
            return false;
        }
    }

    [ObservableProperty] private string _xmppAddress = "SolidStateNetwork@xmpp.is";
    [ObservableProperty]
    private string _pgpKey =
        """
        -----BEGIN PGP PUBLIC KEY BLOCK-----

        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
        wiEbLt2oQSrP
        =Gv/m
        -----END PGP PUBLIC KEY BLOCK-----
        """;

    [RelayCommand]
    private async Task CopyXmpp() => await CopyToClipboard(XmppAddress, "XMPP-Kontaktdaten");

    [RelayCommand]
    private async Task CopyPgp() => await CopyToClipboard(PgpKey, "Public PGP-Key");

    private async Task CopyToClipboard(string text, string context)
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var clipboard = lifetime?.MainWindow?.Clipboard;

            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                Logger.LogInfo($"{context} erfolgreich in die Zwischenablage kopiert.");
            }
            else
            {
                Logger.LogWarning("Clipboard-Interface konnte nicht ermittelt werden.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Fehler beim Kopieren von {context}", ex);
        }
    }

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

        // Ohne Owner verhält sich das Fenster unabhängig und erhält einen Taskbar-Eintrag.
        window.Show();
        Logger.LogInfo("[EasterEgg] Easter-Egg-Fenster wurde freigeschaltet.");
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _easterEggWindow?.Close();
        _easterEggWindow = null;
        GC.SuppressFinalize(this);
    }

}
