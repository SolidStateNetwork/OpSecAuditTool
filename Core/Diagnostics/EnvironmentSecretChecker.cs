using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Prüft Umgebungsvariablen auf Namen, die auf versehentlich exponierte Geheimnisse hindeuten.
/// Erweitert um moderne Cloud-, AI- und Developer-Tokens (OpenAI, Gemini, Anthropic, AWS, GCP etc.).
/// </summary>
public sealed class EnvironmentSecretChecker : OpSecCheckerBase
{
    private static readonly string[] SensitiveNameFragments =
    {
        "API_KEY", "APIKEY", "ACCESS_TOKEN", "AUTH_TOKEN", "BEARER_TOKEN",
        "PASSWORD", "PASSWD", "PRIVATE_KEY", "CLIENT_SECRET", "AWS_SECRET",
        "GITHUB_TOKEN", "GITLAB_TOKEN", "SECRET_KEY", "DB_PASS", "DB_PASSWORD",
        "OPENAI_API_KEY", "GEMINI_API_KEY", "ANTHROPIC_API_KEY", "AZURE_KEY",
        "GCP_KEY", "SERVICE_ACCOUNT", "JWT_SECRET", "HUGGINGFACE_TOKEN",
        "NPM_TOKEN", "SLACK_TOKEN", "DISCORD_TOKEN"
    };

    public override string Name => "Sensible Umgebungsvariablen & AI/Cloud-Tokens";
    public override string Category => "OpSec / Zugangsdaten";

    protected override Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Prüfe Namen vererbter Umgebungsvariablen auf Zugangsdaten und AI/Cloud-Tokens.");
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

        if (matchingNames.Count == 0)
        {
            Logger.LogInfo("Keine bekannten API-Tokens oder Geheimnisse in Umgebungsvariablen gefunden.");
            return Task.FromResult(Pass(
                "Keine typischen Zugangsdaten in Umgebungsvariablen erkannt.",
                "Variablennamen wurden gegen moderne AI/Cloud-Tokenmuster geprüft; Werte werden aus Sicherheitsgründen niemals protokolliert."));
        }

        Logger.LogWarning($"{matchingNames.Count} sensible Umgebungsvariable(n) mit potenziellen Tokens entdeckt.");
        return Task.FromResult(Warning(
            $"{matchingNames.Count} potenziell sensible Umgebungsvariable(n) erkannt.",
            $"Betroffene Namen:\n• {string.Join("\n• ", matchingNames)}\n\n" +
            "Umgebungsvariablen werden an Kindprozesse vererbt und können in Diagnose- oder Absturzberichten auftauchen. " +
            "Die Werte wurden bewusst weder angezeigt noch gespeichert. Empfehlung: Nutze für API-Keys einen Secret-Manager oder flüchtige Shell-Scopes."));
    }
}
