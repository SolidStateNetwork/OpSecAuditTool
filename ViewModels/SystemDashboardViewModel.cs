using System;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpSecAuditTool.Models;
using OpSecAuditTool.Services;
using OpSecAuditTool.Theme;

namespace OpSecAuditTool.ViewModels;

/// <summary>
/// Sub-ViewModel für statische Systeminformationen und dynamische Sicherheitszustände.
/// </summary>
public sealed partial class SystemDashboardViewModel : ObservableObject
{
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

    [ObservableProperty] private IBrush _publicIpColor = UiPalette.TextPrimary;
    [ObservableProperty] private IBrush _dnsColor = UiPalette.TextPrimary;
    [ObservableProperty] private IBrush _diskCryptColor = UiPalette.TextPrimary;
    [ObservableProperty] private IBrush _swapColor = UiPalette.TextPrimary;
    [ObservableProperty] private IBrush _firewallColor = UiPalette.TextPrimary;
    [ObservableProperty] private IBrush _torColor = UiPalette.TextPrimary;

    public SystemDashboardViewModel()
    {
        LoadStaticSystemInfo();
    }

    public void LoadStaticSystemInfo()
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
    public async Task RefreshSystem()
    {
        SystemSecurityStatus status = await SystemSecurityScannerService.ScanAsync();

        PublicIp = status.PublicIp;
        DnsServer = status.DnsServer;
        LuksEncryption = status.LuksEncryption;
        SwapStatus = status.SwapStatus;
        FirewallStatus = status.FirewallStatus;
        TorRouting = status.TorRouting;

        PublicIpColor = status.PublicIpColor;
        DnsColor = status.DnsColor;
        DiskCryptColor = status.DiskCryptColor;
        SwapColor = status.SwapColor;
        FirewallColor = status.FirewallColor;
        TorColor = status.TorColor;
    }
}
