using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpSecAuditTool.Core;

namespace OpSecAuditTool.Services;

/// <summary>
/// Erstellt den textbasierten Audit-Bericht und kapselt dessen Dateispeicherung.
/// Netzwerkadressen und Hardwareadressen werden vor dem Export redigiert.
/// </summary>
public static class AuditReportService
{
    private static readonly Regex Ipv4Address = new(
        @"\b(?:\d{1,3}\.){3}\d{1,3}\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex Ipv6Address = new(
        @"(?<![0-9A-Fa-f:])(?:[0-9A-Fa-f]{1,4}:){2,7}[0-9A-Fa-f]{0,4}(?![0-9A-Fa-f:])",
        RegexOptions.CultureInvariant);

    private static readonly Regex MacAddress = new(
        @"\b(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\b",
        RegexOptions.CultureInvariant);

    public static string Save(IEnumerable<CheckResult> results)
    {
        string reportsDirectory = AppPaths.ReportsDirectory;
        Directory.CreateDirectory(reportsDirectory);

        string reportPath = Path.Combine(
            reportsDirectory,
            $"OpSec_Report_{DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}.txt");

        File.WriteAllText(reportPath, CreateContent(results));
        return reportPath;
    }

    private static string CreateContent(IEnumerable<CheckResult> results)
    {
        var builder = new StringBuilder();
        builder.Append(CreatePrivacyPreservingSystemHeader());
        builder.AppendLine(
            $"OpSec Audit Testergebnisse - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
        builder.AppendLine("==========================================");
        builder.AppendLine();
        builder.AppendLine("Datenschutzhinweis: IP- und MAC-Adressen werden im Export automatisch redigiert.");
        builder.AppendLine();

        foreach (CheckResult result in results.OrderBy(result => result.Status.SortOrder()))
        {
            builder.AppendLine($"[{result.Status.ToString().ToUpperInvariant()}] [{result.Category}] {result.Name}");
            builder.AppendLine($"Zusammenfassung: {RedactNetworkIdentifiers(result.Summary)}");
            builder.AppendLine($"Details: {RedactNetworkIdentifiers(result.Details)}");
            builder.AppendLine(new string('-', 40));
        }

        return builder.ToString();
    }

    private static string CreatePrivacyPreservingSystemHeader()
    {
        SystemInfoSnapshot snapshot = SystemInfoService.GetSnapshot();
        var builder = new StringBuilder();
        builder.AppendLine("==========================================");
        builder.AppendLine(" SYSTEM- & UMGEBUNGS-DIAGNOSE");
        builder.AppendLine("==========================================");
        builder.AppendLine($"Betriebssystem:      {snapshot.OperatingSystem}");
        builder.AppendLine($"Kernel-Version:      {snapshot.KernelVersion}");
        builder.AppendLine($"OS-Architektur:      {snapshot.OsArchitecture}");
        builder.AppendLine($"CPU-Kerne (Logisch): {snapshot.LogicalProcessorCount}");
        builder.AppendLine($"Arbeitsspeicher:     {snapshot.TotalMemory}");
        builder.AppendLine($"Anwendungs-Version:  {snapshot.ApplicationVersion}");
        builder.AppendLine($".NET Runtime:        {snapshot.DotnetRuntime}");
        builder.AppendLine($"Laufzeit-RID:        {snapshot.RuntimeIdentifier}");
        builder.AppendLine($"Display-Server:      {snapshot.DisplayServer}");
        builder.AppendLine($"Desktop-Umgebung:    {snapshot.DesktopEnvironment}");
        builder.AppendLine("Hostname:            [NICHT EXPORTIERT]");
        builder.AppendLine("Lokale IP:           [NICHT EXPORTIERT]");
        builder.AppendLine("MAC-Adresse:         [NICHT EXPORTIERT]");
        builder.AppendLine("==========================================");
        builder.AppendLine();
        return builder.ToString();
    }

    private static string RedactNetworkIdentifiers(string text)
    {
        string redacted = Ipv4Address.Replace(text, "[IP REDIGIERT]");
        redacted = Ipv6Address.Replace(redacted, "[IP REDIGIERT]");
        return MacAddress.Replace(redacted, "[MAC REDIGIERT]");
    }
}
