using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core.Diagnostics;

/// <summary>
/// Analysiert lokale AI- & LLM-Entwicklertools (Continue, Aider, GitHub Copilot CLI, Cursor)
/// auf im Klartext gespeicherte API-Schlüssel oder problematische Telemetrie-Freigaben.
/// </summary>
public sealed class AiToolingPrivacyChecker : OpSecCheckerBase
{
    public override string Name => "AI-Tools & LLM-Entwicklungsumgebung Privacy-Prüfung";
    public override string Category => "Anti-Forensik / Hygiene";

    private static readonly string[] SensitiveTokenKeywords =
    {
        "api_key",
        "apiKey",
        "openai-key",
        "sk-proj-",
        "sk-ant-",
        "AIzaSy",
        "ghp_",
        "gho_"
    };

    protected override async Task<CheckResult> PerformCheckAsync()
    {
        Logger.LogTrace("Starte Prüfung der AI- & LLM-Tooling Konfigurationsdateien...");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] targetConfigs =
        {
            Path.Combine(homeDir, ".continue", "config.json"),
            Path.Combine(homeDir, ".continue", "config.yaml"),
            Path.Combine(homeDir, ".aider.conf.yml"),
            Path.Combine(homeDir, ".config", "github-copilot", "hosts.json"),
            Path.Combine(homeDir, ".cursor", "config.json")
        };

        var exposedConfigs = new List<string>();

        foreach (var file in targetConfigs)
        {
            if (!File.Exists(file)) continue;
            try
            {
                string content = await File.ReadAllTextAsync(file);
                foreach (var kw in SensitiveTokenKeywords)
                {
                    if (content.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        exposedConfigs.Add($"{Path.GetFileName(file)} (Enthält potenziellen Token / Keyword '{kw}')");
                        break;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Keinen Absturz provozieren
            }
        }

        if (exposedConfigs.Count > 0)
        {
            return Warning(
                $"{exposedConfigs.Count} AI-/LLM-Konfigurationsdatei(en) mit potenziellen Klartext-Tokens entdeckt!",
                $"Folgende Konfigurationsdateien lokaler AI-Assistenztools enthalten Klartext-Schlüssel oder API-Token-Muster:\n• {string.Join("\n• ", exposedConfigs)}\n\n" +
                "Empfehlung: Nutze für API-Schlüssel nach Möglichkeit Umgebungsvariablen oder den System-Keyring und schließe diese Dateien in Backups/Git aus.");
        }

        return Pass(
            "Keine Klartext-Tokens in AI- / LLM-Tool Konfigurationen entdeckt.",
            "Geprüfte Konfigurationsdateien von Continue, Aider, GitHub Copilot und Cursor weisen keine unverschlüsselten API-Schlüssel auf.");
    }
}
