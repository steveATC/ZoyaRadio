namespace ZoyaRadio.MediaProbe;

internal enum FirefoxMediaSessionChangeStatus
{
    SessionListUnavailable,
    NoFirefoxCandidate,
    SingleFirefoxCandidate,
    AmbiguousFirefoxCandidates,
}

internal sealed record FirefoxMediaSessionChange<T>(
    FirefoxMediaSessionChangeStatus Status,
    T? Candidate,
    int MatchingFirefoxSessionCount)
    where T : class;

internal static class FirefoxMediaSessionLifecycle
{
    public static FirefoxMediaSessionChange<T> Evaluate<T>(
        bool sessionListReadSucceeded,
        IReadOnlyList<T> exactFirefoxMatches)
        where T : class
    {
        if (!sessionListReadSucceeded)
        {
            return new FirefoxMediaSessionChange<T>(
                FirefoxMediaSessionChangeStatus.SessionListUnavailable,
                null,
                0);
        }

        if (exactFirefoxMatches.Count == 0)
        {
            return new FirefoxMediaSessionChange<T>(
                FirefoxMediaSessionChangeStatus.NoFirefoxCandidate,
                null,
                0);
        }

        if (exactFirefoxMatches.Count > 1)
        {
            return new FirefoxMediaSessionChange<T>(
                FirefoxMediaSessionChangeStatus.AmbiguousFirefoxCandidates,
                null,
                exactFirefoxMatches.Count);
        }

        return new FirefoxMediaSessionChange<T>(
            FirefoxMediaSessionChangeStatus.SingleFirefoxCandidate,
            exactFirefoxMatches[0],
            1);
    }
}

internal sealed record FirefoxSessionContinuityEvidence(
    bool SourceIsVerifiedFirefox,
    bool HasUsableMedia,
    bool IsPlaying,
    MediaSnapshot Media)
{
    public bool IsEligible => SourceIsVerifiedFirefox && HasUsableMedia && IsPlaying;

    public static FirefoxSessionContinuityEvidence FromDetails(RadioSessionDetails details)
    {
        return new FirefoxSessionContinuityEvidence(
            details.SourceReadSucceeded
                && string.Equals(
                    details.SourceId,
                    FirefoxSessionSelector.VerifiedSourceId,
                    StringComparison.OrdinalIgnoreCase),
            details.MediaReadSucceeded && AnnouncementText.TryBuild(details.Media, out _),
            details.PlaybackReadSucceeded && PlaybackControlRules.CanStart(details.PlaybackStatus),
            details.Media);
    }
}

internal enum FirefoxSessionContinuityStatus
{
    SelectedWrapperStillValid,
    SelectedWrapperRetainedBecauseCandidateIsIneligible,
    RefreshedWrapperSameLogicalSession,
    ReacquisitionRequired,
}

internal sealed record FirefoxSessionContinuityDecision(
    FirefoxSessionContinuityStatus Status)
{
    public bool PreservesPendingAndActiveWork =>
        Status != FirefoxSessionContinuityStatus.ReacquisitionRequired;

    public bool ShouldRefreshWrapper =>
        Status == FirefoxSessionContinuityStatus.RefreshedWrapperSameLogicalSession;
}

internal static class FirefoxSessionContinuity
{
    public static FirefoxSessionContinuityDecision Evaluate(
        bool sameWrapper,
        FirefoxSessionContinuityEvidence selected,
        FirefoxSessionContinuityEvidence candidate)
    {
        if (!selected.IsEligible)
        {
            return new FirefoxSessionContinuityDecision(
                FirefoxSessionContinuityStatus.ReacquisitionRequired);
        }

        if (sameWrapper)
        {
            return new FirefoxSessionContinuityDecision(
                FirefoxSessionContinuityStatus.SelectedWrapperStillValid);
        }

        if (!candidate.IsEligible)
        {
            return new FirefoxSessionContinuityDecision(
                FirefoxSessionContinuityStatus.SelectedWrapperRetainedBecauseCandidateIsIneligible);
        }

        bool sameTrack = RadioHistory.SameNormalizedText(selected.Media.Title, candidate.Media.Title)
            && RadioHistory.SameNormalizedText(selected.Media.Artist, candidate.Media.Artist);
        return new FirefoxSessionContinuityDecision(
            sameTrack
                ? FirefoxSessionContinuityStatus.RefreshedWrapperSameLogicalSession
                : FirefoxSessionContinuityStatus.ReacquisitionRequired);
    }
}

internal static class FirefoxSessionRebindSafety
{
    public static async Task DiscardPendingBeforeRebindingAsync(
        Action discardPending,
        Func<Task> waitForPendingCompletion)
    {
        ArgumentNullException.ThrowIfNull(discardPending);
        ArgumentNullException.ThrowIfNull(waitForPendingCompletion);

        discardPending();
        await waitForPendingCompletion();
    }
}
