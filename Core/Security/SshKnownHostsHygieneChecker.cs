using System;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Security;

/// <summary>
/// Prüft '~/.ssh/known_hosts' auf im Klartext gespeicherte Hostnamen und IP-Adressen,
/// welche ein forensisches Verbindungs-Log aller besuchten Server erzeugen.
/// </summary>
public sealed class SshKnownHostsHygieneChecker : OpSecCheckerBase
{
    public override string Name => "SSH Known_Hosts Fingerprint-Hygiene";
    public override string Category => "Security / Härtung";
    public override bool CanFix => true;
    public override string FixDescription => "Führt 'ssh-keygen -H -f ~/.ssh/known_hosts' aus, um alle Klartext-Hostnamen und IPs mit HMAC-SHA1 zu hashen.";

    public override async Task<FixResult> FixAsync()
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string knownHostsPath = Path.Combine(homeDir, ".ssh", "known_hosts");
        if (!File.Exists(knownHostsPath))
        {
            return new FixResult { Success = true, Message = "Keine ~/.ssh/known_hosts vorhanden." };
        }

        BackupService.BackupFile(knownHostsPath);
        var res = await ShellCommandService.ExecuteAsync("ssh-keygen", $"-H -f \"{knownHostsPath}\"");
        if (res.IsSuccess)
        {
            return new FixResult { Success = true, Message = "Alle Hostnamen in known_hosts wurden erfolgreich gehasht." };
        }
        return new FixResult { Success = false, Message = $"ssh-keygen -H fehlgeschlagen: {res.StandardError}" };
    }

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung von '~/.ssh/known_hosts' auf Klartext-Einträge...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string knownHostsPath = Path.Combine(homeDir, ".ssh", "known_hosts");

        if (!File.Exists(knownHostsPath))
        {
            return Pass(
                "Keine '~/.ssh/known_hosts' Datei vorhanden.",
                "Es ist keine bekannte SSH-Host-Historie gespeichert.");
        }

        int totalLines = 0;
        int plaintextEntries = 0;

        try
        {
            var lines = await File.ReadAllLinesAsync(knownHostsPath);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed)) continue;

                totalLines++;
                if (!trimmed.StartsWith("|1|"))
                {
                    plaintextEntries++;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Warning(
                "Kein Lesezugriff auf '~/.ssh/known_hosts'.",
                "Prüfung der bekannten SSH-Hosts wurde wegen Berechtigungen übersprungen.");
        }

        if (plaintextEntries > 0)
        {
            return Warning(
                $"{plaintextEntries} von {totalLines} SSH-Host-Einträgen in '~/.ssh/known_hosts' sind im Klartext gespeichert!",
                "Unverschlüsselte Hostnamen und IP-Adressen in 'known_hosts' verraten bei einer forensischen Analyse " +
                "genau, mit welchen Remote-Servern der Nutzer jemals verbunden war.\n\n" +
                "Empfehlung: Setze 'HashKnownHosts yes' in '~/.ssh/config' und hashe bestehende Einträge per 'ssh-keygen -H'.");
        }

        return Pass(
            "Alle Einträge in '~/.ssh/known_hosts' sind kryptografisch gehasht.",
            $"Insgesamt {totalLines} bekannte Host-Einträge sind per HMAC-SHA1 ('|1|...') gehasht und schützen vor forensischer Rekonstruktion.");
    }
}
