using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft aktiven Linux-Swap auf mögliche persistente Speicherreste. Nur zRAM kann
/// ohne weitere Systemabfragen eindeutig als nicht persistent bestätigt werden.
/// </summary>
public sealed class SwapMemoryChecker : IOpSecChecker
{
    public string Name => "Auslagerungsspeicher- und Swap-Prüfung";
    public string Category => "Kernel / Speicher";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Swap-Speichers...");

        try
        {
            if (!File.Exists("/proc/swaps"))
            {
                Logger.LogWarning("/proc/swaps nicht vorhanden. Swap-Status unbekannt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Swap-Status konnte nicht ermittelt werden.",
                    Details = "Das System stellt keine `/proc/swaps`-Schnittstelle bereit. Ein sicherer oder unverschlüsselter Swap lässt sich deshalb nicht bestätigen."
                });
            }

            string[] lines = File.ReadAllLines("/proc/swaps")
                .Where(line => !line.StartsWith("Filename", StringComparison.Ordinal) &&
                               !string.IsNullOrWhiteSpace(line))
                .ToArray();

            if (lines.Length == 0)
            {
                Logger.LogInfo("Kein Swap-Speicher aktiv.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Kein Swap-Speicher aktiv.",
                    Details = "Es sind keine Swap-Partitionen, Swap-Dateien oder zRAM-Geräte eingebunden."
                });
            }

            string[] devices = lines
                .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
                .ToArray();
            string swapDetails = string.Join("\n", lines);

            if (devices.All(device => device.StartsWith("/dev/zram", StringComparison.Ordinal)))
            {
                Logger.LogInfo("Ausschließlich RAM-basierter zRAM-Swap erkannt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Swap liegt ausschließlich im RAM (zRAM).",
                    Details = $"Eingebundene Swap-Geräte:\n{swapDetails}"
                });
            }

            if (devices.Any(device =>
                    device.StartsWith("/dev/mapper/", StringComparison.Ordinal) ||
                    device.StartsWith("/dev/dm-", StringComparison.Ordinal)))
            {
                Logger.LogWarning("Device-Mapper-Swap erkannt; Verschlüsselung nicht eindeutig verifizierbar.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Warning,
                    Summary = "Swap verwendet Device Mapper; Verschlüsselung ist nicht bestätigt.",
                    Details = $"Eingebundene Swap-Geräte:\n{swapDetails}\n\n" +
                              "Ein Pfad unter `/dev/mapper` kann LUKS, aber auch unverschlüsseltes LVM verwenden. Prüfe den zugrunde liegenden Mapper mit `cryptsetup status` oder `lsblk -f`."
                });
            }

            Logger.LogWarning("Persistenter Swap ohne nachweisbare Verschlüsselung erkannt.");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Persistenter Swap ohne nachweisbare Verschlüsselung aktiv.",
                Details = $"Eingebundene Swap-Geräte:\n{swapDetails}\n\n" +
                          "Sensible RAM-Inhalte können auf einem persistenten Datenträger verbleiben."
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Fehler bei der Swap-Prüfung", ex);
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Swap-Analyse fehlgeschlagen.",
                Details = ex.Message
            });
        }
    }
}
