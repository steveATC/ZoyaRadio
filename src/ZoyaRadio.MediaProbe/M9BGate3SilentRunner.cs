using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal enum M9BGate3PreparationOutcome
{
    Ready,
    EnrichmentUnavailable,
    ModelUnavailable,
    GenerationFailed,
    ModelFallback,
    HardDiscard,
    AdvisoryFallback,
    Cancelled,
    StaleDiscarded,
}

internal sealed record M9BGate3PreparationRecord(
    long OpportunityId,
    string Title,
    string Artist,
    bool CandidateReady,
    M9BGate3PreparationOutcome OutcomeKind,
    string Outcome,
    double ElapsedMilliseconds,
    long? TotalDurationNanoseconds,
    long? LoadDurationNanoseconds,
    bool Cancelled,
    string? ContextCardIdentity = null,
    M9BContextSource ContextSource = M9BContextSource.Unknown,
    IReadOnlyList<M9BGenerationDiagnostic>? GenerationDiagnostics = null);

internal sealed record M9BGate3RunEvidence(
    int GenuineTransitions,
    int PreparationOpportunities,
    int ReadyCandidates,
    int SuppressedDeliveries,
    int CancelledPreparations,
    int StaleDisposals,
    int SessionSafetyDisposals,
    int ForbiddenActivityCount,
    bool? SelectedModelUnloaded,
    IReadOnlyList<M9BGate3PreparationRecord> Preparations);

internal sealed class M9BGate3SilentRunner
{
    private const int RequiredTransitions = 12;
    private readonly LocalLog _log;
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly RadioSessionAccess _sessions;
    private readonly RadioBehaviorState _state;
    private readonly M9BGroundedCommentService _comments;
    private readonly ILocalCommentGenerator _generator;
    private readonly AiCommentConfiguration _configuration;
    private readonly CancellationToken _shutdownToken;
    private readonly SemaphoreSlim _eventGate = new(1, 1);
    private readonly object _preparationLock = new();
    private readonly TaskCompletionSource<bool> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<M9BGate3PreparationRecord> _preparations = [];
    private GlobalSystemMediaTransportControlsSession? _selected;
    private CancellationTokenSource? _preparationCancellation;
    private Task? _preparationTask;
    private bool _targetSafe;
    private int _readyCandidates;
    private int _suppressedDeliveries;
    private int _cancelledPreparations;
    private int _staleDisposals;
    private int _sessionSafetyDisposals;

    internal M9BGate3SilentRunner(
        LocalLog log,
        GlobalSystemMediaTransportControlsSessionManager manager,
        ILocalCommentGenerator generator,
        AiCommentConfiguration configuration,
        M9BGroundedCommentService comments,
        CancellationToken shutdownToken,
        ITransitionTargetSource? targetSource = null)
    {
        _log = log;
        _manager = manager;
        _sessions = new RadioSessionAccess(manager, log);
        _generator = generator;
        _configuration = configuration;
        _comments = comments;
        _shutdownToken = shutdownToken;
        _state = new RadioBehaviorState(
            RadioBehaviorConfiguration.Default,
            targetSource ?? new RandomTransitionTargetSource());
    }

    internal async Task<int> RunAsync()
    {
        _log.Write("ZOYA RADIO - M9B GATE 3 SILENT DESKTOP PREPARATION TRIAL");
        _log.Write("SAFETY: preparation only. Speech, ducking, playback control, and all volume changes are disabled by this command.");
        _log.Write("The test observes Firefox only; it never chooses Chrome or another media session as a substitute.");
        await _sessions.ReadAllSessionsAsync();

        if (!await TrySelectAndArmAsync())
        {
            return 1;
        }

        Subscribe();
        await WriteResourceSnapshotAsync("before first M9B preparation");
        _log.Write($"GATE 3 ARMED: observe {RequiredTransitions} genuine Firefox transitions. Current schedule is every {_state.ScheduledThreshold} transitions (existing 2-5 policy).");
        _log.Write("Keep Firefox playing and change tracks naturally or manually. Ctrl+C cancels preparation only; it cannot change playback or audio.");

        try
        {
            await _completed.Task.WaitAsync(_shutdownToken);
        }
        catch (OperationCanceledException)
        {
            _log.Write("GATE 3 CANCELLED: pending local M9B preparation will be discarded. No desktop media action was issued.");
        }
        finally
        {
            Unsubscribe();
            await CancelPreparationAndWaitAsync("Gate 3 shutdown");
        }

        bool? unloaded = await VerifySelectedModelUnloadedAsync();
        M9BGate3RunEvidence evidence = new(
            _state.TotalObservedTransitions,
            _preparations.Count,
            _readyCandidates,
            _suppressedDeliveries,
            _cancelledPreparations,
            _staleDisposals,
            _sessionSafetyDisposals,
            0,
            unloaded,
            _preparations.ToArray());
        M9BGate3Verdict verdict = M9BGate3Verdict.Evaluate(evidence);
        string artifact = M9BGate3Artifacts.Write(evidence, verdict);
        _log.Write($"GATE 3 EVIDENCE: {artifact}");
        _log.Write($"GATE 3 SUMMARY: transitions={evidence.GenuineTransitions}/{RequiredTransitions}; prepared={evidence.PreparationOpportunities}; ready={evidence.ReadyCandidates}; suppressed-deliveries={evidence.SuppressedDeliveries}; cancelled={evidence.CancelledPreparations}; stale-disposals={evidence.StaleDisposals}; session-safety-disposals={evidence.SessionSafetyDisposals}.");
        _log.Write($"GATE 3 MODEL UNLOAD: {(unloaded is true ? "verified" : unloaded is false ? "not verified" : "unavailable")}.");
        _log.Write($"GATE 3 VERDICT: {(verdict.Passed ? "PASS" : "FAIL")} - {string.Join("; ", verdict.Reasons)}");
        _log.Write("GATE 3 SAFETY CONFIRMATION: zero playback commands, zero speech, zero ducking, zero volume changes, zero catch-up, and zero stale delivery were executed.");
        return verdict.Passed ? 0 : 1;
    }

    private async Task<bool> TrySelectAndArmAsync()
    {
        if (!_sessions.TryReadExactFirefoxMatches(out IReadOnlyList<GlobalSystemMediaTransportControlsSession> matches))
        {
            _log.Write("GATE 3 NOT ARMED: the Windows media-session list could not be read.");
            return false;
        }

        if (matches.Count == 0)
        {
            _log.Write("GATE 3 NOT ARMED: zero exact firefox.exe sessions. Start YouTube Music in Firefox and try again.");
            return false;
        }

        if (matches.Count > 1)
        {
            _log.Write($"GATE 3 NOT ARMED: AMBIGUOUS TARGET - {matches.Count} exact firefox.exe sessions. No session will be used.");
            foreach (GlobalSystemMediaTransportControlsSession match in matches)
            {
                _sessions.WriteSessionDetails("AMBIGUOUS FIREFOX CANDIDATE", await _sessions.ReadSessionDetailsAsync(match));
            }

            return false;
        }

        RadioSessionDetails details = await _sessions.ReadSessionDetailsAsync(matches[0]);
        _sessions.WriteSessionDetails("GATE 3 FIREFOX TARGET", details);
        if (!IsEligiblePlayingFirefox(details))
        {
            _log.Write("GATE 3 NOT ARMED: the one exact Firefox session must expose title, artist, and Playing state.");
            return false;
        }

        _selected = details.Session;
        _targetSafe = true;
        _state.Arm(details.Media, DateTimeOffset.Now);
        return true;
    }

    private void Subscribe()
    {
        _selected!.MediaPropertiesChanged += OnMediaPropertiesChanged;
        _manager.SessionsChanged += OnSessionsChanged;
    }

    private void Unsubscribe()
    {
        try
        {
            if (_selected is not null) _selected.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not remove the Gate 3 Firefox media observer", exception);
        }

        try
        {
            _manager.SessionsChanged -= OnSessionsChanged;
        }
        catch (Exception exception)
        {
            _log.Warning("Could not remove the Gate 3 media-session-list observer", exception);
        }
    }

    private async void OnMediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args)
    {
        try
        {
            await HandleMediaPropertiesChangedAsync(sender);
        }
        catch (Exception exception)
        {
            _log.Warning("Unexpected Gate 3 media observer failure", exception);
        }
    }

    private async Task HandleMediaPropertiesChangedAsync(GlobalSystemMediaTransportControlsSession sender)
    {
        RadioPreparationOpportunity? preparation = null;
        RadioTransitionDelivery? delivery = null;
        bool cancelForStale = false;

        await _eventGate.WaitAsync();
        try
        {
            if (!_targetSafe || _selected is null || !ReferenceEquals(sender, _selected))
            {
                _log.Write("GATE 3 STALE MEDIA EVENT IGNORED: it did not come from the selected Firefox session.");
                return;
            }

            MediaSnapshot? media = await _sessions.TryReadMediaAsync(_selected);
            if (media is null)
            {
                _log.Write("GATE 3 MEDIA EVENT IGNORED: Firefox metadata could not be read.");
                return;
            }

            RadioTrackObservation observation = _state.ObserveTrack(media, DateTimeOffset.Now);
            if (observation.Kind == RadioTrackObservationKind.Duplicate)
            {
                return;
            }

            if (observation.Kind == RadioTrackObservationKind.Unusable)
            {
                _log.Write("GATE 3 MEDIA EVENT IGNORED: title or artist is unavailable.");
                return;
            }

            _log.Write($"GATE 3 TRANSITION {observation.TotalObservedTransitions}/{RequiredTransitions}: {MediaSnapshot.Display(observation.PreviousTrack?.Artist)} — {MediaSnapshot.Display(observation.PreviousTrack?.Title)} -> {MediaSnapshot.Display(media.Artist)} — {MediaSnapshot.Display(media.Title)}.");
            if (observation.PendingOpportunityInvalidated)
            {
                _staleDisposals++;
                cancelForStale = true;
                _log.Write(observation.PreparationMissedTransition
                    ? "GATE 3 STALE WORK DISCARDED: Track A changed before its M9B preparation completed; no catch-up is queued."
                    : "GATE 3 STALE WORK DISCARDED: an additional transition invalidated prepared material; no catch-up is queued.");
            }

            preparation = observation.PreparationOpportunity;
            delivery = observation.Delivery;
            if (observation.TotalObservedTransitions >= RequiredTransitions)
            {
                _completed.TrySetResult(true);
            }
        }
        finally
        {
            _eventGate.Release();
        }

        if (cancelForStale)
        {
            await CancelPreparationAndWaitAsync("stale Firefox track transition");
        }

        if (delivery is not null)
        {
            SuppressDelivery(delivery);
        }
        else if (preparation is not null)
        {
            StartPreparation(preparation);
        }
    }

    private async void OnSessionsChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        SessionsChangedEventArgs args)
    {
        try
        {
            await HandleSessionsChangedAsync();
        }
        catch (Exception exception)
        {
            _log.Warning("Gate 3 could not validate a Windows media-session-list change", exception);
            await InvalidateForSessionSafetyAsync("the Windows media-session list could not be validated");
        }
    }

    private async Task HandleSessionsChangedAsync()
    {
        GlobalSystemMediaTransportControlsSession? replacement = null;
        RadioSessionDetails? replacementDetails = null;
        string? unsafeReason = null;

        await _eventGate.WaitAsync();
        try
        {
            if (!_sessions.TryReadExactFirefoxMatches(out IReadOnlyList<GlobalSystemMediaTransportControlsSession> matches))
            {
                _targetSafe = false;
                _sessionSafetyDisposals++;
                unsafeReason = "the Windows media-session list is unavailable";
                _log.Write("GATE 3 FIREFOX GSMTC: session list unavailable; pending work is discarded and the trial stays silent.");
            }
            else if (matches.Count == 0)
            {
                _targetSafe = false;
                _sessionSafetyDisposals++;
                unsafeReason = "zero exact Firefox media-session candidates";
                _log.Write("GATE 3 FIREFOX GSMTC CANDIDATES: ZERO. Pending work is discarded; Chrome is never a fallback target.");
            }
            else if (matches.Count > 1)
            {
                _targetSafe = false;
                _sessionSafetyDisposals++;
                unsafeReason = "ambiguous exact Firefox media-session candidates";
                _log.Write($"GATE 3 FIREFOX GSMTC CANDIDATES: AMBIGUOUS ({matches.Count}). Pending work is discarded and the trial stays silent.");
            }
            else if (_selected is not null && ReferenceEquals(matches[0], _selected))
            {
                _log.Write("GATE 3 GSMTC CHURN: unrelated Windows session change; selected Firefox wrapper remains current.");
                return;
            }
            else
            {
                RadioSessionDetails details = await _sessions.ReadSessionDetailsAsync(matches[0]);
                if (!IsEligiblePlayingFirefox(details))
                {
                    _targetSafe = false;
                    _sessionSafetyDisposals++;
                    unsafeReason = "the sole Firefox replacement candidate was not usable and Playing";
                    _log.Write("GATE 3 FIREFOX GSMTC REPLACEMENT REFUSED: the sole candidate is not a usable Playing Firefox session.");
                }
                else
                {
                    replacement = details.Session;
                    replacementDetails = details;
                }
            }
        }
        finally
        {
            _eventGate.Release();
        }

        if (unsafeReason is not null)
        {
            _state.CancelPendingOpportunityAndReschedule();
            await CancelPreparationAndWaitAsync(unsafeReason);
            _log.Write("GATE 3 SESSION SAFETY: pending work discarded; no delivery can occur while Firefox targeting is unsafe.");
            return;
        }

        if (replacement is not null && replacementDetails is not null)
        {
            await CancelPreparationAndWaitAsync("Firefox GSMTC session replacement");
            await _eventGate.WaitAsync();
            try
            {
                if (_selected is not null) _selected.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _selected = replacement;
                _selected.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _targetSafe = true;
                _state.RebindCurrentTrack(replacementDetails.Media, DateTimeOffset.Now);
                _log.Write($"GATE 3 FIREFOX GSMTC REACQUIRED: exact firefox.exe wrapper replaced safely; fresh 2-5 transition interval is {_state.ScheduledThreshold}.");
                _log.Write("GATE 3 REBIND NOTE: pending opportunity was discarded before reacquisition.");
            }
            finally
            {
                _eventGate.Release();
            }
        }
    }
    private void StartPreparation(RadioPreparationOpportunity opportunity)
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
        lock (_preparationLock)
        {
            if (_preparationTask is not null && !_preparationTask.IsCompleted)
            {
                cancellation.Dispose();
                _log.Write("GATE 3 PREPARATION SKIPPED: an existing preparation is still active; no queue is created.");
                _state.ResolveOpportunity(opportunity.Id, false, null);
                return;
            }

            _preparationCancellation = cancellation;
            _preparationTask = Task.Run(() => PrepareAsync(opportunity, cancellation));
        }
    }

    private async Task PrepareAsync(RadioPreparationOpportunity opportunity, CancellationTokenSource cancellation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool cancelled = false;
        try
        {
            _log.Write($"GATE 3 M9B PREPARATION STARTED: opportunity {opportunity.Id}; Track A remains playing and no audio output is created.");
            await Task.Delay(RadioBehaviorConfiguration.Default.MinimumTrackAgeBeforeSpeech, cancellation.Token);
            if (!await IsCurrentOpportunityStillSafeAsync(opportunity, cancellation.Token))
            {
                stopwatch.Stop();
                _staleDisposals++;
                _preparations.Add(new M9BGate3PreparationRecord(
                    opportunity.Id,
                    opportunity.Context.SubjectTrack.Title!,
                    opportunity.Context.SubjectTrack.Artist!,
                    false,
                    M9BGate3PreparationOutcome.StaleDiscarded,
                    "Firefox state or Track A changed before generation; preparation was discarded safely.",
                    stopwatch.Elapsed.TotalMilliseconds,
                    null,
                    null,
                    false));
                _log.Write("GATE 3 PREPARATION DISCARDED: Firefox state or Track A changed before generation; no catch-up is queued.");
                _state.ResolveOpportunity(opportunity.Id, false, null);
                return;
            }

            M9BCommentSelection selected = await _comments.TrySelectAsync(opportunity.Context, true, cancellation.Token);
            stopwatch.Stop();
            if (selected.Selection is null)
            {
                _preparations.Add(new M9BGate3PreparationRecord(
                    opportunity.Id,
                    opportunity.Context.SubjectTrack.Title!,
                    opportunity.Context.SubjectTrack.Artist!,
                    false,
                    ToGate3Outcome(selected.OutcomeKind),
                    selected.Reason,
                    stopwatch.Elapsed.TotalMilliseconds,
                    selected.Metrics?.TotalDurationNanoseconds,
                    selected.Metrics?.LoadDurationNanoseconds,
                    false,
                    selected.Card?.Identity,
                    selected.ContextSource,
                    selected.GenerationDiagnostics));
                WriteRejectedCandidateDiagnostics(selected);
                _log.Write($"GATE 3 M9B CANDIDATE NOT READY: {selected.Reason} Accepted M8 fallback remains hypothetical; no fallback speech is prepared.");
                _state.ResolveOpportunity(opportunity.Id, false, null);
                return;
            }

            OllamaGenerationMetrics? metrics = selected.Metrics ?? selected.Selection.Generation?.Metrics;
            bool retained = _state.MarkPreparationReady(opportunity.Id);
            _preparations.Add(new M9BGate3PreparationRecord(
                opportunity.Id,
                opportunity.Context.SubjectTrack.Title!,
                opportunity.Context.SubjectTrack.Artist!,
                retained,
                retained ? M9BGate3PreparationOutcome.Ready : M9BGate3PreparationOutcome.StaleDiscarded,
                retained ? "M9B candidate ready for the next transition; Gate 3 will suppress delivery." : "Candidate became stale before it could be retained.",
                stopwatch.Elapsed.TotalMilliseconds,
                metrics?.TotalDurationNanoseconds,
                metrics?.LoadDurationNanoseconds,
                false,
                selected.Card?.Identity,
                selected.ContextSource,
                selected.GenerationDiagnostics));
            if (!retained)
            {
                _staleDisposals++;
                _log.Write("GATE 3 M9B CANDIDATE DISCARDED: it became stale before retention; no catch-up is queued.");
                return;
            }

            _readyCandidates++;
            string temperature = metrics?.LoadDurationNanoseconds is > 0 ? "cold load observed" : "warm/no-load duration observed";
            _log.Write($"GATE 3 M9B CANDIDATE READY: opportunity {opportunity.Id}; {temperature}; elapsed={stopwatch.Elapsed.TotalMilliseconds:0} ms; model text is retained only for safe suppression at the next transition.");
            await WriteResourceSnapshotAsync($"after opportunity {opportunity.Id} generation/unload");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested || _shutdownToken.IsCancellationRequested)
        {
            cancelled = true;
            stopwatch.Stop();
            _cancelledPreparations++;
            _preparations.Add(new M9BGate3PreparationRecord(
                opportunity.Id,
                opportunity.Context.SubjectTrack.Title!,
                opportunity.Context.SubjectTrack.Artist!,
                false,
                M9BGate3PreparationOutcome.Cancelled,
                "Preparation cancelled and discarded safely.",
                stopwatch.Elapsed.TotalMilliseconds,
                null,
                null,
                true));
            _log.Write("GATE 3 M9B PREPARATION CANCELLED: stale/shutdown work was discarded; no catch-up is queued.");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _preparations.Add(new M9BGate3PreparationRecord(
                opportunity.Id,
                opportunity.Context.SubjectTrack.Title!,
                opportunity.Context.SubjectTrack.Artist!,
                false,
                M9BGate3PreparationOutcome.GenerationFailed,
                exception.Message,
                stopwatch.Elapsed.TotalMilliseconds,
                null,
                null,
                false));
            _log.Warning("Gate 3 M9B preparation failed safely", exception);
            _state.ResolveOpportunity(opportunity.Id, false, null);
        }
        finally
        {
            if (cancelled) _state.ResolveOpportunity(opportunity.Id, false, null);
            lock (_preparationLock)
            {
                if (ReferenceEquals(_preparationCancellation, cancellation))
                {
                    _preparationCancellation = null;
                }
            }

            cancellation.Dispose();
        }
    }

    private void WriteRejectedCandidateDiagnostics(M9BCommentSelection selected)
    {
        foreach (M9BGenerationDiagnostic diagnostic in selected.GenerationDiagnostics ?? [])
        {
            string candidate = diagnostic.NormalizedOutput ?? diagnostic.RawOutput ?? "<no model output>";
            candidate = candidate.Replace('\r', ' ').Replace('\n', ' ').Trim();
            string classification = diagnostic.ValidationKind?.ToString() ?? "GenerationFailed";
            _log.Write($"GATE 3 M9B REJECTED CANDIDATE: {candidate} | classification={classification}; reason={diagnostic.Reason}");
        }
    }
    private static M9BGate3PreparationOutcome ToGate3Outcome(M9BCommentSelectionKind outcome) => outcome switch
    {
        M9BCommentSelectionKind.EnrichmentUnavailable => M9BGate3PreparationOutcome.EnrichmentUnavailable,
        M9BCommentSelectionKind.ModelUnavailable => M9BGate3PreparationOutcome.ModelUnavailable,
        M9BCommentSelectionKind.GenerationFailed => M9BGate3PreparationOutcome.GenerationFailed,
        M9BCommentSelectionKind.ModelFallback => M9BGate3PreparationOutcome.ModelFallback,
        M9BCommentSelectionKind.HardDiscard => M9BGate3PreparationOutcome.HardDiscard,
        M9BCommentSelectionKind.AdvisoryFallback => M9BGate3PreparationOutcome.AdvisoryFallback,
        _ => M9BGate3PreparationOutcome.GenerationFailed,
    };
    private async Task<bool> IsCurrentOpportunityStillSafeAsync(RadioPreparationOpportunity opportunity, CancellationToken cancellationToken)
    {
        GlobalSystemMediaTransportControlsSession? selected = _selected;
        if (!_targetSafe || selected is null) return false;
        RadioSessionDetails? rechecked = await _sessions.RecheckStartupTargetAsync(new RadioSessionDetails(
            selected,
            FirefoxSessionSelector.VerifiedSourceId,
            true,
            opportunity.Context.SubjectTrack,
            true,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
            true));
        cancellationToken.ThrowIfCancellationRequested();
        return rechecked is not null && ReferenceEquals(rechecked.Session, selected);
    }

    private void SuppressDelivery(RadioTransitionDelivery delivery)
    {
        _suppressedDeliveries++;
        _state.ResolveOpportunity(delivery.OpportunityId, false, null);
        _log.Write($"GATE 3 DELIVERY SUPPRESSED: prepared Track A material was discarded at transition to Track B. No speech, ducking, playback, or volume operation ran. Fresh interval is {_state.ScheduledThreshold}.");
    }

    private async Task InvalidateForSessionSafetyAsync(string reason)
    {
        _targetSafe = false;
        _sessionSafetyDisposals++;
        _state.CancelPendingOpportunityAndReschedule();
        await CancelPreparationAndWaitAsync(reason);
        _log.Write("GATE 3 SESSION SAFETY: pending work discarded; no delivery can occur while Firefox targeting is unsafe.");
    }

    private async Task CancelPreparationAndWaitAsync(string reason)
    {
        Task? task;
        CancellationTokenSource? cancellation;
        lock (_preparationLock)
        {
            task = _preparationTask;
            cancellation = _preparationCancellation;
            cancellation?.Cancel();
        }

        if (task is not null)
        {
            _log.Write($"GATE 3 PREPARATION CANCELLATION REQUESTED: {reason}.");
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // PrepareAsync records cancellation before completion.
            }
        }
    }

    private async Task WriteResourceSnapshotAsync(string point)
    {
        OllamaRunningModelsResult running = await _generator.GetRunningModelsAsync(
            _configuration.LocalOperationTimeout,
            CancellationToken.None);
        if (!running.Success)
        {
            _log.Write($"GATE 3 RESOURCE SNAPSHOT ({point}): unavailable - {running.Message}");
            return;
        }

        OllamaRunningModel? model = running.Models.FirstOrDefault(IsSelectedModel);
        if (model is null)
        {
            _log.Write($"GATE 3 RESOURCE SNAPSHOT ({point}): selected 14B model is not resident (no VRAM/RAM size observable).");
            return;
        }

        _log.Write($"GATE 3 RESOURCE SNAPSHOT ({point}): loaded-size={model.Size?.ToString() ?? "<not provided>"}; VRAM-size={model.SizeVram?.ToString() ?? "<not provided>"}; context={model.ContextLength?.ToString() ?? "<not provided>"}; parameters={model.Details?.ParameterSize ?? "<not provided>"}; quantization={model.Details?.QuantizationLevel ?? "<not provided>"}.");
    }

    private async Task<bool?> VerifySelectedModelUnloadedAsync()
    {
        OllamaOperationResult unload = await _generator.UnloadAsync(_configuration.LocalOperationTimeout, CancellationToken.None);
        _log.Write($"GATE 3 UNLOAD REQUEST: {(unload.Success ? "accepted" : "not confirmed")} - {unload.Message}");
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            OllamaRunningModelsResult running = await _generator.GetRunningModelsAsync(_configuration.LocalOperationTimeout, CancellationToken.None);
            if (!running.Success)
            {
                _log.Write($"GATE 3 UNLOAD CHECK {attempt}/3: unavailable - {running.Message}");
                return null;
            }

            if (!running.Models.Any(IsSelectedModel))
            {
                _log.Write($"GATE 3 UNLOAD CHECK {attempt}/3: selected 14B model is no longer resident.");
                return true;
            }

            await Task.Delay(250, CancellationToken.None);
        }

        _log.Write("GATE 3 UNLOAD CHECK: selected 14B model still appeared resident after the bounded check.");
        return false;
    }

    private static bool IsSelectedModel(OllamaRunningModel model) =>
        string.Equals(model.Name, M9BConstants.Model, StringComparison.OrdinalIgnoreCase)
        || string.Equals(model.Model, M9BConstants.Model, StringComparison.OrdinalIgnoreCase);

    private static bool IsEligiblePlayingFirefox(RadioSessionDetails details) =>
        details.SourceReadSucceeded
        && string.Equals(details.SourceId, FirefoxSessionSelector.VerifiedSourceId, StringComparison.OrdinalIgnoreCase)
        && details.MediaReadSucceeded
        && details.Media.Title is not null
        && details.Media.Artist is not null
        && details.PlaybackReadSucceeded
        && PlaybackControlRules.CanStart(details.PlaybackStatus);
}

internal static class M9BGate3Artifacts
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };

    internal static string Write(M9BGate3RunEvidence evidence, M9BGate3Verdict verdict)
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "m9b", "gate3", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "M9B_GATE3_EVIDENCE.json"), JsonSerializer.Serialize(evidence, JsonOptions), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(directory, "M9B_GATE3_SUMMARY.md"), BuildSummary(evidence, verdict), new UTF8Encoding(false));
        return directory;
    }

    private static string BuildSummary(M9BGate3RunEvidence evidence, M9BGate3Verdict verdict)
    {
        var text = new StringBuilder("# M9B Gate 3 silent desktop preparation evidence\n\n");
        text.AppendLine("This run prepared local M9B candidate text only. It did not start speech, duck any audio, control playback, or change volume.");
        text.AppendLine();
        text.AppendLine($"- Genuine Firefox transitions: {evidence.GenuineTransitions}/12");
        text.AppendLine($"- Preparation opportunities: {evidence.PreparationOpportunities}");
        text.AppendLine($"- Candidates ready before Track B: {evidence.ReadyCandidates}");
        text.AppendLine($"- Delivery suppressions: {evidence.SuppressedDeliveries}");
        text.AppendLine($"- Cancelled preparations: {evidence.CancelledPreparations}");
        text.AppendLine($"- Stale-work disposals: {evidence.StaleDisposals}");
        text.AppendLine($"- Session-safety disposals: {evidence.SessionSafetyDisposals}");
        text.AppendLine($"- Selected 14B model unload verified: {evidence.SelectedModelUnloaded?.ToString() ?? "unavailable"}");
        text.AppendLine($"- Eligible steady-state opportunities: {verdict.EligibleSteadyStateOpportunities}");
        text.AppendLine($"- Readiness rate: {verdict.ReadinessRate:P0} ({evidence.ReadyCandidates}/{verdict.EligibleSteadyStateOpportunities})");
        text.AppendLine($"- Hard-discard failures: {verdict.HardDiscardFailures}");
        text.AppendLine($"- Enrichment-unavailable results: {verdict.EnrichmentUnavailableResults}");
        text.AppendLine($"- Forbidden media/audio activity: {evidence.ForbiddenActivityCount}");
        text.AppendLine($"- Final Gate 3 verdict: {(verdict.Passed ? "PASS" : "FAIL")} — {string.Join("; ", verdict.Reasons)}");
        text.AppendLine();
        text.AppendLine("## Preparation timing and local diagnostics");
        foreach (M9BGate3PreparationRecord record in evidence.Preparations)
        {
            text.AppendLine($"### Opportunity {record.OpportunityId}: {record.Artist} — {record.Title}");
            text.AppendLine($"- Outcome: `{record.OutcomeKind}`; ready={record.CandidateReady}; cancelled={record.Cancelled}; {record.Outcome}");
            text.AppendLine($"- Context: source=`{record.ContextSource}`; card=`{record.ContextCardIdentity ?? "unavailable"}`.");
            text.AppendLine($"- Timing: elapsed={record.ElapsedMilliseconds:0} ms; total-ns={record.TotalDurationNanoseconds?.ToString() ?? "unavailable"}; load-ns={record.LoadDurationNanoseconds?.ToString() ?? "unavailable"}.");
            foreach (M9BGenerationDiagnostic diagnostic in record.GenerationDiagnostics ?? [])
            {
                text.AppendLine($"- Generation attempt {diagnostic.Attempt}: classification=`{diagnostic.ValidationKind?.ToString() ?? "GenerationFailed"}`; reason={diagnostic.Reason}");
                text.AppendLine($"  - Advisory flags: {(diagnostic.AdvisoryFlags.Count == 0 ? "none" : string.Join("; ", diagnostic.AdvisoryFlags))}");
                text.AppendLine($"  - Raw output: {diagnostic.RawOutput ?? "<no model output>"}");
                text.AppendLine($"  - Normalized output: {diagnostic.NormalizedOutput ?? "<not available>"}");
                text.AppendLine($"  - Metrics: total-ns={diagnostic.Metrics?.TotalDurationNanoseconds?.ToString() ?? "unavailable"}; load-ns={diagnostic.Metrics?.LoadDurationNanoseconds?.ToString() ?? "unavailable"}.");
            }

            text.AppendLine();
        }

        return text.ToString();
    }
}

internal static class M9BGate3SilentCommand
{
    internal static async Task<int> RunAsync()
    {
        string logDirectory = new LocalAppDataLogFolderProvider().GetLogFolderPath();
        using LocalLog log = LocalLog.Create("m9b-gate3-silent", logDirectory);
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, args) =>
        {
            args.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += handler;

        MusicBrainzWikidataEnricher? liveResolver = null;
        try
        {
            log.Write("Requesting the Windows global media-session manager for silent observation...");
            GlobalSystemMediaTransportControlsSessionManager manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            AiCommentConfiguration configuration = AiCommentConfiguration.Default with { Model = M9BConstants.Model };
            configuration.Validate();
            using var generator = new OllamaClient(configuration);
            MusicContextEnrichmentConfiguration contextConfiguration = MusicContextEnrichmentConfiguration.Default with
            {
                UserAgent = "ZoyaRadio/0.8 (private-local-M9B-gate3-silent)",
            };
            var cache = new M9BContextCache(M9BContextCachePaths.DefaultDirectory, contextConfiguration);
            var identityCache = new MusicContextCache(Path.Combine(M9BContextCachePaths.DefaultDirectory, "live-identity"), contextConfiguration);
            liveResolver = new MusicBrainzWikidataEnricher(contextConfiguration, identityCache);
            var provider = new M9BContextProvider(M9BContextCatalog.Load(), liveResolver, cache, log);
            var comments = new M9BGroundedCommentService(provider, generator, configuration, new M9BRecentCommentState(), log);
            return await new M9BGate3SilentRunner(log, manager, generator, configuration, comments, cancellation.Token).RunAsync();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            log.Write("GATE 3 CANCELLED BEFORE ARMING: no desktop media action was issued.");
            return 0;
        }
        catch (Exception exception)
        {
            log.Warning("M9B Gate 3 could not start", exception);
            return 1;
        }
        finally
        {
            liveResolver?.Dispose();
            Console.CancelKeyPress -= handler;
        }
    }
}