using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft, ob Hardware-Sicherheitsschlüssel (FIDO2 / U2F YubiKey, Nitrokey, SoloKeys)
/// oder Smartcards am USB-Bus angeschlossen sind und vom System erkannt werden.
/// </summary>
public sealed class HardwareAuthTokenChecker : OpSecCheckerBase
{
    public override string Name => "Hardware-Token (FIDO2 / U2F / YubiKey) Erkennung";
    public override string Category => "Security / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Erkennung von FIDO2 / U2F Hardware-Tokens am USB-Bus...");

        if (!OperatingSystem.IsLinux())
        {
            return Pass(
                "Hardware-Token Prüfung abgeschlossen.",
                "Auf Nicht-Linux Systemen wird standardmäßig von WebAuthn-kompatibler FIDO2-Unterstützung ausgegangen.");
        }

        var res = await ShellCommandService.ExecuteAsync("lsusb", "");
        if (res.IsSuccess && !string.IsNullOrWhiteSpace(res.StandardOutput))
        {
            string output = res.StandardOutput;
            bool foundYubico = output.Contains("Yubico", StringComparison.OrdinalIgnoreCase) ||
                               output.Contains("YubiKey", StringComparison.OrdinalIgnoreCase);
            bool foundNitrokey = output.Contains("Nitrokey", StringComparison.OrdinalIgnoreCase);
            bool foundSoloKeys = output.Contains("SoloKeys", StringComparison.OrdinalIgnoreCase) ||
                                 output.Contains("Solo", StringComparison.OrdinalIgnoreCase);
            bool foundTitan = output.Contains("Google Inc. Titan", StringComparison.OrdinalIgnoreCase) ||
                              output.Contains("Feitian", StringComparison.OrdinalIgnoreCase);

            if (foundYubico || foundNitrokey || foundSoloKeys || foundTitan)
            {
                string keyType = foundYubico ? "Yubico YubiKey" :
                                 foundNitrokey ? "Nitrokey FIDO/OpenPGP" :
                                 foundSoloKeys ? "SoloKeys FIDO2" : "Google Titan / Feitian Token";

                return Pass(
                    $"Hardware-Sicherheitsschlüssel ({keyType}) aktiv verbunden.",
                    "Das System erkennt einen dedizierten FIDO2/U2F-Hardware-Token am USB-Bus, welcher Phishing-resistente Logins ermöglicht.");
            }
        }

        bool isPcscdActive = ProcessInspectionService.IsAnyRunning("pcscd");
        if (isPcscdActive)
        {
            return Pass(
                "Smartcard-Dienst (pcscd) ist aktiv.",
                "Der PC/SC-Smartcard-Daemon läuft, was die Nutzung von PGP-Karten oder HSM-Tokens gestattet.");
        }

        return Warning(
            "Kein Hardware-Sicherheitsschlüssel (FIDO2 / YubiKey / Nitrokey) angeschlossen.",
            "Am USB-Bus konnte kein dedizierter FIDO2- oder U2F-Sicherheits-Token erkannt werden.\n\n" +
            "Empfehlung: Setze für kritische Konten (SSH, Git, Cloud-Logins, Passwort-Manager) Phishing-resistente FIDO2-Hardware ein.");
    }
}
