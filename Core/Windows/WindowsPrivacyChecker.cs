using System.Collections.Generic;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Bewertet ausgewählte Windows-Datenschutz- und Telemetrieeinstellungen.
/// </summary>
public sealed class WindowsPrivacyChecker : IOpSecChecker
{
    public string Name => "Windows-Telemetrie- & Aktivitätsverlauf";
    public string Category => "Windows / Datenschutz";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe lokale Datenschutzrichtlinien.");
        int? telemetry = WindowsRegistryReader.ReadLocalMachineInt32(
            @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
            "AllowTelemetry");
        int? activityPublishing = WindowsRegistryReader.ReadLocalMachineInt32(
            @"SOFTWARE\Policies\Microsoft\Windows\System",
            "PublishUserActivities");
        int? advertisingId = WindowsRegistryReader.ReadCurrentUserInt32(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
            "Enabled");

        var findings = new List<string>();
        if (telemetry is null or > 1)
        {
            findings.Add("Diagnosedaten sind nicht durch eine restriktive Richtlinie begrenzt.");
        }
        if (activityPublishing is null or not 0)
        {
            findings.Add("Das Veröffentlichen des Aktivitätsverlaufs ist nicht explizit deaktiviert.");
        }
        if (advertisingId is null or not 0)
        {
            findings.Add("Die benutzerspezifische Werbe-ID ist nicht explizit deaktiviert.");
        }

        return Task.FromResult(findings.Count == 0
            ? Result(
                CheckStatus.Pass,
                "Die geprüften Windows-Datenschutzrichtlinien sind restriktiv.",
                "Telemetrie, Aktivitätsverlauf und Werbe-ID sind durch lokale Richtlinien eingeschränkt.")
            : Result(
                CheckStatus.Warning,
                $"{findings.Count} Datenschutzoption(en) sind nicht gehärtet.",
                string.Join("\n", findings) +
                "\n\nNicht gesetzte Richtlinien bedeuten nicht automatisch Datenabfluss, lassen aber Windows-Standardwerte zu."));
    }

    private CheckResult Result(CheckStatus status, string summary, string details) => new()
    {
        Name = Name,
        Category = Category,
        Status = status,
        Summary = summary,
        Details = details
    };
}
