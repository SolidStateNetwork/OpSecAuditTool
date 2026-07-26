using System.Collections.Generic;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Bewertet Windows-Kontoschutz anhand von UAC- und Anmeldeeinstellungen.
/// </summary>
public sealed class WindowsAccountProtectionChecker : IOpSecChecker
{
    private const string SystemPolicyKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

    public string Name => "Windows-UAC- & Rechteausweitungsprüfung";
    public string Category => "Windows / Konten";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe UAC-Konfiguration.");
        int? enableLua = WindowsRegistryReader.ReadLocalMachineInt32(
            SystemPolicyKey,
            "EnableLUA");
        int? consentBehavior = WindowsRegistryReader.ReadLocalMachineInt32(
            SystemPolicyKey,
            "ConsentPromptBehaviorAdmin");

        if (enableLua == 0)
        {
            return Task.FromResult(Result(
                CheckStatus.Fail,
                "Benutzerkontensteuerung (UAC) ist deaktiviert.",
                "Prozesse können ohne die normale UAC-Sicherheitsgrenze erhöhte Änderungen ausführen."));
        }

        if (enableLua != 1)
        {
            return Task.FromResult(Result(
                CheckStatus.Warning,
                "UAC-Status konnte nicht gelesen werden.",
                "Die Prüfung fordert bewusst keine Administratorrechte an."));
        }

        if (consentBehavior == 0)
        {
            return Task.FromResult(Result(
                CheckStatus.Warning,
                "UAC ist aktiv, Administratoren werden aber ohne Nachfrage erhöht.",
                "ConsentPromptBehaviorAdmin steht auf automatischer Erhöhung. Eine Bestätigung auf dem sicheren Desktop ist empfehlenswert."));
        }

        var details = new List<string>
        {
            "Benutzerkontensteuerung ist aktiviert.",
            $"Bestätigungsrichtlinie für Administratoren: {consentBehavior?.ToString() ?? "Standard"}."
        };
        return Task.FromResult(Result(
            CheckStatus.Pass,
            "UAC und Bestätigungsabfrage sind aktiviert.",
            string.Join("\n", details)));
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
