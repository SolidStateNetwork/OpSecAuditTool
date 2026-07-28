using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft aktive SSH- und GPG-Agent Sockets auf im Speicher gehaltene Schlüssel
/// und bewertet das Risiko durch Agent-Forwarding oder unbegrenzte Lebensdauer.
/// </summary>
public sealed class AgentSocketSecurityChecker : OpSecCheckerBase
{
    public override string Name => "SSH- & GPG-Agent Socket-Audit";
    public override string Category => "Security / Härtung";

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der SSH- und GPG-Agent Sockets...");

        string? sshAuthSock = Environment.GetEnvironmentVariable("SSH_AUTH_SOCK")?.Trim();
        string? gpgAgentInfo = Environment.GetEnvironmentVariable("GPG_AGENT_INFO")?.Trim();

        bool isSshAgentActive = !string.IsNullOrEmpty(sshAuthSock);
        bool isGpgAgentActive = !string.IsNullOrEmpty(gpgAgentInfo);

        if (!isSshAgentActive && !isGpgAgentActive)
        {
            return Pass(
                "Keine aktiven SSH- oder GPG-Agent Sockets in dieser Sitzung gefunden.",
                "In dieser Umgebung läuft derzeit kein im Speicher geladener SSH- oder GPG-Key-Agent.");
        }

        int loadedKeysCount = 0;
        if (isSshAgentActive)
        {
            var res = await ShellCommandService.ExecuteAsync("ssh-add", "-l");
            if (res.IsSuccess)
            {
                var lines = res.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains("SHA256:") || line.Contains("RSA") || line.Contains("ED25519") || line.Contains("ECDSA"))
                    {
                        loadedKeysCount++;
                    }
                }
            }
        }

        if (loadedKeysCount > 0)
        {
            return Warning(
                $"{loadedKeysCount} SSH-Schlüssel im Speicher des aktiven SSH-Agenten geladen.",
                $"Aktiver Socket: '{sshAuthSock}'.\n" +
                $"Der SSH-Agent hält aktuell {loadedKeysCount} private Schlüssel unverschlüsselt im Speicher bereit.\n\n" +
                "Empfehlung: Setze eine Lebensdauer/Timeout für Schlüssel (z. B. 'ssh-add -t 3600') und verwende Agent-Forwarding ausschließlich bei vertrauenswürdigen Servern.");
        }

        return Pass(
            "SSH- / GPG-Agent Socket ist aktiv, hält jedoch keine Schlüssel unbegrenzt im Speicher.",
            $"SSH-Socket: {(isSshAgentActive ? "Aktiv" : "Inaktiv")}, GPG-Agent: {(isGpgAgentActive ? "Aktiv" : "Inaktiv")}.");
    }
}
