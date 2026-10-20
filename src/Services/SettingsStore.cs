using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace WIFISensitivitySearch.Services;

/// <summary>
/// Persists per-profile "Start" (probe-start dBm) overrides and the global RX Expected PEP value
/// to JSON files in LocalAppData. Best-effort I/O — errors swallowed.
/// </summary>
public static class SettingsStore
{
    private static string DirPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WIFISensitivitySearch");
    private static string FilePath => Path.Combine(DirPath, "start_overrides.json");
    private static string EpePath  => Path.Combine(DirPath, "rx_expected_pep.json");

    public const double DefaultRxExpectedPepDbm = 30.0;

    public static Dictionary<string, double> LoadStartOverrides()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var json = File.ReadAllText(FilePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
            return dict ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SaveStartOverrides(Dictionary<string, double> overrides)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public static double LoadRxExpectedPep()
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public static void SaveRxExpectedPep(double value)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    public static string OverridesPath => FilePath;
}

