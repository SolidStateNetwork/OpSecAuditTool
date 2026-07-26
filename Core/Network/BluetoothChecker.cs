using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Ermittelt, ob Bluetooth-Hardware beziehungsweise der zugehörige Dienst aktiv ist.
/// </summary>
public sealed class BluetoothChecker : IOpSecChecker
{
    public string Name => "Bluetooth-Schnittstellen-Sicherheitsprüfung";
    public string Category => "System / Härtung";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung der Bluetooth-Schnittstellen...");

        try
        {
            bool isBlockedByRfkill = CheckRfkillBlocked();

            bool isBluetoothDaemonRunning = ProcessInspectionService.IsAnyRunning("bluetoothd");

            if (isBlockedByRfkill || !isBluetoothDaemonRunning)
            {
                Logger.LogInfo("Bluetooth ist entweder deaktiviert (rfkill) oder der Daemon läuft nicht.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Bluetooth ist deaktiviert / blockiert.",
                    Details = isBlockedByRfkill
                        ? "Bluetooth ist per 'rfkill' (Hard/Soft Block) vollständig deaktiviert."
                        : "Der 'bluetoothd'-Dienst läuft nicht im Hintergrund. Keine Funk-Schnittstelle aktiv."
                });
            }

            Logger.LogWarning("Bluetooth-Dienst ist aktiv!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Bluetooth-Adapter ist aktiv und lauscht.",
                Details = "Der 'bluetoothd'-Daemon ist im System aktiv.\n\n" +
                          "Hinweis: Wenn du keine Bluetooth-Geräte nutzt, blockiere die Schnittstelle via 'rfkill block bluetooth' oder stoppe den Dienst ('systemctl stop bluetooth')."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Bluetooth-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Bluetooth Audit fehlgeschlagen.",
                Details = $"Fehler: {ex.Message}"
            });
        }
    }

    private static bool CheckRfkillBlocked()
    {
        string sysRfkill = "/sys/class/rfkill";
        if (!Directory.Exists(sysRfkill)) return false;

        foreach (string directory in Directory.GetDirectories(sysRfkill))
        {
            string typePath = Path.Combine(directory, "type");
            if (File.Exists(typePath) && File.ReadAllText(typePath).Trim() == "bluetooth")
            {
                string softPath = Path.Combine(directory, "soft");
                string hardPath = Path.Combine(directory, "hard");

                bool softBlocked = File.Exists(softPath) && File.ReadAllText(softPath).Trim() == "1";
                bool hardBlocked = File.Exists(hardPath) && File.ReadAllText(hardPath).Trim() == "1";

                if (softBlocked || hardBlocked) return true;
            }
        }

        return false;
    }
}
