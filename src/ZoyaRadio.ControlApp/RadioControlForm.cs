using ZoyaRadio.MediaProbe;

namespace ZoyaRadio.ControlApp;

internal sealed class RadioControlForm : Form
{
    private readonly RadioRuntimeController _controller;
    private readonly RadioWindowCloseCoordinator _closeCoordinator = new();
    private readonly Button _startButton = new() { Text = "Start", AutoSize = true };
    private readonly Button _stopButton = new() { Text = "Stop", AutoSize = true };
    private readonly CheckBox _silentMode = new() { Text = "Silent Mode", AutoSize = true };
    private readonly Button _clearHistoryButton = new() { Text = "Clear History", AutoSize = true };
    private readonly Button _openLogsButton = new() { Text = "Open Logs Folder", AutoSize = true };
    private readonly Label _titleValue = CreateValueLabel();
    private readonly Label _artistValue = CreateValueLabel();
    private readonly Label _firefoxValue = CreateValueLabel();
    private readonly Label _statusValue = CreateValueLabel();
    private readonly Label _cleanupNotice = new()
    {
        AutoSize = true,
        ForeColor = Color.DarkOrange,
        Text = "Stopping and restoring protected Firefox audio state...",
        Visible = false,
    };
    private bool _updatingView;

    public RadioControlForm(RadioRuntimeController controller)
    {
        _controller = controller;
        Text = "Zoya Radio";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(620, 360);
        ClientSize = new Size(680, 390);
        Font = new Font("Segoe UI", 10F);

        Controls.Add(BuildLayout());

        _startButton.Click += OnStartClicked;
        _stopButton.Click += OnStopClicked;
        _silentMode.CheckedChanged += OnSilentModeChanged;
        _clearHistoryButton.Click += OnClearHistoryClicked;
        _openLogsButton.Click += OnOpenLogsClicked;
        FormClosing += OnFormClosing;
        _controller.StatusChanged += OnControllerStatusChanged;

        ApplySnapshot(_controller.CurrentStatus);
    }

    private Control BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 7,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var heading = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Text = "Zoya Radio control",
        };
        layout.Controls.Add(heading, 0, 0);
        layout.SetColumnSpan(heading, 2);

        AddField(layout, 1, "Current title", _titleValue);
        AddField(layout, 2, "Current artist", _artistValue);
        AddField(layout, 3, "Firefox", _firefoxValue);
        AddField(layout, 4, "Status", _statusValue);

        layout.Controls.Add(_cleanupNotice, 0, 5);
        layout.SetColumnSpan(_cleanupNotice, 2);

        var controls = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 18, 0, 0),
        };
        controls.Controls.AddRange(
        [
            _startButton,
            _stopButton,
            _silentMode,
            _clearHistoryButton,
            _openLogsButton,
        ]);
        layout.Controls.Add(controls, 0, 6);
        layout.SetColumnSpan(controls, 2);

        var historyTip = new ToolTip();
        historyTip.SetToolTip(
            _clearHistoryButton,
            "Clears only the small in-memory recent-track and comment context. Logs and files are not deleted.");

        return layout;
    }

    private static void AddField(TableLayoutPanel layout, int row, string labelText, Control value)
    {
        var label = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Text = $"{labelText}:",
            Margin = new Padding(0, 8, 12, 8),
        };
        value.Margin = new Padding(0, 8, 0, 8);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(value, 1, row);
    }

    private static Label CreateValueLabel()
    {
        return new Label
        {
            AutoSize = true,
            MaximumSize = new Size(480, 0),
            Text = "<not provided>",
        };
    }

    private async void OnStartClicked(object? sender, EventArgs eventArgs)
    {
        try
        {
            await _controller.StartAsync();
        }
        catch (Exception exception)
        {
            ShowError("Zoya Radio could not start", exception.Message);
        }
    }

    private async void OnStopClicked(object? sender, EventArgs eventArgs)
    {
        try
        {
            RadioControlStopResult result = await _controller.StopAsync();
            if (!result.SafeToClose)
            {
                ShowError(
                    "Protected cleanup could not be confirmed",
                    result.Message + Environment.NewLine + Environment.NewLine +
                    "Check Firefox volume manually before closing Zoya Radio.");
            }
        }
        catch (Exception exception)
        {
            ShowError("Zoya Radio could not stop safely", exception.Message);
        }
    }

    private void OnSilentModeChanged(object? sender, EventArgs eventArgs)
    {
        if (_updatingView)
        {
            return;
        }

        try
        {
            _controller.SetSilentMode(_silentMode.Checked);
        }
        catch (Exception exception)
        {
            ShowError("Silent Mode could not be changed", exception.Message);
            ApplySnapshot(_controller.CurrentStatus);
        }
    }

    private void OnClearHistoryClicked(object? sender, EventArgs eventArgs)
    {
        try
        {
            _controller.ClearRecentHistory();
        }
        catch (Exception exception)
        {
            ShowError("Recent history could not be cleared", exception.Message);
        }
    }

    private void OnOpenLogsClicked(object? sender, EventArgs eventArgs)
    {
        try
        {
            _controller.OpenLogsFolder();
        }
        catch (Exception exception)
        {
            ShowError("The logs folder could not be opened", exception.Message);
        }
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (_closeCoordinator.CloseAllowed)
        {
            _controller.StatusChanged -= OnControllerStatusChanged;
            return;
        }

        eventArgs.Cancel = true;
        if (_closeCoordinator.ShutdownInProgress)
        {
            return;
        }

        try
        {
            bool safeToClose = await _closeCoordinator.RequestCloseAsync(_controller.StopAsync);
            if (!safeToClose)
            {
                ShowError(
                    "Zoya Radio is still open for safety",
                    "Protected Firefox audio restoration could not be confirmed. " +
                    "Check Firefox volume manually, then press Stop and try closing again.");
                return;
            }

            await _controller.DisposeAsync();
            BeginInvoke(Close);
        }
        catch (Exception exception)
        {
            ShowError(
                "Zoya Radio is still open for safety",
                $"Shutdown did not reach a confirmed safe result: {exception.Message}");
        }
    }

    private void OnControllerStatusChanged(RadioControlSnapshot snapshot)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => ApplySnapshot(snapshot));
            return;
        }

        ApplySnapshot(snapshot);
    }

    private void ApplySnapshot(RadioControlSnapshot snapshot)
    {
        RadioControlViewState state = RadioControlViewState.FromSnapshot(snapshot);
        _updatingView = true;
        try
        {
            _startButton.Enabled = state.StartEnabled;
            _stopButton.Enabled = state.StopEnabled;
            _silentMode.Enabled = state.SilentModeEnabled;
            _silentMode.Checked = state.SilentModeChecked;
            _clearHistoryButton.Enabled = state.ClearHistoryEnabled;
            _openLogsButton.Enabled = state.OpenLogsEnabled;
            _cleanupNotice.Visible = state.CleanupVisible;
            _titleValue.Text = state.TitleText;
            _artistValue.Text = state.ArtistText;
            _firefoxValue.Text = state.FirefoxStatusText;
            _statusValue.Text = state.StatusText;
        }
        finally
        {
            _updatingView = false;
        }
    }

    private void ShowError(string heading, string message)
    {
        MessageBox.Show(this, message, heading, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
