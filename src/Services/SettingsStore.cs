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
        try
        {
            Directory.CreateDirectory(DirPath);
            var json = JsonSerializer.Serialize(overrides, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Swallow — settings persistence is best-effort.
        }
    }

    public static double LoadRxExpectedPep()
    {
        try
        {
            if (!File.Exists(EpePath)) return DefaultRxExpectedPepDbm;
            var text = File.ReadAllText(EpePath).Trim();
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
        }
        catch { }
        return DefaultRxExpectedPepDbm;
    }

    public static void SaveRxExpectedPep(double value)
    {
        try
        {
            Directory.CreateDirectory(DirPath);
            File.WriteAllText(EpePath, value.ToString("0.0", CultureInfo.InvariantCulture));
        }
        catch
        {
            // Swallow.
        }
    }

    public static string OverridesPath => FilePath;
}

