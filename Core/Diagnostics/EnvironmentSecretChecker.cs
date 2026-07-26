using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Prüft Umgebungsvariablen auf Namen, die auf versehentlich exponierte Geheimnisse hindeuten.
/// </summary>
public sealed class EnvironmentSecretChecker : IOpSecChecker
{
    private static readonly string[] SensitiveNameFragments =
    {
        "API_KEY", "APIKEY", "ACCESS_TOKEN", "AUTH_TOKEN", "BEARER_TOKEN",
        "PASSWORD", "PASSWD", "PRIVATE_KEY", "CLIENT_SECRET", "AWS_SECRET",
        "GITHUB_TOKEN", "GITLAB_TOKEN"
    };

    public string Name => "Sensible Umgebungsvariablen";
    public string Category => "OpSec / Zugangsdaten";

    public Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace("Prüfe Namen vererbter Umgebungsvariablen auf Zugangsdaten.");
        var matchingNames = new List<string>();

        foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
        {
            string name = variable.Key?.ToString() ?? string.Empty;
            string value = variable.Value?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value) &&
                SensitiveNameFragments.Any(fragment =>
                    name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                matchingNames.Add(name);
            }
        }

        matchingNames = matchingNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(matchingNames.Count == 0
            ? Result(
                CheckStatus.Pass,
                "Keine typischen Zugangsdaten in Umgebungsvariablen erkannt.",
                "Variablennamen wurden geprüft; Werte werden aus Sicherheitsgründen niemals protokolliert.")
            : Result(
                CheckStatus.Warning,
                $"{matchingNames.Count} potenziell sensible Umgebungsvariable(n) erkannt.",
                $"Betroffene Namen:\n• {string.Join("\n• ", matchingNames)}\n\n" +
                "Umgebungsvariablen werden an Kindprozesse vererbt und können in Diagnose- oder Absturzberichten auftauchen. " +
                "Die Werte wurden bewusst weder angezeigt noch gespeichert."));
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
