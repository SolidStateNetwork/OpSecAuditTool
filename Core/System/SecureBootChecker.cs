using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.System;

/// <summary>
/// Ermittelt den UEFI-Secure-Boot-Zustand unter Linux.
/// Nutzt natives Sysfs-Parsing mit automatischem Fallback auf mokutil --sb-state.
/// </summary>
public sealed class SecureBootChecker : OpSecCheckerBase
{
    public override string Name => "UEFI Secure Boot Statusprüfung";
    public override string Category => "System / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung des Secure-Boot-Status...");

        string efiPath = "/sys/firmware/efi";

        if (!Directory.Exists(efiPath))
        {
            Logger.LogWarning("System läuft im Legacy/CSM-Modus (kein UEFI).");
            return Warning(
                "Legacy BIOS Modus aktiv (Kein UEFI).",
                "Das System wurde im Legacy-BIOS/CSM-Modus gebootet. Secure Boot steht in dieser Konfiguration nicht zur Verfügung.");
        }

        string efivarsPath = "/sys/firmware/efi/efivars";
        bool? isSecureBootEnabled = null;

        if (Directory.Exists(efivarsPath))
        {
            try
            {
                var files = Directory.GetFiles(efivarsPath, "SecureBoot-*");
                if (files.Length > 0)
                {
                    byte[] data = await File.ReadAllBytesAsync(files[0]);
                    if (data.Length >= 5)
                    {
                        isSecureBootEnabled = (data[4] == 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"Direktes Lesen von efivars verwehrt: {ex.Message}");
            }
        }

        // Fallback über mokutil --sb-state, falls efivars ohne Root nicht zugänglich oder leer waren
        if (isSecureBootEnabled == null)
        {
            try
            {
                var mokResult = await ShellCommandService.ExecuteAsync("mokutil", "--sb-state");
                if (mokResult.IsSuccess && !string.IsNullOrWhiteSpace(mokResult.StandardOutput))
                {
                    if (mokResult.StandardOutput.Contains("SecureBoot enabled", StringComparison.OrdinalIgnoreCase))
                    {
                        isSecureBootEnabled = true;
                    }
                    else if (mokResult.StandardOutput.Contains("SecureBoot disabled", StringComparison.OrdinalIgnoreCase))
                    {
                        isSecureBootEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrace($"mokutil Fallback nicht möglich: {ex.Message}");
            }
        }

        if (isSecureBootEnabled == true)
        {
            Logger.LogInfo("Secure Boot ist im UEFI aktiv und schützt den Boot-Prozess.");
            return Pass(
                "Secure Boot ist aktiv.",
                "Der UEFI-Bootloader und der Kernel sind vor Manipulationen (Evil Maid / Bootkit-Angriffe) geschützt.");
        }

        Logger.LogWarning("Secure Boot ist im UEFI deaktiviert oder konnte nicht verifiziert werden.");
        return Warning(
            "Secure Boot ist deaktiviert oder inaktiv.",
            "Das System läuft im UEFI-Modus, jedoch ist Secure Boot nicht aktiviert.\n\n" +
            "Hinweis: Aktiviere Secure Boot im Mainboard-UEFI, um die Bootloader-Integrität zu sichern.");
    }
}
