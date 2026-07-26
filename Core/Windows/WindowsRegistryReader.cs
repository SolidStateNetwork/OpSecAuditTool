using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace OpSecAuditTool.Core.Windows;

/// <summary>
/// Liest sicherheitsrelevante Windows-Konfigurationen ohne Schreibzugriff und ohne
/// Anforderung erhöhter Rechte. Nicht lesbare Werte werden als unbekannt behandelt.
/// </summary>
internal static class WindowsRegistryReader
{
    public static int? ReadLocalMachineInt32(string subKey, string valueName) =>
        OperatingSystem.IsWindows()
            ? ReadInt32(RegistryHive.LocalMachine, subKey, valueName)
            : null;

    public static int? ReadCurrentUserInt32(string subKey, string valueName) =>
        OperatingSystem.IsWindows()
            ? ReadInt32(RegistryHive.CurrentUser, subKey, valueName)
            : null;

    public static IReadOnlyList<string> ReadLocalMachineValueNames(string subKey) =>
        OperatingSystem.IsWindows()
            ? ReadValueNames(RegistryHive.LocalMachine, subKey)
            : [];

    public static IReadOnlyList<string> ReadCurrentUserValueNames(string subKey) =>
        OperatingSystem.IsWindows()
            ? ReadValueNames(RegistryHive.CurrentUser, subKey)
            : [];

    [SupportedOSPlatform("windows")]
    private static int? ReadInt32(
        RegistryHive hive,
        string subKey,
        string valueName)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(
                hive,
                Environment.Is64BitOperatingSystem
                    ? RegistryView.Registry64
                    : RegistryView.Registry32);
            using RegistryKey? key = baseKey.OpenSubKey(subKey, writable: false);
            object? value = key?.GetValue(valueName);
            return value switch
            {
                int intValue => intValue,
                long longValue when longValue is >= int.MinValue and <= int.MaxValue =>
                    (int)longValue,
                string text when int.TryParse(text, out int parsed) => parsed,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<string> ReadValueNames(
        RegistryHive hive,
        string subKey)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(
                hive,
                Environment.Is64BitOperatingSystem
                    ? RegistryView.Registry64
                    : RegistryView.Registry32);
            using RegistryKey? key = baseKey.OpenSubKey(subKey, writable: false);
            return key?.GetValueNames() ?? [];
        }
        catch
        {
            return [];
        }
    }
}
