using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Ermittelt unter Linux den rfkill-Blockzustand und den Prozessstatus von
/// bluetoothd. Ein fehlender Daemon beweist nicht, dass der Funkadapter vollständig
/// ausgeschaltet ist.
/// </summary>
public sealed class BluetoothChecker : IOpSecChecker
{
    public string Name => "Linux-Bluetooth-rfkill- und Daemonstatus";
    public string Category => "System / Funk";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Bluetooth-rfkill- und Daemonstatus...");

        try
        {
            bool isBlockedByRfkill = CheckRfkillBlocked();
            bool isBluetoothDaemonRunning = ProcessInspectionService.IsAnyRunning("bluetoothd");

            if (isBlockedByRfkill)
            {
                Logger.LogInfo("Bluetooth ist per rfkill blockiert.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Bluetooth ist per rfkill blockiert.",
                    Details = "Mindestens eine Bluetooth-rfkill-Schnittstelle meldet einen Hardware- oder Software-Block."
                });
            }

            if (!isBluetoothDaemonRunning)
            {
                Logger.LogInfo("Kein bluetoothd-Prozess erkannt; Funkstatus bleibt unbekannt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Bluetooth-Daemon läuft nicht; Funkstatus ist nicht bestätigt.",
                    Details = "Ein fehlender `bluetoothd`-Prozess bedeutet nicht zwingend, dass vorhandene Bluetooth-Hardware per rfkill blockiert oder vollständig stromlos ist."
                });
            }

            Logger.LogWarning("Bluetooth-Daemon ist aktiv und rfkill meldet keinen Block.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Bluetooth-Daemon ist aktiv und nicht per rfkill blockiert.",
                Details = "Deaktiviere Bluetooth bei Nichtgebrauch, wenn dein Bedrohungsmodell Funkerkennung oder unnötige lokale Angriffsfläche vermeiden soll."
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
                Summary = "Bluetooth-Status konnte nicht vollständig geprüft werden.",
                Details = ex.Message
            });
        }
    }

    private static bool CheckRfkillBlocked()
    {
        const string sysRfkill = "/sys/class/rfkill";
        if (!Directory.Exists(sysRfkill))
        {
            return false;
        }

        foreach (string directory in Directory.GetDirectories(sysRfkill))
        {
            string typePath = Path.Combine(directory, "type");
            if (!File.Exists(typePath) || File.ReadAllText(typePath).Trim() != "bluetooth")
            {
                continue;
            }

            string softPath = Path.Combine(directory, "soft");
            string hardPath = Path.Combine(directory, "hard");
            bool softBlocked = File.Exists(softPath) && File.ReadAllText(softPath).Trim() == "1";
            bool hardBlocked = File.Exists(hardPath) && File.ReadAllText(hardPath).Trim() == "1";
            if (softBlocked || hardBlocked)
            {
                return true;
            }
        }

        return false;
    }
}
