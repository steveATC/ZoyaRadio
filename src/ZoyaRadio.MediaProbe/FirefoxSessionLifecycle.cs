namespace ZoyaRadio.MediaProbe;

internal enum FirefoxMediaSessionChangeStatus
{
    SessionListUnavailable,
    NoFirefoxCandidate,
    OriginalSessionStillValid,
    ReplacementCandidate,
    AmbiguousFirefoxCandidates,
}

internal sealed record FirefoxMediaSessionChange<T>(
    FirefoxMediaSessionChangeStatus Status,
    T? Replacement,
    int MatchingFirefoxSessionCount)
    where T : class;

internal static class FirefoxMediaSessionLifecycle
{
    public static FirefoxMediaSessionChange<T> Evaluate<T>(
        bool sessionListReadSucceeded,
        T? selectedSession,
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

        T onlyMatch = exactFirefoxMatches[0];
        return ReferenceEquals(onlyMatch, selectedSession)
            ? new FirefoxMediaSessionChange<T>(
                FirefoxMediaSessionChangeStatus.OriginalSessionStillValid,
                null,
                1)
            : new FirefoxMediaSessionChange<T>(
                FirefoxMediaSessionChangeStatus.ReplacementCandidate,
                onlyMatch,
                1);
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
