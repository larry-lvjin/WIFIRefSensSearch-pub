using System.Collections.Generic;

namespace WIFISensitivitySearch.Models;

public class TestProfile
{
    public required string DisplayName { get; init; }
    public required string StandardKey { get; init; }
    public required string Bandwidth { get; init; }
    public required string RateLabel { get; init; }
    public required string FdefFormat { get; init; }
    public required int PayloadSize { get; init; }
    public required double PerLimitPercent { get; init; }
    /// <summary>Intermediate levels (dBm) to walk through after association, before the probe begins.
    /// E.g. for 11b: [-55, -65, -75] → step down to probe-start. Prevents sudden link loss from a -45 → -83 jump.</summary>
    public required double[] RampDownLevelsDbm { get; init; }
    /// <summary>Probe-phase starting level (dBm). The probe steps down from here by 0.5 dB.</summary>
    public required double InitialLevelDbm { get; init; }
    public required int[] Channels { get; init; }

    public static IReadOnlyList<TestProfile> All { get; } = new TestProfile[]
    {
        new()
        {
            DisplayName = "11b 20M 2.4G CCK_11M",
            StandardKey = "11b",
            Bandwidth = "BW20",
            RateLabel = "CCK_11M",
            FdefFormat = "NHT",
            PayloadSize = 1024,
            PerLimitPercent = 8,
            RampDownLevelsDbm = new[] { -55.0, -65.0, -75.0 },
            InitialLevelDbm = -83,
            Channels = new[] { 3, 7, 11 },
        },
        new()
        {
            DisplayName = "11g OFDM 20M 2.4G OFDM_54M",
            StandardKey = "11g",
            Bandwidth = "BW20",
            RateLabel = "OFDM_54M",
            FdefFormat = "NHT",
            PayloadSize = 1000,
            PerLimitPercent = 10,
            RampDownLevelsDbm = new[] { -55.0, -65.0 },
            InitialLevelDbm = -73,
            Channels = new[] { 3, 7, 13 },
        },
        new()
        {
            DisplayName = "11g/n 20M 2.4G MCS7",
            StandardKey = "11gn",
            Bandwidth = "BW20",
            RateLabel = "MCS7",
            FdefFormat = "HT",
            PayloadSize = 1024,
            PerLimitPercent = 10,
            RampDownLevelsDbm = new[] { -55.0, -65.0 },
            InitialLevelDbm = -68,
            Channels = new[] { 3, 7, 13 },
        },
        new()
        {
            DisplayName = "11g/n 40M 2.4G MCS7",
            StandardKey = "11gn",
            Bandwidth = "BW40",
            RateLabel = "MCS7",
            FdefFormat = "HT",
            PayloadSize = 1024,
            PerLimitPercent = 10,
            RampDownLevelsDbm = new[] { -55.0, -60.0 },
            InitialLevelDbm = -63,
            Channels = new[] { 3, 7, 11 },
        },
        new()
        {
            DisplayName = "11a 20M 5G OFDM_54M",
            StandardKey = "11a",
            Bandwidth = "BW20",
            RateLabel = "OFDM_54M",
            FdefFormat = "NHT",
            PayloadSize = 1000,
            PerLimitPercent = 10,
            RampDownLevelsDbm = new[] { -55.0, -65.0 },
            InitialLevelDbm = -73,
            Channels = new[] { 36, 100, 149 },
        },
        new()
        {
            DisplayName = "11a/n 20M 5G MCS7",
            StandardKey = "11an",
            Bandwidth = "BW20",
            RateLabel = "MCS7",
            FdefFormat = "HT",
            PayloadSize = 1024,
            PerLimitPercent = 10,
            RampDownLevelsDbm = new[] { -55.0, -65.0 },
            InitialLevelDbm = -68,
            Channels = new[] { 36, 100, 149 },
        },
        new()
        {
            DisplayName = "11a/n 40M 5G MCS7",
            StandardKey = "11an",
            Bandwidth = "BW40",
            RateLabel = "MCS7",
            FdefFormat = "HT",
            PayloadSize = 1024,
            PerLimitPercent = 10,
            RampDownLevelsDbm = new[] { -55.0, -60.0 },
            InitialLevelDbm = -63,
            Channels = new[] { 38, 102, 159 },
        },
    };
}
