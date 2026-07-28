using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Bewertet das aktuell verbundene Windows-WLAN und dessen Authentifizierung.
/// </summary>
public sealed class WindowsWirelessChecker : IOpSecChecker
{
    public string Name => "Windows-WLAN-Schnittstellenprüfung";
    public string Category => "Windows / Funk";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("[Windows] Prüfe aktive WLAN-Schnittstellen.");
        NetworkInterface[] wirelessInterfaces = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(network => network.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            .ToArray();
        NetworkInterface[] activeInterfaces = wirelessInterfaces
            .Where(network => network.OperationalStatus == OperationalStatus.Up)
            .ToArray();

        return Task.FromResult(activeInterfaces.Length == 0
            ? Result(
                CheckStatus.Pass,
                "Keine aktive WLAN-Schnittstelle erkannt.",
                wirelessInterfaces.Length == 0
                    ? "Das System meldet keine WLAN-Hardware."
                    : "Vorhandene WLAN-Schnittstellen sind derzeit nicht aktiv.")
            : Result(
                CheckStatus.Warning,
                $"{activeInterfaces.Length} WLAN-Schnittstelle(n) sind aktiv.",
                $"Aktiv: {string.Join(", ", activeInterfaces.Select(network => network.Name))}.\n\n" +
                "Deaktiviere WLAN während sensibler Offline-Arbeiten, um Probe Requests und Netzwerkerkennung zu vermeiden."));
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
