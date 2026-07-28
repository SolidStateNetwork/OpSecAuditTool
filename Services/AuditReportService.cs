using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using OpSecAuditTool.Core;

namespace OpSecAuditTool.Services;

/// <summary>
/// Erstellt Audit-Berichte in den Formaten Text, JSON und Markdown und kapselt deren Speicherung.
/// </summary>
public static class AuditReportService
{
    public static string Save(IEnumerable<CheckResult> results)
    {
        string reportsDirectory = EnsureReportsDirectory();
        string reportPath = Path.Combine(
            reportsDirectory,
            $"OpSec_Report_{GetTimestamp()}.txt");

        File.WriteAllText(reportPath, CreateTxtContent(results), Encoding.UTF8);
        return reportPath;
    }

    public static string SaveJson(IEnumerable<CheckResult> results)
    {
        string reportsDirectory = EnsureReportsDirectory();
        string reportPath = Path.Combine(
            reportsDirectory,
            $"OpSec_Report_{GetTimestamp()}.json");

        List<CheckResult> resultsList = results.OrderBy(result => result.Status.SortOrder()).ToList();
        var reportObject = new
        {
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            SystemSnapshot = SystemInfoService.GetSnapshot(),
            Summary = new
            {
                Total = resultsList.Count,
                Passed = resultsList.Count(result => result.Status == CheckStatus.Pass),
                Warning = resultsList.Count(result => result.Status == CheckStatus.Warning),
                Critical = resultsList.Count(result => result.Status == CheckStatus.Fail)
            },
            Results = resultsList
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        File.WriteAllText(reportPath, JsonSerializer.Serialize(reportObject, options), Encoding.UTF8);
        return reportPath;
    }

    public static string SaveMarkdown(IEnumerable<CheckResult> results)
    {
        string reportsDirectory = EnsureReportsDirectory();
        string reportPath = Path.Combine(
            reportsDirectory,
            $"OpSec_Report_{GetTimestamp()}.md");

        File.WriteAllText(reportPath, CreateMarkdownContent(results), Encoding.UTF8);
        return reportPath;
    }

    private static string EnsureReportsDirectory()
    {
        string reportsDirectory = AppPaths.ReportsDirectory;
        Directory.CreateDirectory(reportsDirectory);
        return reportsDirectory;
    }

    private static string GetTimestamp() =>
        DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

    private static string CreateTxtContent(IEnumerable<CheckResult> results)
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

    private static string CreateMarkdownContent(IEnumerable<CheckResult> results)
    {
        List<CheckResult> resultsList = results.OrderBy(result => result.Status.SortOrder()).ToList();
        SystemInfoSnapshot snapshot = SystemInfoService.GetSnapshot();

        int passed = resultsList.Count(result => result.Status == CheckStatus.Pass);
        int warnings = resultsList.Count(result => result.Status == CheckStatus.Warning);
        int criticals = resultsList.Count(result => result.Status == CheckStatus.Fail);
        int total = resultsList.Count;
        int score = total > 0 ? (passed * 100) / total : 0;

        var sb = new StringBuilder();
        sb.AppendLine("# 🛡️ OpSec Audit Security Report");
        sb.AppendLine();
        sb.AppendLine($"**Erstellt am:** {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}  ");
        sb.AppendLine($"**System:** {snapshot.OperatingSystem} ({snapshot.OsArchitecture})  ");
        sb.AppendLine($"**Hostname:** {snapshot.Hostname}  ");
        sb.AppendLine($"**Sicherheits-Score:** **{score}%**");
        sb.AppendLine();

        sb.AppendLine("## 📊 Übersicht");
        sb.AppendLine();
        sb.AppendLine("| Gesamt | Bestanden ✅ | Warnungen ⚠️ | Kritisch ❌ |");
        sb.AppendLine("|:------:|:------------:|:------------:|:-----------:|");
        sb.AppendLine($"| **{total}** | {passed} | {warnings} | {criticals} |");
        sb.AppendLine();

        sb.AppendLine("## 🔍 Testergebnisse");
        sb.AppendLine();
        sb.AppendLine("| Status | Kategorie | Prüfung | Zusammenfassung |");
        sb.AppendLine("|:------:|:---------:|:--------|:----------------|");

        foreach (CheckResult result in resultsList)
        {
            string badge = result.Status switch
            {
                CheckStatus.Pass => "✅ PASS",
                CheckStatus.Warning => "⚠️ WARNING",
                CheckStatus.Fail => "❌ CRITICAL",
                _ => result.Status.ToString()
            };

            string safeSummary = result.Summary.Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|");
            sb.AppendLine($"| {badge} | {result.Category} | {result.Name} | {safeSummary} |");
        }

        sb.AppendLine();
        sb.AppendLine("## 📝 Detailnotizen");
        sb.AppendLine();

        foreach (CheckResult result in resultsList)
        {
            string icon = result.Status switch
            {
                CheckStatus.Pass => "✅",
                CheckStatus.Warning => "⚠️",
                CheckStatus.Fail => "❌",
                _ => "ℹ️"
            };

            sb.AppendLine($"### {icon} {result.Name} ({result.Category})");
            sb.AppendLine($"**Zusammenfassung:** {result.Summary}  ");
            if (!string.IsNullOrWhiteSpace(result.Details))
            {
                sb.AppendLine();
                sb.AppendLine("```text");
                sb.AppendLine(result.Details.Trim());
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
