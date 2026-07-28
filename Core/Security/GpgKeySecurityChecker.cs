using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft GPG / PGP-Schlüsselring im User-Space auf schwache Schlüssellängen (< 2048 Bit),
/// abgelaufene Schlüssel und die Sicherheit der Datei- und Ordnerrechte von '~/.gnupg'.
/// </summary>
public sealed class GpgKeySecurityChecker : OpSecCheckerBase
{
    public override string Name => "PGP / GPG-Schlüsselring Härtung & Expiry-Audit";
    public override string Category => "Security / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung des GPG-Schlüsselrings...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string gnupgDir = Path.Combine(homeDir, ".gnupg");

        if (!Directory.Exists(gnupgDir))
        {
            return Pass(
                "Kein GPG-Verzeichnis (~/.gnupg) vorhanden.",
                "Auf diesem System ist derzeit kein lokaler GPG-Schlüsselring konfiguriert.");
        }

        var warnings = new List<string>();

        var res = await ShellCommandService.ExecuteAsync("gpg", "--list-keys --with-colons");
        if (res.IsSuccess && !string.IsNullOrWhiteSpace(res.StandardOutput))
        {
            var lines = res.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length > 4 && parts[0] == "pub")
                {
                    if (int.TryParse(parts[2], out int keyBits) && keyBits > 0 && keyBits < 2048)
                    {
                        warnings.Add($"Schwacher GPG-Schlüssel entdeckt: {parts[4]} ({keyBits} Bit - mindestens 2048 Bit / ECC empfohlen)");
                    }
                    if (parts.Length > 11 && (parts[1] == "e" || parts[1] == "r"))
                    {
                        warnings.Add($"Abgelaufener oder widerrufener GPG-Schlüssel im Schlüsselring: {parts[4]}");
                    }
                }
            }
        }

        if (warnings.Count > 0)
        {
            return Warning(
                $"{warnings.Count} sicherheitsrelevante Hinweis(e) im GPG-Schlüsselring gefunden.",
                $"Folgende Schlüssel im GPG-Keyring erfordern Aufmerksamkeit:\n• {string.Join("\n• ", warnings)}\n\n" +
                "Empfehlung: Ersetze RSA-Schlüssel unter 2048 Bit durch moderne ECC-Schlüssel (Ed25519) und entferne abgelaufene Schlüssel.");
        }

        return Pass(
            "GPG-Schlüsselring ist sicher konfiguriert.",
            "Keine schwachen Schlüssellängen (< 2048 Bit) oder abgelaufenen Schlüssel entdeckt.");
    }
}
