using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Network;

/// <summary>
/// Sucht nach TCP-Listenern, die nicht ausschließlich an lokale Adressen gebunden sind.
/// </summary>
public sealed class ExternalListenerChecker : IOpSecChecker
{
    public string Name => "Extern gebundene TCP-Dienste";
    public string Category => "Netzwerk / Angriffsfläche";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Prüfe TCP-Listener auf Bindungen außerhalb von Loopback.");
        try
        {
            IPEndPoint[] listeners = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Where(endpoint => IsExternallyBound(endpoint.Address))
                .OrderBy(endpoint => endpoint.Port)
                .ToArray();

            if (listeners.Length == 0)
            {
                return Task.FromResult(Result(
                    CheckStatus.Pass,
                    "Keine extern gebundenen TCP-Listener erkannt.",
                    "Lauschende TCP-Dienste sind nicht auf LAN-/WLAN-Schnittstellen oder alle Adressen gebunden."));
            }

            string[] displayedListeners = listeners
                .Take(20)
                .Select(endpoint => $"{endpoint.Address}:{endpoint.Port}")
                .ToArray();
            string remainder = listeners.Length > displayedListeners.Length
                ? $"\n… und {listeners.Length - displayedListeners.Length} weitere."
                : string.Empty;
            return Task.FromResult(Result(
                CheckStatus.Warning,
                $"{listeners.Length} TCP-Dienst(e) lauschen außerhalb von Loopback.",
                $"Erkannte Bindungen:\n• {string.Join("\n• ", displayedListeners)}{remainder}\n\n" +
                "Eine Bindung bedeutet nicht automatisch Internet-Erreichbarkeit. Prüfe dennoch, ob Dienst und Firewall-Regel benötigt werden."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result(
                CheckStatus.Warning,
                "TCP-Listener konnten nicht vollständig geprüft werden.",
                ex.Message));
        }
    }

    private static bool IsExternallyBound(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        return address.Equals(IPAddress.Any) ||
               address.Equals(IPAddress.IPv6Any) ||
               address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6;
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
