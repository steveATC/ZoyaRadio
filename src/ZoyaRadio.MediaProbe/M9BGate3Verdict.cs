namespace ZoyaRadio.MediaProbe;

internal sealed record M9BGate3Verdict(
    bool Passed,
    int EligibleSteadyStateOpportunities,
    double ReadinessRate,
    int HardDiscardFailures,
    int AdvisoryResults,
    int EnrichmentUnavailableResults,
    IReadOnlyList<string> Reasons)
{
    internal static M9BGate3Verdict Evaluate(M9BGate3RunEvidence evidence)
    {
        int unavailable = evidence.Preparations.Count(item => item.OutcomeKind == M9BGate3PreparationOutcome.EnrichmentUnavailable);
        int hardDiscards = evidence.Preparations.Count(item => item.OutcomeKind == M9BGate3PreparationOutcome.HardDiscard);
        int advisory = evidence.Preparations.Sum(item => (item.GenerationDiagnostics ?? [])
            .Count(diagnostic => diagnostic.ValidationKind == M9BCommentValidationKind.Advisory));
        int readyRecords = evidence.Preparations.Count(item => item.OutcomeKind == M9BGate3PreparationOutcome.Ready && item.CandidateReady);
        int eligible = evidence.Preparations.Count(item => IsEligibleSteadyState(item.OutcomeKind));
        double readinessRate = eligible == 0 ? 0 : (double)readyRecords / eligible;
        var reasons = new List<string>();

        if (evidence.GenuineTransitions < 12)
        {
            reasons.Add("fewer than 12 genuine Firefox transitions");
        }

        if (evidence.ReadyCandidates != readyRecords)
        {
            reasons.Add("ready-candidate summary does not match typed preparation evidence");
        }

        if (readyRecords == 0)
        {
            reasons.Add("no candidate became ready before a transition");
        }

        if (evidence.SuppressedDeliveries == 0)
        {
            reasons.Add("no prepared delivery was safely suppressed");
        }

        if (evidence.SelectedModelUnloaded is not true)
        {
            reasons.Add("selected 14B model unload was not verified");
        }

        if (evidence.ForbiddenActivityCount != 0)
        {
            reasons.Add("forbidden playback, speech, ducking, or volume activity was recorded");
        }

        return new M9BGate3Verdict(
            reasons.Count == 0,
            eligible,
            readinessRate,
            hardDiscards,
            advisory,
            unavailable,
            reasons);
    }

    private static bool IsEligibleSteadyState(M9BGate3PreparationOutcome outcome) => outcome is
        M9BGate3PreparationOutcome.Ready or
        M9BGate3PreparationOutcome.ModelUnavailable or
        M9BGate3PreparationOutcome.GenerationFailed or
        M9BGate3PreparationOutcome.ModelFallback or
        M9BGate3PreparationOutcome.HardDiscard or
        M9BGate3PreparationOutcome.AdvisoryFallback;
}