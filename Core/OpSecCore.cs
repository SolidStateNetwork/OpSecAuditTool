using System;
using System.Threading.Tasks;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.Core;

/// <summary>
/// Einheitlicher Ausgang einer Sicherheitsprüfung.
/// </summary>
public enum CheckStatus
{
    Pass,
    Warning,
    Fail
}

public static class CheckStatusExtensions
{
    /// <summary>
    /// Sortiert sichere Ergebnisse zuerst und kritische Ergebnisse zuletzt.
    /// </summary>
    public static int SortOrder(this CheckStatus status) => status switch
    {
        CheckStatus.Pass => 0,
        CheckStatus.Warning => 1,
        CheckStatus.Fail => 2,
        _ => 3
    };
}

/// <summary>
/// Fachliches Ergebnis eines Checkers; enthält keine UI-spezifischen Typen.
/// </summary>
public sealed class CheckResult
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public CheckStatus Status { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Vertrag für eine unabhängige, asynchron ausführbare Audit-Prüfung.
/// </summary>
public interface IOpSecChecker
{
    string Name { get; }
    string Category { get; }
    Task<CheckResult> ExecuteAsync();
}

/// <summary>
/// Abstrakte Basisklasse für OpSec-Checker zur Reduzierung von Boilerplate-Code und
/// Standardisierung von Fehlerbehandlung und Protokollierung.
/// </summary>
public abstract class OpSecCheckerBase : IOpSecChecker
{
    public abstract string Name { get; }
    public abstract string Category { get; }

    public async Task<CheckResult> ExecuteAsync()
    {
        Logger.LogTrace($"Starte Prüfung '{Name}'...");
        try
        {
            return await PerformCheckAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Prüfung '{Name}' mit Warnung beendet: {ex.Message}");
            return ErrorResult("Die Prüfung konnte nicht vollständig ausgeführt werden.", ex);
        }
    }

    protected abstract Task<CheckResult> PerformCheckAsync();

    protected CheckResult Pass(string summary, string details = "") =>
        Result(CheckStatus.Pass, summary, details);

    protected CheckResult Warning(string summary, string details = "") =>
        Result(CheckStatus.Warning, summary, details);

    protected CheckResult Fail(string summary, string details = "") =>
        Result(CheckStatus.Fail, summary, details);

    protected CheckResult Result(CheckStatus status, string summary, string details = "") => new()
    {
        Name = Name,
        Category = Category,
        Status = status,
        Summary = summary,
        Details = details
    };

    protected CheckResult ErrorResult(string summary, Exception ex) =>
        Result(CheckStatus.Warning, summary, $"Fehler: {ex.Message}");
}

