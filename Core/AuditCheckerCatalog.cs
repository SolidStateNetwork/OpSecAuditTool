using System;
using OpSecAuditTool.Core.Diagnostics;
using OpSecAuditTool.Core.Forensics;
using OpSecAuditTool.Core.Network;
using OpSecAuditTool.Core.Security;
using OpSecAuditTool.Core.System;
using OpSecAuditTool.Core.Windows;

namespace OpSecAuditTool.Core;

/// <summary>
/// Erstellt ausschließlich Checker, die auf dem aktuellen Betriebssystem sinnvoll
/// ausgewertet werden können. Dadurch verfälschen plattformspezifische Prüfungen
/// weder die Fehlerzahl noch die Prozentberechnung anderer Systeme.
/// </summary>
public static class AuditCheckerCatalog
{
    public static IOpSecChecker[] CreateAll()
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindows();
        }

        if (OperatingSystem.IsLinux())
        {
            return CreateLinux();
        }

        return CreatePortableCommon();
    }

    private static IOpSecChecker[] CreateWindows() =>
    [
        .. CreatePortableCommon(),
        new WindowsFirewallChecker(),
        new WindowsDefenderChecker(),
        new WindowsBitLockerChecker(),
        new WindowsSecureBootChecker(),
        new WindowsAccountProtectionChecker(),
        new WindowsCredentialProtectionChecker(),
        new WindowsRemoteAccessChecker(),
        new WindowsStartupPersistenceChecker(),
        new WindowsPrivacyChecker(),
        new WindowsWirelessChecker()
    ];

    private static IOpSecChecker[] CreatePortableCommon() =>
    [
        new IpPublicChecker(),
        new DnsLeakChecker(),
        new ExternalListenerChecker(),
        new HostnameTimezoneChecker(),
        new TorrentLeakChecker(),
        new TorStatusChecker(),
        new ShellHistoryChecker(),
        new ClipboardChecker(),
        new BrowserStorageChecker(),
        new ThumbnailCacheChecker(),
        new RecentFilesChecker(),
        new CrashReportChecker(),
        new EnvironmentSecretChecker()
    ];

    private static IOpSecChecker[] CreateLinux() =>
    [
        .. CreatePortableCommon(),
        new FirewallChecker(),
        new SwapMemoryChecker(),
        new MacSpoofChecker(),
        new TmpFsChecker(),
        new SudoersChecker(),
        new UsbGuardChecker(),
        new BluetoothChecker(),
        new SshHardeningChecker(),
        new CoreDumpChecker(),
        new PtraceScopeChecker(),
        new UserDataPermissionsChecker(),
        new TelemetryChecker(),
        new DiskEncryptionChecker(),
        new SecureBootChecker(),
        new JournaldChecker(),
        new AslrChecker(),
        new CronJobChecker(),
        new FailedServicesChecker(),
        new DisplayServerChecker(),
        new KernelLockdownChecker(),
        new WifiSecurityChecker(),
        new KernelModuleChecker(),
        new TrashChecker()
    ];
}
