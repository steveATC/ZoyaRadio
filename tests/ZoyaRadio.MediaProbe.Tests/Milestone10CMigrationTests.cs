using ZoyaRadio.MediaProbe;

internal static class Milestone10CMigrationTests
{
    public static void AdapterReproducesM8HappyPathAndReservesBeforeDelivery()
    {
        RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
        var legacy = new RadioBehaviorState(configuration, new TargetSource(2, 5));
        var migrated = new M10CRuntimeMigrationState(configuration, new TargetSource(2, 5));
        MediaSnapshot initial = Track("Initial", "Artist 0");
        legacy.Arm(initial, At(0));
        migrated.Arm(initial, At(0));
        AssertParity(legacy, migrated);

        MediaSnapshot trackA = Track("Track A", "Artist A");
        RadioTrackObservation legacyPreparation = legacy.ObserveTrack(trackA, At(10));
        RadioTrackObservation migratedPreparation = migrated.ObserveTrack(trackA, At(10));
        AssertObservationParity(legacyPreparation, migratedPreparation);
        long opportunityId = migratedPreparation.PreparationOpportunity!.Id;
        TestAssert.True(migrated.MarkPreparationReady(opportunityId, Prepared(migratedPreparation.PreparationOpportunity!)));
        TestAssert.True(legacy.MarkPreparationReady(opportunityId));

        MediaSnapshot trackB = Track("Track B", "Artist B");
        RadioTrackObservation legacyDelivery = legacy.ObserveTrack(trackB, At(20));
        RadioTrackObservation migratedDelivery = migrated.ObserveTrack(trackB, At(20));
        AssertObservationParity(legacyDelivery, migratedDelivery);
        M10CDeliveryReservation? reservation = migrated.TryReserveDelivery(
            migratedDelivery.Delivery!, At(20));
        TestAssert.True(reservation is not null);
        TestAssert.True(migrated.ResolveOpportunity(
            opportunityId,
            true,
            "A complete accepted comment remains bounded in history.",
            At(21),
            reservation));
        TestAssert.True(legacy.ResolveOpportunity(
            opportunityId,
            true,
            "A complete accepted comment remains bounded in history."));
        AssertParity(legacy, migrated);
    }

    public static void AdapterMissedAndRapidTransitionsDiscardMaterialAndKeepM8Schedule()
    {
        RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
        var legacy = new RadioBehaviorState(configuration, new TargetSource(2, 4, 5));
        var migrated = new M10CRuntimeMigrationState(configuration, new TargetSource(2, 4, 5));
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        migrated.Arm(Track("Initial", "Artist 0"), At(0));

        RadioTrackObservation legacyPreparation = legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        RadioTrackObservation migratedPreparation = migrated.ObserveTrack(Track("Track A", "Artist A"), At(10));
        AssertObservationParity(legacyPreparation, migratedPreparation);
        RadioTrackObservation legacyMiss = legacy.ObserveTrack(Track("Track B", "Artist B"), At(20));
        RadioTrackObservation migratedMiss = migrated.ObserveTrack(Track("Track B", "Artist B"), At(20));
        AssertObservationParity(legacyMiss, migratedMiss);
        AssertParity(legacy, migrated);

        foreach (int transition in new[] { 30, 40 })
        {
            string suffix = transition == 30 ? "C" : "D";
            AssertObservationParity(
                legacy.ObserveTrack(Track($"Track {suffix}", $"Artist {suffix}"), At(transition)),
                migrated.ObserveTrack(Track($"Track {suffix}", $"Artist {suffix}"), At(transition)));
        }

        RadioTrackObservation legacyNextPreparation = legacy.ObserveTrack(Track("Track E", "Artist E"), At(50));
        RadioTrackObservation migratedNextPreparation = migrated.ObserveTrack(Track("Track E", "Artist E"), At(50));
        AssertObservationParity(legacyNextPreparation, migratedNextPreparation);
        TestAssert.True(migratedNextPreparation.PreparationOpportunity is not null);
        long opportunityId = migratedNextPreparation.PreparationOpportunity!.Id;
        TestAssert.True(migrated.MarkPreparationReady(opportunityId, Prepared(migratedNextPreparation.PreparationOpportunity!)));
        TestAssert.True(legacy.MarkPreparationReady(opportunityId));
        RadioTrackObservation legacyDelivery = legacy.ObserveTrack(Track("Track F", "Artist F"), At(60));
        RadioTrackObservation migratedDelivery = migrated.ObserveTrack(Track("Track F", "Artist F"), At(60));
        AssertObservationParity(legacyDelivery, migratedDelivery);
        RadioTrackObservation legacyRapid = legacy.ObserveTrack(Track("Track G", "Artist G"), At(61));
        RadioTrackObservation migratedRapid = migrated.ObserveTrack(Track("Track G", "Artist G"), At(61));
        AssertObservationParity(legacyRapid, migratedRapid);
        TestAssert.True(migrated.TryReserveDelivery(migratedDelivery.Delivery!, At(41)) is null);
        AssertParity(legacy, migrated);
    }

    public static void AdapterControlsAndRebindPreserveLegacyParity()
    {
        RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
        var legacy = new RadioBehaviorState(configuration, new TargetSource(2, 4, 3, 5));
        var migrated = new M10CRuntimeMigrationState(configuration, new TargetSource(2, 4, 3, 5));
        legacy.Arm(Track("Initial", "Artist 0"), At(0));
        migrated.Arm(Track("Initial", "Artist 0"), At(0));
        legacy.ObserveTrack(Track("Track A", "Artist A"), At(10));
        migrated.ObserveTrack(Track("Track A", "Artist A"), At(10));

        TestAssert.Equal(legacy.SetSilentMode(true), migrated.SetSilentMode(true));
        TestAssert.Equal(legacy.SetSilentMode(false), migrated.SetSilentMode(false));
        AssertParity(legacy, migrated);
        TestAssert.Equal(legacy.ClearRecentHistory(), migrated.ClearRecentHistory());
        AssertParity(legacy, migrated);
        TestAssert.Equal(legacy.RebindCurrentTrack(Track("Rebound", "Artist R"), At(30)),
            migrated.RebindCurrentTrack(Track("Rebound", "Artist R"), At(30)));
        AssertParity(legacy, migrated);
    }

    public static void AdapterRetiresOwnedPreparedResourceExactlyOnce()
    {
        RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
        var migrated = new M10CRuntimeMigrationState(configuration, new TargetSource(2, 5));
        migrated.Arm(Track("Initial", "Artist 0"), At(0));
        RadioTrackObservation preparation = migrated.ObserveTrack(Track("Track A", "Artist A"), At(10));
        RadioPreparationOpportunity opportunity = preparation.PreparationOpportunity
            ?? throw new InvalidOperationException("Expected preparation.");
        var speech = new FakePreparedSpeech();
        TestAssert.True(migrated.MarkPreparationReady(opportunity.Id, Prepared(opportunity, speech)));
        RadioTrackObservation delivery = migrated.ObserveTrack(Track("Track B", "Artist B"), At(20));
        RadioTransitionDelivery due = delivery.Delivery
            ?? throw new InvalidOperationException("Expected delivery.");
        M10CDeliveryReservation? reservation = migrated.TryReserveDelivery(due, At(20));
        TestAssert.True(reservation is not null);
        TestAssert.True(migrated.ResolveOpportunity(due.OpportunityId, false, null, At(21), reservation));
        TestAssert.Equal(1, speech.DisposeCalls);
        TestAssert.False(migrated.ResolveOpportunity(due.OpportunityId, false, null, At(22)));
        TestAssert.Equal(1, speech.DisposeCalls);
    }
    private static void AssertParity(RadioBehaviorState legacy, M10CRuntimeMigrationState migrated)
    {
        RadioRuntimeState expected = legacy.GetRuntimeState();
        TestAssert.Equal(expected, migrated.GetRuntimeState());
        TestAssert.Equal(legacy.TransitionCounter, migrated.TransitionCounter);
        TestAssert.Equal(legacy.ScheduledThreshold, migrated.ScheduledThreshold);
        TestAssert.Equal(legacy.TotalObservedTransitions, migrated.TotalObservedTransitions);
        SequenceEqual(legacy.RecentTracks, migrated.RecentTracks);
        SequenceEqual(legacy.RecentAcceptedAiComments, migrated.RecentAcceptedAiComments);
    }

    private static void AssertObservationParity(RadioTrackObservation expected, RadioTrackObservation actual)
    {
        TestAssert.Equal(expected.Kind, actual.Kind);
        TestAssert.Equal(expected.PreviousTrack, actual.PreviousTrack);
        TestAssert.Equal(expected.CurrentTrack, actual.CurrentTrack);
        TestAssert.Equal(expected.TransitionCounter, actual.TransitionCounter);
        TestAssert.Equal(expected.ScheduledThreshold, actual.ScheduledThreshold);
        TestAssert.Equal(expected.TotalObservedTransitions, actual.TotalObservedTransitions);
        TestAssert.Equal(expected.PendingOpportunityInvalidated, actual.PendingOpportunityInvalidated);
        TestAssert.Equal(expected.PreparationMissedTransition, actual.PreparationMissedTransition);
        TestAssert.Equal(expected.PreparationOpportunity?.Id, actual.PreparationOpportunity?.Id);
        if (expected.PreparationOpportunity is not null && actual.PreparationOpportunity is not null)
        {
            TestAssert.Equal(expected.PreparationOpportunity.Context.SubjectTrack, actual.PreparationOpportunity.Context.SubjectTrack);
            TestAssert.Equal(expected.PreparationOpportunity.Context.SubjectTrackStartedAt, actual.PreparationOpportunity.Context.SubjectTrackStartedAt);
            SequenceEqual(expected.PreparationOpportunity.Context.TrustedListeningObservations,
                actual.PreparationOpportunity.Context.TrustedListeningObservations);
            SequenceEqual(expected.PreparationOpportunity.Context.RecentAcceptedAiComments,
                actual.PreparationOpportunity.Context.RecentAcceptedAiComments);
        }

        TestAssert.Equal(expected.Delivery?.OpportunityId, actual.Delivery?.OpportunityId);
        if (expected.Delivery is not null && actual.Delivery is not null)
        {
            TestAssert.Equal(expected.Delivery.CommentedTrack, actual.Delivery.CommentedTrack);
            TestAssert.Equal(expected.Delivery.NewTrack, actual.Delivery.NewTrack);
            TestAssert.Equal(expected.Delivery.TransitionObservedAt, actual.Delivery.TransitionObservedAt);
        }

        SequenceEqual(expected.TrustedListeningObservations, actual.TrustedListeningObservations);
    }

    private static void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
    {
        TestAssert.Equal(expected.Count, actual.Count);
        for (int index = 0; index < expected.Count; index++)
        {
            TestAssert.Equal(expected[index], actual[index]);
        }
    }

    private static PreparedTransitionOpportunity Prepared(
        RadioPreparationOpportunity opportunity,
        FakePreparedSpeech? speech = null) => new(
        opportunity,
        new CommentSelection("A bounded test comment.", CommentTextSource.FixedFallback, 4, "test", null, false),
        speech ?? new FakePreparedSpeech(),
        At(11));
    private static MediaSnapshot Track(string title, string artist) => new(title, artist, null);
    private static DateTimeOffset At(int seconds) => new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero).AddSeconds(seconds);

    private sealed class TargetSource(params int[] values) : ITransitionTargetSource
    {
        private int _next;

        public int NextInclusive(int minimumInclusive, int maximumInclusive)
        {
            int value = values[_next++];
            if (value < minimumInclusive || value > maximumInclusive)
            {
                throw new InvalidOperationException("Test supplied an out-of-range transition target.");
            }

            return value;
        }
    }

    private sealed class FakePreparedSpeech : IPreparedAnnouncementSpeech
    {
        public int DisposeCalls { get; private set; }
        public TimeSpan MeasuredDuration => TimeSpan.FromSeconds(1);

        public Task<SpeechPlaybackResult> SpeakAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            Task.FromResult(new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));

        public void Dispose() => DisposeCalls++;
    }
}