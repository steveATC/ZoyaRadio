using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal interface ITransitionCommentProvider
{
    Task<CommentSelection> SelectAsync(
        RadioPreparationContext context,
        bool aiEnabled,
        CancellationToken cancellationToken);
}

internal interface ITransitionRadioHost
{
    Task<RadioOpportunitySnapshot> ReadSnapshotAsync(
        long opportunityId,
        CancellationToken cancellationToken);

    Task<IPreparedAnnouncementSpeech> PrepareSpeechAsync(
        string text,
        CancellationToken cancellationToken);

    Task<TransitionDuckingResult> RunDuckedAnnouncementAsync(
        RadioTransitionDelivery transition,
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        DateTimeOffset speechStartDeadline,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken);

    Task<TransitionDuckingResult> RunDuckedLinerAsync(
        ReusableLinerTransition transition,
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        DateTimeOffset speechStartDeadline,
        CancellationToken cancellationToken);
}

internal static class TransitionPreparationSafety
{
    public static RadioSafetyDecision Evaluate(
        RadioPreparationOpportunity opportunity,
        RadioOpportunitySnapshot snapshot,
        DateTimeOffset now,
        TimeSpan minimumTrackAge,
        bool requireMinimumAge)
    {
        RadioSafetyDecision shared = EvaluateShared(opportunity.Id, snapshot);
        if (!shared.CanProceed)
        {
            return shared;
        }

        if (!snapshot.MediaReadSucceeded
            || !AnnouncementText.HasSameTitleAndArtist(
                opportunity.Context.SubjectTrack,
                snapshot.CurrentMedia))
        {
            return Block("Track A changed before preparation completed");
        }

        if (!snapshot.PlaybackReadSucceeded
            || !PlaybackControlRules.CanStart(snapshot.PlaybackStatus))
        {
            return Block("Firefox is not currently Playing");
        }

        TimeSpan age = now - opportunity.Context.SubjectTrackStartedAt;
        if (requireMinimumAge && age < minimumTrackAge)
        {
            return Block("Track A has not reached the preparation age");
        }

        return new RadioSafetyDecision(true, "Track A is current, stable, and Playing");
    }

    private static RadioSafetyDecision EvaluateShared(
        long opportunityId,
        RadioOpportunitySnapshot snapshot)
    {
        if (!snapshot.Armed)
        {
            return Block("automatic behavior is not armed");
        }

        if (snapshot.SilentMode)
        {
            return Block("Silent Mode is ON");
        }

        if (!snapshot.OpportunityStillPending)
        {
            return Block("the preparation opportunity is no longer current");
        }

        if (!snapshot.SessionListReadSucceeded)
        {
            return Block("the Windows media-session list could not be read");
        }

        if (snapshot.MatchingFirefoxSessionCount != 1)
        {
            return Block($"Windows reports {snapshot.MatchingFirefoxSessionCount} exact firefox.exe media sessions");
        }

        if (!snapshot.SameSelectedSession)
        {
            return Block("the available Firefox media session is not the originally selected session");
        }

        if (!snapshot.SourceReadSucceeded
            || !string.Equals(
                snapshot.SourceId,
                FirefoxSessionSelector.VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Block("the selected Firefox source identifier could not be revalidated");
        }

        _ = opportunityId;
        return new RadioSafetyDecision(true, "shared preparation checks passed");
    }

    private static RadioSafetyDecision Block(string reason) => new(false, reason);
}

internal static class TransitionDeliverySafety
{
    public static RadioSafetyDecision Evaluate(
        RadioTransitionDelivery transition,
        RadioOpportunitySnapshot snapshot,
        DateTimeOffset now,
        TimeSpan deliveryWindow)
    {
        if (!snapshot.Armed)
        {
            return Block("automatic behavior is not armed");
        }

        if (snapshot.SilentMode)
        {
            return Block("Silent Mode is ON");
        }

        if (!snapshot.OpportunityStillPending)
        {
            return Block("the prepared opportunity is no longer current");
        }

        if (!snapshot.SessionListReadSucceeded || snapshot.MatchingFirefoxSessionCount != 1)
        {
            return Block("the exact Firefox media-session target is missing or ambiguous");
        }

        if (!snapshot.SameSelectedSession)
        {
            return Block("the Firefox media session is not the originally selected object");
        }

        if (!snapshot.SourceReadSucceeded
            || !string.Equals(
                snapshot.SourceId,
                FirefoxSessionSelector.VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Block("the selected Firefox source identifier could not be revalidated");
        }

        if (!snapshot.MediaReadSucceeded
            || !AnnouncementText.HasSameTitleAndArtist(transition.NewTrack, snapshot.CurrentMedia))
        {
            return Block("Track B changed before delivery could begin");
        }

        if (!snapshot.PlaybackReadSucceeded
            || !PlaybackControlRules.CanStart(snapshot.PlaybackStatus))
        {
            return Block("Track B is not Playing");
        }

        TimeSpan elapsed = now - transition.TransitionObservedAt;
        if (elapsed < TimeSpan.Zero || elapsed > deliveryWindow)
        {
            return Block(
                $"the {deliveryWindow.TotalSeconds:0.0}-second transition delivery window was missed");
        }

        return new RadioSafetyDecision(true, "Track B and the original Firefox media session are current and Playing");
    }

    private static RadioSafetyDecision Block(string reason) => new(false, reason);
}

internal sealed class PreparedTransitionOpportunity : IDisposable
{
    private bool _disposed;

    public PreparedTransitionOpportunity(
        RadioPreparationOpportunity opportunity,
        CommentSelection selection,
        IPreparedAnnouncementSpeech speech,
        DateTimeOffset readyAt)
    {
        Opportunity = opportunity;
        Selection = selection;
        Speech = speech;
        ReadyAt = readyAt;
    }

    public RadioPreparationOpportunity Opportunity { get; }

    public CommentSelection Selection { get; }

    public IPreparedAnnouncementSpeech Speech { get; }

    public DateTimeOffset ReadyAt { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Speech.Dispose();
    }
}

internal enum TransitionPreparationStatus
{
    Ready,
    Abandoned,
    Failed,
}

internal sealed record TransitionPreparationResult(
    TransitionPreparationStatus Status,
    string Reason,
    PreparedTransitionOpportunity? Prepared);

internal enum TransitionDeliveryStatus
{
    Completed,
    Abandoned,
    Failed,
}

internal sealed record TransitionDeliveryResult(
    TransitionDeliveryStatus Status,
    string Reason,
    TransitionDuckingResult? Ducking);

internal sealed class TransitionRadioPipeline
{
    private readonly RadioBehaviorConfiguration _configuration;
    private readonly ITransitionCommentProvider _commentProvider;
    private readonly ITransitionRadioHost _host;
    private readonly IRadioClock _clock;
    private readonly IAnnouncementLog _log;
    private readonly Action<RadioActivitySnapshot>? _activityChanged;

    public TransitionRadioPipeline(
        RadioBehaviorConfiguration configuration,
        ITransitionCommentProvider commentProvider,
        ITransitionRadioHost host,
        IRadioClock clock,
        IAnnouncementLog log,
        Action<RadioActivitySnapshot>? activityChanged = null)
    {
        configuration.Validate();
        _configuration = configuration;
        _commentProvider = commentProvider;
        _host = host;
        _clock = clock;
        _log = log;
        _activityChanged = activityChanged;
    }

    public async Task<TransitionPreparationResult> PrepareAsync(
        RadioPreparationOpportunity opportunity,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        CommentSelection? selection = null;
        IPreparedAnnouncementSpeech? speech = null;

        try
        {
            _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxTarget));
            RadioOpportunitySnapshot initial = await _host.ReadSnapshotAsync(
                opportunity.Id,
                cancellationToken);
            RadioSafetyDecision initialSafety = TransitionPreparationSafety.Evaluate(
                opportunity,
                initial,
                _clock.Now,
                _configuration.MinimumTrackAgeBeforeSpeech,
                requireMinimumAge: false);
            if (!initialSafety.CanProceed)
            {
                return AbandonPreparation(initialSafety.Reason);
            }

            TimeSpan age = _clock.Now - opportunity.Context.SubjectTrackStartedAt;
            TimeSpan remainingAge = _configuration.MinimumTrackAgeBeforeSpeech - age;
            if (remainingAge > TimeSpan.Zero)
            {
                _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.WaitingForTrackStability));
                _log.Write(
                    $"TRACK A STABILITY WAIT: {remainingAge.TotalSeconds:0.0} second(s) remain before preparation may begin.");
                await _clock.DelayAsync(remainingAge, cancellationToken);
            }

            RadioOpportunitySnapshot eligible = await _host.ReadSnapshotAsync(
                opportunity.Id,
                cancellationToken);
            RadioSafetyDecision eligibleSafety = TransitionPreparationSafety.Evaluate(
                opportunity,
                eligible,
                _clock.Now,
                _configuration.MinimumTrackAgeBeforeSpeech,
                requireMinimumAge: true);
            if (!eligibleSafety.CanProceed)
            {
                return AbandonPreparation(eligibleSafety.Reason);
            }

            _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.GeneratingComment));
            _log.Write($"TRACK A ELIGIBLE FOR PREPARATION: {_clock.Now:O}");
            _log.Write("TRANSITION PREPARATION STARTED: AI work begins while Track A remains Playing.");
            selection = await _commentProvider.SelectAsync(
                opportunity.Context,
                aiEnabled,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _log.Write($"AI COMMENT READY: {_clock.Now:O}");

            RadioOpportunitySnapshot beforeSpeech = await _host.ReadSnapshotAsync(
                opportunity.Id,
                cancellationToken);
            RadioSafetyDecision beforeSpeechSafety = TransitionPreparationSafety.Evaluate(
                opportunity,
                beforeSpeech,
                _clock.Now,
                _configuration.MinimumTrackAgeBeforeSpeech,
                requireMinimumAge: true);
            if (!beforeSpeechSafety.CanProceed)
            {
                return AbandonPreparation(beforeSpeechSafety.Reason);
            }

            _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.PreparingSpeech));
            speech = await _host.PrepareSpeechAsync(selection.Text, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (speech.MeasuredDuration > _configuration.MaximumSpeechDuration)
            {
                string reason =
                    $"prepared speech measured {speech.MeasuredDuration.TotalSeconds:0.###} seconds, exceeding the " +
                    $"configured automatic maximum of {_configuration.MaximumSpeechDuration.TotalSeconds:0.###} seconds";
                _log.Write(
                    $"PREPARED SPEECH DISCARDED: {reason}. No Core Audio target will be enumerated, " +
                    "Firefox will not be ducked, speech will not start, and no catch-up delivery will be queued.");
                return AbandonPreparation(reason);
            }

            RadioOpportunitySnapshot ready = await _host.ReadSnapshotAsync(
                opportunity.Id,
                cancellationToken);
            RadioSafetyDecision readySafety = TransitionPreparationSafety.Evaluate(
                opportunity,
                ready,
                _clock.Now,
                _configuration.MinimumTrackAgeBeforeSpeech,
                requireMinimumAge: true);
            if (!readySafety.CanProceed)
            {
                return AbandonPreparation(readySafety.Reason);
            }

            var prepared = new PreparedTransitionOpportunity(
                opportunity,
                selection,
                speech,
                _clock.Now);
            speech = null;
            _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.PreparedForTransition));
            _log.Write($"NICOLE AUDIO READY: {prepared.ReadyAt:O}");
            _log.Write("WAITING FOR TRANSITION: prepared Track-A material is held in memory; no pause or duck has occurred.");
            return new TransitionPreparationResult(
                TransitionPreparationStatus.Ready,
                "Track-A comment and speech are ready for the next transition",
                prepared);
        }
        catch (OperationCanceledException)
        {
            return AbandonPreparation("preparation was cancelled or Track A changed before it finished");
        }
        catch (Exception exception)
        {
            _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
            _log.Warning("Transition preparation failed", exception);
            return new TransitionPreparationResult(
                TransitionPreparationStatus.Failed,
                exception.Message,
                null);
        }
        finally
        {
            speech?.Dispose();
        }
    }

    public async Task<TransitionDeliveryResult> DeliverAsync(
        PreparedTransitionOpportunity prepared,
        RadioTransitionDelivery transition,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken)
    {
        if (prepared.Opportunity.Id != transition.OpportunityId
            || !AnnouncementText.HasSameTitleAndArtist(
                prepared.Opportunity.Context.SubjectTrack,
                transition.CommentedTrack))
        {
            return AbandonDelivery("the prepared material does not belong to this transition");
        }

        try
        {
            _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxTarget));
            _log.Write($"TRACK A -> TRACK B TRANSITION OBSERVED: {transition.TransitionObservedAt:O}");
            _log.Write(
                $"  Track A: {MediaSnapshot.Display(transition.CommentedTrack.Title)} - {MediaSnapshot.Display(transition.CommentedTrack.Artist)}");
            _log.Write(
                $"  Track B: {MediaSnapshot.Display(transition.NewTrack.Title)} - {MediaSnapshot.Display(transition.NewTrack.Artist)}");

            RadioOpportunitySnapshot snapshot = await _host.ReadSnapshotAsync(
                transition.OpportunityId,
                cancellationToken);
            RadioSafetyDecision safety = TransitionDeliverySafety.Evaluate(
                transition,
                snapshot,
                _clock.Now,
                _configuration.TransitionDeliveryWindow);
            if (!safety.CanProceed)
            {
                return AbandonDelivery(safety.Reason);
            }

            DateTimeOffset deadline = transition.TransitionObservedAt + _configuration.TransitionDeliveryWindow;
            _log.Write(
                $"TRANSITION DELIVERY WINDOW: {_configuration.TransitionDeliveryWindow.TotalSeconds:0.0} seconds; " +
                $"speech must begin by {deadline:O}.");
            _log.Write("DELIVERY MEDIA TARGET REVALIDATED: PASS - Track B is current and Playing.");
            _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.ValidatingFirefoxAudio));

            TransitionDuckingResult ducking = await _host.RunDuckedAnnouncementAsync(
                transition,
                prepared.Speech,
                _configuration.MaximumSpeechDuration,
                deadline,
                simulatePostDuckFailure,
                cancellationToken);

            return ducking.Status switch
            {
                TransitionDuckingStatus.Completed => new TransitionDeliveryResult(
                    TransitionDeliveryStatus.Completed,
                    "prepared Track-A speech completed over ducked Track B and exact volume restoration passed",
                    ducking),
                TransitionDuckingStatus.Refused => AbandonDelivery(ducking.Reason, ducking),
                TransitionDuckingStatus.Cancelled => new TransitionDeliveryResult(
                    TransitionDeliveryStatus.Abandoned,
                    ducking.Reason,
                    ducking),
                _ => new TransitionDeliveryResult(
                    TransitionDeliveryStatus.Failed,
                    ducking.Reason,
                    ducking),
            };
        }
        catch (OperationCanceledException)
        {
            return AbandonDelivery("delivery was cancelled; any active duck ran protected restoration");
        }
        catch (Exception exception)
        {
            _log.Warning("Transition delivery failed", exception);
            return new TransitionDeliveryResult(
                TransitionDeliveryStatus.Failed,
                exception.Message,
                null);
        }
    }

    private TransitionPreparationResult AbandonPreparation(string reason)
    {
        _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
        _log.Write($"PREPARATION ABANDONED: {reason}. No catch-up comment will be queued.");
        return new TransitionPreparationResult(TransitionPreparationStatus.Abandoned, reason, null);
    }

    private TransitionDeliveryResult AbandonDelivery(
        string reason,
        TransitionDuckingResult? ducking = null)
    {
        _activityChanged?.Invoke(new RadioActivitySnapshot(RadioActivityState.OpportunityAbandoned));
        _log.Write($"TRANSITION DELIVERY ABANDONED: {reason}. Silence is the safe result.");
        return new TransitionDeliveryResult(TransitionDeliveryStatus.Abandoned, reason, ducking);
    }
}
