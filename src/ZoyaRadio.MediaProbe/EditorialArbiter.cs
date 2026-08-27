namespace ZoyaRadio.MediaProbe;

internal enum EditorialDecisionStage { Preparation, Airtime }
internal enum EditorialDecisionOutcome { Prepare, ReserveForAirtime, ContinueMusic }
internal enum EditorialContentFormat { ExistingComment, ReusableLiner }
internal enum EditorialDeliveryTreatment { DuckOverMusic }
internal enum EditorialPolicyPriority { Routine, Timely }
internal enum EditorialMusicPressure { SpeechPermitted, PreferMusic }

internal enum EditorialDecisionReason
{
    CandidateConsidered,
    OpportunityMismatch,
    OpportunityUnavailable,
    PreparationCapacityUnavailable,
    MaterialMissing,
    MaterialNotReady,
    MaterialExpired,
    PayloadUnavailable,
    BindingMismatch,
    TreatmentUnavailable,
    CooldownBlocked,
    SpacingBlocked,
    ProtectedPreparationBoundary,
    MissedCommentBoundary,
    MusicPressurePreferred,
    PreparationPolicyDisabled,
    AirtimePolicyDisabled,
    PolicyPriorityPreferred,
    BoundedTieBreakApplied,
    PreparationSelected,
    AirtimeSelected,
    DirectMusicContinuationSelected,
}

internal sealed record EditorialPolicyProfile(bool PreparationEnabled, bool AirtimeEnabled)
{
    public static EditorialPolicyProfile M10CEquivalent { get; } = new(true, true);
}

internal sealed record EditorialPressureSnapshot(
    EditorialMusicPressure MusicPressure,
    bool SpeechSpacingSatisfied,
    bool FormatCooldownSatisfied)
{
    public static EditorialPressureSnapshot Clear { get; } = new(
        EditorialMusicPressure.SpeechPermitted,
        SpeechSpacingSatisfied: true,
        FormatCooldownSatisfied: true);
}

internal sealed record EditorialPreparationCandidate(
    string CandidateId,
    long OpportunityId,
    EditorialContentFormat Format,
    EditorialDeliveryTreatment RequiredTreatment,
    EditorialPolicyPriority Priority,
    bool OpportunityMatches,
    bool OpportunityAvailable,
    bool PreparationCapacityAvailable);

internal sealed record EditorialAirtimeCandidate(
    string CandidateId,
    long OpportunityId,
    PreparedMaterialId MaterialId,
    EditorialContentFormat Format,
    EditorialDeliveryTreatment RequiredTreatment,
    EditorialPolicyPriority Priority,
    bool OpportunityMatches,
    PreparedMaterialLifecycleStatus? MaterialStatus,
    bool HasPayload,
    bool BindingMatches,
    DateTimeOffset? ExpiresAt,
    EditorialPressureSnapshot? CandidatePressure = null,
    bool ProtectedPreparationBoundaryClear = true,
    bool MissedCommentBoundaryClear = true);

internal sealed record EditorialPreparationInput(
    IReadOnlyList<EditorialPreparationCandidate> Candidates,
    IReadOnlySet<EditorialDeliveryTreatment> AvailableTreatments,
    EditorialPressureSnapshot Pressure,
    int TieBreakValue = 0);

internal sealed record EditorialAirtimeInput(
    DateTimeOffset ObservedAt,
    IReadOnlyList<EditorialAirtimeCandidate> Candidates,
    IReadOnlySet<EditorialDeliveryTreatment> AvailableTreatments,
    EditorialPressureSnapshot Pressure,
    int TieBreakValue = 0);

internal sealed record EditorialDecisionTrace(
    EditorialDecisionReason Reason,
    string? CandidateId = null);

internal sealed record EditorialDecision(
    EditorialDecisionStage Stage,
    EditorialDecisionOutcome Outcome,
    string? SelectedCandidateId,
    PreparedMaterialId? SelectedMaterialId,
    IReadOnlyList<EditorialDecisionTrace> Trace);

internal sealed class EditorialArbiter
{
    private readonly EditorialPolicyProfile _policy;

    public EditorialArbiter(EditorialPolicyProfile policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _policy = policy;
    }

    public EditorialDecision DecidePreparation(EditorialPreparationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateCommon(input.Candidates, input.AvailableTreatments, input.Pressure);

        var trace = new List<EditorialDecisionTrace>();
        var eligible = new List<EditorialPreparationCandidate>();

        foreach (EditorialPreparationCandidate candidate in input.Candidates)
        {
            ValidateCandidate(candidate.CandidateId, candidate.OpportunityId);
            trace.Add(new(EditorialDecisionReason.CandidateConsidered, candidate.CandidateId));

            EditorialDecisionReason? refusal = PreparationRefusal(candidate, input);
            if (refusal is not null)
            {
                trace.Add(new(refusal.Value, candidate.CandidateId));
                continue;
            }

            eligible.Add(candidate);
        }

        EditorialPreparationCandidate? selected = Select(eligible, input.TieBreakValue, trace);
        if (selected is null)
        {
            trace.Add(new(EditorialDecisionReason.DirectMusicContinuationSelected));
            return Decision(EditorialDecisionStage.Preparation, EditorialDecisionOutcome.ContinueMusic, null, null, trace);
        }

        trace.Add(new(EditorialDecisionReason.PreparationSelected, selected.CandidateId));
        return Decision(EditorialDecisionStage.Preparation, EditorialDecisionOutcome.Prepare,
            selected.CandidateId, null, trace);
    }

    public EditorialDecision DecideAirtime(EditorialAirtimeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateCommon(input.Candidates, input.AvailableTreatments, input.Pressure);

        var trace = new List<EditorialDecisionTrace>();
        var eligible = new List<EditorialAirtimeCandidate>();

        foreach (EditorialAirtimeCandidate candidate in input.Candidates)
        {
            ValidateCandidate(candidate.CandidateId, candidate.OpportunityId);
            if (string.IsNullOrWhiteSpace(candidate.MaterialId.Value))
            {
                throw new ArgumentException("Airtime candidates require a non-empty material identity.", nameof(input));
            }

            trace.Add(new(EditorialDecisionReason.CandidateConsidered, candidate.CandidateId));
            EditorialDecisionReason? refusal = AirtimeRefusal(candidate, input);
            if (refusal is not null)
            {
                trace.Add(new(refusal.Value, candidate.CandidateId));
                continue;
            }

            eligible.Add(candidate);
        }

        EditorialAirtimeCandidate? selected = Select(eligible, input.TieBreakValue, trace);
        if (selected is null)
        {
            trace.Add(new(EditorialDecisionReason.DirectMusicContinuationSelected));
            return Decision(EditorialDecisionStage.Airtime, EditorialDecisionOutcome.ContinueMusic, null, null, trace);
        }

        trace.Add(new(EditorialDecisionReason.AirtimeSelected, selected.CandidateId));
        return Decision(EditorialDecisionStage.Airtime, EditorialDecisionOutcome.ReserveForAirtime,
            selected.CandidateId, selected.MaterialId, trace);
    }

    private EditorialDecisionReason? PreparationRefusal(
        EditorialPreparationCandidate candidate,
        EditorialPreparationInput input)
    {
        if (!candidate.OpportunityMatches) return EditorialDecisionReason.OpportunityMismatch;
        if (!candidate.OpportunityAvailable) return EditorialDecisionReason.OpportunityUnavailable;
        if (!candidate.PreparationCapacityAvailable) return EditorialDecisionReason.PreparationCapacityUnavailable;
        if (!input.AvailableTreatments.Contains(candidate.RequiredTreatment)) return EditorialDecisionReason.TreatmentUnavailable;
        if (!_policy.PreparationEnabled) return EditorialDecisionReason.PreparationPolicyDisabled;
        return PressureRefusal(input.Pressure);
    }

    private EditorialDecisionReason? AirtimeRefusal(
        EditorialAirtimeCandidate candidate,
        EditorialAirtimeInput input)
    {
        if (!candidate.OpportunityMatches) return EditorialDecisionReason.OpportunityMismatch;
        if (candidate.MaterialStatus is null) return EditorialDecisionReason.MaterialMissing;
        if (candidate.MaterialStatus != PreparedMaterialLifecycleStatus.Ready) return EditorialDecisionReason.MaterialNotReady;
        if (candidate.ExpiresAt is not null && input.ObservedAt >= candidate.ExpiresAt.Value) return EditorialDecisionReason.MaterialExpired;
        if (!candidate.HasPayload) return EditorialDecisionReason.PayloadUnavailable;
        if (!candidate.BindingMatches) return EditorialDecisionReason.BindingMismatch;
        if (!input.AvailableTreatments.Contains(candidate.RequiredTreatment)) return EditorialDecisionReason.TreatmentUnavailable;
        if (!_policy.AirtimeEnabled) return EditorialDecisionReason.AirtimePolicyDisabled;
        if (!candidate.ProtectedPreparationBoundaryClear) return EditorialDecisionReason.ProtectedPreparationBoundary;
        if (!candidate.MissedCommentBoundaryClear) return EditorialDecisionReason.MissedCommentBoundary;
        return PressureRefusal(candidate.CandidatePressure ?? input.Pressure);
    }

    private static EditorialDecisionReason? PressureRefusal(EditorialPressureSnapshot pressure)
    {
        if (!pressure.FormatCooldownSatisfied) return EditorialDecisionReason.CooldownBlocked;
        if (!pressure.SpeechSpacingSatisfied) return EditorialDecisionReason.SpacingBlocked;
        if (pressure.MusicPressure == EditorialMusicPressure.PreferMusic) return EditorialDecisionReason.MusicPressurePreferred;
        return null;
    }

    private static T? Select<T>(
        IReadOnlyList<T> eligible,
        int tieBreakValue,
        List<EditorialDecisionTrace> trace)
        where T : class
    {
        if (eligible.Count == 0) return null;

        EditorialPolicyPriority best = eligible.Max(candidate => PriorityOf(candidate));
        T[] finalists = eligible.Where(candidate => PriorityOf(candidate) == best).ToArray();
        if (finalists.Length != eligible.Count)
        {
            trace.Add(new(EditorialDecisionReason.PolicyPriorityPreferred));
        }

        if (finalists.Length == 1) return finalists[0];

        int index = (int)((uint)tieBreakValue % (uint)finalists.Length);
        trace.Add(new(EditorialDecisionReason.BoundedTieBreakApplied));
        return finalists[index];
    }

    private static EditorialPolicyPriority PriorityOf<T>(T candidate) => candidate switch
    {
        EditorialPreparationCandidate preparation => preparation.Priority,
        EditorialAirtimeCandidate airtime => airtime.Priority,
        _ => throw new ArgumentOutOfRangeException(nameof(candidate)),
    };

    private static void ValidateCommon<T>(
        IReadOnlyList<T> candidates,
        IReadOnlySet<EditorialDeliveryTreatment> treatments,
        EditorialPressureSnapshot pressure)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(treatments);
        ArgumentNullException.ThrowIfNull(pressure);
    }

    private static void ValidateCandidate(string candidateId, long opportunityId)
    {
        if (string.IsNullOrWhiteSpace(candidateId))
        {
            throw new ArgumentException("Editorial candidates require a non-empty identity.", nameof(candidateId));
        }

        if (opportunityId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(opportunityId), "Editorial opportunity identities must be positive.");
        }
    }

    private static EditorialDecision Decision(
        EditorialDecisionStage stage,
        EditorialDecisionOutcome outcome,
        string? candidateId,
        PreparedMaterialId? materialId,
        List<EditorialDecisionTrace> trace) =>
        new(stage, outcome, candidateId, materialId, trace.ToArray());
}
