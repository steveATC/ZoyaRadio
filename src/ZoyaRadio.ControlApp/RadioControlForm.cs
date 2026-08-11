using ZoyaRadio.MediaProbe;

namespace ZoyaRadio.ControlApp;

internal sealed class RadioControlForm : Form
{
    private readonly RadioRuntimeController _controller;
    private readonly PrivateAlphaSetupController _setupController;
    private readonly RadioWindowCloseCoordinator _closeCoordinator = new();
    private readonly Button _startButton = new() { Text = "Start", AutoSize = true };
    private readonly Button _stopButton = new() { Text = "Stop", AutoSize = true };
    private readonly CheckBox _silentMode = new() { Text = "Silent Mode", AutoSize = true };
    private readonly Button _clearHistoryButton = new() { Text = "Clear History", AutoSize = true };
    private readonly Button _openLogsButton = new() { Text = "Open Logs Folder", AutoSize = true };
    private readonly Button _checkReadinessButton = new() { Text = "Check Readiness", AutoSize = true };
    private readonly Button _getQwenButton = new() { Text = "Get Qwen through Ollama", AutoSize = true };
    private readonly Button _cancelSetupButton = new()
    {
        Text = "Cancel Model Setup",
        AutoSize = true,
        Enabled = false,
    };
    private readonly Button _ollamaWebsiteButton = new() { Text = "Official Ollama Download Page", AutoSize = true };
    private readonly Label _readinessValue = CreateValueLabel();
    private readonly Label _titleValue = CreateValueLabel();
    private readonly Label _artistValue = CreateValueLabel();
    private readonly Label _firefoxValue = CreateValueLabel();
    private readonly Label _statusValue = CreateValueLabel();
    private readonly Label _activityValue = CreateValueLabel();
    private readonly Label _cleanupNotice = new()
    {
        AutoSize = true,
        ForeColor = Color.DarkOrange,
        Text = "Stopping and restoring protected Firefox audio state...",
        Visible = false,
    };
    private bool _updatingView;
    private bool _setupInProgress;
    private PrivateAlphaReadinessSnapshot? _readiness;
    private CancellationTokenSource? _setupCancellation;
    private Task? _modelSetupTask;

    public RadioControlForm(
        RadioRuntimeController controller,
        PrivateAlphaSetupController setupController)
    {
        _controller = controller;
        _setupController = setupController;
        Text = $"Zoya Radio {PrivateAlphaIdentity.CandidateVersion}";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 500);
        ClientSize = new Size(790, 560);
        Font = new Font("Segoe UI", 10F);

        Controls.Add(BuildLayout());

        _startButton.Click += OnStartClicked;
        _stopButton.Click += OnStopClicked;
        _silentMode.CheckedChanged += OnSilentModeChanged;
        _clearHistoryButton.Click += OnClearHistoryClicked;
        _openLogsButton.Click += OnOpenLogsClicked;
        _checkReadinessButton.Click += OnCheckReadinessClicked;
        _getQwenButton.Click += OnGetQwenClicked;
        _cancelSetupButton.Click += OnCancelSetupClicked;
        _ollamaWebsiteButton.Click += OnOpenOllamaWebsiteClicked;
        FormClosing += OnFormClosing;
        Shown += OnShown;
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
            RowCount = 10,
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

        AddField(layout, 1, "Readiness", _readinessValue);
        AddField(layout, 2, "Current title", _titleValue);
        AddField(layout, 3, "Current artist", _artistValue);
        AddField(layout, 4, "Firefox", _firefoxValue);
        AddField(layout, 5, "Radio activity", _activityValue);
        AddField(layout, 6, "Status", _statusValue);

        var setupControls = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
        };
        setupControls.Controls.AddRange(
        [
            _checkReadinessButton,
            _getQwenButton,
            _cancelSetupButton,
            _ollamaWebsiteButton,
        ]);
        layout.Controls.Add(setupControls, 0, 7);
        layout.SetColumnSpan(setupControls, 2);

        layout.Controls.Add(_cleanupNotice, 0, 8);
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
        layout.Controls.Add(controls, 0, 9);
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

    private async void OnShown(object? sender, EventArgs eventArgs)
    {
        await RefreshReadinessAsync(showDialog: false);
    }

    private async void OnStartClicked(object? sender, EventArgs eventArgs)
    {
        try
        {
            PrivateAlphaReadinessSnapshot? readiness = await RefreshReadinessAsync(showDialog: false);
            if (readiness is null || !readiness.CanStartSafely)
            {
                ShowError(
                    "Zoya Radio is not ready",
                    readiness is null
                        ? "Readiness could not be checked. No radio session was started."
                        : string.Join(
                            Environment.NewLine,
                            PrivateAlphaReadinessPresenter.Present(readiness).Details));
                return;
            }

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

    private async void OnCheckReadinessClicked(object? sender, EventArgs eventArgs)
    {
        await RefreshReadinessAsync(showDialog: true);
    }

    private void OnGetQwenClicked(object? sender, EventArgs eventArgs)
    {
        if (_modelSetupTask is { IsCompleted: false })
        {
            return;
        }

        _modelSetupTask = RunModelSetupAsync();
    }

    private async Task RunModelSetupAsync()
    {
        PrivateAlphaReadinessSnapshot? readiness = await RefreshReadinessAsync(showDialog: false);
        if (readiness is null || !readiness.CanRequestModel)
        {
            ShowError(
                "Qwen model setup is not ready",
                "Start the separately installed Ollama application, confirm enough free space, " +
                "then use Check Readiness.");
            return;
        }

        DialogResult consent = MessageBox.Show(
            this,
            OllamaSetupPolicy.DownloadDisclosure + Environment.NewLine + Environment.NewLine +
            "Obtain qwen3:4b-instruct now through your installed local Ollama application?",
            "Explicit model-download consent",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (consent != DialogResult.Yes)
        {
            _readinessValue.Text = "Model setup cancelled before any request was sent.";
            return;
        }

        _setupCancellation = new CancellationTokenSource();
        SetSetupInProgress(true);
        var progress = new Progress<OllamaPullProgress>(update =>
        {
            string percentage = update.Percent is double value ? $" ({value:N1}%)" : string.Empty;
            _readinessValue.Text = $"Qwen setup: {update.Status}{percentage}";
        });

        try
        {
            OllamaPullResult result = await _setupController.RequestQwenAsync(
                explicitConsent: true,
                progress,
                _setupCancellation.Token);
            MessageBox.Show(
                this,
                result.Message,
                result.Succeeded ? "Qwen setup completed" : "Qwen setup did not complete",
                MessageBoxButtons.OK,
                result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            ShowError("Qwen setup failed safely", exception.Message);
        }
        finally
        {
            _setupCancellation.Dispose();
            _setupCancellation = null;
            SetSetupInProgress(false);
            await RefreshReadinessAsync(showDialog: false);
        }
    }

    private void OnCancelSetupClicked(object? sender, EventArgs eventArgs)
    {
        _setupCancellation?.Cancel();
    }

    private void OnOpenOllamaWebsiteClicked(object? sender, EventArgs eventArgs)
    {
        try
        {
            _setupController.OpenOfficialOllamaDownloadPage();
        }
        catch (Exception exception)
        {
            ShowError("The official Ollama page could not be opened", exception.Message);
        }
    }

    private async Task<PrivateAlphaReadinessSnapshot?> RefreshReadinessAsync(bool showDialog)
    {
        try
        {
            _checkReadinessButton.Enabled = false;
            PrivateAlphaReadinessSnapshot snapshot = await _setupController.CheckAsync();
            _readiness = snapshot;
            PrivateAlphaReadinessPresentation presentation =
                PrivateAlphaReadinessPresenter.Present(snapshot);
            _readinessValue.Text = presentation.Summary;
            ApplySnapshot(_controller.CurrentStatus);

            if (showDialog)
            {
                MessageBox.Show(
                    this,
                    presentation.Summary + Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, presentation.Details),
                    "Zoya Radio readiness",
                    MessageBoxButtons.OK,
                    snapshot.ReadyForNormalOperation
                        ? MessageBoxIcon.Information
                        : MessageBoxIcon.Warning);
            }

            return snapshot;
        }
        catch (Exception exception)
        {
            _readinessValue.Text = $"Readiness check failed safely: {exception.Message}";
            return null;
        }
        finally
        {
            _checkReadinessButton.Enabled = !_setupInProgress;
        }
    }

    private void SetSetupInProgress(bool value)
    {
        _setupInProgress = value;
        _checkReadinessButton.Enabled = !value;
        _getQwenButton.Enabled = !value && _readiness?.CanRequestModel == true;
        _cancelSetupButton.Enabled = value;
        ApplySnapshot(_controller.CurrentStatus);
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
            _setupCancellation?.Cancel();
            if (_modelSetupTask is not null)
            {
                await _modelSetupTask;
            }

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
            _setupController.Dispose();
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
            _startButton.Enabled = state.StartEnabled
                && !_setupInProgress
                && (_readiness?.CanStartSafely ?? true);
            _stopButton.Enabled = state.StopEnabled;
            _silentMode.Enabled = state.SilentModeEnabled;
            _silentMode.Checked = state.SilentModeChecked;
            _clearHistoryButton.Enabled = state.ClearHistoryEnabled;
            _openLogsButton.Enabled = state.OpenLogsEnabled;
            _getQwenButton.Enabled = !_setupInProgress
                && state.StartEnabled
                && _readiness?.CanRequestModel == true;
            _cancelSetupButton.Enabled = _setupInProgress;
            _cleanupNotice.Visible = state.CleanupVisible;
            _titleValue.Text = state.TitleText;
            _artistValue.Text = state.ArtistText;
            _activityValue.Text = state.ActivityText;
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
