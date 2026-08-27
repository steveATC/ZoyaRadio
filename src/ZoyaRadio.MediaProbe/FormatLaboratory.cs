using System.Collections.ObjectModel;
using System.Globalization;

namespace ZoyaRadio.MediaProbe;

internal enum CreativeBindingKind
{
    Evergreen,
    ContextBound,
}

internal enum CreativeContextFactId
{
    CurrentTrackTitle,
    CurrentTrackArtist,
    OpportunityClass,
    OpportunityDecisionReason,
    TotalObservedTransitions,
    RecentTracks,
    RecentAcceptedComments,
    LastCompletedHostSpeechTransition,
}

internal enum CreativeFactScope
{
    Track,
    Artist,
    Opportunity,
    Session,
    CommentHistory,
    OperationalPolicy,
}

internal enum ContentOutputKind
{
    ExactSpokenScript,
}

internal enum CreativeDirectionStatus
{
    Created,
    Unavailable,
    Failed,
    Cancelled,
}

internal enum ContentGenerationStatus
{
    Generated,
    Unavailable,
    Failed,
    Cancelled,
    NoCandidate,
}

internal enum ContentValidationStatus
{
    Valid,
    Rejected,
    Failed,
    Cancelled,
}

internal enum FormatLaboratoryOutcome
{
    GeneratedAndValid,
    DirectionUnavailable,
    DirectionRejected,
    UnsupportedFormat,
    ContractMismatch,
    ProducerUnavailable,
    GenerationFailed,
    ValidationRejected,
    ValidatorFailed,
    Cancelled,
    NoCandidate,
}

internal sealed record CreativeFormatIdentity(string Id, int Version);

internal sealed record CreativeComponentIdentity(string Id, int Version);

internal sealed record CreativePersonalityIdentity(string Id, int Version);

internal sealed record CreativeContextPermission(
    CreativeContextFactId Fact,
    CreativeFactScope Scope);

internal sealed record CreativeOptionalIntent(
    string? CastIntent,
    string? PerformanceIntent,
    string? LoreIntent);

internal sealed record CreativeDirectionBrief(
    int SchemaVersion,
    string DirectionId,
    CreativeFormatIdentity Format,
    CreativePersonalityIdentity Personality,
    string SpeakerRole,
    string OnAirSituation,
    string EditorialObjective,
    string Structure,
    CreativeBindingKind Binding,
    IReadOnlyList<CreativeContextPermission> ContextPermissions,
    IReadOnlyList<string> Prohibitions,
    CreativeOptionalIntent OptionalIntent);

internal sealed record ContentOutputContract(
    CreativeFormatIdentity Format,
    ContentOutputKind OutputKind,
    int MinimumWords,
    int MaximumWords,
    int MaximumParagraphs,
    int MaximumCandidates,
    CreativeComponentIdentity Validator);

internal sealed record FormatLaboratoryScenario(
    string ScenarioId,
    CreativeFormatIdentity Format,
    CreativePersonalityIdentity Personality,
    CreativeContextPacket Context,
    ContentOutputContract OutputContract);

internal sealed record ContentGenerationRequest(
    CreativeDirectionBrief Direction,
    CreativeContextPacket Context,
    ContentOutputContract OutputContract);

internal sealed record GeneratedContentCandidate(
    string CandidateId,
    CreativeFormatIdentity Format,
    string Script,
    IReadOnlyList<CreativeContextFactId> DeclaredFactsUsed);

internal sealed record CreativeDirectionResult(
    CreativeDirectionStatus Status,
    CreativeDirectionBrief? Direction,
    string? Detail = null);

internal sealed record ContentGenerationResult(
    ContentGenerationStatus Status,
    CreativeFormatIdentity Format,
    IReadOnlyList<GeneratedContentCandidate> Candidates,
    string? Detail = null);

internal sealed record ContentValidationResult(
    ContentValidationStatus Status,
    string? Detail = null);

internal sealed record FormatLaboratoryRunRecord(
    FormatLaboratoryOutcome Outcome,
    CreativeDirectionBrief? Direction,
    GeneratedContentCandidate? SelectedCandidate,
    int ReceivedCandidateCount,
    int DiscardedCandidateCount,
    string? Detail);

internal interface ICreativeDirectionSource
{
    Task<CreativeDirectionResult> CreateAsync(
        FormatLaboratoryScenario scenario,
        CancellationToken cancellationToken);
}

internal interface IFormatContentProducer
{
    CreativeFormatIdentity Format { get; }

    Task<ContentGenerationResult> GenerateAsync(
        ContentGenerationRequest request,
        CancellationToken cancellationToken);
}

internal interface IFormatContentValidator
{
    CreativeFormatIdentity Format { get; }

    CreativeComponentIdentity Identity { get; }

    Task<ContentValidationResult> ValidateAsync(
        ContentGenerationRequest request,
        GeneratedContentCandidate candidate,
        CancellationToken cancellationToken);
}

internal sealed record FormatLaboratoryLimits(
    int IdentifierTextElements,
    int DirectionTextElements,
    int ScriptTextElements,
    int DetailTextElements,
    int MaximumPermissions,
    int MaximumProhibitions,
    int MaximumAlternatives)
{
    public static FormatLaboratoryLimits Default { get; } = new(
        IdentifierTextElements: 128,
        DirectionTextElements: 600,
        ScriptTextElements: 2000,
        DetailTextElements: 500,
        MaximumPermissions: 16,
        MaximumProhibitions: 12,
        MaximumAlternatives: 3);

    public void Validate()
    {
        RequirePositive(IdentifierTextElements, nameof(IdentifierTextElements));
        RequirePositive(DirectionTextElements, nameof(DirectionTextElements));
        RequirePositive(ScriptTextElements, nameof(ScriptTextElements));
        RequirePositive(DetailTextElements, nameof(DetailTextElements));
        RequireRange(MaximumPermissions, 1, 32, nameof(MaximumPermissions));
        RequireRange(MaximumProhibitions, 1, 24, nameof(MaximumProhibitions));
        RequireRange(MaximumAlternatives, 1, 3, nameof(MaximumAlternatives));
    }

    private static void RequirePositive(int value, string parameterName)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void RequireRange(int value, int minimum, int maximum, string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal sealed class FormatLaboratory
{
    public const int CurrentDirectionSchemaVersion = 1;

    private readonly ICreativeDirectionSource _directionSource;
    private readonly IReadOnlyDictionary<CreativeFormatIdentity, IFormatContentProducer> _producers;
    private readonly IReadOnlyDictionary<
        (CreativeFormatIdentity Format, CreativeComponentIdentity Validator),
        IFormatContentValidator> _validators;
    private readonly FormatLaboratoryLimits _limits;

    public FormatLaboratory(
        ICreativeDirectionSource directionSource,
        IReadOnlyList<IFormatContentProducer> producers,
        IReadOnlyList<IFormatContentValidator> validators,
        FormatLaboratoryLimits limits)
    {
        ArgumentNullException.ThrowIfNull(directionSource);
        ArgumentNullException.ThrowIfNull(producers);
        ArgumentNullException.ThrowIfNull(validators);
        ArgumentNullException.ThrowIfNull(limits);
        limits.Validate();

        _directionSource = directionSource;
        _limits = limits;
        _producers = new ReadOnlyDictionary<CreativeFormatIdentity, IFormatContentProducer>(
            BuildProducerMap(producers));
        _validators = new ReadOnlyDictionary<
            (CreativeFormatIdentity, CreativeComponentIdentity),
            IFormatContentValidator>(BuildValidatorMap(validators));
    }

    public async Task<FormatLaboratoryRunRecord> RunAsync(
        FormatLaboratoryScenario scenario,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ValidateScenarioShape(scenario);

        if (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }

        CreativeDirectionResult directionResult;
        try
        {
            directionResult = await _directionSource.CreateAsync(scenario, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Cancelled();
        }
        catch (Exception exception)
        {
            return Failure(
                FormatLaboratoryOutcome.DirectionUnavailable,
                BoundDetail($"Direction source failed: {exception.GetType().Name}"));
        }

        if (directionResult.Status == CreativeDirectionStatus.Cancelled)
        {
            return Cancelled();
        }

        if (directionResult.Status != CreativeDirectionStatus.Created
            || directionResult.Direction is null)
        {
            return Failure(
                FormatLaboratoryOutcome.DirectionUnavailable,
                BoundDetail(directionResult.Detail ?? directionResult.Status.ToString()));
        }

        CreativeDirectionBrief direction = CopyDirection(directionResult.Direction);
        string? directionProblem = ValidateDirection(direction, scenario);
        if (directionProblem is not null)
        {
            return Failure(
                direction.Format == scenario.Format
                    ? FormatLaboratoryOutcome.DirectionRejected
                    : FormatLaboratoryOutcome.ContractMismatch,
                BoundDetail(directionProblem));
        }

        if (!_producers.TryGetValue(scenario.Format, out IFormatContentProducer? producer))
        {
            return Failure(
                FormatLaboratoryOutcome.ProducerUnavailable,
                "No producer implements the exact format contract.",
                direction);
        }

        var validatorKey = (scenario.Format, scenario.OutputContract.Validator);
        if (!_validators.TryGetValue(validatorKey, out IFormatContentValidator? validator))
        {
            return Failure(
                FormatLaboratoryOutcome.UnsupportedFormat,
                "No validator implements the exact format contract.",
                direction);
        }

        var request = new ContentGenerationRequest(
            direction,
            scenario.Context,
            CopyOutputContract(scenario.OutputContract));

        if (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }

        ContentGenerationResult generation;
        try
        {
            generation = await producer.GenerateAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Cancelled();
        }
        catch (Exception exception)
        {
            return Failure(
                FormatLaboratoryOutcome.GenerationFailed,
                BoundDetail($"Producer failed: {exception.GetType().Name}"),
                direction);
        }

        if (generation.Status == ContentGenerationStatus.Cancelled)
        {
            return Cancelled();
        }

        if (generation.Format != scenario.Format)
        {
            return Failure(
                FormatLaboratoryOutcome.ContractMismatch,
                "The producer result format did not match the requested contract.",
                direction,
                generation.Candidates?.Count ?? 0);
        }

        if (generation.Status != ContentGenerationStatus.Generated)
        {
            FormatLaboratoryOutcome outcome = generation.Status switch
            {
                ContentGenerationStatus.NoCandidate => FormatLaboratoryOutcome.NoCandidate,
                ContentGenerationStatus.Unavailable => FormatLaboratoryOutcome.ProducerUnavailable,
                _ => FormatLaboratoryOutcome.GenerationFailed,
            };
            return Failure(
                outcome,
                BoundDetail(generation.Detail ?? generation.Status.ToString()),
                direction,
                generation.Candidates?.Count ?? 0);
        }

        IReadOnlyList<GeneratedContentCandidate> rawCandidates =
            generation.Candidates ?? Array.Empty<GeneratedContentCandidate>();
        int received = rawCandidates.Count;
        int candidateCap = Math.Min(
            scenario.OutputContract.MaximumCandidates,
            _limits.MaximumAlternatives);
        int consideredCount = Math.Min(received, candidateCap);
        int discarded = Math.Max(0, received - consideredCount);

        for (int index = 0; index < consideredCount; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Cancelled(received);
            }

            GeneratedContentCandidate candidate = rawCandidates[index];
            string? candidateProblem = ValidateCandidate(candidate, request);
            if (candidateProblem is not null)
            {
                discarded++;
                continue;
            }

            ContentValidationResult validation;
            try
            {
                validation = await validator.ValidateAsync(request, candidate, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Cancelled(received);
            }
            catch (Exception exception)
            {
                return Failure(
                    FormatLaboratoryOutcome.ValidatorFailed,
                    BoundDetail($"Validator failed: {exception.GetType().Name}"),
                    direction,
                    received);
            }

            if (validation.Status == ContentValidationStatus.Cancelled)
            {
                return Cancelled(received);
            }

            if (validation.Status == ContentValidationStatus.Failed)
            {
                return Failure(
                    FormatLaboratoryOutcome.ValidatorFailed,
                    BoundDetail(validation.Detail ?? "Validator failed."),
                    direction,
                    received);
            }

            if (validation.Status != ContentValidationStatus.Valid)
            {
                discarded++;
                continue;
            }

            discarded += consideredCount - index - 1;
            GeneratedContentCandidate selected = CopyCandidate(candidate);
            return new FormatLaboratoryRunRecord(
                FormatLaboratoryOutcome.GeneratedAndValid,
                direction,
                selected,
                received,
                discarded,
                null);
        }

        return new FormatLaboratoryRunRecord(
            received == 0
                ? FormatLaboratoryOutcome.NoCandidate
                : FormatLaboratoryOutcome.ValidationRejected,
            direction,
            null,
            received,
            received,
            received == 0
                ? "The producer returned no candidates."
                : "No bounded candidate passed the format contract.");
    }

    private Dictionary<CreativeFormatIdentity, IFormatContentProducer> BuildProducerMap(
        IReadOnlyList<IFormatContentProducer> producers)
    {
        var result = new Dictionary<CreativeFormatIdentity, IFormatContentProducer>();
        foreach (IFormatContentProducer producer in producers)
        {
            ArgumentNullException.ThrowIfNull(producer);
            ValidateFormatIdentity(producer.Format);
            if (!result.TryAdd(producer.Format, producer))
            {
                throw new ArgumentException(
                    "Only one producer may implement an exact format contract.",
                    nameof(producers));
            }
        }

        return result;
    }

    private Dictionary<
        (CreativeFormatIdentity, CreativeComponentIdentity),
        IFormatContentValidator> BuildValidatorMap(
        IReadOnlyList<IFormatContentValidator> validators)
    {
        var result = new Dictionary<
            (CreativeFormatIdentity, CreativeComponentIdentity),
            IFormatContentValidator>();
        foreach (IFormatContentValidator validator in validators)
        {
            ArgumentNullException.ThrowIfNull(validator);
            ValidateFormatIdentity(validator.Format);
            ValidateComponentIdentity(validator.Identity);
            if (!result.TryAdd((validator.Format, validator.Identity), validator))
            {
                throw new ArgumentException(
                    "Only one validator may implement an exact format contract.",
                    nameof(validators));
            }
        }

        return result;
    }

    private void ValidateScenarioShape(FormatLaboratoryScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario.Format);
        ArgumentNullException.ThrowIfNull(scenario.Personality);
        ArgumentNullException.ThrowIfNull(scenario.Context);
        ArgumentNullException.ThrowIfNull(scenario.OutputContract);
        ValidateIdentifier(scenario.ScenarioId, nameof(scenario.ScenarioId));
        ValidateFormatIdentity(scenario.Format);
        ValidatePersonalityIdentity(scenario.Personality);
        ValidateOutputContract(scenario.OutputContract);

        if (scenario.OutputContract.Format != scenario.Format)
        {
            throw new ArgumentException(
                "The output contract must belong to the scenario format.",
                nameof(scenario));
        }
    }

    private string? ValidateDirection(
        CreativeDirectionBrief direction,
        FormatLaboratoryScenario scenario)
    {
        if (direction.SchemaVersion != CurrentDirectionSchemaVersion)
        {
            return "The direction schema version is unsupported.";
        }

        if (direction.Format != scenario.Format)
        {
            return "The direction format did not match the scenario.";
        }

        if (direction.Personality != scenario.Personality)
        {
            return "The stable personality identity did not match the scenario.";
        }

        if (!IsIdentifierValid(direction.DirectionId)
            || !IsIdentifierValid(direction.SpeakerRole))
        {
            return "A direction identifier exceeded its bound or was empty.";
        }

        if (!IsDirectionTextValid(direction.OnAirSituation)
            || !IsDirectionTextValid(direction.EditorialObjective)
            || !IsDirectionTextValid(direction.Structure))
        {
            return "A direction field exceeded its bound or was empty.";
        }

        if (direction.ContextPermissions is null
            || direction.ContextPermissions.Count > _limits.MaximumPermissions)
        {
            return "Context permissions exceeded the laboratory bound.";
        }

        if (direction.ContextPermissions.Distinct().Count()
            != direction.ContextPermissions.Count)
        {
            return "Context permissions must be unique.";
        }

        foreach (CreativeContextPermission permission in direction.ContextPermissions)
        {
            if (permission.Scope != ExpectedScope(permission.Fact))
            {
                return "A context permission used the wrong factual scope.";
            }
        }

        if (direction.Prohibitions is null
            || direction.Prohibitions.Count > _limits.MaximumProhibitions
            || direction.Prohibitions.Any(text => !IsDirectionTextValid(text)))
        {
            return "Direction prohibitions exceeded their bounds.";
        }

        if (!OptionalIntentIsValid(direction.OptionalIntent))
        {
            return "Optional direction intent exceeded its bounds.";
        }

        return null;
    }

    private string? ValidateCandidate(
        GeneratedContentCandidate candidate,
        ContentGenerationRequest request)
    {
        if (candidate is null)
        {
            return "The producer returned a null candidate.";
        }

        if (candidate.Format != request.OutputContract.Format)
        {
            return "The candidate format did not match the output contract.";
        }

        if (!IsIdentifierValid(candidate.CandidateId)
            || string.IsNullOrWhiteSpace(candidate.Script)
            || !IsWithinTextElements(candidate.Script, _limits.ScriptTextElements))
        {
            return "The candidate identifier or script exceeded its bounds.";
        }

        int wordCount = CountWords(candidate.Script);
        if (wordCount < request.OutputContract.MinimumWords
            || wordCount > request.OutputContract.MaximumWords
            || CountParagraphs(candidate.Script) > request.OutputContract.MaximumParagraphs)
        {
            return "The candidate did not satisfy the mechanical output contract.";
        }

        if (candidate.DeclaredFactsUsed is null
            || candidate.DeclaredFactsUsed.Count > _limits.MaximumPermissions
            || candidate.DeclaredFactsUsed.Distinct().Count()
                != candidate.DeclaredFactsUsed.Count)
        {
            return "Declared fact use exceeded its bound or contained duplicates.";
        }

        IReadOnlySet<CreativeContextFactId> permitted = request.Direction.ContextPermissions
            .Select(permission => permission.Fact)
            .ToHashSet();

        foreach (CreativeContextFactId fact in candidate.DeclaredFactsUsed)
        {
            if (!permitted.Contains(fact) || !IsFactUsable(request.Context, fact))
            {
                return "The candidate declared use of an unavailable or unpermitted fact.";
            }
        }

        return null;
    }

    private void ValidateOutputContract(ContentOutputContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ValidateFormatIdentity(contract.Format);
        ValidateComponentIdentity(contract.Validator);

        if (contract.MinimumWords < 1
            || contract.MaximumWords < contract.MinimumWords
            || contract.MaximumWords > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contract),
                "Word bounds are invalid.");
        }

        if (contract.MaximumParagraphs is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contract),
                "Paragraph bounds are invalid.");
        }

        if (contract.MaximumCandidates < 1
            || contract.MaximumCandidates > _limits.MaximumAlternatives)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contract),
                "Candidate bounds exceed the laboratory policy.");
        }
    }

    private void ValidateFormatIdentity(CreativeFormatIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ValidateIdentifier(identity.Id, nameof(identity.Id));
        if (identity.Version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(identity.Version));
        }
    }

    private void ValidateComponentIdentity(CreativeComponentIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ValidateIdentifier(identity.Id, nameof(identity.Id));
        if (identity.Version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(identity.Version));
        }
    }

    private void ValidatePersonalityIdentity(CreativePersonalityIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ValidateIdentifier(identity.Id, nameof(identity.Id));
        if (identity.Version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(identity.Version));
        }
    }

    private void ValidateIdentifier(string value, string parameterName)
    {
        if (!IsIdentifierValid(value))
        {
            throw new ArgumentException(
                "A required identifier was empty or exceeded its bound.",
                parameterName);
        }
    }

    private bool IsIdentifierValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && IsWithinTextElements(value, _limits.IdentifierTextElements);

    private bool IsDirectionTextValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && IsWithinTextElements(value, _limits.DirectionTextElements);

    private bool OptionalIntentIsValid(CreativeOptionalIntent? intent)
    {
        if (intent is null)
        {
            return false;
        }

        return IsOptionalDirectionTextValid(intent.CastIntent)
            && IsOptionalDirectionTextValid(intent.PerformanceIntent)
            && IsOptionalDirectionTextValid(intent.LoreIntent);
    }

    private bool IsOptionalDirectionTextValid(string? value) =>
        value is null || IsDirectionTextValid(value);

    private static CreativeFactScope ExpectedScope(CreativeContextFactId fact) =>
        fact switch
        {
            CreativeContextFactId.CurrentTrackTitle => CreativeFactScope.Track,
            CreativeContextFactId.CurrentTrackArtist => CreativeFactScope.Artist,
            CreativeContextFactId.OpportunityClass => CreativeFactScope.Opportunity,
            CreativeContextFactId.OpportunityDecisionReason => CreativeFactScope.Opportunity,
            CreativeContextFactId.TotalObservedTransitions => CreativeFactScope.Session,
            CreativeContextFactId.RecentTracks => CreativeFactScope.Session,
            CreativeContextFactId.RecentAcceptedComments => CreativeFactScope.CommentHistory,
            CreativeContextFactId.LastCompletedHostSpeechTransition =>
                CreativeFactScope.OperationalPolicy,
            _ => throw new ArgumentOutOfRangeException(nameof(fact)),
        };

    private static bool IsFactUsable(
        CreativeContextPacket context,
        CreativeContextFactId fact) =>
        fact switch
        {
            CreativeContextFactId.CurrentTrackTitle =>
                context.CurrentTrack.Title.State == CreativeContextFactState.Known,
            CreativeContextFactId.CurrentTrackArtist =>
                context.CurrentTrack.Artist.State == CreativeContextFactState.Known,
            CreativeContextFactId.OpportunityClass =>
                context.Opportunity.OpportunityClass.State == CreativeContextFactState.Known,
            CreativeContextFactId.OpportunityDecisionReason =>
                context.Opportunity.DecisionReason.State == CreativeContextFactState.Known,
            CreativeContextFactId.TotalObservedTransitions =>
                context.Session.TotalObservedTransitions is not null,
            CreativeContextFactId.RecentTracks =>
                context.RecentTracks.State == CreativeContextCollectionState.KnownComplete
                && context.RecentTracks.Items.Count > 0,
            CreativeContextFactId.RecentAcceptedComments =>
                context.RecentAcceptedComments.State
                    == CreativeContextCollectionState.KnownComplete
                && context.RecentAcceptedComments.Items.Count > 0,
            CreativeContextFactId.LastCompletedHostSpeechTransition =>
                context.RecentUse.LastCompletedHostSpeechTransition is not null,
            _ => false,
        };

    private CreativeDirectionBrief CopyDirection(CreativeDirectionBrief direction) =>
        direction with
        {
            Format = direction.Format with { },
            Personality = direction.Personality with { },
            ContextPermissions = new ReadOnlyCollection<CreativeContextPermission>(
                direction.ContextPermissions?.Select(permission => permission with { }).ToArray()
                ?? []),
            Prohibitions = new ReadOnlyCollection<string>(
                direction.Prohibitions?.ToArray() ?? []),
            OptionalIntent = direction.OptionalIntent is null
                ? null!
                : direction.OptionalIntent with { },
        };

    private static ContentOutputContract CopyOutputContract(ContentOutputContract contract) =>
        contract with
        {
            Format = contract.Format with { },
            Validator = contract.Validator with { },
        };

    private static GeneratedContentCandidate CopyCandidate(
        GeneratedContentCandidate candidate) =>
        candidate with
        {
            Format = candidate.Format with { },
            DeclaredFactsUsed = new ReadOnlyCollection<CreativeContextFactId>(
                candidate.DeclaredFactsUsed.ToArray()),
        };

    private static int CountWords(string value) =>
        value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static int CountParagraphs(string value) =>
        value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Count(paragraph => !string.IsNullOrWhiteSpace(paragraph));

    private static bool IsWithinTextElements(string value, int maximum) =>
        StringInfo.ParseCombiningCharacters(value).Length <= maximum;

    private string BoundDetail(string detail)
    {
        int[] offsets = StringInfo.ParseCombiningCharacters(detail);
        if (offsets.Length <= _limits.DetailTextElements)
        {
            return detail;
        }

        return detail[..offsets[_limits.DetailTextElements]];
    }

    private static FormatLaboratoryRunRecord Cancelled(int received = 0) =>
        new(
            FormatLaboratoryOutcome.Cancelled,
            null,
            null,
            received,
            received,
            "The laboratory run was cancelled.");

    private static FormatLaboratoryRunRecord Failure(
        FormatLaboratoryOutcome outcome,
        string detail,
        CreativeDirectionBrief? direction = null,
        int received = 0) =>
        new(
            outcome,
            direction,
            null,
            received,
            received,
            detail);
}
