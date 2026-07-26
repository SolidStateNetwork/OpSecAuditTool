using System.Threading.Tasks;

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
