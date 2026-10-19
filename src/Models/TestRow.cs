using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WIFISensitivitySearch.Models;

public class TestRow : INotifyPropertyChanged
{
    private TestProfile _profile = null!;
    public required TestProfile Profile
    {
        get => _profile;
        init
        {
            _profile = value;
            // Default Start to the profile's probe-start; can be overridden after construction.
            _start = value.InitialLevelDbm;
        }
    }

    public string Type => Profile.DisplayName;
    public string Threshold => $"PER<{Profile.PerLimitPercent:0.#}%";

    private bool _isSelected = true;
    /// <summary>行是否参与测试。勾选=执行, 不勾选=跳过。默认勾选。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    private double _start;
    /// <summary>Probe-start level (dBm). Editable from the UI; persisted across sessions.</summary>
    public double Start
    {
        get => _start;
        set
        {
            if (Math.Abs(_start - value) < 0.0001) return;
            _start = value;
            OnPropertyChanged();
        }
    }

    public string Channel1Label => $"CH{Profile.Channels[0]}";
    public string Channel2Label => $"CH{Profile.Channels[1]}";
    public string Channel3Label => $"CH{Profile.Channels[2]}";

    private string _channel1Result = string.Empty;
    public string Channel1Result { get => _channel1Result; set => Set(ref _channel1Result, value); }

    private string _channel2Result = string.Empty;
    public string Channel2Result { get => _channel2Result; set => Set(ref _channel2Result, value); }

    private string _channel3Result = string.Empty;
    public string Channel3Result { get => _channel3Result; set => Set(ref _channel3Result, value); }

    public void SetChannelResult(int channelIndex, string value)
    {
        switch (channelIndex)
        {
            case 0: Channel1Result = value; break;
            case 1: Channel2Result = value; break;
            case 2: Channel3Result = value; break;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
