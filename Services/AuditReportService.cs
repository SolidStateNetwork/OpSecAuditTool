using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpSecAuditTool.Core;

namespace OpSecAuditTool.Services;

/// <summary>
/// Erstellt den textbasierten Audit-Bericht und kapselt dessen Dateispeicherung.
/// </summary>
public static class AuditReportService
{
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
        builder.Append(SystemInfoService.GetSystemReportHeader());
        builder.AppendLine(
            $"OpSec Audit Testergebnisse - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
        builder.AppendLine("==========================================");
        builder.AppendLine();

        foreach (CheckResult result in results.OrderBy(result => result.Status.SortOrder()))
        {
            builder.AppendLine($"[{result.Status.ToString().ToUpperInvariant()}] [{result.Category}] {result.Name}");
            builder.AppendLine($"Zusammenfassung: {result.Summary}");
            builder.AppendLine($"Details: {result.Details}");
            builder.AppendLine(new string('-', 40));
        }

        return builder.ToString();
    }
}
