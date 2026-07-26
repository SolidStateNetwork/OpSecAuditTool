using System.Collections.Generic;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Prüft Windows-Schutzmechanismen für Anmeldedaten wie Credential Guard.
/// </summary>
public sealed class WindowsCredentialProtectionChecker : IOpSecChecker
{
    public string Name => "Windows-Anmeldeinformationsschutz";
    public string Category => "Windows / Zugangsdaten";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe WDigest und LSA-Schutz.");
        int? wdigest = WindowsRegistryReader.ReadLocalMachineInt32(
            @"SYSTEM\CurrentControlSet\Control\SecurityProviders\WDigest",
            "UseLogonCredential");
        int? runAsPpl = WindowsRegistryReader.ReadLocalMachineInt32(
            @"SYSTEM\CurrentControlSet\Control\Lsa",
            "RunAsPPL");
        var findings = new List<string>();

        if (wdigest == 1)
        {
            findings.Add("WDigest kann Klartext-Anmeldeinformationen im LSASS-Speicher behalten.");
        }
        if (runAsPpl is not (1 or 2))
        {
            findings.Add("LSA Protected Process Light (RunAsPPL) ist nicht explizit aktiviert.");
        }

        if (wdigest == 1)
        {
            return Task.FromResult(Result(
                CheckStatus.Fail,
                "Unsichere WDigest-Anmeldeinformationsspeicherung ist aktiviert.",
                string.Join("\n", findings)));
        }

        if (findings.Count > 0)
        {
            return Task.FromResult(Result(
                CheckStatus.Warning,
                "Der Windows-Anmeldeinformationsschutz ist nur teilweise gehärtet.",
                string.Join("\n", findings)));
        }

        return Task.FromResult(Result(
            CheckStatus.Pass,
            "WDigest ist sicher konfiguriert und LSA-Schutz ist aktiv.",
            "Die geprüften Einstellungen erschweren das Auslesen von Zugangsdaten aus dem LSASS-Prozess."));
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
