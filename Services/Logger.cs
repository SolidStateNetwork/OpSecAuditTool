using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OpSecAuditTool.Services;

/// <summary>
/// Schweregrad eines strukturierten Protokolleintrags.
/// </summary>
public enum LogLevel
{
    Trace,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Unveränderlicher Protokolleintrag für Datei- und UI-Ausgabe.
/// </summary>
public sealed class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ExceptionDetails { get; init; }
    public bool IsRaw { get; init; }

    public override string ToString()
    {
        if (IsRaw) return Message;

        string exceptionSuffix = string.IsNullOrEmpty(ExceptionDetails)
            ? string.Empty
            : $" | Details: {ExceptionDetails}";

        return $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}{exceptionSuffix}";
    }
}

/// <summary>
/// Thread-sicherer Sitzungslogger mit portabler Dateiablage und Live-Ereignis für die UI.
/// </summary>
public static class Logger
{
    private static readonly string LogsDirectory;
    private static readonly object SyncRoot = new();
    private static readonly string SessionTimestamp =
        DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

    // Die Sitzungshistorie ermöglicht der UI, den Filter für Trace-Einträge nachträglich zu ändern.
    private static readonly List<LogEntry> SessionLogs = new();

    public static event Action<LogEntry>? OnLogAdded;

    static Logger()
    {
        try
        {
            LogsDirectory = AppPaths.LogsDirectory;
            Directory.CreateDirectory(LogsDirectory);
            CleanupOldLogs(maxFilesToKeep: 5);
        }
        catch
        {
            LogsDirectory = AppDirectoryFallback();
        }
    }

    /// <summary>
    /// Liefert eine Momentaufnahme aller Logs der aktuellen Sitzung.
    /// </summary>
    public static IReadOnlyList<LogEntry> GetSessionLogs()
    {
        lock (SyncRoot)
        {
            return SessionLogs.ToList();
        }
    }

    private static string GetCurrentLogFilePath()
    {
        string fileName = $"OpSec_Log_{SessionTimestamp}.txt";
        return Path.Combine(LogsDirectory, fileName);
    }

    private static void CleanupOldLogs(int maxFilesToKeep)
    {
        try
        {
            if (!Directory.Exists(LogsDirectory))
            {
                return;
            }

            string[] obsoleteLogFiles = Directory
                .GetFiles(LogsDirectory, "OpSec_Log_*.txt")
                .OrderByDescending(File.GetLastWriteTime)
                .Skip(maxFilesToKeep)
                .ToArray();

            foreach (string logFile in obsoleteLogFiles)
            {
                File.Delete(logFile);
            }
        }
        catch
        {
            // Logging darf den Anwendungsstart nicht verhindern.
        }
    }

    private static string AppDirectoryFallback()
    {
        try { return AppDomain.CurrentDomain.BaseDirectory; }
        catch { return Directory.GetCurrentDirectory(); }
    }

    public static void LogInfo(string message) => Log(LogLevel.Info, message);
    public static void LogWarning(string message) => Log(LogLevel.Warning, message);
    public static void LogError(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
    public static void LogCritical(string message, Exception? ex = null) => Log(LogLevel.Critical, message, ex);

    public static void LogTrace(string message) => Log(LogLevel.Trace, message);

    public static void LogRaw(string rawText)
    {
        var entry = new LogEntry
        {
            IsRaw = true,
            Message = rawText,
            Level = LogLevel.Info
        };

        WriteEntry(entry);
    }

    private static void Log(LogLevel level, string message, Exception? ex = null)
    {
        var entry = new LogEntry
        {
            Level = level,
            Message = message,
            ExceptionDetails = ex?.ToString()
        };

        WriteEntry(entry);
    }

    private static void WriteEntry(LogEntry entry)
    {
        lock (SyncRoot)
        {
            SessionLogs.Add(entry);

            try
            {
                File.AppendAllText(GetCurrentLogFilePath(), entry.ToString() + Environment.NewLine);
            }
            catch
            {
                // Die RAM-Historie und UI-Ausgabe funktionieren auch ohne Schreibzugriff weiter.
            }
        }

        // Events werden außerhalb des Locks ausgelöst, damit UI-Handler den Logger nicht blockieren.
        OnLogAdded?.Invoke(entry);
    }
}
