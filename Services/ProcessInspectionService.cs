using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OpSecAuditTool.Services;

/// <summary>
/// Liest laufende Prozessnamen aus und gibt alle nativen Process-Handles zuverlässig frei.
/// Nutzt einen Kurzzeit-Cache (500 ms), um Systemaufrufe bei parallelen Abfragen zu reduzieren.
/// </summary>
public static class ProcessInspectionService
{
    private static readonly object CacheLock = new();
    private static string[]? _cachedNames;
    private static DateTime _cacheExpiration = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMilliseconds(500);

    public static bool IsAnyRunning(params string[] processNames)
    {
        HashSet<string> expectedNames = processNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return GetRunningNames().Any(expectedNames.Contains);
    }

    public static IReadOnlyList<string> FindRunning(IEnumerable<string> processNames)
    {
        HashSet<string> runningNames = GetRunningNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        return processNames
            .Where(runningNames.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void InvalidateCache()
    {
        lock (CacheLock)
        {
            _cachedNames = null;
            _cacheExpiration = DateTime.MinValue;
        }
    }

    private static string[] GetRunningNames()
    {
        lock (CacheLock)
        {
            if (_cachedNames != null && DateTime.UtcNow < _cacheExpiration)
            {
                return _cachedNames;
            }
        }

        Process[] processes = Process.GetProcesses();

        try
        {
            string[] names = processes.Select(process => process.ProcessName).ToArray();

            lock (CacheLock)
            {
                _cachedNames = names;
                _cacheExpiration = DateTime.UtcNow.Add(CacheDuration);
            }

            return names;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }
}
