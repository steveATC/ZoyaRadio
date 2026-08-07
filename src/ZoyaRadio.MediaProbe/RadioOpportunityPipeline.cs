using Windows.Media.Control;

namespace ZoyaRadio.MediaProbe;

internal interface IRadioClock
{
    DateTimeOffset Now { get; }

    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

internal sealed class SystemRadioClock : IRadioClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
    }
}

internal interface IRadioCommentProvider
{
    Task<CommentSelection> SelectAsync(
        RadioCommentContext context,
        bool aiEnabled,
        CancellationToken cancellationToken);
}

internal interface IRadioOpportunityHost
{
    Task<RadioOpportunitySnapshot> ReadSnapshotAsync(
        long opportunityId,
        CancellationToken cancellationToken);

    Task<IPreparedAnnouncementSpeech> PrepareSpeechAsync(
        string text,
        CancellationToken cancellationToken);

    Task<AnnouncementSequenceResult> RunAnnouncementAsync(
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        CancellationToken cancellationToken);
}

internal sealed record RadioOpportunitySnapshot(
    bool Armed,
    bool SilentMode,
    bool OpportunityStillPending,
    bool SessionListReadSucceeded,
    int MatchingFirefoxSessionCount,
    bool SameSelectedSession,
    string? SourceId,
    bool SourceReadSucceeded,
    MediaSnapshot CurrentMedia,
    bool MediaReadSucceeded,
    GlobalSystemMediaTransportControlsSessionPlaybackStatus? PlaybackStatus,
    bool PlaybackReadSucceeded);

internal sealed record RadioSafetyDecision(bool CanProceed, string Reason);

internal static class RadioOpportunitySafety
{
    public static RadioSafetyDecision Evaluate(
        RadioCommentOpportunity opportunity,
        RadioOpportunitySnapshot snapshot,
        DateTimeOffset now,
        TimeSpan minimumTrackAge,
        bool requireMinimumAge)
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
            return Block("the comment opportunity is no longer current");
        }

        if (!snapshot.SessionListReadSucceeded)
        {
            return Block("the Windows media-session list could not be read");
        }

        if (snapshot.MatchingFirefoxSessionCount == 0)
        {
            return Block("the intended firefox.exe session is missing");
        }

        if (snapshot.MatchingFirefoxSessionCount > 1)
        {
            return Block("more than one firefox.exe session is available");
        }

        if (!snapshot.SameSelectedSession)
        {
            return Block("the available Firefox session is not the originally selected session");
        }

        if (!snapshot.SourceReadSucceeded
            || !string.Equals(
                snapshot.SourceId,
                FirefoxSessionSelector.VerifiedSourceId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Block("the selected session source identifier could not be revalidated");
        }

        if (!snapshot.MediaReadSucceeded
            || !AnnouncementText.HasSameTitleAndArtist(
                opportunity.Context.CurrentTrack,
                snapshot.CurrentMedia))
        {
            return Block("the current title or artist changed");
        }

        if (!snapshot.PlaybackReadSucceeded
            || !PlaybackControlRules.CanStart(snapshot.PlaybackStatus))
        {
            return Block("Firefox is not currently Playing");
        }

        TimeSpan age = now - opportunity.Context.CurrentTrackStartedAt;
        if (requireMinimumAge && age < minimumTrackAge)
        {
            return Block("the current track has not reached the minimum age");
        }

        return new RadioSafetyDecision(true, "all automatic playback checks passed");
    }

    private static RadioSafetyDecision Block(string reason)
    {
        return new RadioSafetyDecision(false, reason);
    }
}

internal enum RadioOpportunityResultStatus
{
    Completed,
    Abandoned,
    Failed,
}

internal sealed record RadioOpportunityResult(
    RadioOpportunityResultStatus Status,
    string Reason,
    CommentSelection? Selection,
    AnnouncementSequenceResult? AnnouncementResult);

internal sealed class RadioOpportunityPipeline
{
    private readonly RadioBehaviorConfiguration _configuration;
    private readonly IRadioCommentProvider _commentProvider;
    private readonly IRadioOpportunityHost _host;
    private readonly IRadioClock _clock;
    private readonly IAnnouncementLog _log;

    public RadioOpportunityPipeline(
        RadioBehaviorConfiguration configuration,
        IRadioCommentProvider commentProvider,
        IRadioOpportunityHost host,
        IRadioClock clock,
        IAnnouncementLog log)
    {
        configuration.Validate();
        _configuration = configuration;
        _commentProvider = commentProvider;
        _host = host;
        _clock = clock;
        _log = log;
    }

    public async Task<RadioOpportunityResult> RunAsync(
        RadioCommentOpportunity opportunity,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        try
        {
            RadioOpportunitySnapshot initial = await _host.ReadSnapshotAsync(
                opportunity.Id,
                cancellationToken);
            RadioSafetyDecision initialSafety = RadioOpportunitySafety.Evaluate(
                opportunity,
                initial,
                _clock.Now,
                _configuration.MinimumTrackAgeBeforeSpeech,
                requireMinimumAge: false);

            if (!initialSafety.CanProceed)
            {
                return Abandoned($"generation was not started because {initialSafety.Reason}");
            }

            _log.Write("COMMENT OPPORTUNITY: generation begins while Firefox remains Playing.");
            CommentSelection selection = await _commentProvider.SelectAsync(
                opportunity.Context,
                aiEnabled,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            TimeSpan currentAge = _clock.Now - opportunity.Context.CurrentTrackStartedAt;
            TimeSpan remainingAge = _configuration.MinimumTrackAgeBeforeSpeech - currentAge;
            if (remainingAge > TimeSpan.Zero)
            {
                _log.Write(
                    $"MINIMUM TRACK AGE: waiting {remainingAge.TotalSeconds:0.0} more second(s) before playback revalidation.");
                await _clock.DelayAsync(remainingAge, cancellationToken);
            }
            else
            {
                _log.Write("MINIMUM TRACK AGE: already satisfied after comment generation.");
            }

            RadioOpportunitySnapshot beforePreparation = await _host.ReadSnapshotAsync(
                opportunity.Id,
                cancellationToken);
            RadioSafetyDecision preparationSafety = RadioOpportunitySafety.Evaluate(
                opportunity,
                beforePreparation,
                _clock.Now,
                _configuration.MinimumTrackAgeBeforeSpeech,
                requireMinimumAge: true);

            if (!preparationSafety.CanProceed)
            {
                return Abandoned($"pending interruption was abandoned because {preparationSafety.Reason}", selection);
            }

            _log.Write("Preparing the already-generated Unicode comment with the selected local voice...");
            using IPreparedAnnouncementSpeech preparedSpeech = await _host.PrepareSpeechAsync(
                selection.Text,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            RadioOpportunitySnapshot beforePause = await _host.ReadSnapshotAsync(
                opportunity.Id,
                cancellationToken);
            RadioSafetyDecision pauseSafety = RadioOpportunitySafety.Evaluate(
                opportunity,
                beforePause,
                _clock.Now,
                _configuration.MinimumTrackAgeBeforeSpeech,
                requireMinimumAge: true);
            TimeSpan verifiedAge = _clock.Now - opportunity.Context.CurrentTrackStartedAt;
            _log.Write(
                $"MINIMUM TRACK AGE {(verifiedAge >= _configuration.MinimumTrackAgeBeforeSpeech ? "VERIFIED" : "NOT VERIFIED")}: " +
                $"{verifiedAge.TotalSeconds:0.0} seconds " +
                $"(required: {_configuration.MinimumTrackAgeBeforeSpeech.TotalSeconds:0.0}).");
            _log.Write(
                $"PLAYBACK REVALIDATION: {(pauseSafety.CanProceed ? "PASS" : "BLOCKED")} - {pauseSafety.Reason}.");

            if (!pauseSafety.CanProceed)
            {
                return Abandoned($"pending interruption was abandoned because {pauseSafety.Reason}", selection);
            }

            AnnouncementSequenceResult announcement = await _host.RunAnnouncementAsync(
                preparedSpeech,
                _configuration.MaximumSpeechDuration,
                cancellationToken);

            if (!announcement.TestPassed)
            {
                return new RadioOpportunityResult(
                    RadioOpportunityResultStatus.Failed,
                    "the pause/speech/resume sequence did not fully pass",
                    selection,
                    announcement);
            }

            return new RadioOpportunityResult(
                RadioOpportunityResultStatus.Completed,
                "the automatic comment completed and Firefox playback was restored",
                selection,
                announcement);
        }
        catch (OperationCanceledException)
        {
            return Abandoned("the pending comment was cancelled before it could safely begin or finish");
        }
        catch (Exception exception)
        {
            _log.Warning("Automatic comment pipeline failed", exception);
            return new RadioOpportunityResult(
                RadioOpportunityResultStatus.Failed,
                exception.Message,
                null,
                null);
        }
    }

    private RadioOpportunityResult Abandoned(string reason, CommentSelection? selection = null)
    {
        _log.Write($"PENDING COMMENT ABANDONED: {reason}. No catch-up comment will be queued.");
        return new RadioOpportunityResult(
            RadioOpportunityResultStatus.Abandoned,
            reason,
            selection,
            null);
    }
}
