using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft aktiven Linux-Swap auf mögliche unverschlüsselte Speicherreste.
/// </summary>
public sealed class SwapMemoryChecker : IOpSecChecker
{
    public string Name => "Auslagerungsspeicher-Verschlüsselung (Swap)";
    public string Category => "Kernel / Speicher";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Starte Prüfung des Swap-Speichers...");

        try
        {
            if (!File.Exists("/proc/swaps"))
            {
                Logger.LogInfo("/proc/swaps nicht vorhanden. Swap-Status unklar.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Keine Swap-Informationen gefunden.",
                    Details = "Das System stellt keine '/proc/swaps'-Schnittstelle bereit."
                });
            }

            var lines = File.ReadAllLines("/proc/swaps")
                .Where(line => !line.StartsWith("Filename", StringComparison.Ordinal) &&
                               !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count == 0)
            {
                Logger.LogInfo("Kein Swap-Speicher aktiv (z. B. Swapless System oder Zram).");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Kein unverschlüsselter Swap-Speicher aktiv.",
                    Details = "Es sind keine physischen Swap-Partitionen oder Swap-Dateien auf der Festplatte eingebunden."
                });
            }

            bool isEncrypted = false;
            var swapDetails = string.Join("\n", lines);

            if (lines.All(line =>
                    line.Contains("/dev/mapper/", StringComparison.Ordinal) ||
                    line.Contains("/dev/dm-", StringComparison.Ordinal) ||
                    line.Contains("/dev/zram", StringComparison.Ordinal)))
            {
                isEncrypted = true;
            }

            if (isEncrypted)
            {
                Logger.LogInfo("Verschlüsselter oder RAM-basierter Swap erkannt.");
                return Task.FromResult(new CheckResult
                {
                    Name = Name,
                    Category = Category,
                    Status = CheckStatus.Pass,
                    Summary = "Swap-Speicher ist verschlüsselt oder im RAM (zRAM).",
                    Details = $"Eingebundene Swap-Geräte:\n{swapDetails}"
                });
            }

            Logger.LogWarning("Unverschlüsselter Swap-Speicher auf der Festplatte erkannt!");
            return Task.FromResult(new CheckResult
            {
                Name = Name,
                Category = Category,
                Status = CheckStatus.Warning,
                Summary = "Mögliches Forensik-Risiko! Unverschlüsselter Swap aktiv.",
                Details = $"Swap-Partition/Datei liegt unverschlüsselt vor:\n{swapDetails}\n\nHinweis: Sensible RAM-Inhalte könnten unverschlüsselt auf der Festplatte landen."
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
                Details = $"Fehler: {ex.Message}"
            });
        }
    }
}
