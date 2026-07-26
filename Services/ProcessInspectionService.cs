using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OpSecAuditTool.Services;

/// <summary>
/// Liest laufende Prozessnamen aus und gibt alle nativen Process-Handles zuverlässig frei.
/// </summary>
public static class ProcessInspectionService
{
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

    private static IReadOnlyList<string> GetRunningNames()
    {
        Process[] processes = Process.GetProcesses();

        try
        {
            return processes.Select(process => process.ProcessName).ToArray();
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
