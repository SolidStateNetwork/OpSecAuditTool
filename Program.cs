using System;
using System.Globalization;
using Avalonia;
using OpSecAuditTool.Services;

namespace OpSecAuditTool;

/// <summary>
/// Prozess-Einstiegspunkt mit zentraler Absturzprotokollierung und Avalonia-Konfiguration.
/// </summary>
internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Globaler Absturz-Schutz für unvorhergesehene, asynchrone Exceptions
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Logger.LogCritical("UNHANDLED CRASH! Die Anwendung ist unerwartet abgestürzt.", ex);
        };

        try
        {
            // System-Hardwareinformationen und Zeitstempel als sauberen Header ins Log schreiben
            string logHeader = SystemInfoService.GetSystemReportHeader() +
                               $"OpSec Audit Log - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}\n" +
                               "==========================================\n\n";

            Logger.LogRaw(logHeader);

            Logger.LogInfo("OpSec Audit Tool wird gestartet...");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Logger.LogCritical("Schwerwiegender Fehler beim Starten der Anwendung!", ex);
        }
        finally
        {
            Logger.LogInfo("OpSec Audit Tool beendet.");
        }
    }

    /// <summary>
    /// Konfiguriert die Avalonia-Anwendung (Plattformerkennung und Tracing).
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
