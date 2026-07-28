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
/// Ergebnis einer automatischen Sofort-Härtung (Quick-Fix).
/// </summary>
public sealed class FixResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
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
    public bool CanFix { get; set; }
    public string FixDescription { get; set; } = string.Empty;
    public IOpSecChecker? Checker { get; set; }
}

/// <summary>
/// Vertrag für eine unabhängige, asynchron ausführbare Audit-Prüfung.
/// </summary>
public interface IOpSecChecker
{
    string Name { get; }
    string Category { get; }
    bool CanFix => false;
    string FixDescription => string.Empty;
    Task<CheckResult> ExecuteAsync();
    Task<FixResult> FixAsync() => Task.FromResult(new FixResult { Success = false, Message = "Für diese Prüfung ist kein automatischer Fix verfügbar." });
}

/// <summary>
/// Abstrakte Basisklasse für OpSec-Checker zur Reduzierung von Boilerplate-Code und
/// Standardisierung von Fehlerbehandlung und Protokollierung.
/// </summary>
public abstract class OpSecCheckerBase : IOpSecChecker
{
    public abstract string Name { get; }
    public abstract string Category { get; }
    public virtual bool CanFix => false;
    public virtual string FixDescription => string.Empty;

    public virtual Task<FixResult> FixAsync() =>
        Task.FromResult(new FixResult { Success = false, Message = "Für diese Prüfung ist kein automatischer Fix verfügbar." });

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
        Details = details,
        CanFix = CanFix,
        FixDescription = FixDescription,
        Checker = this
    };

    protected CheckResult ErrorResult(string summary, Exception ex) =>
        Result(CheckStatus.Warning, summary, $"Fehler: {ex.Message}");
}

