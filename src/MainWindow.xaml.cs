using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WIFISensitivitySearch.Models;
using WIFISensitivitySearch.Services;

namespace WIFISensitivitySearch
{
    public partial class MainWindow : Window
    {
        private readonly ScpiClient _scpi = new();
        private readonly DutSerialClient _dut = new();
        private readonly ObservableCollection<TestRow> _rows = new();
        private CancellationTokenSource? _runCts;
        private TaskCompletionSource? _pauseTcs;
        private Task? _runTask;

        public MainWindow()
        {
            InitializeComponent();
            BuildRows();
            ResultGrid.ItemsSource = _rows;
            _dut.LogSink = Log;
            _dut.ConnectionStatusChanged += OnDutStatusChanged;
            PopulateDutPorts();
            // Load the persisted RX Expected PEP value (default 30) into the textbox.
            RxExpectedPepBox.Text = SettingsStore.LoadRxExpectedPep().ToString("0.#", CultureInfo.InvariantCulture);
        }

        private void RxExpectedPepBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = (RxExpectedPepBox.Text ?? "").Trim();
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                // Revert to last saved value on bad input.
                RxExpectedPepBox.Text = SettingsStore.LoadRxExpectedPep().ToString("0.#", CultureInfo.InvariantCulture);
                return;
            }
            SettingsStore.SaveRxExpectedPep(v);
        }

        private void OnDutStatusChanged(string status)
        {
            Log($"DUT CONNECTION STATUS: {status}");
            Dispatcher.BeginInvoke(() => UpdateDutStatusLabel());
        }

        private void UpdateDutStatusLabel()
        {
            if (!_dut.IsOpen)
            {
                DutStatusText.Text = "Not Connected";
                DutStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x8F, 0xA0, 0xC7));
                DutStatusDot.Fill = new SolidColorBrush(Color.FromRgb(0xD4, 0x3F, 0x3F));
                return;
            }
            var status = _dut.ConnectionStatus;
            string label = string.IsNullOrEmpty(status)
                ? $"Connected ({_dut.PortName})"
                : $"Connected ({_dut.PortName}) — {status}";
            DutStatusText.Text = label;
            // Green when DUT is non-idle (test-ready); amber when idle (needs AT+WLCONN).
            bool ready = !_dut.IsIdle;
            DutStatusText.Foreground = new SolidColorBrush(
                ready
                    ? Color.FromRgb(0x2B, 0xB6, 0x73)
                    : Color.FromRgb(0xE0, 0x8A, 0x2A));
            DutStatusDot.Fill = new SolidColorBrush(
                ready
                    ? Color.FromRgb(0x2B, 0xB6, 0x73)
                    : Color.FromRgb(0xE0, 0x8A, 0x2A));
        }

        private void PopulateDutPorts()
        {
            var ports = DutSerialClient.ListPortNames();
            DutPortBox.Items.Clear();
            foreach (var p in ports) DutPortBox.Items.Add(p);
            var preferred = ports.FirstOrDefault(p => p.Equals("COM19", StringComparison.OrdinalIgnoreCase))
                            ?? ports.FirstOrDefault()
                            ?? "COM19";
            DutPortBox.Text = preferred;
        }

        private void BuildRows()
        {
            _rows.Clear();
            var overrides = SettingsStore.LoadStartOverrides();
            foreach (var p in TestProfile.All)
            {
                var row = new TestRow { Profile = p };
                if (overrides.TryGetValue(p.DisplayName, out var ov))
                    row.Start = ov;
                row.PropertyChanged += Row_PropertyChanged;
                _rows.Add(row);
            }
        }

        private void Row_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(TestRow.Start)) return;
            // Persist only rows whose Start differs from the profile default; keep file lean.
            var dict = new Dictionary<string, double>();
            foreach (var r in _rows)
            {
                if (Math.Abs(r.Start - r.Profile.InitialLevelDbm) > 0.0001)
                    dict[r.Profile.DisplayName] = r.Start;
            }
            SettingsStore.SaveStartOverrides(dict);
            if (sender is TestRow tr)
                Log($"Start override saved: {tr.Profile.DisplayName} = {tr.Start:0.#} dBm");
        }

        private void Log(string msg)
        {
            void append()
            {
                LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                LogBox.ScrollToEnd();
            }
            if (Dispatcher.CheckAccess()) append();
            else Dispatcher.BeginInvoke(append);
        }

        private void SetStatus(bool connected)
        {
            void apply()
            {
                StatusText.Text = connected ? "Connected" : "Not Connected";
                StatusText.Foreground = connected
                    ? new SolidColorBrush(Color.FromRgb(0x2B, 0xB6, 0x73))
                    : new SolidColorBrush(Color.FromRgb(0x8F, 0xA0, 0xC7));
                StatusDot.Fill = connected
                    ? new SolidColorBrush(Color.FromRgb(0x2B, 0xB6, 0x73))
                    : new SolidColorBrush(Color.FromRgb(0xD4, 0x3F, 0x3F));
                ConnectButton.IsEnabled = !connected;
                DisconnectButton.IsEnabled = connected;
                StartButton.IsEnabled = connected;
            }
            if (Dispatcher.CheckAccess()) apply();
            else Dispatcher.BeginInvoke(apply);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (LicenseGuard.IsExpired) return;

            var addr = (VisaAddressBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(addr)) { Log("VISA address is empty."); return; }

            ConnectButton.IsEnabled = false;
            try
            {
                Log($"Connecting to {addr} ...");
                await _scpi.ConnectAsync(addr);
                var idn = await _scpi.QueryAsync("*IDN?", timeoutMs: 3000);
                Log($"*IDN? -> {idn}");
                await _scpi.WriteAsync("*CLS");
                await _scpi.CheckErrorAsync();
                // Apply the user-configured (or default 30) RX Expected PEP — one-shot global write.
                if (!double.TryParse((RxExpectedPepBox.Text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var epe))
                    epe = SettingsStore.DefaultRxExpectedPepDbm;
                await _scpi.WriteAsync($"CONFigure:WLAN:SIGNaling1:RFSettings:EPEPower {epe.ToString("0.#", CultureInfo.InvariantCulture)}");
                var (epeOk, epeRaw) = await _scpi.CheckErrorAsync();
                if (epeOk) Log($"Global RFSettings:EPEPower = {epe:0.#} dBm");
                else       Log($"WARN: EPEPower={epe:0.#} not accepted → {epeRaw}");
                SetStatus(true);
            }
            catch (Exception ex)
            {
                Log($"Connect failed: {ex.Message}");
                _scpi.Disconnect();
                SetStatus(false);
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try { _scpi.Disconnect(); Log("Disconnected."); }
            catch (Exception ex) { Log($"Disconnect error: {ex.Message}"); }
            SetStatus(false);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_scpi.IsConnected) { Log("Not connected."); return; }
            if (_runTask is { IsCompleted: false }) { Log("Test already running."); return; }

            double cableLoss = 0.5;
            if (!double.TryParse((CableLossBox.Text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out cableLoss))
            {
                Log("Cable Loss is not a number; using 0.5 dB.");
                cableLoss = 0.5;
            }

            var modeProfiles = SelectedProfiles().ToHashSet();
            // 只执行「勾选」且符合当前 Test Mode 的行; 保持 TestProfile.All 的顺序。
            var selected = _rows.Where(r => r.IsSelected && modeProfiles.Contains(r.Profile))
                                .Select(r => r.Profile).ToList();
            if (selected.Count == 0) { Log("No profile selected (检查 Test Mode 与行勾选框)."); return; }

            foreach (var r in _rows)
            {
                if (selected.Contains(r.Profile))
                {
                    r.Channel1Result = string.Empty;
                    r.Channel2Result = string.Empty;
                    r.Channel3Result = string.Empty;
                }
            }

            _runCts = new CancellationTokenSource();
            _pauseTcs = null;
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;

            var tester = new SensitivityTester(_scpi, _dut)
            {
                LogSink = Log,
                Token = _runCts.Token,
                PauseGate = async () =>
                {
                    var tcs = _pauseTcs;
                    if (tcs is not null) await tcs.Task.ConfigureAwait(false);
                },
            };

            _runTask = Task.Run(async () =>
            {
                try
                {
                    Log($"Starting tests — {selected.Count} profile(s), cable loss {cableLoss:0.0} dB.");
                    foreach (var prof in selected)
                    {
                        _runCts.Token.ThrowIfCancellationRequested();
                        var row = _rows.First(r => ReferenceEquals(r.Profile, prof));
                        await tester.RunRowAsync(row, cableLoss);
                    }
                    Log("All tests complete.");
                }
                catch (OperationCanceledException)
                {
                    Log("Tests cancelled.");
                }
                catch (Exception ex)
                {
                    Log($"Test error: {ex.Message}");
                }
                finally
                {
                    _ = Dispatcher.BeginInvoke(() =>
                    {
                        StartButton.IsEnabled = _scpi.IsConnected;
                        PauseButton.IsEnabled = false;
                        StopButton.IsEnabled = false;
                        DisconnectButton.IsEnabled = _scpi.IsConnected;
                        PauseButton.Content = "Pause";
                    });
                }
            });
            await Task.Yield();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e) => TogglePause();

        // 表头全选框: 勾选 → 全部行勾选; 取消 → 全部行取消。
        private void SelectAllBox_Checked(object sender, RoutedEventArgs e) => SetAllRowsSelected(true);
        private void SelectAllBox_Unchecked(object sender, RoutedEventArgs e) => SetAllRowsSelected(false);

        private void SetAllRowsSelected(bool selected)
        {
            foreach (var r in _rows) r.IsSelected = selected;
        }

        private void TogglePause()
        {
            if (_runTask is null || _runTask.IsCompleted)
            {
                Log("Nothing to pause — no test running.");
                return;
            }
            if (_pauseTcs is null)
            {
                _pauseTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                PauseButton.Content = "Resume";
                Log("Paused.");
            }
            else
            {
                var tcs = _pauseTcs;
                _pauseTcs = null;
                tcs.TrySetResult();
                PauseButton.Content = "Pause";
                Log("Resumed.");
            }
        }

        private void DutConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var port = (DutPortBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(port)) { Log("DUT port is empty."); return; }
            if (!int.TryParse((DutBaudBox.Text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var baud) || baud <= 0)
            {
                Log("DUT baud is invalid; using 1500000.");
                baud = 1500000;
            }
            try
            {
                _dut.Open(port, baud);
                Log($"DUT serial opened: {port} @ {baud} 8N1 — AT+WLCONN auto-loop started");
                DutConnectButton.IsEnabled = false;
                DutDisconnectButton.IsEnabled = true;
                UpdateDutStatusLabel();
            }
            catch (Exception ex)
            {
                Log($"DUT open failed: {ex.Message}");
                DutConnectButton.IsEnabled = true;
                DutDisconnectButton.IsEnabled = false;
                UpdateDutStatusLabel();
            }
        }

        private void DutDisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try { _dut.Close(); Log("DUT serial closed."); }
            catch (Exception ex) { Log($"DUT close error: {ex.Message}"); }
            DutConnectButton.IsEnabled = true;
            DutDisconnectButton.IsEnabled = false;
            UpdateDutStatusLabel();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try { _runCts?.Cancel(); }
            catch { }
            if (_pauseTcs is not null) { _pauseTcs.TrySetResult(); _pauseTcs = null; }
            Log("Stop requested.");
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export sensitivity result",
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                FileName = $"WIFI_Sensitivity_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx",
                OverwritePrompt = true,
                AddExtension = true,
            };
            if (dlg.ShowDialog(this) != true) return;

            try
            {
                ResultExporter.ExportXlsx(dlg.FileName, _rows);
                Log($"Excel exported: {dlg.FileName}");
            }
            catch (Exception ex)
            {
                Log($"Excel export failed: {ex.Message}");
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Excel",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IEnumerable<TestProfile> SelectedProfiles()
        {
            var idx = TestModeBox.SelectedIndex;
            var all = TestProfile.All;
            return idx switch
            {
                0 => all,
                1 => all.Where(p => p.Bandwidth == "BW20"),
                2 => all.Where(p => p.Bandwidth == "BW40"),
                3 => all.Where(p => p.DisplayName.StartsWith("11b ")),
                4 => all.Where(p => p.DisplayName.StartsWith("11g OFDM ")),
                5 => all.Where(p => p.DisplayName == "11g/n 20M 2.4G MCS7"),
                6 => all.Where(p => p.DisplayName == "11g/n 40M 2.4G MCS7"),
                7 => all.Where(p => p.DisplayName.StartsWith("11a 20M ")),
                8 => all.Where(p => p.DisplayName == "11a/n 20M 5G MCS7"),
                9 => all.Where(p => p.DisplayName == "11a/n 40M 5G MCS7"),
                _ => all,
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            try { _runCts?.Cancel(); } catch { }
            try { _scpi.Dispose(); } catch { }
            try { _dut.Dispose(); } catch { }
            base.OnClosed(e);
        }

        private void CableLossBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
