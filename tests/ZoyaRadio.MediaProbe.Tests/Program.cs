using System.Net;
using System.Text.Json;
using Windows.Media.Control;
using ZoyaRadio.MediaProbe;

var tests = new (string Name, Func<Task> Run)[]
{
    ("zero Firefox matches", () => RunSynchronous(ZeroFirefoxMatches)),
    ("case-insensitive exact Firefox match", () => RunSynchronous(CaseInsensitiveExactMatch)),
    ("partial Firefox name is not a match", () => RunSynchronous(PartialNameDoesNotMatch)),
    ("multiple Firefox matches remain ambiguous", () => RunSynchronous(MultipleFirefoxMatches)),
    ("audio duck selector refuses when Firefox is absent", () => RunSynchronous(AudioDuckNoFirefoxCandidate)),
    ("audio duck selector chooses one exact active Firefox session", () => RunSynchronous(AudioDuckSelectsOneFirefoxCandidate)),
    ("audio duck selector ignores unrelated applications", () => RunSynchronous(AudioDuckIgnoresUnrelatedApplications)),
    ("audio duck selector refuses ambiguous active Firefox sessions", () => RunSynchronous(AudioDuckRefusesAmbiguousFirefoxCandidates)),
    ("audio duck target is relative to captured volume", () => RunSynchronous(AudioDuckLevelIsRelative)),
    ("normal audio duck restores exact captured state", AudioDuckNormalRestoresExactState),
    ("audio duck pre-cancellation issues no volume command", AudioDuckPreCancellationIssuesNoCommand),
    ("audio duck cancellation restores exact captured state", AudioDuckCancellationRestoresExactState),
    ("simulated audio duck failure restores exact captured state", AudioDuckSimulatedFailureRestoresExactState),
    ("muted and near-zero sessions receive no volume command", AudioDuckRefusesUnsuitableStartingState),
    ("audio duck restoration never redirects to a replacement session", AudioDuckDoesNotRedirectRestoration),
    ("confirmation defaults to no", () => RunSynchronous(ConfirmationDefaultsToNo)),
    ("only explicit y or yes confirms", () => RunSynchronous(ExplicitConfirmationOnly)),
    ("control starts only from Playing", () => RunSynchronous(StartingStateMustBePlaying)),
    ("announcement requires title and artist", () => RunSynchronous(AnnouncementRequiresMetadata)),
    ("announcement preserves Unicode text", () => RunSynchronous(AnnouncementPreservesUnicode)),
    ("metadata recheck compares title and artist", () => RunSynchronous(MetadataRecheckUsesTitleAndArtist)),
    ("normal announcement sequence restores playback", NormalSequenceRestoresPlayback),
    ("speech failure uses protected playback recovery", SpeechFailureRestoresPlayback),
    ("speech timeout uses protected playback recovery", SpeechTimeoutRestoresPlayback),
    ("simulated speech failure uses real recovery path", SimulatedFailureRestoresPlayback),
    ("speech cancellation uses protected playback recovery", SpeechCancellationRestoresPlayback),
    ("Nicole is the selected CPU neural voice", () => RunSynchronous(NeuralSpeechDefaultsSelectNicole)),
    ("neural provider is selected normally", NeuralProviderIsSelectedNormally),
    ("explicit Windows-TTS mode bypasses neural provider", ExplicitWindowsModeBypassesNeural),
    ("neural initialization failure selects Windows fallback", NeuralInitializationFailureUsesWindows),
    ("missing neural model is safely unavailable", () => RunSynchronous(MissingNeuralModelIsSafe)),
    ("neural synthesis failure selects Windows fallback", NeuralSynthesisFailureUsesWindows),
    ("neural cancellation does not start fallback work", NeuralCancellationDoesNotFallback),
    ("Unicode text survives neural provider selection", UnicodeSurvivesSpeechProviderSelection),
    ("neural PCM encoder produces a valid in-memory WAV", () => RunSynchronous(NeuralWaveEncodingIsValid)),
    ("neural playback failure triggers protected recovery", SpeechFailureRestoresPlayback),
    ("neural preparation occurs before the pause path", NeuralPreparationPrecedesPausePath),
    ("stale track during neural preparation prevents control", StaleTrackDuringNeuralPreparationBlocksControl),
    ("prepared neural audio is disposed after pipeline use", PreparedNeuralAudioIsDisposed),
    ("Ollama endpoint permits loopback only", () => RunSynchronous(OllamaEndpointIsLoopbackOnly)),
    ("host persona defaults to Zoya and Steve", () => RunSynchronous(HostPersonaUsesExpectedDefaults)),
    ("host and listener names render from configuration", () => RunSynchronous(HostPersonaRendersConfiguredNames)),
    ("comment prompt sends only title and artist metadata", () => RunSynchronous(CommentPromptUsesAllowedMetadataOnly)),
    ("comment prompt covers manual grounding failures", () => RunSynchronous(CommentPromptCoversManualGroundingFailures)),
    ("persona prompt distinguishes safe imagination from unsupported facts", () => RunSynchronous(PersonaPromptDocumentsCreativeBoundary)),
    ("persona configuration reaches comment generation", PersonaConfigurationReachesGenerator),
    ("Ollama request is local, non-streaming, non-thinking, and unload-on-return", OllamaRequestUsesSafeOptions),
    ("comment validator normalizes harmless whitespace", () => RunSynchronous(CommentValidatorNormalizesWhitespace)),
    ("comment validator rejects empty output", () => RunSynchronous(CommentValidatorRejectsEmpty)),
    ("comment validator rejects short output", () => RunSynchronous(CommentValidatorRejectsShort)),
    ("comment validator rejects long output", () => RunSynchronous(CommentValidatorRejectsLong)),
    ("comment validator rejects multiple paragraphs", () => RunSynchronous(CommentValidatorRejectsParagraphs)),
    ("comment validator rejects commercial radio cliches", () => RunSynchronous(CommentValidatorRejectsCliches)),
    ("comment validator rejects Milestone 4 radio stock phrases", () => RunSynchronous(CommentValidatorRejectsPersonaCliches)),
    ("comment validator rejects model commentary", () => RunSynchronous(CommentValidatorRejectsModelCommentary)),
    ("exact FALLBACK selects fixed template", ExactFallbackSelectsFixedTemplate),
    ("FALLBACK sentinel is never final spoken text", FallbackSentinelIsNeverFinalText),
    ("safe FALLBACK is a successful generate-only result", SafeFallbackIsSuccessfulGenerateOnlyResult),
    ("HTTP failure selects fixed fallback", HttpFailureUsesFallback),
    ("timeout selects fixed fallback", TimeoutUsesFallback),
    ("malformed response selects fixed fallback", MalformedResponseUsesFallback),
    ("unexpected generator exception still unloads and falls back", GeneratorExceptionStillUnloads),
    ("valid AI comment still unloads selected model", ValidAiCommentStillUnloads),
    ("no-AI mode bypasses Ollama", NoAiBypassesOllama),
    ("simulated AI failure bypasses Ollama", SimulatedAiFailureBypassesOllama),
    ("stale track blocks prepared comment", () => RunSynchronous(StaleTrackBlocksPreparedComment)),
    ("radio settings validate safe defaults and bounds", () => RunSynchronous(RadioConfigurationValidatesBounds)),
    ("radio schedule requests inclusive 2-5 bounds", () => RunSynchronous(RadioScheduleUsesInclusiveBounds)),
    ("one genuine track transition increments exactly once", () => RunSynchronous(RadioTransitionIncrementsOnce)),
    ("duplicate media events do not increment radio schedule", () => RunSynchronous(RadioDuplicateDoesNotIncrement)),
    ("completed announcement resets counter with fresh interval", () => RunSynchronous(RadioCompletionChoosesFreshInterval)),
    ("Silent Mode prevents generation pause and speech", RadioSilentModePreventsPipelineWork),
    ("leaving Silent Mode starts a fresh interval", () => RunSynchronous(RadioLeavingSilentChoosesFreshInterval)),
    ("minimum track age delays automatic pause path", RadioMinimumAgeDelaysAnnouncement),
    ("track change during generation abandons pending comment", RadioTrackChangeAbandonsPipeline),
    ("track change during age wait abandons pending comment", RadioTrackChangeDuringWaitAbandonsPipeline),
    ("pending track change creates no catch-up queue", () => RunSynchronous(RadioPendingChangeCreatesNoQueue)),
    ("Paused playback prevents automatic generation and control", RadioPausedPlaybackBlocksPipeline),
    ("wrong missing and ambiguous Firefox targets block control", () => RunSynchronous(RadioTargetSelectionMustBeExact)),
    ("recent track history remains bounded", () => RunSynchronous(RadioTrackHistoryIsBounded)),
    ("recent AI comment history remains bounded", () => RunSynchronous(RadioCommentHistoryIsBounded)),
    ("clear history resets state without playback or speech", () => RunSynchronous(RadioClearHistoryIsHarmless)),
    ("repeated artist uses normalized exact matching", () => RunSynchronous(RadioRepeatedArtistUsesExactNormalization)),
    ("non-identical artist strings are not fuzzily merged", () => RunSynchronous(RadioArtistMatchingIsNotFuzzy)),
    ("radio prompt includes compact recent comments and trusted context", () => RunSynchronous(RadioPromptIncludesBoundedContext)),
    ("radio prompt preserves Unicode metadata", () => RunSynchronous(RadioPromptPreservesUnicode)),
    ("exact duplicate AI comment selects fixed fallback", RadioDuplicateAiCommentUsesFallback),
    ("radio no-AI mode bypasses Ollama", RadioNoAiBypassesOllama),
    ("radio Ollama timeout unloads and selects fallback", RadioTimeoutUsesFallback),
    ("radio maximum speech duration uses protected recovery", RadioSpeechLimitUsesProtectedRecovery),
    ("radio pipeline passes configured speech limit", RadioPipelinePassesSpeechLimit),
    ("transition schedule prepares Track A one transition ahead", () => RunSynchronous(TransitionSchedulePreparesOneAhead)),
    ("unrelated GSMTC collection changes preserve selected Firefox work", () => RunSynchronous(ChromeCollectionChangesPreserveSelectedFirefoxWork)),
    ("refreshed wrapper preserves same logical Firefox session", () => RunSynchronous(RefreshedFirefoxWrapperPreservesLogicalContinuity)),
    ("Chrome-driven wrapper churn preserves pending schedule", () => RunSynchronous(ChromeWrapperChurnPreservesPendingSchedule)),
    ("invalid selected Firefox requires protected reacquisition", () => RunSynchronous(InvalidFirefoxSelectionRequiresReacquisition)),
    ("different logical Firefox session requires protected reacquisition", () => RunSynchronous(DifferentFirefoxSessionRequiresReacquisition)),
    ("unsafe Firefox media-session candidates are refused", () => RunSynchronous(FirefoxMediaRebindRefusesUnsafeCandidates)),
    ("pending material is discarded before Firefox rebinding", FirefoxRebindWaitsForPendingDiscard),
    ("Firefox rebinding starts a fresh transition interval", () => RunSynchronous(SessionRebindResetsPendingOpportunity)),
    ("unfinished preparation misses its transition without catch-up", () => RunSynchronous(UnfinishedTransitionPreparationIsAbandoned)),
    ("transition prompt contains Track A and no speculative Track B", () => RunSynchronous(TransitionPromptContainsTrackAOnly)),
    ("AI and Nicole preparation finish before transition delivery", TransitionPreparationPrecedesDelivery),
    ("prepared Track-A speech is delivered only after A-to-B transition", PreparedSpeechWaitsForTransition),
    ("normal transition delivery uses ducking without playback controls", TransitionDeliveryUsesDuckingOnly),
    ("delivery captures changed Firefox volume at transition time", TransitionDeliveryCapturesCurrentVolume),
    ("successful transition speech restores exact volume", TransitionSpeechSuccessRestoresVolume),
    ("failed transition speech restores exact volume", TransitionSpeechFailureRestoresVolume),
    ("cancelled transition speech restores exact volume", TransitionSpeechCancellationRestoresVolume),
    ("simulated post-duck transition failure restores exact volume", TransitionSimulatedFailureRestoresVolume),
    ("ambiguous transition media target causes silence", TransitionAmbiguousTargetCausesSilence),
    ("ambiguous Core Audio target causes no automatic duck or speech", TransitionAmbiguousAudioTargetCausesSilence),
    ("automatic ducking selects Firefox and leaves Chrome unchanged", TransitionDuckingLeavesChromeUnchanged),
    ("transient inactive Firefox Core Audio target is retried safely", TransitionAudioTargetRetriesTransientInactiveFirefox),
    ("inactive Firefox Core Audio retry is bounded by the delivery window", TransitionAudioTargetRetryIsBounded),
    ("rapid A-B-C transition invalidates stale prepared material", () => RunSynchronous(TransitionRapidSkipInvalidatesPrepared)),
    ("late transition delivery is abandoned", TransitionLateDeliveryIsAbandoned),
    ("changed Track B abandons stale prepared speech", TransitionChangedTrackBIsAbandoned),
    ("Silent Mode invalidates pending transition preparation", () => RunSynchronous(TransitionSilentModeInvalidatesPreparation)),
    ("transition no-AI mode bypasses Ollama", TransitionNoAiBypassesOllama),
    ("transition AI generation still unloads Ollama", TransitionAiStillUnloadsOllama),
    ("transition preparation preserves Unicode metadata", TransitionPreparationPreservesUnicode),
    ("only one transition opportunity remains pending", () => RunSynchronous(OnlyOneTransitionOpportunityRemainsPending)),
    ("prepared transition audio is disposable after use", TransitionPreparedAudioIsDisposed),
    ("prepared speech at or below the automatic maximum remains deliverable", TransitionPreparedDurationAtOrBelowLimitIsDeliverable),
    ("over-duration prepared speech is discarded before ducking", TransitionOverDurationPreparedSpeechIsDiscardedBeforeDucking),
    ("over-duration prepared speech does not block later opportunities", TransitionOverDurationPreparedSpeechDoesNotBlockLaterOpportunity),
    ("M10A threshold-two golden trace matches legacy", () => RunSynchronous(Milestone10AKernelTests.ThresholdTwoGoldenTraceMatchesLegacy)),
    ("M10A threshold-five trace prepares only one ahead", () => RunSynchronous(Milestone10AKernelTests.ThresholdFivePreparesOnlyOneAhead)),
    ("M10A duplicate and unusable observations do not advance", () => RunSynchronous(Milestone10AKernelTests.DuplicateAndUnusableObservationsDoNotAdvance)),
    ("M10A missed preparation starts a fresh schedule", () => RunSynchronous(Milestone10AKernelTests.MissedPreparationStartsFreshSchedule)),
    ("M10A rapid transition invalidates unresolved delivery", () => RunSynchronous(Milestone10AKernelTests.RapidTransitionInvalidatesUnresolvedDelivery)),
    ("M10A stale ready and resolve IDs are refused", () => RunSynchronous(Milestone10AKernelTests.StaleReadyAndResolveAreRefusedWithoutReschedule)),
    ("M10A failed resolution is typed and non-canonical", () => RunSynchronous(Milestone10AKernelTests.FailedResolutionIsTypedAndDoesNotCommitComment)),
    ("M10A Silent Mode invalidates and resumes fresh", () => RunSynchronous(Milestone10AKernelTests.SilentModeInvalidatesAndResumesFresh)),
    ("M10A Clear History matches legacy", () => RunSynchronous(Milestone10AKernelTests.ClearHistoryMatchesLegacyWithAndWithoutPending)),
    ("M10A rebind matches legacy", () => RunSynchronous(Milestone10AKernelTests.RebindMatchesLegacyWithAndWithoutPending)),
    ("M10A explicit cancellation and no-op match legacy", () => RunSynchronous(Milestone10AKernelTests.ExplicitCancellationMatchesLegacyAndNoOpIsStable)),
    ("M10A disarm and re-arm preserve opportunity identity", () => RunSynchronous(Milestone10AKernelTests.DisarmAndRearmPreserveMonotonicOpportunityId)),
    ("M10A bounded history and comments match legacy", () => RunSynchronous(Milestone10AKernelTests.BoundedHistoryAndCommentsMatchLegacy)),
    ("M10A inclusive target bounds are deterministic", () => RunSynchronous(Milestone10AKernelTests.InclusiveTargetsAndOutOfRangeValuesAreDeterministic)),
    ("M10B preparing through completed disposal", () => RunSynchronous(Milestone10BLifecycleTests.PreparingAttachReserveCompleteDisposesOnce)),
    ("M10B prepared abandon and failed preparation ownership", () => RunSynchronous(Milestone10BLifecycleTests.PreparedAbandonAndFailedPreparationOwnCorrectly)),
    ("M10B late and duplicate payload ownership", () => RunSynchronous(Milestone10BLifecycleTests.LateAndDuplicatePayloadsRemainCallerOwned)),
    ("M10B registration and expiry boundary", () => RunSynchronous(Milestone10BLifecycleTests.ExpiredRegistrationAndBoundaryAreDeterministic)),
    ("M10B reservation conflict and stale reservation", () => RunSynchronous(Milestone10BLifecycleTests.ReservationConflictsAndWrongReservationAreStable)),
    ("M10B reusable release and completed return", () => RunSynchronous(Milestone10BLifecycleTests.ReusableReleaseAndCompletionReturnToInventory)),
    ("M10B reusable retire and single-use refusal", () => RunSynchronous(Milestone10BLifecycleTests.ReusableRetireDisposesAndSingleCannotReturn)),
    ("M10B exact opportunity track and session binding", () => RunSynchronous(Milestone10BLifecycleTests.BindingRefusalUsesExactOpportunityTrackAndSession)),
    ("M10B explicit binding invalidation", () => RunSynchronous(Milestone10BLifecycleTests.ExplicitBindingInvalidationIsTypedAndExact)),
    ("M10B ready and reserved expiry disposal", () => RunSynchronous(Milestone10BLifecycleTests.ReadyAndReservedExpiryDisposeExactlyOnce)),
    ("M10B state collections are read-only", () => RunSynchronous(Milestone10BLifecycleTests.StateCollectionsAreReadOnly)),
    ("M10B M10A happy-path composition", () => RunSynchronous(Milestone10BLifecycleTests.M10AHappyPathCompositionCompletesAndDisposes)),
    ("M10B M10A missed and rapid invalidation composition", () => RunSynchronous(Milestone10BLifecycleTests.M10AMissedAndRapidTransitionsDisposeCorrectly)),
    ("M10B M10A control invalidation composition", () => RunSynchronous(Milestone10BLifecycleTests.M10AControlInvalidationsDisposeBoundMaterial)),
    ("M10B M10A resolution and stale identity isolation", () => RunSynchronous(Milestone10BLifecycleTests.M10AResolutionAndStaleIdentityRemainIsolated)),
    ("M10B malformed identity and policy inputs", () => RunSynchronous(Milestone10BLifecycleTests.MalformedIdentityAndPolicyInputsAreRejected)),
    ("M10C adapter reproduces M8 happy path and reserves material", () => RunSynchronous(Milestone10CMigrationTests.AdapterReproducesM8HappyPathAndReservesBeforeDelivery)),
    ("M10C adapter discards missed and rapid material safely", () => RunSynchronous(Milestone10CMigrationTests.AdapterMissedAndRapidTransitionsDiscardMaterialAndKeepM8Schedule)),
    ("M10C adapter retires owned prepared speech once", () => RunSynchronous(Milestone10CMigrationTests.AdapterRetiresOwnedPreparedResourceExactlyOnce)),
    ("M10C adapter controls and rebind preserve M8 parity", () => RunSynchronous(Milestone10CMigrationTests.AdapterControlsAndRebindPreserveLegacyParity)),
    ("M11A preparation eligibility and direct music are typed", () => RunSynchronous(Milestone11AEditorialArbiterTests.PreparationEligibilityAndDirectMusicAreTyped)),
    ("M11A airtime hard eligibility refusals are typed", () => RunSynchronous(Milestone11AEditorialArbiterTests.AirtimeHardEligibilityRefusalsAreTyped)),
    ("M11A music pressure spacing and cooldown can select music", () => RunSynchronous(Milestone11AEditorialArbiterTests.MusicPressureSpacingAndCooldownCanSelectMusic)),
    ("M11A priority and bounded tie break are deterministic", () => RunSynchronous(Milestone11AEditorialArbiterTests.PriorityAndBoundedTieBreakAreDeterministic)),
    ("M11A M10C-equivalent profile routes existing comment", () => RunSynchronous(Milestone11AEditorialArbiterTests.M10CEquivalentProfileRoutesExistingComment)),
    ("M11A adapter policy suppression does not reserve material", () => RunSynchronous(Milestone11AEditorialArbiterTests.AdapterPolicySuppressionDoesNotReserveMaterial)),    ("M11B comment preparation outranks reusable liner", () => RunSynchronous(Milestone11BReusableLinerTests.CommentPreparationOutranksLiner)),
    ("M11B comment airtime outranks eligible reusable liner", () => RunSynchronous(Milestone11BReusableLinerTests.CommentAirtimeOutranksEligibleLiner)),
    ("M11B missed comment boundary does not fall back to liner", () => RunSynchronous(Milestone11BReusableLinerTests.MissedCommentBoundaryDoesNotFallBackToLiner)),
    ("M11B comment preparation boundary blocks liner", () => RunSynchronous(Milestone11BReusableLinerTests.PreparationBoundaryBlocksLiner)),
    ("M11B liner cooldown requires transition and elapsed gates", () => RunSynchronous(Milestone11BReusableLinerTests.CooldownRequiresBothTransitionAndTimeGates)),
    ("M11B liner host-speech spacing boundary is inclusive", () => RunSynchronous(Milestone11BReusableLinerTests.SpeechSpacingBoundaryIsInclusive)),
    ("M11B silent mode and unavailable treatment select music", () => RunSynchronous(Milestone11BReusableLinerTests.SilentAndUnavailableTreatmentSelectMusic)),
    ("M11B missed liner reservation returns to Ready", () => RunSynchronous(Milestone11BReusableLinerTests.ReleaseReturnsReusableMaterialToReady)),
    ("M11B completed liner returns and resets cooldown", () => RunSynchronous(Milestone11BReusableLinerTests.CompletionReturnsToInventoryAndResetsCooldown)),
    ("M11B prepared liner payload supports serial replay", () => RunSynchronous(Milestone11BReusableLinerTests.SerialReplayUsesOnePreparedPayload)),
    ("M11B session invalidation disposes liner exactly once", () => RunSynchronous(Milestone11BReusableLinerTests.SessionInvalidationDisposesExactlyOnce)),
    ("M11B over-duration liner is rejected and disposed", () => RunSynchronous(Milestone11BReusableLinerTests.OverDurationSpeechIsRejectedAndDisposed)),
    ("M11B cancellation before coordinator acceptance disposes coordinator-owned speech", CancellationBeforeCoordinatorAcceptanceDisposesExactlyOnce),
    ("M11B cancellation during ready liner acceptance disposes coordinator-owned speech", CancellationDuringReadyLinerAcceptanceDisposesExactlyOnce),
    ("M11B rejected ready liner preparation disposes speech exactly once", () => RunSynchronous(RejectedReadyLinerPreparationDisposesExactlyOnce)),
    ("M11B rejection preserves existing prepared liner payload", () => RunSynchronous(RejectedReadyLinerPreservesExistingPayload)),
    ("M11B duration validation exception leaves inventory unchanged", () => RunSynchronous(DurationValidationExceptionLeavesInventoryUnchanged)),
    ("M11B compatibility rejection disposal runs once when disposal throws", () => RunSynchronous(CompatibilityRejectionDisposesOnceWhenDisposeThrows)),
    ("M11B compatibility validation exception does not dispose", () => RunSynchronous(CompatibilityValidationExceptionDoesNotDispose)),
    ("M11B accepted ready liner preparation transfers disposal ownership", AcceptedReadyLinerPreparationTransfersDisposalOwnership),
    ("M11B non-ready liner preparation retains coordinator cleanup", () => RunSynchronous(NonReadyLinerPreparationRetainsCoordinatorCleanup)),
    ("M11B preparation uses only approved fixed copy", Milestone11BReusableLinerTests.FixedPreparationUsesOnlyApprovedCopy),
    ("M11B delivery uses shared duck host and prepared speech", Milestone11BReusableLinerTests.DeliveryUsesSharedDuckHostAndPreparedSpeech),
    ("M11B delivery safety requires exact current Playing target", () => RunSynchronous(Milestone11BReusableLinerTests.DeliverySafetyRequiresExactCurrentPlayingTarget)),
    ("M11B concurrent reuse is refused until release", () => RunSynchronous(Milestone11BReusableLinerTests.ConcurrentReuseIsRefusedUntilRelease)),
    ("M11B pre-cancelled preparation creates no payload", Milestone11BReusableLinerTests.PreCancelledPreparationCreatesNoPayload),
    ("M12A identical input is deterministic and copied", () => RunSynchronous(Milestone12ACreativeContextTests.IdenticalInputProducesIdenticalPacketAndCopiesCollections)),
    ("M12A accepted history caps retain newest and report omissions", () => RunSynchronous(Milestone12ACreativeContextTests.AcceptedCapsRetainNewestItemsAndReportOmissions)),
    ("M12A unknown empty and omitted collection states differ", () => RunSynchronous(Milestone12ACreativeContextTests.UnknownKnownEmptyAndOmittedStatesRemainDistinct)),
    ("M12A Unicode bounds preserve text elements and hostile data stays inert", () => RunSynchronous(Milestone12ACreativeContextTests.UnicodeBoundsPreserveTextElementsAndHostileTextIsInert)),
    ("M12A track and artist facts remain separate", () => RunSynchronous(Milestone12ACreativeContextTests.TrackAndArtistFactsCannotSubstituteForEachOther)),
    ("M12A provenance and explicit session facts are stable", () => RunSynchronous(Milestone12ACreativeContextTests.ProvenanceAndExplicitSessionFactsAreStable)),
    ("M12A cleared input cannot recover prior context", () => RunSynchronous(Milestone12ACreativeContextTests.ClearOrResetInputCannotRecoverEarlierContext)),
    ("M12A limit policy refuses expanded history", () => RunSynchronous(Milestone12ACreativeContextTests.LimitPolicyRejectsUnboundedOrExpandedHistory)),
    ("M12A packet surface contains facts only", () => RunSynchronous(Milestone12ACreativeContextTests.PacketSurfaceContainsFactsNotCreativeOrProviderPayloads)),
    ("M12A assembler is runtime unreachable and side-effect free", () => RunSynchronous(Milestone12ACreativeContextTests.AssemblerHasNoRuntimeReachabilityOrSideEffectApis)),
    ("M12A representative inspection fixtures are readable and bounded", () => RunSynchronous(Milestone12ACreativeContextTests.RepresentativeInspectionFixturesAreReadableAndBounded)),
    ("M12B direction and script remain separate types", () => RunSynchronous(Milestone12BCreativeBoundaryTests.DirectionAndScriptAreSeparateTypes)),
    ("M12B one object may fill both conceptual roles", Milestone12BCreativeBoundaryTests.OneObjectCanFillBothConceptualRoles),
    ("M12B exact format and version dispatch are required", Milestone12BCreativeBoundaryTests.ExactFormatAndVersionDispatchAreRequired),
    ("M12B contract and personality mismatches are refused", Milestone12BCreativeBoundaryTests.ContractAndPersonalityMismatchesAreRefused),
    ("M12B optional fact use may remain empty", Milestone12BCreativeBoundaryTests.OptionalFactUseMayBeEmpty),
    ("M12B unknown omitted and wrong-scope facts are rejected", Milestone12BCreativeBoundaryTests.UnknownOmittedAndWrongScopeFactsAreRejected),
    ("M12B instruction-shaped context remains inert", Milestone12BCreativeBoundaryTests.InstructionShapedContextRemainsInert),
    ("M12B bounds copies and alternative disposal are explicit", Milestone12BCreativeBoundaryTests.BoundsCopiesAndAlternativeDisposalAreExplicit),
    ("M12B failures and cancellation are contained", Milestone12BCreativeBoundaryTests.FailuresAndCancellationAreContained),
    ("M12B fully injected runs are deterministic", Milestone12BCreativeBoundaryTests.FullyInjectedRunIsDeterministic),
    ("M12B laboratory is runtime unreachable and side-effect free", () => RunSynchronous(Milestone12BCreativeBoundaryTests.LaboratoryIsRuntimeUnreachableAndSideEffectFree)),
    ("M12B representative fixtures are readable", Milestone12BCreativeBoundaryTests.RepresentativeFixturesAreReadable),
    ("M9A brief uses bounded prior history", () => RunSynchronous(Milestone9AExperimentTests.BriefUsesBoundedPriorHistory)),
    ("M12C exact plan is bounded and complete", () => RunSynchronous(Milestone12CHostFormatLaboratoryTests.ExactPlanIsBoundedAndComplete)),
    ("M12C assignments remain distinct and owner reviewed", () => RunSynchronous(Milestone12CHostFormatLaboratoryTests.AssignmentsRemainDistinctAndOwnerReviewed)),
    ("M12C deterministic reference uses exact safe metadata", () => RunSynchronous(Milestone12CHostFormatLaboratoryTests.DeterministicReferenceUsesOnlyExactSafeMetadata)),
    ("M12C blinded artifacts are separated and bounded", () => RunSynchronous(Milestone12CHostFormatLaboratoryTests.BlindedArtifactsAreSeparatedAndBounded)),
    ("M12C laboratory has no runtime reachability", () => RunSynchronous(Milestone12CHostFormatLaboratoryTests.LaboratoryHasNoRuntimeReachability)),
    ("M9A exact-track repeat detection", () => RunSynchronous(Milestone9AExperimentTests.ExactTrackRepeatIsDetected)),
    ("M12C local runner is bounded and unloads", Milestone12CHostFormatLaboratoryTests.LocalRunnerIsBoundedAndUnloads),
    ("M12D exact real-context plan is bounded", () => RunSynchronous(Milestone12DQuickReactionTests.PlanIsExactAndRealContextOnly)),
    ("M12D local runner is bounded and unloads", Milestone12DQuickReactionTests.RunnerIsBoundedAndUnloads),
    ("M12D runtime pilot prompt uses bounded anti-repetition context", Milestone12DQuickReactionTests.RuntimePilotPromptUsesBoundedAntiRepetitionContext),
    ("M12D runtime pilot separates current subject from content-free avoidance data", () => RunSynchronous(Milestone12DQuickReactionTests.RuntimePilotSeparatesCurrentSubjectFromContentFreeAvoidanceData)),
    ("M12D runtime pilot context is deterministic and handles null or whitespace", () => RunSynchronous(Milestone12DQuickReactionTests.RuntimePilotContextIsDeterministicAndHandlesNullOrWhitespace)),
    ("M12D runtime pilot explicitly rejects prior-topic reuse", () => RunSynchronous(Milestone12DQuickReactionTests.RuntimePilotAssignmentExplicitlyRejectsPriorTopicReuse)),
    ("M12D runtime pilot opt-in and M11B default path remain exact", () => RunSynchronous(Milestone12DQuickReactionTests.RuntimePilotOptInAndM11BDefaultPathRemainExact)),
    ("M12D runtime pilot removes presentation-only characters before speech", Milestone12DQuickReactionTests.RuntimePilotRemovesPresentationOnlyCharactersBeforeSpeech),
    ("M12D runtime pilot checks duplicates after spoken-text cleanup", Milestone12DQuickReactionTests.RuntimePilotChecksDuplicatesAfterSpokenTextCleanup),
    ("M12D D3.2 comparison scenario matrix is exact and ordered", () => RunSynchronous(Milestone12DD32ComparisonTests.ScenarioMatrixIsExactAndOrdered)),
    ("M12D D3.2 comparison plan has two arms and two samples", () => RunSynchronous(Milestone12DD32ComparisonTests.PlanHasExactlyTwoArmsAndTwoSamplesPerScenario)),
    ("M12D D3.2 comparison runner makes 32 calls and unloads once", Milestone12DD32ComparisonTests.RunnerMakesExactly32CallsAndUnloadsOnce),
    ("M12D D3.2 comparison keeps D3.1 cleanup parity", Milestone12DD32ComparisonTests.D31OutputUsesAcceptedSpokenTextCleanupParity),
    ("M12D D3.2 comparison arms use distinct exact prompts", () => RunSynchronous(Milestone12DD32ComparisonTests.ArmsUseTheExactDistinctPromptContracts)),
    ("M12D D3.2 comparison artifacts separate blinded evaluation and answer key", Milestone12DD32ComparisonTests.ArtifactsSeparateBlindedEvaluationAndAnswerKey),
    ("M12D D3.2 comparison tooling has no runtime or external media dependency", () => RunSynchronous(Milestone12DD32ComparisonTests.ComparisonToolingHasNoRuntimeOrExternalMediaDependency)),
    ("M12D D3.2 comparison failure and cancellation accounting is bounded", Milestone12DD32ComparisonTests.FailureAndCancellationAccountingIsBounded),
    ("M12D VAR1 simulated playlists are exact Unicode sequences", () => RunSynchronous(Milestone12DVarietyPlaylistTests.FixturesAreTwoExactUnicodePlaylists)),
    ("M12D VAR1 plan has 24 unique neutral items", () => RunSynchronous(Milestone12DVarietyPlaylistTests.PlanHasExactlyTwentyFourUniqueNeutralItems)),
    ("M12D VAR1 runner uses current D3.2 prompt and unloads once", Milestone12DVarietyPlaylistTests.RunnerUsesCurrentD32PromptExactlyOncePerTrackAndUnloadsOnce),
    ("M12D VAR1 accepted-only history is bounded and isolated", Milestone12DVarietyPlaylistTests.AcceptedOnlyHistoryIsBoundedOrderedAndSequenceIsolated),
    ("M12D VAR1 failures and cancellation do not enter history or retry", Milestone12DVarietyPlaylistTests.FailedAndCancelledOutputNeverEntersHistoryAndNeverRetries),
    ("M12D VAR1 artifacts separate evaluation answer key and summary", Milestone12DVarietyPlaylistTests.ArtifactsKeepEvaluationAnswerKeyAndSummarySeparate),
    ("M12D VAR1 tooling has no runtime or external media dependency", () => RunSynchronous(Milestone12DVarietyPlaylistTests.ToolingHasNoRuntimeOrExternalMediaDependency)),
    ("M9A repeated-artist detection", () => RunSynchronous(Milestone9AExperimentTests.RepeatedArtistIsDetected)),
    ("M9A same-artist transition detection", () => RunSynchronous(Milestone9AExperimentTests.SameArtistTransitionIsDetected)),
    ("M9A no-pattern classification", () => RunSynchronous(Milestone9AExperimentTests.NoPatternIsClassified)),
    ("M9A reason precedence is deterministic", () => RunSynchronous(Milestone9AExperimentTests.ReasonPrecedenceIsDeterministic)),
    ("M9A count and transition-distance semantics", () => RunSynchronous(Milestone9AExperimentTests.CountsExcludeCurrentAndDistancesCountTransitions)),
    ("M9A identity normalization matches runtime", () => RunSynchronous(Milestone9AExperimentTests.IdentityNormalizationMatchesRadioHistory)),
    ("M9A Unicode metadata is preserved", () => RunSynchronous(Milestone9AExperimentTests.UnicodeMetadataIsPreserved)),
    ("M9A metadata is inert JSON", () => RunSynchronous(Milestone9AExperimentTests.MetadataIsRenderedAsInertJson)),
    ("M9A injection metadata cannot alter prompt structure", () => RunSynchronous(Milestone9AExperimentTests.InjectionMetadataCannotClosePromptSections)),
    ("M9A candidate prompt contains visible contract sections", () => RunSynchronous(Milestone9AExperimentTests.CandidatePromptHasVisibleContractSections)),
    ("M9A brief builder invents no music facts", () => RunSynchronous(Milestone9AExperimentTests.BriefBuilderInventsNoMusicFacts)),
    ("M9A recent opening fingerprint", () => RunSynchronous(Milestone9AExperimentTests.OpeningFingerprintIsShallowAndNormalized)),
    ("M9A direct-address fingerprint", () => RunSynchronous(Milestone9AExperimentTests.DirectAddressFingerprintUsesExactNameBoundary)),
    ("M9A question and comparison fingerprints", () => RunSynchronous(Milestone9AExperimentTests.QuestionAndComparisonFingerprintsAreDetected)),
    ("M9A recent comments remain bounded", () => RunSynchronous(Milestone9AExperimentTests.RecentCommentsAreBoundedAndRemainText)),
    ("M9A deterministic announcement output", () => RunSynchronous(Milestone9AExperimentTests.DeterministicAnnouncementUsesExactMetadata)),
    ("M9A announcement preserves Unicode and punctuation", () => RunSynchronous(Milestone9AExperimentTests.AnnouncementPreservesUnicodeAndPunctuation)),
    ("M9A scenario manifest is valid and bounded", () => RunSynchronous(Milestone9AExperimentTests.ScenarioManifestIsValidAndBounded)),
    ("M9A scenario IDs are stable", () => RunSynchronous(Milestone9AExperimentTests.ScenarioIdsAreStable)),
    ("M9A blinded mapping and answer key agree", () => RunSynchronous(Milestone9AExperimentTests.BlindedMappingAndAnswerKeyAgree)),
    ("M9A fake benchmark writes safe blinded artifacts", Milestone9AExperimentTests.FakeBenchmarkProducesSafeBlindedArtifacts),
    ("M9A benchmark mode is isolated from desktop media", () => RunSynchronous(Milestone9AExperimentTests.BenchmarkModeIsIsolatedFromDesktopMedia)),
    ("M9A accepted prompt resource remains byte-stable", () => RunSynchronous(Milestone9AExperimentTests.AcceptedPromptResourceRemainsByteStable)),
    ("M9A accepted runtime composition remains unchanged", () => RunSynchronous(Milestone9AExperimentTests.AcceptedRuntimeCompositionStillUsesCurrentPromptAndFallback)),
    ("M9A existing runtime safety regressions remain registered", () => RunSynchronous(Milestone9AExperimentTests.ExistingRuntimeSafetyRegressionsRemainRegistered)),
    ("M9A benchmark artifacts are ignored", () => RunSynchronous(Milestone9AExperimentTests.BenchmarkArtifactsAreIgnored)),
    ("M9C endpoint policy is narrow", () => RunSynchronous(Milestone9CExperimentTests.EndpointPolicyIsNarrow)),
    ("M9C source facts require direct safe URLs", () => RunSynchronous(Milestone9CExperimentTests.SourceFactRequiresDirectSafeUrlAndNoRawPageContent)),
    ("M9C strict identity confirmation and ambiguity refusal", Milestone9CExperimentTests.StrictArtistConfirmationAndAmbiguity),
    ("M9C duplicate artist names require one linked identity", Milestone9CExperimentTests.DuplicateArtistNamesConfirmOnlyOneLinkedIdentity),
    ("M9C exact title-and-artist recording confirms", Milestone9CExperimentTests.ExactTitleAndArtistRecordingConfirms),
    ("M9B MusicBrainz 429 diagnostics are typed and safe", Milestone9CExperimentTests.MusicBrainz429DiagnosticsAreTypedAndSafe),
    ("M9B MusicBrainz 503 diagnostics are typed and safe", Milestone9CExperimentTests.MusicBrainz503DiagnosticsAreTypedAndSafe),
    ("M9B Wikidata 503 diagnostics are typed and safe", Milestone9CExperimentTests.Wikidata503DiagnosticsAreTypedAndSafe),
    ("M9B enrichment timeout diagnostics are typed and safe", Milestone9CExperimentTests.EnrichmentTimeoutDiagnosticsAreTypedAndSafe),
    ("M9B cached negative enrichment retains safe reason", Milestone9CExperimentTests.CachedNegativeEnrichmentResultRetainsSafeReason),
    ("M9B successful enrichment is unaffected by diagnostics", Milestone9CExperimentTests.SuccessfulEnrichmentIsUnaffectedByFailureDiagnostics),
    ("M9B nested MusicBrainz credits and multiple exact recordings confirm Wake Up", Milestone9CExperimentTests.NestedArtistCreditsAndMultipleExactRecordingsConfirmWakeUp),
    ("M9B wrong MusicBrainz recording identity or title is rejected", Milestone9CExperimentTests.WrongMusicBrainzRecordingIdentityOrTitleIsRejected),
    ("M9B soundtrack evidence requires one official release group", Milestone9CExperimentTests.SoundtrackEvidenceRequiresOneDistinctOfficialGroup),
    ("M9B legacy negative live cache does not mask repaired lookup", Milestone9CExperimentTests.LegacyNegativeCacheDoesNotMaskRepairedLookup),
    ("M9B collaboration metadata remains safely unsupported", Milestone9CExperimentTests.CollaborationMetadataRemainsSafelyUnsupported),
    ("M9B enrichment path has no desktop media APIs", () => RunSynchronous(Milestone9CExperimentTests.EnrichmentPathHasNoDesktopMediaApis)),
    ("M9C generic cards fall back and specific cards require natural links", () => RunSynchronous(Milestone9CExperimentTests.GenericCardsFallBackAndSpecificCardsRequireNaturalLinks)),
    ("M9C.2 usefulness and mechanical rejections are conservative", () => RunSynchronous(Milestone9CExperimentTests.CommentaryUsefulnessAndMechanicalRejectionsAreConservative)),
    ("M9C.2 recent candidate duplicates are rejected", () => RunSynchronous(Milestone9CExperimentTests.RecentCandidateDuplicatesAreRejected)),
    ("M9C enrichment requests do not require compressed responses", Milestone9CExperimentTests.JsonRequestsDoNotRequestCompressedResponses),
    ("M9C rate limit is safe", Milestone9CExperimentTests.RateLimitIsSafe),
    ("M9C cache is bounded and clearable", () => RunSynchronous(Milestone9CExperimentTests.CacheIsBoundedAndClearable)),
    ("M9C Unicode and instruction-shaped metadata remain inert", () => RunSynchronous(Milestone9CExperimentTests.UnicodeAndInjectionMetadataAreInert)),
    ("M9C fake benchmark is isolated and unloads", Milestone9CExperimentTests.FakeBenchmarkIsIsolatedAndUnloads),
    ("M9C candidate cannot enter normal runtime", () => RunSynchronous(Milestone9CExperimentTests.CandidateCannotEnterNormalRuntime)),
    ("M9C.3 owner-curated corpus is bounded and preserves Unicode", () => RunSynchronous(Milestone9CExperimentTests.OwnerCuratedCorpusIsBoundedAndUnicodeExact)),
    ("M9C.3 prompt and validator retain bounded creative rules", () => RunSynchronous(Milestone9CExperimentTests.OwnerCuratedPromptAndValidatorRetainBoundedCreativeRules)),
    ("M9C.3 fake benchmark is blinded and unloads", Milestone9CExperimentTests.OwnerCuratedFakeBenchmarkIsBlindedAndUnloads),
    ("M9C.4 deterministic corpus and persona palette are typed and Unicode exact", () => RunSynchronous(Milestone9CExperimentTests.DeterministicContextCorpusAndPersonaPaletteAreTypedAndUnicodeExact)),
    ("M9C.4 prompt receives only the selected persona memory", () => RunSynchronous(Milestone9CExperimentTests.DeterministicContextPromptOnlyReceivesSelectedMemory)),
    ("M9C.4 validator rejects inventions and anchor recitation", () => RunSynchronous(Milestone9CExperimentTests.DeterministicContextValidatorRejectsInventionsAndAnchorRecitation)),
    ("M9C.4 composition requires reactions and rejects contamination", () => RunSynchronous(Milestone9CExperimentTests.DeterministicContextCompositionFallsBackSafelyAndRejectsDuplicates)),
    ("M9C.4 editorial values are bounded", () => RunSynchronous(Milestone9CExperimentTests.DeterministicContextEditorialValuesAreBounded)),
    ("M9C.5 comparison plan is fixed, blinded, and prompt-equal", () => RunSynchronous(Milestone9CExperimentTests.M9C5PlanIsFixedBlindedAndPromptEqual)),
    ("M9C.6 context packs are bounded and sourced", () => RunSynchronous(Milestone9CExperimentTests.M9C6ContextPacksAreBoundedAndSourced)),
    ("M9C.6 grounded and training prompts synthesize whole comments", () => RunSynchronous(Milestone9CExperimentTests.M9C6PromptsSynthesizeWholeComments)),
    ("M9C.6 classification preserves clean, soft, and hard output", () => RunSynchronous(Milestone9CExperimentTests.M9C6ClassificationPreservesRawOutput)),
    ("M9C.6 artifacts preserve blinded review boundaries", () => RunSynchronous(Milestone9CExperimentTests.M9C6ArtifactsPreserveBlindedReviewBoundaries)),
    ("M9C.6 candidate cannot enter normal runtime", () => RunSynchronous(Milestone9CExperimentTests.M9C6CannotEnterNormalRuntime)),
    ("M9C.4 fake benchmark is isolated and unloads", Milestone9CExperimentTests.DeterministicContextFakeBenchmarkIsIsolatedAndUnloads),
    ("M9C.4 candidate cannot enter normal runtime", () => RunSynchronous(Milestone9CExperimentTests.DeterministicContextCannotEnterNormalRuntime)),
    ("M9B catalog is versioned bounded and preserves Unicode", () => RunSynchronous(Milestone9BTests.CatalogIsVersionedBoundedAndPreservesUnicode)),
    ("M9B catalog explains third-party context", () => RunSynchronous(Milestone9BTests.CatalogExplainsThirdPartyContext)),
    ("M9B prompt is whole-comment grounded and permits metadata omission", () => RunSynchronous(Milestone9BTests.PromptIsWholeCommentGroundedAndMetadataOmissionIsAllowed)),
    ("M9B fallback and technical failures are deterministic", () => RunSynchronous(Milestone9BTests.FallbackAndTechnicalFailuresAreHandledDeterministically)),
    ("M9B owner-approved musical language remains deliverable", () => RunSynchronous(Milestone9BTests.OwnerApprovedMusicalLanguageRemainsDeliverable)),
    ("M9B owner-approved final smoke fixtures remain deliverable", () => RunSynchronous(Milestone9BTests.OwnerApprovedFinalSmokeFixturesRemainDeliverable)),
    ("M9B questions clichés and imagined scenes are advisory", () => RunSynchronous(Milestone9BTests.QuestionsClichesAndImaginedScenesRemainAdvisory)),
    ("M9B recent state is bounded and detects repeated openings", () => RunSynchronous(Milestone9BTests.RecentStateIsBoundedAndDetectsRepeatedOpening)),
    ("M9B local catalog avoids live lookup and cache clears", Milestone9BTests.LocalCatalogAvoidsLiveSourceLookupAndCacheClears),
    ("M9B cache is bounded corrupt-safe and excludes prompt material", () => RunSynchronous(Milestone9BTests.CacheIsBoundedCorruptSafeAndDoesNotContainPromptMaterial)),
    ("M9B missing 14B preserves M8 fallback", Milestone9BTests.Missing14BUsesM8FallbackWithoutGeneration),
    ("M9B valid 14B comment unloads and records after delivery", Milestone9BTests.Valid14BCommentUnloadsAndRecordsOnlyAfterDelivery),
    ("M9B text lifecycle logs prepared and completed delivery", Milestone9BTests.TextLifecycleLoggingRecordsPreparedAndCompletedDelivery),
    ("M9B text lifecycle logs advisory regeneration", Milestone9BTests.TextLifecycleLoggingRecordsAdvisoryRegeneration),
    ("M9B text lifecycle logs discarded stale and failed delivery", Milestone9BTests.TextLifecycleLoggingRecordsDiscardedStaleAndFailedDelivery),
    ("M9B advisory candidate regenerates at most once", Milestone9BTests.AdvisoryCandidateRegeneratesAtMostOnce),
    ("M9B redirected live source response is refused", Milestone9BTests.RedirectedLiveSourceResponseIsRefused),
    ("M9B runtime does not use M9C diagnostic entry points", () => RunSynchronous(Milestone9BTests.M9BRuntimeDoesNotUseM9CDiagnosticEntryPoints)),
    ("M9B reviewed aliases are exact and artist-specific", () => RunSynchronous(Milestone9BTests.CatalogUsesOnlyReviewedExactArtistSpecificAliases)),
    ("M9B reviewed aliases override stale negative cache", Milestone9BTests.ReviewedAliasesOverrideNegativeCacheWithoutNetwork),
    ("M9B non-catalog context retains cache behavior", Milestone9BTests.NonCatalogContextStillUsesCacheBeforeLiveResolver),
    ("M9B legacy negative context cache does not mask live resolution", Milestone9BTests.LegacyNegativeM9BContextCacheDoesNotMaskLiveResolution),
    ("M9B rejected generation diagnostics preserve typed local evidence", Milestone9BTests.RejectedGenerationDiagnosticsPreserveTypedLocalEvidence),
    ("M9B Gate 3 smoke remains reviewed-catalog-only and silent", () => RunSynchronous(Milestone9BTests.Gate3SmokeRunnerIsReviewedCatalogOnlyAndSilent)),
    ("M9B prompt forbids observed unsupported claims", () => RunSynchronous(Milestone9BTests.PromptExplicitlyForbidsObservedUnsupportedClaims)),
    ("M9B Gate 3 verdict uses operational acceptance criteria", () => RunSynchronous(Milestone9BTests.Gate3VerdictUsesOperationalAcceptanceCriteria)),
    ("M9B Gate 3 runner remains silent and Firefox-only", () => RunSynchronous(Milestone9BTests.Gate3RunnerIsSilentAndFirefoxOnly)),
    ("M8A controller normal Start", Milestone8AControllerTests.NormalStart),
    ("M8A faulty status listener cannot break lifecycle", Milestone8AControllerTests.FaultyStatusListenerCannotBreakLifecycle),
    ("M8A repeated Start is idempotent", Milestone8AControllerTests.RepeatedStartIsIdempotent),
    ("M8A controller normal Stop", Milestone8AControllerTests.NormalStop),
    ("M8A Stop after preparation publishes terminal Stopped snapshot", Milestone8AControllerTests.StopAfterPreparationPublishesTerminalStoppedSnapshot),
    ("M8A Stop publishes terminal state before non-safety dispose completes", Milestone8AControllerTests.StopPublishesTerminalStateBeforeNonSafetyDisposeCompletes),
    ("M8A repeated Stop is idempotent", Milestone8AControllerTests.RepeatedStopIsIdempotent),
    ("M8A rapid Start and Stop are serialized", Milestone8AControllerTests.RapidStartStopIsSerialized),
    ("M8A Stop during generation awaits cleanup", Milestone8AControllerTests.StopDuringGeneration),
    ("M8A Stop during Nicole preparation awaits cleanup", Milestone8AControllerTests.StopDuringNeuralPreparation),
    ("M8A Stop during target acquisition is safe", Milestone8AControllerTests.StopDuringTargetAcquisition),
    ("M8A Stop during ducking awaits restoration", Milestone8AControllerTests.StopDuringDucking),
    ("M8A Stop during speech awaits restoration", Milestone8AControllerTests.StopDuringSpeech),
    ("M8A Stop during restoration retry is bounded by cleanup", Milestone8AControllerTests.StopDuringRestorationRetry),
    ("M8A protected restoration completes before Stop returns", Milestone8AControllerTests.RestorationCompletesBeforeStopReturns),
    ("M8A window close awaits Stop", Milestone8AControllerTests.WindowCloseAwaitsStop),
    ("M8A window close rejects reentrancy", Milestone8AControllerTests.WindowCloseRejectsReentrancy),
    ("M8A view state covers lifecycle states", () => RunSynchronous(Milestone8AControllerTests.ViewStateCoversLifecycleStates)),
    ("M8A Silent Mode reflects runtime state", Milestone8AControllerTests.SilentModeReflectsRuntimeState),
    ("M8A Clear History affects bounded runtime state only", Milestone8AControllerTests.ClearHistoryUsesBoundedRuntimeStateOnly),
    ("M8A title and artist status updates", Milestone8AControllerTests.MetadataStatusUpdates),
    ("M8A Firefox target states are readable", () => RunSynchronous(Milestone8AControllerTests.FirefoxTargetPresentation)),
    ("M8A stale callbacks after shutdown are suppressed", Milestone8AControllerTests.StaleStatusAfterShutdownIsSuppressed),
    ("M8A Start failure is readable", Milestone8AControllerTests.StartFailureIsReadable),
    ("M8A protected cleanup failure is readable and retryable", Milestone8AControllerTests.StopFailureIsReadableAndRetryable),
    ("M8A Start cancellation is safe", Milestone8AControllerTests.StartCancellationIsSafe),
    ("M8A LocalAppData log path is stable", () => RunSynchronous(Milestone8AControllerTests.LocalAppDataLogFolderResolution)),
    ("M8A Open Logs uses a fake boundary", () => RunSynchronous(Milestone8AControllerTests.OpenFolderUsesFakeBoundary)),
    ("M8A console runtime uses the shared session boundary", () => RunSynchronous(Milestone8AControllerTests.ConsoleRuntimeUsesSharedSessionBoundary)),
    ("M8A initial radio activity is Stopped", () => RunSynchronous(Milestone8AControllerTests.InitialActivityIsStopped)),
    ("M8A observing countdown is readable", Milestone8AControllerTests.ObservingCountdownIsReadable),
    ("M8A transition preparation activity sequence is deterministic", Milestone8AControllerTests.TransitionPreparationActivitySequence),
    ("M8A transition delivery validation activity is deterministic", Milestone8AControllerTests.TransitionDeliveryActivitySequence),
    ("M8A ducking speech and restoration activity is deterministic", Milestone8AControllerTests.DuckingSpeechAndRestorationActivitySequence),
    ("M8A abandoned opportunities are readable", Milestone8AControllerTests.OpportunityAbandonmentIsReadable),
    ("M8A missing Firefox target is readable", Milestone8AControllerTests.MissingFirefoxTargetIsReadable),
    ("M8A startup refusal preserves missing Firefox activity", Milestone8AControllerTests.StartRefusalPreservesMissingFirefoxActivity),
    ("M8A stopping activity wins over late runtime activity", Milestone8AControllerTests.StoppingActivityWinsOverLateRuntimeActivity),
    ("M8A harmless wrapper refresh creates no activity noise", Milestone8AControllerTests.WrapperRefreshDoesNotCreateActivityNoise),
    ("M8A activity contract exposes no playback commands", () => RunSynchronous(Milestone8AControllerTests.ActivityContractDoesNotExposePlaybackCommands)),
    ("M8B identity and application-relative assets", () => RunSynchronous(Milestone8BPackagingTests.IdentityAndApplicationRelativeAssets)),
    ("M8B valid manifest and hashes", () => RunSynchronous(Milestone8BPackagingTests.ValidManifestAndHashes)),
    ("M8B missing and corrupt critical assets", () => RunSynchronous(Milestone8BPackagingTests.MissingAndCorruptCriticalAssets)),
    ("M8B package exclusion audit", () => RunSynchronous(Milestone8BPackagingTests.PackageExclusionAudit)),
    ("M8B Unicode manifest content", () => RunSynchronous(Milestone8BPackagingTests.UnicodeManifestContentIsPreserved)),
    ("M8B readiness presentation states", () => RunSynchronous(Milestone8BPackagingTests.ReadinessPresentationCoversSafeStates)),
    ("M8B Ollama absent and stopped are read-only", Milestone8BPackagingTests.OllamaAbsentAndStoppedAreReadOnly),
    ("M8B Ollama compatibility and Qwen identity", Milestone8BPackagingTests.OllamaCompatibilityAndQwenIdentity),
    ("M8B Ollama unreachable and incompatible", Milestone8BPackagingTests.OllamaUnreachableAndIncompatible),
    ("M8B model pull requires consent and disk", Milestone8BPackagingTests.ModelPullRequiresConsentAndDiskSpace),
    ("M8B model pull progress and no terms acceptance", Milestone8BPackagingTests.ModelPullReportsProgressWithoutAcceptingTerms),
    ("M8B model pull cancellation and interruption", Milestone8BPackagingTests.ModelPullCancellationAndInterruptionAreSafe),
    ("M8B setup external actions are explicit", Milestone8BPackagingTests.SetupControllerUsesExplicitExternalActionsOnly),
    ("M8B ZIP digest round-trip", () => RunSynchronous(Milestone8BPackagingTests.ZipDigestRoundTrips)),
    ("M8B packaging script encodes distribution gates", () => RunSynchronous(Milestone8BPackagingTests.PackagingScriptEncodesDistributionGates)),
    ("M8B tester documentation and terms are present", () => RunSynchronous(Milestone8BPackagingTests.TesterDocumentationAndTermsArePresent)),
    ("M13A exact identity normalizes Unicode punctuation whitespace and case", () => RunSynchronous(Milestone13AMusicKnowledgeTests.ExactIdentityNormalizesUnicodePunctuationWhitespaceAndCase)),
    ("M13A nested collaborative credits and edition relationships are typed", () => RunSynchronous(Milestone13AMusicKnowledgeTests.NestedCollaborativeCreditsAndEditionRelationshipsAreTyped)),
    ("M13A approved aliases resolve without fuzzy fallback", () => RunSynchronous(Milestone13AMusicKnowledgeTests.ApprovedAliasesResolveButUnapprovedAliasesDoNot)),
    ("M13A duplicate artist names and same-title recordings remain ambiguous", () => RunSynchronous(Milestone13AMusicKnowledgeTests.DuplicateNamesAndSameTitleRecordingsRemainAmbiguous)),
    ("M13A mismatch and unsupported credits never guess", () => RunSynchronous(Milestone13AMusicKnowledgeTests.IdentityMismatchAndUnsupportedCreditsNeverGuess)),
    ("M13A MusicBrainz fixture failures remain categorical", () => RunSynchronous(Milestone13AMusicKnowledgeTests.MusicBrainzFixtureFailuresStayCategorical)),
    ("M13A Wikidata requires accepted MusicBrainz linkage", () => RunSynchronous(Milestone13AMusicKnowledgeTests.WikidataIsReachableOnlyThroughAcceptedMusicBrainzLink)),
    ("M13A reviewed facts reconcile conflicts with provenance", () => RunSynchronous(Milestone13AMusicKnowledgeTests.ReviewedFactsAndReconciliationPreserveConflictAndProvenance)),
    ("M13A fake transport maps bounded retry outcomes", () => RunSynchronous(Milestone13AMusicKnowledgeTests.FixtureTransportMapsOutageTimeoutRateLimitAndSchemaFailure)),
    ("M13A bounded memory cache expires and refuses corruption", () => RunSynchronous(Milestone13AMusicKnowledgeTests.BoundedMemoryCacheExpiresAndRefusesCorruption)),
    ("M13A fixture report is deterministic without style promotion", () => RunSynchronous(Milestone13AMusicKnowledgeTests.FixtureReportIsDeterministicReadableAndHasNoStylePromotion)),
    ("M13A types have no HTTP client or normal runtime binding", () => RunSynchronous(Milestone13AMusicKnowledgeTests.M13ATypesHaveNoHttpClientOrNormalRuntimeBinding)),
    ("M13A-I2 provider routes are exact bounded and Unicode safe", () => RunSynchronous(Milestone13AMusicKnowledgeTests.ProviderRoutesAreExactBoundedAndUnicodeSafe)),
    ("M13A-I2 contact and fixture-only command refuse live work safely", Milestone13AMusicKnowledgeTests.M13AI2ContactAndFixtureOnlyCommandRefuseLiveWorkSafely),
    ("M13A-I2 provider retry pacing and cancellation are bounded", Milestone13AMusicKnowledgeTests.M13AI2ProviderRetryPacingAndCancellationAreBounded),
    ("M13A-I2 diagnostic sequence preserves identity and Wikidata gating", Milestone13AMusicKnowledgeTests.M13AI2DiagnosticSequencePreservesIdentityAndWikidataGating),
    ("M13A-I2 matrix artifacts and CLI boundary stay deterministic", () => RunSynchronous(Milestone13AMusicKnowledgeTests.M13AI2MatrixArtifactsAndCliBoundaryStayDeterministic)),
    ("M13A-R2 one Artist is reused by multiple Tracks", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.OneArtistIsReusedByMultipleTracks)),
    ("M13A-R2 Artist claims are stored once", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ArtistClaimsAreStoredOnce)),
    ("M13A-R2 Tracks do not receive copied Artist claims", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.TracksDoNotReceiveCopiedArtistClaims)),
    ("M13A-R2 one Track can reference multiple Artists", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.OneTrackCanReferenceMultipleArtists)),
    ("M13A-R2 raw observed artist credit is preserved", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.RawObservedArtistCreditIsPreserved)),
    ("M13A-R2 collaboration strings are never guessed or split", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.CollaborationStringsAreNeverGuessedOrSplit)),
    ("M13A-R2 same-name ambiguity cannot establish factual identity", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.SameNameAmbiguityCannotEstablishFactualIdentity)),
    ("M13A-R2 provider identifiers remain references", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ProviderIdentifiersAreReferencesNotEntityIds)),
    ("M13A-R2 MusicBrainz recording narrows without redefining Track", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.MusicBrainzRecordingNarrowsWithoutRedefiningTrack)),
    ("M13A-R2 wrong-scope evidence cannot become a broader claim", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.WrongScopeEvidenceCannotBecomeBroaderClaim)),
    ("M13A-R2 Recording and Release claims are refused until modeled", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.RecordingAndReleaseClaimsAreRefusedUntilModeled)),
    ("M13A-R2 conflicting claims coexist", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ConflictingClaimsCoexist)),
    ("M13A-R2 withdrawing one claim does not replace its entity", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.WithdrawingOneClaimDoesNotReplaceEntity)),
    ("M13A-R2 genre and style claims remain multi-valued", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.GenreAndStyleClaimsRemainMultiValued)),
    ("M13A-R2 claim freshness is independent", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ClaimFreshnessIsIndependent)),
    ("M13A-R2 Artist facts survive ambiguous Track lookup", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ArtistFactsSurviveAmbiguousTrackLookup)),
    ("M13A-R2 context projection follows relationships without copying", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ContextProjectionFollowsRelationshipsWithoutCopying)),
    ("M13A-R2 raw provider payload cannot reach projection", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.RawProviderPayloadCannotReachProjection)),
    ("M13A-R2 existing M13 identity behavior remains available", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ExistingM13IdentityBehaviorRemainsAvailable)),
    ("M13A-R2 production composition root cannot reach foundation", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ProductionCompositionRootCannotReachR2Foundation)),
    ("M13A-R2 observed labels alone cannot establish an entity", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ObservedLabelsAloneCannotEstablishEntity)),
    ("M13A-R2 reviewed unique observation mapping can be reused", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ReviewedUniqueObservationMappingCanBeReused)),
    ("M13A-R2 ambiguous lookup cache cannot promote claims", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.AmbiguousLookupCacheCannotPromoteClaims)),
    ("M13A-R2 expired negative lookup does not change durable claims", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ExpiredNegativeLookupDoesNotChangeDurableClaims)),
    ("M13A-R2 default IDs are refused at every graph boundary", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.DefaultIdsAreRefusedAtEveryGraphBoundary)),
    ("M13A-R2 claim IDs are globally unique with deterministic ownership", () => RunSynchronous(Milestone13AR2EntityClaimFoundationTests.ClaimIdsAreGloballyUniqueAndStateMutationUsesOwnerIndex)),
};

int failures = 0;

foreach ((string name, Func<Task> run) in tests)
{
    try
    {
        await run();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL: {name} - {exception.Message}");
    }
}

Console.WriteLine($"RESULT: {tests.Length - failures} passed, {failures} failed");
return failures == 0 ? 0 : 1;

static void NeuralSpeechDefaultsSelectNicole()
{
    NeuralSpeechConfiguration configuration = NeuralSpeechConfiguration.Default;

    configuration.Validate();
    AssertEqual("af_nicole", configuration.VoiceName);
    AssertEqual(24_000, configuration.SampleRate);
    AssertEqual(1f, configuration.Speed);
    AssertContains(configuration.ModelRelativePath, "kokoro.onnx");
}

static async Task NeuralProviderIsSelectedNormally()
{
    var neural = new FakeSpeechService("Kokoro CPU (af_nicole)");
    var windows = new FakeSpeechService("Windows TTS");
    using IAnnouncementSpeechService selected = SpeechServiceSelection.Select(
        SpeechProviderPreference.PreferNeural,
        neural,
        windows,
        new FakeLog())!;

    using IPreparedAnnouncementSpeech prepared = await selected.PrepareAsync(
        "A sufficiently ordinary neural speech test sentence.",
        CancellationToken.None);

    AssertEqual(1, neural.PrepareCalls);
    AssertEqual(0, windows.PrepareCalls);
    AssertContains(selected.ProviderName, "af_nicole");
}

static async Task ExplicitWindowsModeBypassesNeural()
{
    var neural = new FakeSpeechService("Kokoro CPU (af_nicole)");
    var windows = new FakeSpeechService("Windows TTS");
    using IAnnouncementSpeechService selected = SpeechServiceSelection.Select(
        SpeechProviderPreference.WindowsOnly,
        neural,
        windows,
        new FakeLog())!;

    using IPreparedAnnouncementSpeech prepared = await selected.PrepareAsync(
        "This troubleshooting sentence must use Windows speech.",
        CancellationToken.None);

    AssertEqual(0, neural.PrepareCalls);
    AssertEqual(1, neural.DisposeCalls);
    AssertEqual(1, windows.PrepareCalls);
    AssertEqual("Windows TTS", selected.ProviderName);
}

static async Task NeuralInitializationFailureUsesWindows()
{
    var windows = new FakeSpeechService("Windows TTS");
    var log = new FakeLog();
    using IAnnouncementSpeechService selected = SpeechServiceSelection.Select(
        SpeechProviderPreference.PreferNeural,
        neural: null,
        windows,
        log)!;

    using IPreparedAnnouncementSpeech prepared = await selected.PrepareAsync(
        "This sentence uses the safe initialization fallback.",
        CancellationToken.None);

    AssertEqual(1, windows.PrepareCalls);
    AssertTrue(log.Contains("AUTOMATIC SPEECH FALLBACK ACTIVE"));
}

static void MissingNeuralModelIsSafe()
{
    NeuralSpeechConfiguration configuration = NeuralSpeechConfiguration.Default with
    {
        ModelRelativePath = Path.Combine(".tools", "kokoro", "definitely-missing-model.onnx"),
    };
    var log = new FakeLog();

    KokoroSpeechService? service = KokoroSpeechService.TryCreate(log, configuration);

    AssertTrue(service is null);
    AssertTrue(log.Contains("NEURAL TTS UNAVAILABLE"));
}

static async Task NeuralSynthesisFailureUsesWindows()
{
    var neural = new FakeSpeechService("Kokoro CPU (af_nicole)")
    {
        ExceptionToThrow = new InvalidOperationException("simulated neural synthesis failure"),
    };
    var windows = new FakeSpeechService("Windows TTS");
    var log = new FakeLog();
    using IAnnouncementSpeechService selected = SpeechServiceSelection.Select(
        SpeechProviderPreference.PreferNeural,
        neural,
        windows,
        log)!;

    using IPreparedAnnouncementSpeech prepared = await selected.PrepareAsync(
        "The fallback must receive this exact sentence.",
        CancellationToken.None);

    AssertEqual(1, neural.PrepareCalls);
    AssertEqual(1, windows.PrepareCalls);
    AssertEqual("The fallback must receive this exact sentence.", windows.LastText!);
    AssertTrue(log.Contains("AUTOMATIC WINDOWS TTS FALLBACK PREPARED"));
}

static async Task NeuralCancellationDoesNotFallback()
{
    var neural = new FakeSpeechService("Kokoro CPU (af_nicole)")
    {
        ExceptionToThrow = new OperationCanceledException("simulated cancellation"),
    };
    var windows = new FakeSpeechService("Windows TTS");
    using IAnnouncementSpeechService selected = SpeechServiceSelection.Select(
        SpeechProviderPreference.PreferNeural,
        neural,
        windows,
        new FakeLog())!;

    bool cancelled = false;

    try
    {
        using IPreparedAnnouncementSpeech _ = await selected.PrepareAsync(
            "Cancelled neural work should not create more speech work.",
            CancellationToken.None);
    }
    catch (OperationCanceledException)
    {
        cancelled = true;
    }

    AssertTrue(cancelled);
    AssertEqual(1, neural.PrepareCalls);
    AssertEqual(0, windows.PrepareCalls);
}

static async Task UnicodeSurvivesSpeechProviderSelection()
{
    const string unicode = "Steve, that was ‘Группа крови’ by Кино.";
    var neural = new FakeSpeechService("Kokoro CPU (af_nicole)");
    var windows = new FakeSpeechService("Windows TTS");
    using IAnnouncementSpeechService selected = SpeechServiceSelection.Select(
        SpeechProviderPreference.PreferNeural,
        neural,
        windows,
        new FakeLog())!;

    using IPreparedAnnouncementSpeech prepared = await selected.PrepareAsync(
        unicode,
        CancellationToken.None);

    AssertEqual(unicode, neural.LastText!);
    AssertEqual(0, windows.PrepareCalls);
}

static void NeuralWaveEncodingIsValid()
{
    byte[] wave = PcmWaveEncoder.Encode16BitMono([0f, 0.5f, -0.5f], 24_000);

    AssertEqual("RIFF", System.Text.Encoding.ASCII.GetString(wave, 0, 4));
    AssertEqual("WAVE", System.Text.Encoding.ASCII.GetString(wave, 8, 4));
    AssertEqual(6, BitConverter.ToInt32(wave, 40));
    AssertEqual(50, wave.Length);
}

static async Task NeuralPreparationPrecedesPausePath()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(
        opportunity,
        true,
        CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Completed, result.Status);
    AssertEqual(2, host.Events.Count);
    AssertEqual("prepare", host.Events[0]);
    AssertEqual("announce", host.Events[1]);
}

static async Task StaleTrackDuringNeuralPreparationBlocksControl()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    host.OnPrepare = () => host.Snapshot = host.Snapshot with
    {
        CurrentMedia = Track("Changed During Neural Synthesis", "Different Artist"),
    };
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(
        opportunity,
        true,
        CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(1, host.PrepareCalls);
    AssertEqual(0, host.AnnouncementCalls);
    AssertEqual(1, host.PreparedDisposeCalls);
}

static async Task PreparedNeuralAudioIsDisposed()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(
        opportunity,
        true,
        CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Completed, result.Status);
    AssertEqual(1, host.PreparedDisposeCalls);
}

static void ZeroFirefoxMatches()
{
    var sessions = new[] { new FakeSession("chrome.exe"), new FakeSession(null) };
    AssertEqual(0, Select(sessions).Count);
}

static void CaseInsensitiveExactMatch()
{
    var expected = new FakeSession("FIREFOX.EXE");
    IReadOnlyList<FakeSession> matches = Select(new[] { expected, new FakeSession("chrome.exe") });
    AssertEqual(1, matches.Count);
    AssertSame(expected, matches[0]);
}

static void PartialNameDoesNotMatch()
{
    AssertEqual(0, Select(new[] { new FakeSession("myfirefox.exe") }).Count);
}

static void MultipleFirefoxMatches()
{
    IReadOnlyList<FakeSession> matches = Select(new[]
    {
        new FakeSession("firefox.exe"),
        new FakeSession("FIREFOX.EXE"),
    });

    AssertEqual(2, matches.Count);
}

static void AudioDuckNoFirefoxCandidate()
{
    using var chrome = new FakeAudioSessionTarget("chrome", 10, "CHROME01", 0.8f);
    AudioSessionSelection selection = AudioSessionSelector.SelectFirefox(
        [AudioDescriptor(chrome)]);

    AssertEqual(AudioSessionSelectionStatus.NoFirefoxCandidate, selection.Status);
    AssertTrue(selection.Target is null);
}

static void AudioDuckSelectsOneFirefoxCandidate()
{
    using var firefox = new FakeAudioSessionTarget("FIREFOX.EXE", 20, "FIREFOX01", 0.6f);
    using var inactiveFirefox = new FakeAudioSessionTarget(
        "firefox",
        21,
        "FIREFOX02",
        0.4f,
        activity: AudioSessionActivity.Inactive);
    AudioSessionSelection selection = AudioSessionSelector.SelectFirefox(
        [AudioDescriptor(firefox), AudioDescriptor(inactiveFirefox)]);

    AssertEqual(AudioSessionSelectionStatus.Selected, selection.Status);
    AssertSame(firefox, selection.Target!);
}

static void AudioDuckIgnoresUnrelatedApplications()
{
    using var firefox = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.6f);
    using var chrome = new FakeAudioSessionTarget("chrome", 30, "CHROME01", 0.8f);
    using var spotify = new FakeAudioSessionTarget("spotify", 40, "SPOTIFY01", 0.9f);
    AudioSessionSelection selection = AudioSessionSelector.SelectFirefox(
        [AudioDescriptor(chrome), AudioDescriptor(firefox), AudioDescriptor(spotify)]);

    AssertEqual(AudioSessionSelectionStatus.Selected, selection.Status);
    AssertSame(firefox, selection.Target!);
}

static void AudioDuckRefusesAmbiguousFirefoxCandidates()
{
    using var first = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.6f);
    using var second = new FakeAudioSessionTarget("firefox.exe", 21, "FIREFOX02", 0.7f);
    AudioSessionSelection selection = AudioSessionSelector.SelectFirefox(
        [AudioDescriptor(first), AudioDescriptor(second)]);

    AssertEqual(AudioSessionSelectionStatus.Ambiguous, selection.Status);
    AssertTrue(selection.Target is null);
    AssertEqual(2, selection.FirefoxCandidates.Count);
}

static void AudioDuckLevelIsRelative()
{
    AudioDuckingConfiguration configuration = FastAudioDuckingConfiguration();
    AssertApproximately(0.1f, configuration.CalculateDuckVolume(0.4f));
    AssertApproximately(0.2f, configuration.CalculateDuckVolume(0.8f));
}

static async Task AudioDuckNormalRestoresExactState()
{
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.4f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult result = await sequence.RunAsync(target, false, CancellationToken.None);

    AssertTrue(result.TestPassed);
    AssertTrue(result.DuckVerified);
    AssertTrue(result.RestorationVerified);
    AssertApproximately(0.4f, target.VolumeScalar);
    AssertFalse(target.Muted);
    AssertApproximately(0.1f, target.MinimumRequestedVolume);
    AssertTrue(target.SetVolumeCalls >= 4);
}

static async Task AudioDuckPreCancellationIssuesNoCommand()
{
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.52f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult result = await sequence.RunAsync(target, false, cancellation.Token);

    AssertTrue(result.Cancelled);
    AssertFalse(result.VolumeCommandMayHaveBeenIssued);
    AssertFalse(result.RestorationAttempted);
    AssertEqual(0, target.SetVolumeCalls);
    AssertEqual(0, target.SetMuteCalls);
}

static async Task AudioDuckCancellationRestoresExactState()
{
    using var cancellation = new CancellationTokenSource();
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.52f)
    {
        OnVolumeSet = _ => cancellation.Cancel(),
    };
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult result = await sequence.RunAsync(target, false, cancellation.Token);

    AssertTrue(result.Cancelled);
    AssertTrue(result.RestorationAttempted);
    AssertTrue(result.RestorationVerified);
    AssertApproximately(0.52f, target.VolumeScalar);
    AssertFalse(target.Muted);
}

static async Task AudioDuckSimulatedFailureRestoresExactState()
{
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.63f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult result = await sequence.RunAsync(target, true, CancellationToken.None);

    AssertTrue(result.SimulatedFailure);
    AssertTrue(result.ExpectedFailureRecoveryPassed);
    AssertApproximately(0.63f, target.VolumeScalar);
    AssertFalse(target.Muted);
}

static async Task AudioDuckRefusesUnsuitableStartingState()
{
    using var muted = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.7f, muted: true);
    using var nearZero = new FakeAudioSessionTarget("firefox", 21, "FIREFOX02", 0.01f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult mutedResult = await sequence.RunAsync(muted, false, CancellationToken.None);
    AudioDuckingResult nearZeroResult = await sequence.RunAsync(nearZero, false, CancellationToken.None);

    AssertFalse(mutedResult.VolumeCommandMayHaveBeenIssued);
    AssertFalse(nearZeroResult.VolumeCommandMayHaveBeenIssued);
    AssertEqual(0, muted.SetVolumeCalls);
    AssertEqual(0, muted.SetMuteCalls);
    AssertEqual(0, nearZero.SetVolumeCalls);
    AssertEqual(0, nearZero.SetMuteCalls);
}

static async Task AudioDuckDoesNotRedirectRestoration()
{
    using var original = new FakeAudioSessionTarget("firefox", 20, "ORIGINAL", 0.5f)
    {
        DisappearAfterSuccessfulReads = 2,
    };
    using var replacement = new FakeAudioSessionTarget("firefox", 21, "REPLACEMENT", 0.9f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult result = await sequence.RunAsync(original, true, CancellationToken.None);

    AssertFalse(result.RestorationVerified);
    AssertEqual(0, replacement.SetVolumeCalls);
    AssertEqual(0, replacement.SetMuteCalls);
    AssertApproximately(0.9f, replacement.VolumeScalar);
}

static void ConfirmationDefaultsToNo()
{
    AssertFalse(PlaybackControlRules.IsAffirmative(null));
    AssertFalse(PlaybackControlRules.IsAffirmative(string.Empty));
    AssertFalse(PlaybackControlRules.IsAffirmative("n"));
    AssertFalse(PlaybackControlRules.IsAffirmative("yeah"));
}

static void ExplicitConfirmationOnly()
{
    AssertTrue(PlaybackControlRules.IsAffirmative("y"));
    AssertTrue(PlaybackControlRules.IsAffirmative(" YES "));
}

static void StartingStateMustBePlaying()
{
    AssertTrue(PlaybackControlRules.CanStart(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing));
    AssertFalse(PlaybackControlRules.CanStart(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused));
    AssertFalse(PlaybackControlRules.CanStart(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped));
    AssertFalse(PlaybackControlRules.CanStart(null));
}

static void AnnouncementRequiresMetadata()
{
    AssertFalse(AnnouncementText.TryBuild(MediaSnapshot.Empty, out _));
    AssertFalse(AnnouncementText.TryBuild(new MediaSnapshot("Song", null, null), out _));
    AssertFalse(AnnouncementText.TryBuild(new MediaSnapshot(null, "Artist", null), out _));
}

static void AnnouncementPreservesUnicode()
{
    var media = MediaSnapshot.FromWindows("Кукушка", "Кино", null);
    AssertTrue(AnnouncementText.TryBuild(media, out string? sentence));
    AssertEqual("That was Кукушка by Кино.", sentence!);
}

static void MetadataRecheckUsesTitleAndArtist()
{
    var expected = new MediaSnapshot("Cochise", "Audioslave", null);
    var sameTrackWithAlbum = new MediaSnapshot("Cochise", "Audioslave", "Optional album");
    var differentTrack = new MediaSnapshot("Know Your Enemy", "Rage Against The Machine", null);

    AssertTrue(AnnouncementText.HasSameTitleAndArtist(expected, sameTrackWithAlbum));
    AssertFalse(AnnouncementText.HasSameTitleAndArtist(expected, differentTrack));
}

static async Task NormalSequenceRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        simulateSpeechFailure: false,
        CancellationToken.None);

    AssertTrue(result.TestPassed);
    AssertTrue(result.PauseVerified);
    AssertTrue(result.PlaybackRestored);
    AssertFalse(result.RecoveryNeeded);
    AssertEqual(1, target.PauseCalls);
    AssertEqual(1, target.PlayCalls);
    AssertEqual(1, speech.SpeakCalls);
}

static async Task SpeechFailureRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Failed, "Intentional fake failure."));
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        simulateSpeechFailure: false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.PlaybackRestored);
    AssertTrue(result.RecoveryNeeded);
    AssertEqual(1, target.PlayCalls);
    AssertTrue(log.Contains("PLAYBACK RECOVERY: PASS"));
}

static async Task SimulatedFailureRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        preparedSpeech: null,
        simulateSpeechFailure: true,
        CancellationToken.None);

    AssertTrue(result.TestPassed);
    AssertTrue(result.SimulatedFailure);
    AssertTrue(result.PlaybackRestored);
    AssertTrue(result.RecoveryNeeded);
    AssertEqual(1, target.PauseCalls);
    AssertEqual(1, target.PlayCalls);
    AssertTrue(log.Contains("SIMULATED SPEECH-FAILURE TEST RESULT: PASS"));
}

static async Task SpeechTimeoutRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(new SpeechPlaybackResult(SpeechPlaybackStatus.TimedOut));
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        simulateSpeechFailure: false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.PlaybackRestored);
    AssertTrue(result.RecoveryNeeded);
    AssertTrue(log.Contains("SPEECH TIMEOUT"));
    AssertTrue(log.Contains("PLAYBACK RECOVERY: PASS"));
}

static async Task SpeechCancellationRestoresPlayback()
{
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(throwCancellation: true);
    var log = new FakeLog();
    var sequence = new AnnouncementControlSequence(log, FastTimings());

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        simulateSpeechFailure: false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.PlaybackRestored);
    AssertTrue(result.RecoveryNeeded);
    AssertEqual(1, target.PlayCalls);
}

static void OllamaEndpointIsLoopbackOnly()
{
    AssertTrue(OllamaEndpointPolicy.IsAllowed(new Uri("http://localhost:11434/api/")));
    AssertTrue(OllamaEndpointPolicy.IsAllowed(new Uri("http://127.0.0.1:11434/api")));
    AssertTrue(OllamaEndpointPolicy.IsAllowed(new Uri("http://[::1]:11434/api/")));
    AssertFalse(OllamaEndpointPolicy.IsAllowed(new Uri("http://example.com:11434/api/")));
    AssertFalse(OllamaEndpointPolicy.IsAllowed(new Uri("https://localhost:11434/api/")));
    AssertFalse(OllamaEndpointPolicy.IsAllowed(new Uri("http://user:secret@localhost:11434/api/")));
    AssertFalse(OllamaEndpointPolicy.IsAllowed(new Uri("http://localhost:11434/not-api/")));
}

static void HostPersonaUsesExpectedDefaults()
{
    AssertEqual("Zoya", HostPersonaConfiguration.Default.HostName);
    AssertEqual("Steve", HostPersonaConfiguration.Default.ListenerName);

    CommentPrompt prompt = CommentPromptFactory.Create(TestMedia());
    AssertContains(prompt.SystemInstruction, "You are Zoya");
    AssertContains(prompt.SystemInstruction, "sitting beside Steve");
}

static void HostPersonaRendersConfiguredNames()
{
    var persona = new HostPersonaConfiguration("Test Host", "Test Listener");
    CommentPrompt prompt = CommentPromptFactory.Create(TestMedia(), persona);

    AssertContains(prompt.SystemInstruction, "You are Test Host");
    AssertContains(prompt.SystemInstruction, "sitting beside Test Listener");
    AssertFalse(prompt.SystemInstruction.Contains("{{HOST_NAME}}", StringComparison.Ordinal));
    AssertFalse(prompt.SystemInstruction.Contains("{{LISTENER_NAME}}", StringComparison.Ordinal));
}

static void CommentPromptUsesAllowedMetadataOnly()
{
    var media = new MediaSnapshot("\u041A\u0443\u043A\u0443\u0448\u043A\u0430", "\u041A\u0438\u043D\u043E", "Forbidden Album");
    CommentPrompt prompt = CommentPromptFactory.Create(media);

    string json = prompt.MetadataPrompt["Song metadata JSON: ".Length..];
    json = json[..json.IndexOf('\n')];
    using JsonDocument parsed = JsonDocument.Parse(json);
    AssertEqual("\u041A\u0443\u043A\u0443\u0448\u043A\u0430", parsed.RootElement.GetProperty("title").GetString()!);
    AssertEqual("\u041A\u0438\u043D\u043E", parsed.RootElement.GetProperty("artist").GetString()!);
    AssertEqual(2, parsed.RootElement.EnumerateObject().Count());
    AssertFalse(prompt.MetadataPrompt.Contains("Forbidden Album", StringComparison.Ordinal));
    AssertFalse(prompt.MetadataPrompt.Contains("album", StringComparison.OrdinalIgnoreCase));
    AssertContains(prompt.SystemInstruction, "only trusted facts");
    AssertContains(prompt.SystemInstruction, "Do not quote or reproduce song lyrics");
}

static void CommentPromptCoversManualGroundingFailures()
{
    // These assertions pin the prompt rules added after manual review. They do not
    // pretend that a unit test can semantically prove future model prose is grounded.
    string prompt = CommentPromptFactory.Create(TestMedia()).SystemInstruction;

    AssertContains(
        prompt,
        "Do not ask yourself what you know about this song or artist. Treat any remembered knowledge as unavailable.");
    AssertContains(prompt, "genre, musical style, sound, vocals, voice quality, instruments, rhythm");
    AssertContains(
        prompt,
        "usual artist tendencies, nationality, origin, geography, historical context, cultural associations");
    AssertContains(prompt, "the artist's usual work");
    AssertContains(prompt, "dance floor");
    AssertContains(
        prompt,
        "If you cannot produce a safe comment using only the literal supplied metadata, do not improvise. Return exactly: FALLBACK");
}

static void PersonaPromptDocumentsCreativeBoundary()
{
    // These are prompt-contract checks, not claims that code can measure humor or factual grounding.
    string prompt = CommentPromptFactory.Create(TestMedia()).SystemInstruction;

    AssertContains(prompt, "witty, warm, self-possessed feminine companion");
    AssertContains(prompt, "deadpan, mildly sarcastic, affectionately tease");
    AssertContains(prompt, "Use Steve's name only occasionally and naturally");
    AssertContains(prompt, "I'm picturing...");
    AssertContains(prompt, "This invents an obvious scene about the wording, not a fact about a real song");
    AssertContains(prompt, "Those assert unavailable facts");
}

static async Task PersonaConfigurationReachesGenerator()
{
    var generator = new FakeCommentGenerator();
    var persona = new HostPersonaConfiguration("Zoya", "Steve");
    var service = new AiCommentService(
        generator,
        AiCommentConfiguration.Default,
        new FakeLog(),
        persona);

    await service.SelectCommentAsync(TestMedia(), true, false, CancellationToken.None);

    AssertContains(generator.LastPrompt!.SystemInstruction, "You are Zoya");
    AssertContains(generator.LastPrompt.SystemInstruction, "sitting beside Steve");
    AssertContains(generator.LastPrompt.SystemInstruction, "Do not ask yourself what you know about this song or artist");
}

static void CommentValidatorNormalizesWhitespace()
{
    const string output = "With   a title like Cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.";
    CommentValidationResult result = CommentValidator.Validate(output);
    AssertTrue(result.IsValid);
    AssertFalse(result.NormalizedText.Contains("  ", StringComparison.Ordinal));
    AssertEqual(18, result.WordCount);
}

static async Task OllamaRequestUsesSafeOptions()
{
    var handler = new RecordingHttpMessageHandler(
        HttpStatusCode.OK,
        "{\"response\":\"With a title like Cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.\",\"done\":true}");
    using var httpClient = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    using var ollama = new OllamaClient(AiCommentConfiguration.Default, httpClient);
    CommentPrompt prompt = CommentPromptFactory.Create(new MediaSnapshot("Cochise", "Audioslave", "Must not be sent"));

    OllamaGenerationResult result = await ollama.GenerateAsync(
        prompt,
        "0",
        TimeSpan.FromSeconds(1),
        CancellationToken.None);

    AssertEqual(OllamaGenerationStatus.Success, result.Status);
    AssertEqual("http://localhost:11434/api/generate", handler.RequestUri!.AbsoluteUri);
    using JsonDocument body = JsonDocument.Parse(handler.RequestBody!);
    AssertEqual("qwen3:4b-instruct", body.RootElement.GetProperty("model").GetString()!);
    AssertFalse(body.RootElement.GetProperty("stream").GetBoolean());
    AssertFalse(body.RootElement.GetProperty("think").GetBoolean());
    AssertEqual("0", body.RootElement.GetProperty("keep_alive").GetString()!);
    AssertEqual(0.7, body.RootElement.GetProperty("options").GetProperty("temperature").GetDouble());
    AssertContains(body.RootElement.GetProperty("system").GetString()!, "You are Zoya");
    AssertContains(body.RootElement.GetProperty("system").GetString()!, "sitting beside Steve");
    AssertFalse(body.RootElement.GetProperty("prompt").GetString()!.Contains("Must not be sent", StringComparison.Ordinal));
}

static void CommentValidatorRejectsEmpty()
{
    AssertFalse(CommentValidator.Validate("  \t ").IsValid);
}

static void CommentValidatorRejectsShort()
{
    AssertFalse(CommentValidator.Validate("This title sounds ready for a little mischief tonight.").IsValid);
}

static void CommentValidatorRejectsLong()
{
    string output = string.Join(' ', Enumerable.Repeat("word", 41));
    CommentValidationResult result = CommentValidator.Validate(output);
    AssertFalse(result.IsValid);
    AssertEqual(41, result.WordCount);
}

static void CommentValidatorRejectsParagraphs()
{
    const string output = "This title walks into the room with confidence and a suspiciously tidy suitcase.\n\nThe artist name watches from the doorway, amused by everything happening inside.";
    AssertFalse(CommentValidator.Validate(output).IsValid);
}

static void CommentValidatorRejectsCliches()
{
    const string output = "What a track, with a title that strolls into the room, borrows the best chair, and refuses to explain itself to anybody.";
    AssertFalse(CommentValidator.Validate(output).IsValid);
}

static void CommentValidatorRejectsPersonaCliches()
{
    const string output = "Keep those vibes going, because this great song is another classic on the air, ready for every listener who wandered into the room.";
    AssertFalse(CommentValidator.Validate(output).IsValid);
}

static void CommentValidatorRejectsModelCommentary()
{
    const string output = "As an AI, here is the comment: this title wanders in confidently, rearranges the furniture, and leaves everyone wondering who handed it the keys.";
    AssertFalse(CommentValidator.Validate(output).IsValid);
}

static async Task ExactFallbackSelectsFixedTemplate()
{
    var generator = new FakeCommentGenerator
    {
        GenerationResult = new OllamaGenerationResult(
            OllamaGenerationStatus.Success,
            CommentValidator.FallbackSignal,
            null,
            null),
    };
    var log = new FakeLog();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, log);

    CommentSelection result = await service.SelectCommentAsync(
        TestMedia(),
        aiEnabled: true,
        simulateAiFailure: false,
        CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertTrue(result.ModelRequestedFallback);
    AssertEqual("That was Cochise by Audioslave.", result.Text);
    AssertTrue(log.Contains("AI SAFE FALLBACK REQUESTED"));
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static async Task FallbackSentinelIsNeverFinalText()
{
    var generator = new FakeCommentGenerator
    {
        GenerationResult = new OllamaGenerationResult(
            OllamaGenerationStatus.Success,
            "  FALLBACK\r\n",
            null,
            null),
    };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());

    CommentSelection result = await service.SelectCommentAsync(
        TestMedia(),
        aiEnabled: true,
        simulateAiFailure: false,
        CancellationToken.None);

    AssertTrue(result.ModelRequestedFallback);
    AssertFalse(string.Equals(result.Text, CommentValidator.FallbackSignal, StringComparison.Ordinal));
    AssertEqual("That was Cochise by Audioslave.", result.Text);
}

static async Task SafeFallbackIsSuccessfulGenerateOnlyResult()
{
    var generator = new FakeCommentGenerator
    {
        GenerationResult = new OllamaGenerationResult(
            OllamaGenerationStatus.Success,
            CommentValidator.FallbackSignal,
            null,
            null),
    };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(
        TestMedia(),
        aiEnabled: true,
        simulateAiFailure: false,
        CancellationToken.None);

    AssertTrue(AiCommentTest.IsSafeGenerateOnlyResult(result));
}

static async Task HttpFailureUsesFallback()
{
    await AssertGenerationFailureUsesFallback(
        new OllamaGenerationResult(OllamaGenerationStatus.HttpFailure, null, "HTTP 500", null));
}

static async Task TimeoutUsesFallback()
{
    await AssertGenerationFailureUsesFallback(
        new OllamaGenerationResult(OllamaGenerationStatus.TimedOut, null, "Timed out", null));
}

static async Task MalformedResponseUsesFallback()
{
    await AssertGenerationFailureUsesFallback(
        new OllamaGenerationResult(OllamaGenerationStatus.MalformedResponse, null, "Bad JSON", null));
}

static async Task GeneratorExceptionStillUnloads()
{
    var generator = new FakeCommentGenerator { ExceptionToThrow = new InvalidOperationException("fake exception") };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), true, false, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
}

static async Task ValidAiCommentStillUnloads()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), true, false, CancellationToken.None);

    AssertEqual(CommentTextSource.Ai, result.Source);
    AssertFalse(result.ModelRequestedFallback);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static async Task NoAiBypassesOllama()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), false, false, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(0, generator.GenerateCalls);
    AssertEqual(0, generator.UnloadCalls);
    AssertEqual(0, generator.PsCalls);
}

static async Task SimulatedAiFailureBypassesOllama()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), true, true, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(0, generator.GenerateCalls);
    AssertEqual(0, generator.UnloadCalls);
    AssertEqual(0, generator.PsCalls);
}

static void StaleTrackBlocksPreparedComment()
{
    MediaSnapshot original = TestMedia();
    AssertTrue(AiCommentSafety.CanSpeakPreparedComment(
        original,
        "FIREFOX.EXE",
        true,
        new MediaSnapshot("Cochise", "Audioslave", "Optional album"),
        true,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
        true));
    AssertFalse(AiCommentSafety.CanSpeakPreparedComment(
        original,
        "firefox.exe",
        true,
        new MediaSnapshot("Know Your Enemy", "Rage Against The Machine", null),
        true,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
        true));
    AssertFalse(AiCommentSafety.CanSpeakPreparedComment(
        original,
        "firefox.exe",
        true,
        original,
        true,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused,
        true));
}

static void RadioConfigurationValidatesBounds()
{
    RadioBehaviorConfiguration.Default.Validate();

    bool minimumRejected = false;
    bool reversedRejected = false;

    try
    {
        (RadioBehaviorConfiguration.Default with { MinimumTransitionsBetweenComments = 0 }).Validate();
    }
    catch (InvalidOperationException)
    {
        minimumRejected = true;
    }

    try
    {
        (RadioBehaviorConfiguration.Default with
        {
            MinimumTransitionsBetweenComments = 5,
            MaximumTransitionsBetweenComments = 2,
        }).Validate();
    }
    catch (InvalidOperationException)
    {
        reversedRejected = true;
    }

    AssertTrue(minimumRejected);
    AssertTrue(reversedRejected);
}

static void RadioScheduleUsesInclusiveBounds()
{
    var targets = new FakeTransitionTargetSource(2, 5);
    var state = new RadioBehaviorState(RadioBehaviorConfiguration.Default, targets);
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);

    AssertEqual(2, state.ScheduledThreshold);
    AssertEqual((2, 5), targets.Requests[0]);

    RadioTrackObservation preparation = state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));
    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));
    RadioTrackObservation due = state.ObserveTrack(Track("C", "Artist C"), now.AddSeconds(20));
    AssertTrue(due.Delivery is not null);
    AssertTrue(state.ResolveOpportunity(due.Delivery!.OpportunityId, true, "A sufficiently long accepted comment for history."));
    AssertEqual(5, state.ScheduledThreshold);
    AssertEqual((2, 5), targets.Requests[1]);
}

static void RadioTransitionIncrementsOnce()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);

    RadioTrackObservation result = state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));

    AssertEqual(RadioTrackObservationKind.Transition, result.Kind);
    AssertEqual(1, state.TransitionCounter);
    AssertEqual(1, state.TotalObservedTransitions);
}

static void RadioDuplicateDoesNotIncrement()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));

    RadioTrackObservation duplicate = state.ObserveTrack(
        new MediaSnapshot("  b  ", "ARTIST   B", "Album arrived later"),
        now.AddSeconds(11));

    AssertEqual(RadioTrackObservationKind.Duplicate, duplicate.Kind);
    AssertEqual(1, state.TransitionCounter);
    AssertEqual(1, state.TotalObservedTransitions);
}

static void RadioCompletionChoosesFreshInterval()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2, 4));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    RadioTrackObservation preparation = state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));
    AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));
    RadioTrackObservation due = state.ObserveTrack(Track("C", "Artist C"), now.AddSeconds(20));

    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(due.Delivery is not null);
    AssertTrue(state.ResolveOpportunity(
        due.Delivery!.OpportunityId,
        announcementCompleted: true,
        "This accepted AI comment is long enough to represent a completed automatic announcement."));
    AssertEqual(0, state.TransitionCounter);
    AssertEqual(4, state.ScheduledThreshold);
    AssertEqual(1, state.RecentAcceptedAiComments.Count);
}

static async Task RadioSilentModePreventsPipelineWork()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var provider = new FakeRadioCommentProvider();
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity) with { SilentMode = true });
    var pipeline = new RadioOpportunityPipeline(
        configuration,
        provider,
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(0, provider.Calls);
    AssertEqual(0, host.PrepareCalls);
    AssertEqual(0, host.AnnouncementCalls);
}

static void RadioLeavingSilentChoosesFreshInterval()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2, 5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    state.SetSilentMode(true);
    state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));

    RadioModeChange enabled = state.SetSilentMode(false);

    AssertFalse(enabled.SilentMode);
    AssertEqual(0, state.TransitionCounter);
    AssertEqual(5, state.ScheduledThreshold);
}

static async Task RadioMinimumAgeDelaysAnnouncement()
{
    DateTimeOffset started = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    var clock = new FakeRadioClock(started.AddSeconds(1));
    RadioCommentOpportunity opportunity = TestRadioOpportunity(started);
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity))
    {
        Clock = clock,
    };
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        clock,
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Completed, result.Status);
    AssertEqual(1, clock.Delays.Count);
    AssertEqual(TimeSpan.FromSeconds(7), clock.Delays[0]);
    AssertEqual(started.AddSeconds(8), host.AnnouncementObservedAt);
}

static async Task RadioTrackChangeAbandonsPipeline()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    var provider = new FakeRadioCommentProvider
    {
        OnSelect = () => host.Snapshot = host.Snapshot with
        {
            CurrentMedia = Track("Unexpected New Track", "Different Artist"),
        },
    };
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        provider,
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(1, provider.Calls);
    AssertEqual(0, host.PrepareCalls);
    AssertEqual(0, host.AnnouncementCalls);
}

static async Task RadioTrackChangeDuringWaitAbandonsPipeline()
{
    DateTimeOffset started = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(started);
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    var clock = new FakeRadioClock(started.AddSeconds(1))
    {
        OnDelay = () => host.Snapshot = host.Snapshot with
        {
            CurrentMedia = Track("A New Track During The Wait", "Different Artist"),
        },
    };
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        clock,
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(1, clock.Delays.Count);
    AssertEqual(1, host.PrepareCalls);
    AssertEqual(0, host.AnnouncementCalls);
    AssertEqual(1, host.PreparedDisposeCalls);
}

static void RadioPendingChangeCreatesNoQueue()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2, 5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    RadioTrackObservation preparation = state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));
    AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));
    RadioTrackObservation due = state.ObserveTrack(Track("C", "Artist C"), now.AddSeconds(20));
    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(due.Delivery is not null);

    RadioTrackObservation changedAgain = state.ObserveTrack(
        Track("D", "Artist D"),
        now.AddSeconds(30));

    AssertTrue(changedAgain.PendingOpportunityInvalidated);
    AssertTrue(changedAgain.PreparationOpportunity is null);
    AssertTrue(changedAgain.Delivery is null);
    AssertTrue(state.GetRuntimeState().PendingOpportunityId is null);
    AssertEqual(0, state.TransitionCounter);
    AssertEqual(5, state.ScheduledThreshold);
}

static async Task RadioPausedPlaybackBlocksPipeline()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var provider = new FakeRadioCommentProvider();
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity) with
    {
        PlaybackStatus = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused,
    });
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        provider,
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Abandoned, result.Status);
    AssertEqual(0, provider.Calls);
    AssertEqual(0, host.AnnouncementCalls);
}

static void RadioTargetSelectionMustBeExact()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    RadioOpportunitySnapshot valid = ValidRadioSnapshot(opportunity);

    AssertFalse(RadioOpportunitySafety.Evaluate(
        opportunity,
        valid with { MatchingFirefoxSessionCount = 0 },
        now,
        TimeSpan.FromSeconds(8),
        true).CanProceed);
    AssertFalse(RadioOpportunitySafety.Evaluate(
        opportunity,
        valid with { MatchingFirefoxSessionCount = 2 },
        now,
        TimeSpan.FromSeconds(8),
        true).CanProceed);
    AssertFalse(RadioOpportunitySafety.Evaluate(
        opportunity,
        valid with { SameSelectedSession = false },
        now,
        TimeSpan.FromSeconds(8),
        true).CanProceed);
    AssertFalse(RadioOpportunitySafety.Evaluate(
        opportunity,
        valid with { SourceId = "chrome.exe" },
        now,
        TimeSpan.FromSeconds(8),
        true).CanProceed);
}

static void RadioTrackHistoryIsBounded()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default with
    {
        MinimumTransitionsBetweenComments = 10,
        MaximumTransitionsBetweenComments = 10,
        RecentTrackLimit = 3,
    };
    var state = new RadioBehaviorState(configuration, new FakeTransitionTargetSource(10));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);

    for (int index = 1; index <= 5; index++)
    {
        state.ObserveTrack(Track($"Track {index}", $"Artist {index}"), now.AddSeconds(index));
    }

    AssertEqual(3, state.RecentTracks.Count);
    AssertEqual("Track 3", state.RecentTracks[0].Title!);
    AssertEqual("Track 5", state.RecentTracks[2].Title!);
}

static void RadioCommentHistoryIsBounded()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default with
    {
        MinimumTransitionsBetweenComments = 2,
        MaximumTransitionsBetweenComments = 2,
        RecentCommentLimit = 2,
    };
    var state = new RadioBehaviorState(configuration, new FakeTransitionTargetSource(2, 2, 2, 2));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);

    for (int index = 1; index <= 3; index++)
    {
        RadioTrackObservation preparation = state.ObserveTrack(
            Track($"Track {index}A", $"Artist {index}A"),
            now.AddSeconds(index * 2));
        AssertTrue(preparation.PreparationOpportunity is not null);
        AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));
        RadioTrackObservation due = state.ObserveTrack(
            Track($"Track {index}B", $"Artist {index}B"),
            now.AddSeconds((index * 2) + 1));
        state.ResolveOpportunity(due.Delivery!.OpportunityId, true, $"Accepted AI comment number {index}");
    }

    AssertEqual(2, state.RecentAcceptedAiComments.Count);
    AssertEqual("Accepted AI comment number 2", state.RecentAcceptedAiComments[0]);
    AssertEqual("Accepted AI comment number 3", state.RecentAcceptedAiComments[1]);
}

static void RadioClearHistoryIsHarmless()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default with
    {
        MinimumTransitionsBetweenComments = 2,
        MaximumTransitionsBetweenComments = 2,
    };
    var state = new RadioBehaviorState(configuration, new FakeTransitionTargetSource(2, 2));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    state.Arm(Track("A", "Artist A"), now);
    RadioTrackObservation due = state.ObserveTrack(Track("B", "Artist B"), now.AddSeconds(10));
    AssertTrue(due.PreparationOpportunity is not null);

    bool invalidated = state.ClearRecentHistory();

    AssertTrue(invalidated);
    AssertEqual(0, state.RecentTracks.Count);
    AssertEqual(0, state.RecentAcceptedAiComments.Count);
    AssertEqual(0, state.TransitionCounter);
    AssertTrue(state.GetRuntimeState().PendingOpportunityId is null);
}

static void RadioRepeatedArtistUsesExactNormalization()
{
    var history = new RadioHistory(10, 6);
    history.AddTrack(Track("One", "  Bee   Gees "));
    history.AddTrack(Track("Two", "bee gees"));

    IReadOnlyList<string> observations = history.BuildTrustedObservations(Track("Two", "BEE GEES"));

    AssertEqual(1, observations.Count);
    AssertContains(observations[0], "appeared 2 times");
}

static void RadioArtistMatchingIsNotFuzzy()
{
    var history = new RadioHistory(10, 6);
    history.AddTrack(Track("One", "Artist"));
    history.AddTrack(Track("Two", "Artist feat. X"));

    IReadOnlyList<string> observations = history.BuildTrustedObservations(Track("Two", "Artist feat. X"));

    AssertEqual(0, observations.Count);
}

static void RadioPromptIncludesBoundedContext()
{
    DateTimeOffset started = DateTimeOffset.Parse("2026-08-07T12:00:00Z");
    var context = new RadioCommentContext(
        Track("Previous Title", "Previous Artist"),
        Track("Current Title", "Current Artist"),
        started,
        ["The exact artist string 'Current Artist' has appeared 2 times in the last 6 observed tracks."],
        ["A recent opening that should not be copied exactly."]);

    CommentPrompt prompt = CommentPromptFactory.CreateForRadio(context, HostPersonaConfiguration.Default);
    const string prefix = "Automatic radio context JSON: ";
    string json = prompt.MetadataPrompt[prefix.Length..];
    json = json[..json.IndexOf('\n')];
    using JsonDocument parsed = JsonDocument.Parse(json);

    AssertEqual("Previous Title", parsed.RootElement.GetProperty("previousTitle").GetString()!);
    AssertEqual("Current Artist", parsed.RootElement.GetProperty("currentArtist").GetString()!);
    AssertEqual(1, parsed.RootElement.GetProperty("trustedListeningObservations").GetArrayLength());
    AssertEqual(1, parsed.RootElement.GetProperty("recentAcceptedComments").GetArrayLength());
    AssertEqual(6, parsed.RootElement.EnumerateObject().Count());
    AssertContains(prompt.MetadataPrompt, "Comment primarily on the previous track");
    AssertContains(prompt.SystemInstruction, "Recent accepted comments are style-avoidance examples");
    AssertFalse(prompt.MetadataPrompt.Contains("album", StringComparison.OrdinalIgnoreCase));
}

static void RadioPromptPreservesUnicode()
{
    var context = new RadioCommentContext(
        Track("Кукушка", "Кино"),
        Track("Дай Дай", "Русский Оркестр"),
        DateTimeOffset.Parse("2026-08-07T12:00:00Z"),
        [],
        []);
    CommentPrompt prompt = CommentPromptFactory.CreateForRadio(context, HostPersonaConfiguration.Default);
    const string prefix = "Automatic radio context JSON: ";
    string json = prompt.MetadataPrompt[prefix.Length..];
    json = json[..json.IndexOf('\n')];
    using JsonDocument parsed = JsonDocument.Parse(json);

    AssertEqual("Кукушка", parsed.RootElement.GetProperty("previousTitle").GetString()!);
    AssertEqual("Кино", parsed.RootElement.GetProperty("previousArtist").GetString()!);
    AssertEqual("Дай Дай", parsed.RootElement.GetProperty("currentTitle").GetString()!);
}

static async Task RadioDuplicateAiCommentUsesFallback()
{
    const string generated = "With a title like Cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.";
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    var context = new RadioCommentContext(
        TestMedia(),
        Track("Next Track", "Next Artist"),
        DateTimeOffset.Now,
        [],
        ["  with A TITLE like cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.  "]);

    CommentSelection result = await service.SelectRadioCommentAsync(context, true, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual("That was Cochise by Audioslave.", result.Text);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertTrue(AiCommentService.IsExactRecentComment(generated, context.RecentAcceptedAiComments));
}

static async Task RadioNoAiBypassesOllama()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    var context = new RadioCommentContext(
        TestMedia(),
        Track("Next Track", "Next Artist"),
        DateTimeOffset.Now,
        [],
        []);

    CommentSelection result = await service.SelectRadioCommentAsync(context, false, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(0, generator.GenerateCalls);
    AssertEqual(0, generator.UnloadCalls);
    AssertEqual(0, generator.PsCalls);
}

static async Task RadioTimeoutUsesFallback()
{
    var generator = new FakeCommentGenerator
    {
        GenerationResult = new OllamaGenerationResult(
            OllamaGenerationStatus.TimedOut,
            null,
            "Timed out",
            null),
    };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    var context = new RadioCommentContext(
        TestMedia(),
        Track("Next Track", "Next Artist"),
        DateTimeOffset.Now,
        [],
        []);

    CommentSelection result = await service.SelectRadioCommentAsync(context, true, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual("That was Cochise by Audioslave.", result.Text);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static async Task RadioSpeechLimitUsesProtectedRecovery()
{
    RadioBehaviorConfiguration configuration = RadioBehaviorConfiguration.Default;
    var target = new FakePlaybackTarget();
    var speech = new FakePreparedSpeech(new SpeechPlaybackResult(SpeechPlaybackStatus.TimedOut));
    var log = new FakeLog();
    AnnouncementSequenceTimings timings = FastTimings() with
    {
        SpeechTimeout = configuration.MaximumSpeechDuration,
    };
    var sequence = new AnnouncementControlSequence(log, timings);

    AnnouncementSequenceResult result = await sequence.RunAsync(
        target,
        speech,
        false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.PlaybackRestored);
    AssertEqual(1, target.PauseCalls);
    AssertEqual(1, target.PlayCalls);
    AssertEqual(configuration.MaximumSpeechDuration, speech.LastTimeout);
    AssertTrue(log.Contains("speech time limit reached"));
}

static async Task RadioPipelinePassesSpeechLimit()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-07T12:00:20Z");
    RadioCommentOpportunity opportunity = TestRadioOpportunity(now.AddSeconds(-20));
    var host = new FakeRadioOpportunityHost(ValidRadioSnapshot(opportunity));
    var pipeline = new RadioOpportunityPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeRadioCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());

    RadioOpportunityResult result = await pipeline.RunAsync(opportunity, true, CancellationToken.None);

    AssertEqual(RadioOpportunityResultStatus.Completed, result.Status);
    AssertEqual(TimeSpan.FromSeconds(25), host.MaximumSpeechDuration);
}

static void TransitionSchedulePreparesOneAhead()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(3));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:00Z");
    state.Arm(Track("Initial", "Artist 0"), now);

    RadioTrackObservation first = state.ObserveTrack(Track("Track A1", "Artist 1"), now.AddSeconds(10));
    RadioTrackObservation preparation = state.ObserveTrack(Track("Track A", "Artist A"), now.AddSeconds(20));
    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));
    RadioTrackObservation delivery = state.ObserveTrack(Track("Track B", "Artist B"), now.AddSeconds(30));

    AssertTrue(first.PreparationOpportunity is null);
    AssertEqual("Track A", preparation.PreparationOpportunity!.Context.SubjectTrack.Title!);
    AssertTrue(delivery.Delivery is not null);
    AssertEqual(preparation.PreparationOpportunity.Id, delivery.Delivery!.OpportunityId);
    AssertEqual("Track A", delivery.Delivery.CommentedTrack.Title!);
    AssertEqual("Track B", delivery.Delivery.NewTrack.Title!);
}

static void ChromeCollectionChangesPreserveSelectedFirefoxWork()
{
    var selectedWrapper = new object();
    FirefoxSessionContinuityEvidence selected = EligibleFirefoxEvidence(
        "Superman",
        "Goldfinger");
    FirefoxMediaSessionChange<object> collection = FirefoxMediaSessionLifecycle.Evaluate<object>(
        true,
        [selectedWrapper]);

    FirefoxSessionContinuityDecision chromeAppeared = FirefoxSessionContinuity.Evaluate(
        true,
        selected,
        selected);
    FirefoxSessionContinuityDecision chromeMetadataChanged = FirefoxSessionContinuity.Evaluate(
        true,
        selected,
        selected);
    FirefoxSessionContinuityDecision chromeDisappeared = FirefoxSessionContinuity.Evaluate(
        true,
        selected,
        selected);

    AssertEqual(FirefoxMediaSessionChangeStatus.SingleFirefoxCandidate, collection.Status);
    AssertSame(selectedWrapper, collection.Candidate!);
    AssertEqual(FirefoxSessionContinuityStatus.SelectedWrapperStillValid, chromeAppeared.Status);
    AssertEqual(FirefoxSessionContinuityStatus.SelectedWrapperStillValid, chromeMetadataChanged.Status);
    AssertEqual(FirefoxSessionContinuityStatus.SelectedWrapperStillValid, chromeDisappeared.Status);
    AssertTrue(chromeAppeared.PreservesPendingAndActiveWork);
    AssertTrue(chromeMetadataChanged.PreservesPendingAndActiveWork);
    AssertTrue(chromeDisappeared.PreservesPendingAndActiveWork);
    AssertFalse(chromeAppeared.ShouldRefreshWrapper);
}

static void RefreshedFirefoxWrapperPreservesLogicalContinuity()
{
    var selectedWrapper = new object();
    var refreshedWrapper = new object();
    FirefoxSessionContinuityEvidence selected = EligibleFirefoxEvidence(
        "Superman",
        "Goldfinger");
    FirefoxSessionContinuityEvidence refreshed = EligibleFirefoxEvidence(
        "  superman  ",
        "GOLDFINGER");
    FirefoxMediaSessionChange<object> collection = FirefoxMediaSessionLifecycle.Evaluate<object>(
        true,
        [refreshedWrapper]);

    FirefoxSessionContinuityDecision decision = FirefoxSessionContinuity.Evaluate(
        ReferenceEquals(selectedWrapper, refreshedWrapper),
        selected,
        refreshed);
    FirefoxSessionContinuityDecision candidateNotReady = FirefoxSessionContinuity.Evaluate(
        false,
        selected,
        refreshed with { IsPlaying = false });

    AssertEqual(FirefoxMediaSessionChangeStatus.SingleFirefoxCandidate, collection.Status);
    AssertSame(refreshedWrapper, collection.Candidate!);
    AssertEqual(
        FirefoxSessionContinuityStatus.RefreshedWrapperSameLogicalSession,
        decision.Status);
    AssertTrue(decision.PreservesPendingAndActiveWork);
    AssertTrue(decision.ShouldRefreshWrapper);
    AssertEqual(
        FirefoxSessionContinuityStatus.SelectedWrapperRetainedBecauseCandidateIsIneligible,
        candidateNotReady.Status);
    AssertTrue(candidateNotReady.PreservesPendingAndActiveWork);
    AssertFalse(candidateNotReady.ShouldRefreshWrapper);
}

static void ChromeWrapperChurnPreservesPendingSchedule()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(3));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-10T12:00:00Z");
    state.Arm(Track("Initial", "Artist 0"), now);
    state.ObserveTrack(Track("Track A1", "Artist 1"), now.AddSeconds(10));
    RadioTrackObservation preparation = state.ObserveTrack(
        Track("Track A", "Artist A"),
        now.AddSeconds(20));
    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));

    RadioRuntimeState before = state.GetRuntimeState();
    int counterBefore = state.TransitionCounter;
    int scheduleBefore = state.ScheduledThreshold;
    FirefoxSessionContinuityEvidence evidence = EligibleFirefoxEvidence(
        before.CurrentTrack.Title!,
        before.CurrentTrack.Artist!);

    for (int index = 0; index < 4; index++)
    {
        FirefoxSessionContinuityDecision wrapperChurn = FirefoxSessionContinuity.Evaluate(
            false,
            evidence,
            evidence);
        AssertTrue(wrapperChurn.PreservesPendingAndActiveWork);
        AssertTrue(wrapperChurn.ShouldRefreshWrapper);
    }

    RadioRuntimeState after = state.GetRuntimeState();
    AssertEqual(before.PendingOpportunityId, after.PendingOpportunityId);
    AssertEqual(before.CurrentTrack, after.CurrentTrack);
    AssertEqual(before.CurrentTrackStartedAt, after.CurrentTrackStartedAt);
    AssertEqual(counterBefore, state.TransitionCounter);
    AssertEqual(scheduleBefore, state.ScheduledThreshold);
}

static void InvalidFirefoxSelectionRequiresReacquisition()
{
    FirefoxSessionContinuityEvidence eligible = EligibleFirefoxEvidence(
        "Don't Cry",
        "Guns N' Roses");
    var paused = eligible with { IsPlaying = false };
    var unreadable = new FirefoxSessionContinuityEvidence(
        false,
        false,
        false,
        MediaSnapshot.Empty);

    FirefoxSessionContinuityDecision pausedDecision = FirefoxSessionContinuity.Evaluate(
        false,
        paused,
        eligible);
    FirefoxSessionContinuityDecision unreadableDecision = FirefoxSessionContinuity.Evaluate(
        false,
        unreadable,
        eligible);

    AssertEqual(
        FirefoxSessionContinuityStatus.ReacquisitionRequired,
        pausedDecision.Status);
    AssertEqual(
        FirefoxSessionContinuityStatus.ReacquisitionRequired,
        unreadableDecision.Status);
    AssertFalse(pausedDecision.PreservesPendingAndActiveWork);
    AssertFalse(unreadableDecision.PreservesPendingAndActiveWork);
}

static void DifferentFirefoxSessionRequiresReacquisition()
{
    FirefoxSessionContinuityEvidence selected = EligibleFirefoxEvidence(
        "Don't Cry",
        "Guns N' Roses");
    FirefoxSessionContinuityEvidence different = EligibleFirefoxEvidence(
        "Sons of Plunder",
        "Disturbed");

    FirefoxSessionContinuityDecision decision = FirefoxSessionContinuity.Evaluate(
        false,
        selected,
        different);

    AssertEqual(
        FirefoxSessionContinuityStatus.ReacquisitionRequired,
        decision.Status);
    AssertFalse(decision.PreservesPendingAndActiveWork);
    AssertFalse(decision.ShouldRefreshWrapper);
}

static void FirefoxMediaRebindRefusesUnsafeCandidates()
{
    var onlyCandidate = new object();
    var firstCandidate = new object();
    var secondCandidate = new object();

    FirefoxMediaSessionChange<object> unavailable =
        FirefoxMediaSessionLifecycle.Evaluate<object>(false, []);
    FirefoxMediaSessionChange<object> absent =
        FirefoxMediaSessionLifecycle.Evaluate<object>(true, []);
    FirefoxMediaSessionChange<object> single =
        FirefoxMediaSessionLifecycle.Evaluate<object>(true, [onlyCandidate]);
    FirefoxMediaSessionChange<object> ambiguous =
        FirefoxMediaSessionLifecycle.Evaluate<object>(true, [firstCandidate, secondCandidate]);

    AssertEqual(FirefoxMediaSessionChangeStatus.SessionListUnavailable, unavailable.Status);
    AssertEqual(FirefoxMediaSessionChangeStatus.NoFirefoxCandidate, absent.Status);
    AssertEqual(FirefoxMediaSessionChangeStatus.SingleFirefoxCandidate, single.Status);
    AssertEqual(FirefoxMediaSessionChangeStatus.AmbiguousFirefoxCandidates, ambiguous.Status);
    AssertTrue(unavailable.Candidate is null);
    AssertTrue(absent.Candidate is null);
    AssertSame(onlyCandidate, single.Candidate!);
    AssertTrue(ambiguous.Candidate is null);
    AssertEqual(2, ambiguous.MatchingFirefoxSessionCount);
}
static async Task FirefoxRebindWaitsForPendingDiscard()
{
    var events = new List<string>();
    var pendingCompletion = new TaskCompletionSource<bool>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    Task discard = FirefoxSessionRebindSafety.DiscardPendingBeforeRebindingAsync(
        () => events.Add("cancel-and-discard"),
        async () =>
        {
            events.Add("wait-for-pipeline");
            await pendingCompletion.Task;
            events.Add("pipeline-drained");
        });

    AssertFalse(discard.IsCompleted);
    AssertEqual(2, events.Count);
    AssertEqual("cancel-and-discard", events[0]);
    AssertEqual("wait-for-pipeline", events[1]);

    pendingCompletion.SetResult(true);
    await discard;
    events.Add("rebind-allowed");

    AssertEqual(4, events.Count);
    AssertEqual("pipeline-drained", events[2]);
    AssertEqual("rebind-allowed", events[3]);
}

static void SessionRebindResetsPendingOpportunity()
{
    var targets = new FakeTransitionTargetSource(2, 5);
    var state = new RadioBehaviorState(RadioBehaviorConfiguration.Default, targets);
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-09T12:00:00Z");
    state.Arm(Track("Initial", "Artist 0"), now);
    RadioTrackObservation preparation = state.ObserveTrack(
        Track("Prepared", "Artist A"),
        now.AddSeconds(10));
    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));

    bool invalidated = state.RebindCurrentTrack(
        Track("Новый трек", "Новый артист"),
        now.AddSeconds(20));
    RadioRuntimeState runtime = state.GetRuntimeState();

    AssertTrue(invalidated);
    AssertTrue(runtime.PendingOpportunityId is null);
    AssertEqual(0, state.TransitionCounter);
    AssertEqual(5, state.ScheduledThreshold);
    AssertEqual("Новый трек", runtime.CurrentTrack.Title!);
    AssertEqual("Новый артист", runtime.CurrentTrack.Artist!);
    AssertEqual(now.AddSeconds(20), runtime.CurrentTrackStartedAt);
}

static void UnfinishedTransitionPreparationIsAbandoned()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2, 5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:00Z");
    state.Arm(Track("Initial", "Artist 0"), now);

    RadioTrackObservation preparation = state.ObserveTrack(Track("Track A", "Artist A"), now.AddSeconds(10));
    RadioTrackObservation transition = state.ObserveTrack(Track("Track B", "Artist B"), now.AddSeconds(20));

    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(transition.PendingOpportunityInvalidated);
    AssertTrue(transition.PreparationMissedTransition);
    AssertTrue(transition.Delivery is null);
    AssertTrue(state.GetRuntimeState().PendingOpportunityId is null);
}

static void TransitionPromptContainsTrackAOnly()
{
    var context = new RadioPreparationContext(
        Track("Track A", "Artist A"),
        DateTimeOffset.Parse("2026-08-08T12:00:00Z"),
        ["The exact artist string 'Artist A' has appeared 2 times in the last 6 observed tracks."],
        ["A recent accepted comment that should not be echoed."]);

    CommentPrompt prompt = CommentPromptFactory.CreateForPreparedTransition(
        context,
        HostPersonaConfiguration.Default);
    const string prefix = "Prepared Track-A context JSON: ";
    string json = prompt.MetadataPrompt[prefix.Length..];
    json = json[..json.IndexOf('\n')];
    using JsonDocument parsed = JsonDocument.Parse(json);

    AssertEqual("Track A", parsed.RootElement.GetProperty("title").GetString()!);
    AssertEqual("Artist A", parsed.RootElement.GetProperty("artist").GetString()!);
    AssertEqual(1, parsed.RootElement.GetProperty("trustedListeningObservations").GetArrayLength());
    AssertEqual(1, parsed.RootElement.GetProperty("recentAcceptedComments").GetArrayLength());
    AssertEqual(4, parsed.RootElement.EnumerateObject().Count());
    AssertContains(prompt.MetadataPrompt, "No future Track B is known");
    AssertFalse(prompt.MetadataPrompt.Contains("previousTitle", StringComparison.Ordinal));
    AssertFalse(prompt.MetadataPrompt.Contains("currentTitle", StringComparison.Ordinal));
    AssertFalse(prompt.MetadataPrompt.Contains("album", StringComparison.OrdinalIgnoreCase));
}

static async Task TransitionPreparationPrecedesDelivery()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:20Z");
    RadioPreparationOpportunity opportunity = TestTransitionPreparation(now.AddSeconds(-20));
    var events = new List<string>();
    var provider = new FakeTransitionCommentProvider(events);
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(opportunity), events);
    var pipeline = new TransitionRadioPipeline(
        RadioBehaviorConfiguration.Default,
        provider,
        host,
        new FakeRadioClock(now),
        new FakeLog());

    TransitionPreparationResult result = await pipeline.PrepareAsync(
        opportunity,
        true,
        CancellationToken.None);

    AssertEqual(TransitionPreparationStatus.Ready, result.Status);
    AssertEqual(2, events.Count);
    AssertEqual("ai", events[0]);
    AssertEqual("tts", events[1]);
    AssertEqual(0, host.DuckedDeliveryCalls);
    AssertEqual(0, host.PauseCalls);
    AssertEqual(0, host.PlayCalls);
    result.Prepared!.Dispose();
}

static async Task PreparedSpeechWaitsForTransition()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:20Z");
    RadioPreparationOpportunity opportunity = TestTransitionPreparation(now.AddSeconds(-20));
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(opportunity));
    var pipeline = new TransitionRadioPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());
    TransitionPreparationResult preparation = await pipeline.PrepareAsync(
        opportunity,
        true,
        CancellationToken.None);

    AssertEqual(0, host.DuckedDeliveryCalls);
    AssertEqual(0, host.Speech.SpeakCalls);

    RadioTransitionDelivery transition = TestTransitionDelivery(opportunity, now);
    host.Snapshot = ValidTransitionSnapshot(opportunity, transition.NewTrack);
    TransitionDeliveryResult delivery = await pipeline.DeliverAsync(
        preparation.Prepared!,
        transition,
        false,
        CancellationToken.None);

    AssertEqual(TransitionDeliveryStatus.Completed, delivery.Status);
    AssertEqual(1, host.DuckedDeliveryCalls);
    AssertEqual(1, host.Speech.SpeakCalls);
    preparation.Prepared!.Dispose();
}

static async Task TransitionDeliveryUsesDuckingOnly()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:20Z");
    RadioPreparationOpportunity opportunity = TestTransitionPreparation(now.AddSeconds(-20));
    RadioTransitionDelivery transition = TestTransitionDelivery(opportunity, now);
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(opportunity, transition.NewTrack));
    var pipeline = new TransitionRadioPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());
    using PreparedTransitionOpportunity prepared = FakePreparedTransition(opportunity, now);

    TransitionDeliveryResult result = await pipeline.DeliverAsync(
        prepared,
        transition,
        false,
        CancellationToken.None);

    AssertEqual(TransitionDeliveryStatus.Completed, result.Status);
    AssertEqual(1, host.DuckedDeliveryCalls);
    AssertEqual(0, host.PauseCalls);
    AssertEqual(0, host.PlayCalls);
}

static async Task TransitionDeliveryCapturesCurrentVolume()
{
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.8f);
    target.ChangeExternally(0.5f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult result = await sequence.RunWithProtectedActionAsync(
        target,
        _ => Task.CompletedTask,
        false,
        CancellationToken.None);

    AssertTrue(result.TestPassed);
    AssertApproximately(0.125f, target.MinimumRequestedVolume);
    AssertApproximately(0.5f, target.VolumeScalar);
}

static async Task TransitionSpeechSuccessRestoresVolume()
{
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.6f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());
    int speechCalls = 0;

    AudioDuckingResult result = await sequence.RunWithProtectedActionAsync(
        target,
        _ =>
        {
            speechCalls++;
            return Task.CompletedTask;
        },
        false,
        CancellationToken.None);

    AssertTrue(result.TestPassed);
    AssertEqual(1, speechCalls);
    AssertApproximately(0.6f, target.VolumeScalar);
}

static async Task TransitionSpeechFailureRestoresVolume()
{
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.6f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult result = await sequence.RunWithProtectedActionAsync(
        target,
        _ => Task.FromException(new InvalidOperationException("simulated speech failure")),
        false,
        CancellationToken.None);

    AssertFalse(result.TestPassed);
    AssertTrue(result.RestorationVerified);
    AssertApproximately(0.6f, target.VolumeScalar);
}

static async Task TransitionSpeechCancellationRestoresVolume()
{
    using var cancellation = new CancellationTokenSource();
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.6f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());

    AudioDuckingResult result = await sequence.RunWithProtectedActionAsync(
        target,
        _ =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        },
        false,
        cancellation.Token);

    AssertTrue(result.Cancelled);
    AssertTrue(result.RestorationVerified);
    AssertApproximately(0.6f, target.VolumeScalar);
}

static async Task TransitionSimulatedFailureRestoresVolume()
{
    using var target = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.6f);
    var sequence = new AudioDuckingSequence(new FakeLog(), FastAudioDuckingConfiguration());
    int actionCalls = 0;

    AudioDuckingResult result = await sequence.RunWithProtectedActionAsync(
        target,
        _ =>
        {
            actionCalls++;
            return Task.CompletedTask;
        },
        true,
        CancellationToken.None);

    AssertTrue(result.ExpectedFailureRecoveryPassed);
    AssertEqual(0, actionCalls);
    AssertApproximately(0.6f, target.VolumeScalar);
}

static async Task TransitionAmbiguousTargetCausesSilence()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:20Z");
    RadioPreparationOpportunity opportunity = TestTransitionPreparation(now.AddSeconds(-20));
    RadioTransitionDelivery transition = TestTransitionDelivery(opportunity, now);
    RadioOpportunitySnapshot ambiguous = ValidTransitionSnapshot(opportunity, transition.NewTrack) with
    {
        MatchingFirefoxSessionCount = 2,
        SameSelectedSession = false,
    };
    var host = new FakeTransitionRadioHost(ambiguous);
    var pipeline = new TransitionRadioPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());
    using PreparedTransitionOpportunity prepared = FakePreparedTransition(opportunity, now);

    TransitionDeliveryResult result = await pipeline.DeliverAsync(
        prepared,
        transition,
        false,
        CancellationToken.None);

    AssertEqual(TransitionDeliveryStatus.Abandoned, result.Status);
    AssertEqual(0, host.DuckedDeliveryCalls);
    AssertEqual(0, host.Speech.SpeakCalls);
}

static async Task TransitionAmbiguousAudioTargetCausesSilence()
{
    using var first = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.6f);
    using var second = new FakeAudioSessionTarget("firefox.exe", 21, "FIREFOX02", 0.7f);
    var provider = new FakeAudioSessionEnumerationProvider(
        AudioDescriptor(first),
        AudioDescriptor(second));
    var ducker = new WindowsTransitionAudioDucker(
        new FakeLog(),
        FastAudioDuckingConfiguration(),
        provider);
    int actionCalls = 0;

    TransitionDuckingResult result = await ducker.RunAsync(
        _ =>
        {
            actionCalls++;
            return Task.CompletedTask;
        },
        false,
        CancellationToken.None);

    AssertEqual(TransitionDuckingStatus.Refused, result.Status);
    AssertEqual(0, actionCalls);
    AssertEqual(0, first.SetVolumeCalls);
    AssertEqual(0, second.SetVolumeCalls);
}

static async Task TransitionDuckingLeavesChromeUnchanged()
{
    using var firefox = new FakeAudioSessionTarget("firefox", 20, "FIREFOX01", 0.5f);
    using var chrome = new FakeAudioSessionTarget("chrome", 30, "CHROME01", 0.8f);
    var provider = new FakeAudioSessionEnumerationProvider(
        AudioDescriptor(chrome),
        AudioDescriptor(firefox));
    var ducker = new WindowsTransitionAudioDucker(
        new FakeLog(),
        FastAudioDuckingConfiguration(),
        provider);
    int actionCalls = 0;

    TransitionDuckingResult result = await ducker.RunAsync(
        _ =>
        {
            actionCalls++;
            return Task.CompletedTask;
        },
        false,
        CancellationToken.None);

    AssertEqual(TransitionDuckingStatus.Completed, result.Status);
    AssertEqual(1, actionCalls);
    AssertApproximately(0.5f, firefox.VolumeScalar);
    AssertTrue(firefox.SetVolumeCalls > 0);
    AssertApproximately(0.8f, chrome.VolumeScalar);
    AssertEqual(0, chrome.SetVolumeCalls);
    AssertEqual(0, chrome.SetMuteCalls);
}

static async Task TransitionAudioTargetRetriesTransientInactiveFirefox()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-09T12:00:00Z");
    var clock = new FakeRadioClock(now);
    var inactiveFirefox = new FakeAudioSessionTarget(
        "firefox",
        20,
        "FIREFOX-INACTIVE",
        0.5f,
        activity: AudioSessionActivity.Inactive);
    var firstChrome = new FakeAudioSessionTarget("chrome", 30, "CHROME-01", 0.8f);
    var recoveredFirefox = new FakeAudioSessionTarget(
        "firefox.exe",
        20,
        "FIREFOX-RECOVERED",
        0.6f);
    var secondChrome = new FakeAudioSessionTarget("chrome", 30, "CHROME-02", 0.8f);
    var provider = new SequencedAudioSessionEnumerationProvider(
        () => FakeAudioEnumeration(inactiveFirefox, firstChrome),
        () => FakeAudioEnumeration(recoveredFirefox, secondChrome));
    var ducker = new WindowsTransitionAudioDucker(
        new FakeLog(),
        FastAudioDuckingConfiguration(),
        provider,
        clock);
    int actionCalls = 0;

    TransitionDuckingResult result = await ducker.RunAsync(
        _ =>
        {
            actionCalls++;
            return Task.CompletedTask;
        },
        false,
        CancellationToken.None,
        now.AddSeconds(2));

    AssertEqual(TransitionDuckingStatus.Completed, result.Status);
    AssertEqual(2, provider.EnumerationCalls);
    AssertEqual(1, clock.Delays.Count);
    AssertEqual(TimeSpan.FromMilliseconds(100), clock.Delays[0]);
    AssertEqual(1, actionCalls);
    AssertEqual(0, inactiveFirefox.SetVolumeCalls);
    AssertApproximately(0.6f, recoveredFirefox.VolumeScalar);
    AssertTrue(recoveredFirefox.SetVolumeCalls > 0);
    AssertApproximately(0.8f, firstChrome.VolumeScalar);
    AssertApproximately(0.8f, secondChrome.VolumeScalar);
    AssertEqual(0, firstChrome.SetVolumeCalls);
    AssertEqual(0, secondChrome.SetVolumeCalls);
}

static async Task TransitionAudioTargetRetryIsBounded()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-09T12:00:00Z");
    var clock = new FakeRadioClock(now);
    var inactiveTargets = new List<FakeAudioSessionTarget>();
    var unrelatedTargets = new List<FakeAudioSessionTarget>();
    var provider = new SequencedAudioSessionEnumerationProvider(
        () =>
        {
            var inactiveFirefox = new FakeAudioSessionTarget(
                "firefox",
                20,
                $"FIREFOX-INACTIVE-{inactiveTargets.Count + 1}",
                0.5f,
                activity: AudioSessionActivity.Inactive);
            var chrome = new FakeAudioSessionTarget(
                "chrome",
                30,
                $"CHROME-{unrelatedTargets.Count + 1}",
                0.8f);
            inactiveTargets.Add(inactiveFirefox);
            unrelatedTargets.Add(chrome);
            return FakeAudioEnumeration(inactiveFirefox, chrome);
        });
    var ducker = new WindowsTransitionAudioDucker(
        new FakeLog(),
        FastAudioDuckingConfiguration(),
        provider,
        clock);
    int actionCalls = 0;

    TransitionDuckingResult result = await ducker.RunAsync(
        _ =>
        {
            actionCalls++;
            return Task.CompletedTask;
        },
        false,
        CancellationToken.None,
        now.AddMilliseconds(350));

    AssertEqual(TransitionDuckingStatus.Refused, result.Status);
    AssertEqual(4, provider.EnumerationCalls);
    AssertEqual(3, clock.Delays.Count);
    AssertEqual(
        TimeSpan.FromMilliseconds(250),
        clock.Delays.Aggregate(TimeSpan.Zero, (sum, delay) => sum + delay));
    AssertEqual(0, actionCalls);
    AssertTrue(inactiveTargets.All(
        target => target.SetVolumeCalls == 0 && target.SetMuteCalls == 0));
    AssertTrue(unrelatedTargets.All(
        target => target.SetVolumeCalls == 0 && target.SetMuteCalls == 0));
}

static void TransitionRapidSkipInvalidatesPrepared()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2, 5));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:00Z");
    state.Arm(Track("Initial", "Artist 0"), now);

    RadioTrackObservation preparation = state.ObserveTrack(Track("Track A", "Artist A"), now.AddSeconds(10));
    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));
    RadioTrackObservation delivery = state.ObserveTrack(Track("Track B", "Artist B"), now.AddSeconds(20));
    RadioTrackObservation rapidSkip = state.ObserveTrack(Track("Track C", "Artist C"), now.AddSeconds(21));

    AssertTrue(delivery.Delivery is not null);
    AssertTrue(rapidSkip.PendingOpportunityInvalidated);
    AssertTrue(rapidSkip.Delivery is null);
    AssertTrue(state.GetRuntimeState().PendingOpportunityId is null);
}

static async Task TransitionLateDeliveryIsAbandoned()
{
    DateTimeOffset transitionTime = DateTimeOffset.Parse("2026-08-08T12:00:20Z");
    RadioPreparationOpportunity opportunity = TestTransitionPreparation(transitionTime.AddSeconds(-20));
    RadioTransitionDelivery transition = TestTransitionDelivery(opportunity, transitionTime);
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(opportunity, transition.NewTrack));
    var pipeline = new TransitionRadioPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionCommentProvider(),
        host,
        new FakeRadioClock(transitionTime.AddSeconds(3)),
        new FakeLog());
    using PreparedTransitionOpportunity prepared = FakePreparedTransition(opportunity, transitionTime);

    TransitionDeliveryResult result = await pipeline.DeliverAsync(
        prepared,
        transition,
        false,
        CancellationToken.None);

    AssertEqual(TransitionDeliveryStatus.Abandoned, result.Status);
    AssertEqual(0, host.DuckedDeliveryCalls);
}

static async Task TransitionChangedTrackBIsAbandoned()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:20Z");
    RadioPreparationOpportunity opportunity = TestTransitionPreparation(now.AddSeconds(-20));
    RadioTransitionDelivery transition = TestTransitionDelivery(opportunity, now);
    var host = new FakeTransitionRadioHost(
        ValidTransitionSnapshot(opportunity, Track("Track C", "Artist C")));
    var pipeline = new TransitionRadioPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());
    using PreparedTransitionOpportunity prepared = FakePreparedTransition(opportunity, now);

    TransitionDeliveryResult result = await pipeline.DeliverAsync(
        prepared,
        transition,
        false,
        CancellationToken.None);

    AssertEqual(TransitionDeliveryStatus.Abandoned, result.Status);
    AssertEqual(0, host.DuckedDeliveryCalls);
}

static void TransitionSilentModeInvalidatesPreparation()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:00Z");
    state.Arm(Track("Initial", "Artist 0"), now);
    RadioTrackObservation preparation = state.ObserveTrack(Track("Track A", "Artist A"), now.AddSeconds(10));

    RadioModeChange change = state.SetSilentMode(true);

    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(change.PendingOpportunityInvalidated);
    AssertTrue(state.GetRuntimeState().PendingOpportunityId is null);
}

static async Task TransitionNoAiBypassesOllama()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    RadioPreparationContext context = TestTransitionPreparation(
        DateTimeOffset.Parse("2026-08-08T12:00:00Z")).Context;

    CommentSelection result = await service.SelectPreparedTransitionCommentAsync(
        context,
        false,
        CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual(0, generator.GenerateCalls);
    AssertEqual(0, generator.UnloadCalls);
    AssertEqual(0, generator.PsCalls);
}

static async Task TransitionAiStillUnloadsOllama()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    RadioPreparationContext context = TestTransitionPreparation(
        DateTimeOffset.Parse("2026-08-08T12:00:00Z")).Context;

    CommentSelection result = await service.SelectPreparedTransitionCommentAsync(
        context,
        true,
        CancellationToken.None);

    AssertEqual(CommentTextSource.Ai, result.Source);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static async Task TransitionPreparationPreservesUnicode()
{
    var generator = new FakeCommentGenerator();
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    var context = new RadioPreparationContext(
        new MediaSnapshot("Кукушка", "Кино", null),
        DateTimeOffset.Parse("2026-08-08T12:00:00Z"),
        [],
        []);

    CommentSelection result = await service.SelectPreparedTransitionCommentAsync(
        context,
        false,
        CancellationToken.None);

    AssertEqual("That was Кукушка by Кино.", result.Text);
    AssertEqual(0, generator.GenerateCalls);
}

static void OnlyOneTransitionOpportunityRemainsPending()
{
    var state = new RadioBehaviorState(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionTargetSource(2));
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:00Z");
    state.Arm(Track("Initial", "Artist 0"), now);

    RadioTrackObservation preparation = state.ObserveTrack(Track("Track A", "Artist A"), now.AddSeconds(10));
    RadioTrackObservation duplicate = state.ObserveTrack(Track("Track A", "Artist A"), now.AddSeconds(11));
    AssertTrue(preparation.PreparationOpportunity is not null);
    AssertTrue(state.MarkPreparationReady(preparation.PreparationOpportunity!.Id));
    RadioTrackObservation delivery = state.ObserveTrack(Track("Track B", "Artist B"), now.AddSeconds(20));

    AssertTrue(duplicate.PreparationOpportunity is null);
    AssertTrue(delivery.PreparationOpportunity is null);
    AssertEqual(preparation.PreparationOpportunity!.Id, delivery.Delivery!.OpportunityId);
}

static Task TransitionPreparedAudioIsDisposed()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-08T12:00:20Z");
    RadioPreparationOpportunity opportunity = TestTransitionPreparation(now.AddSeconds(-20));
    var speech = new FakePreparedSpeech(new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));
    var prepared = new PreparedTransitionOpportunity(
        opportunity,
        FakeTransitionCommentProvider.DefaultSelection,
        speech,
        now);

    prepared.Dispose();

    AssertEqual(1, speech.DisposeCalls);
    return Task.CompletedTask;
}

static async Task CancellationBeforeCoordinatorAcceptanceDisposesExactlyOnce()
{
    using var cancellation = new CancellationTokenSource();
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1))
    {
        OnMeasuredDurationRead = readCount =>
        {
            if (readCount == 1)
            {
                cancellation.Cancel();
            }
        },
    };
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(
        TestTransitionPreparation(DateTimeOffset.Parse("2026-09-09T12:00:00Z"))))
    {
        Speech = speech,
    };
    var pipeline = new ReusableLinerPipeline(host, new FakeLog());
    using var inventory = new ReusableLinerInventory();
    inventory.Arm(
        new PreparedMaterialSessionId("cancelled-before-acceptance"),
        DateTimeOffset.Parse("2026-09-09T12:00:00Z"),
        0);

    (ReusableLinerPreparationResult result, bool accepted) =
        await RadioBehaviorTest.PrepareAndAcceptLinerSpeechAsync(
            pipeline,
            cancellation.Token,
            inventory,
            new object());

    AssertEqual(ReusableLinerPreparationStatus.Ready, result.Status);
    AssertFalse(accepted);
    AssertFalse(inventory.HasPreparedSpeech);
    AssertEqual(2, speech.MeasuredDurationReads);
    AssertEqual(1, speech.DisposeCalls);
}

static async Task CancellationDuringReadyLinerAcceptanceDisposesExactlyOnce()
{
    using var cancellation = new CancellationTokenSource();
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1))
    {
        OnMeasuredDurationRead = readCount =>
        {
            if (readCount == 2)
            {
                cancellation.Cancel();
            }
        },
    };
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(
        TestTransitionPreparation(DateTimeOffset.Parse("2026-09-08T12:00:00Z"))))
    {
        Speech = speech,
    };
    var pipeline = new ReusableLinerPipeline(host, new FakeLog());
    using var inventory = new ReusableLinerInventory();
    inventory.Arm(
        new PreparedMaterialSessionId("cancelled-ready-liner"),
        DateTimeOffset.Parse("2026-09-08T12:00:00Z"),
        10);
    inventory.RecordHostSpeechCompleted(14);
    using var equivalentInventory = new ReusableLinerInventory();
    equivalentInventory.Arm(
        new PreparedMaterialSessionId("cancelled-ready-liner"),
        DateTimeOffset.Parse("2026-09-08T12:00:00Z"),
        10);
    equivalentInventory.RecordHostSpeechCompleted(14);
    PreparedMaterialSessionId? sessionBefore = inventory.SessionId;
    PreparedMaterialId? materialBefore = inventory.MaterialId;
    PreparedMaterialLifecycleStatus? statusBefore = inventory.Status;
    ReusableLinerTransition cooldownBlocked = new(
        15,
        new MediaSnapshot("Track", "Artist", "Album"),
        DateTimeOffset.Parse("2026-09-08T12:20:00Z"),
        sessionBefore!.Value);
    ReusableLinerTransition exactBoundary = new(
        16,
        new MediaSnapshot("Track", "Artist", "Album"),
        DateTimeOffset.Parse("2026-09-08T12:20:00Z"),
        sessionBefore.Value);
    EditorialAirtimeCandidate cooldownBefore = LinerCandidate(inventory, cooldownBlocked);
    EditorialAirtimeCandidate exactBefore = LinerCandidate(inventory, exactBoundary);

    (ReusableLinerPreparationResult result, bool accepted) =
        await RadioBehaviorTest.PrepareAndAcceptLinerSpeechAsync(
            pipeline,
            cancellation.Token,
            inventory,
            new object());

    AssertEqual(ReusableLinerPreparationStatus.Ready, result.Status);
    AssertFalse(accepted);
    AssertFalse(inventory.HasPreparedSpeech);
    AssertEqual(sessionBefore, inventory.SessionId);
    AssertEqual(materialBefore, inventory.MaterialId);
    AssertEqual(statusBefore, inventory.Status);
    AssertEqual(cooldownBefore, LinerCandidate(inventory, cooldownBlocked));
    AssertEqual(exactBefore, LinerCandidate(inventory, exactBoundary));
    AssertEqual(LinerCandidate(equivalentInventory, cooldownBlocked), LinerCandidate(inventory, cooldownBlocked));
    AssertEqual(LinerCandidate(equivalentInventory, exactBoundary), LinerCandidate(inventory, exactBoundary));
    AssertEqual(1, host.PrepareCalls);
    AssertEqual(2, speech.MeasuredDurationReads);
    AssertEqual(1, speech.DisposeCalls);

    var replacement = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1));
    AssertTrue(RadioBehaviorTest.TryAcceptPreparedLinerSpeech(
        new ReusableLinerPreparationResult(
            ReusableLinerPreparationStatus.Ready,
            "fresh speech after cancelled handoff",
            replacement),
        CancellationToken.None,
        inventory));
    AssertTrue(inventory.HasPreparedSpeech);
    AssertEqual(0, replacement.DisposeCalls);

    ReusableLinerReservation first = inventory.TryReserve(exactBoundary)
        ?? throw new InvalidOperationException("Expected first reservation.");
    AssertTrue(inventory.Release(first, exactBoundary.ObservedAt));
    ReusableLinerReservation second = inventory.TryReserve(new ReusableLinerTransition(
        17,
        exactBoundary.CurrentTrack,
        exactBoundary.ObservedAt.AddMinutes(1),
        sessionBefore.Value)) ?? throw new InvalidOperationException("Expected second reservation.");
    AssertTrue(inventory.Complete(second, exactBoundary.ObservedAt.AddMinutes(1)));
    inventory.Invalidate();
    AssertEqual(1, replacement.DisposeCalls);
}

static void RejectedReadyLinerPreparationDisposesExactlyOnce()
{
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1));
    using var inventory = new ReusableLinerInventory();

    bool accepted = RadioBehaviorTest.TryAcceptPreparedLinerSpeech(
        new ReusableLinerPreparationResult(
            ReusableLinerPreparationStatus.Ready,
            "fake preparation completed without armed inventory",
            speech),
        CancellationToken.None,
        inventory);

    AssertFalse(accepted);
    AssertFalse(inventory.HasPreparedSpeech);
    AssertEqual(1, speech.DisposeCalls);
}

static void RejectedReadyLinerPreservesExistingPayload()
{
    var existing = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1));
    var rejected = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1));
    using var inventory = new ReusableLinerInventory();
    inventory.Arm(
        new PreparedMaterialSessionId("existing-ready-liner"),
        DateTimeOffset.Parse("2026-09-09T12:00:00Z"),
        0);
    AssertTrue(inventory.AcceptPreparedSpeech(existing));

    AssertFalse(RadioBehaviorTest.TryAcceptPreparedLinerSpeech(
        new ReusableLinerPreparationResult(
            ReusableLinerPreparationStatus.Ready,
            "duplicate ready speech",
            rejected),
        CancellationToken.None,
        inventory));
    AssertTrue(inventory.HasPreparedSpeech);
    AssertEqual(0, existing.DisposeCalls);
    AssertEqual(1, rejected.DisposeCalls);
}

static void DurationValidationExceptionLeavesInventoryUnchanged()
{
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1))
    {
        OnMeasuredDurationRead = _ => throw new InvalidOperationException("duration read failed"),
    };
    using var inventory = new ReusableLinerInventory();
    inventory.Arm(
        new PreparedMaterialSessionId("duration-exception-liner"),
        DateTimeOffset.Parse("2026-09-09T12:00:00Z"),
        0);
    PreparedMaterialLifecycleStatus? statusBefore = inventory.Status;

    bool threw = false;
    try
    {
        RadioBehaviorTest.TryAcceptPreparedLinerSpeech(
            new ReusableLinerPreparationResult(
                ReusableLinerPreparationStatus.Ready,
                "duration validation throws",
                speech),
            CancellationToken.None,
            inventory);
    }
    catch (InvalidOperationException)
    {
        threw = true;
    }

    AssertTrue(threw);
    AssertEqual(statusBefore, inventory.Status);
    AssertFalse(inventory.HasPreparedSpeech);
    AssertEqual(1, speech.DisposeCalls);
}

static void CompatibilityRejectionDisposesOnceWhenDisposeThrows()
{
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1))
    {
        DisposeException = new InvalidOperationException("dispose failed"),
    };
    using var inventory = new ReusableLinerInventory();

    bool threw = false;
    try
    {
        inventory.AcceptPreparedSpeech(speech);
    }
    catch (InvalidOperationException exception) when (exception.Message == "dispose failed")
    {
        threw = true;
    }

    AssertTrue(threw);
    AssertEqual(1, speech.DisposeCalls);
}

static void CompatibilityValidationExceptionDoesNotDispose()
{
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1))
    {
        OnMeasuredDurationRead = _ => throw new InvalidOperationException("duration read failed"),
    };
    using var inventory = new ReusableLinerInventory();
    inventory.Arm(
        new PreparedMaterialSessionId("compatibility-duration-exception"),
        DateTimeOffset.Parse("2026-09-09T12:00:00Z"),
        0);

    bool threw = false;
    try
    {
        inventory.AcceptPreparedSpeech(speech);
    }
    catch (InvalidOperationException exception) when (exception.Message == "duration read failed")
    {
        threw = true;
    }

    AssertTrue(threw);
    AssertEqual(0, speech.DisposeCalls);
}

static async Task AcceptedReadyLinerPreparationTransfersDisposalOwnership()
{
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1));
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(
        TestTransitionPreparation(DateTimeOffset.Parse("2026-09-08T12:00:00Z"))))
    {
        Speech = speech,
    };
    var pipeline = new ReusableLinerPipeline(host, new FakeLog());
    using var cancellation = new CancellationTokenSource();
    using var inventory = new ReusableLinerInventory();
    inventory.Arm(
        new PreparedMaterialSessionId("accepted-ready-liner"),
        DateTimeOffset.Parse("2026-09-08T12:00:00Z"),
        0);

    (ReusableLinerPreparationResult result, bool accepted) =
        await RadioBehaviorTest.PrepareAndAcceptLinerSpeechAsync(
            pipeline,
            cancellation.Token,
            inventory,
            new object());

    AssertEqual(ReusableLinerPreparationStatus.Ready, result.Status);
    AssertTrue(accepted);
    AssertTrue(inventory.HasPreparedSpeech);
    AssertEqual(0, speech.DisposeCalls);

    cancellation.Cancel();
    AssertTrue(inventory.HasPreparedSpeech);
    AssertEqual(0, speech.DisposeCalls);

    inventory.Invalidate();
    inventory.Invalidate();

    AssertFalse(inventory.HasPreparedSpeech);
    AssertEqual(1, speech.DisposeCalls);
}

static EditorialAirtimeCandidate LinerCandidate(
    ReusableLinerInventory inventory,
    ReusableLinerTransition transition) =>
    inventory.CreateCandidate(
        transition,
        silentMode: false,
        protectedPreparationBoundaryClear: true,
        missedCommentBoundaryClear: true,
        treatmentAvailable: true);

static void NonReadyLinerPreparationRetainsCoordinatorCleanup()
{
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(1));
    using var inventory = new ReusableLinerInventory();

    bool accepted = RadioBehaviorTest.TryAcceptPreparedLinerSpeech(
        new ReusableLinerPreparationResult(
            ReusableLinerPreparationStatus.Abandoned,
            "fake preparation abandoned",
            speech),
        CancellationToken.None,
        inventory);

    AssertFalse(accepted);
    AssertFalse(inventory.HasPreparedSpeech);
    AssertEqual(1, speech.DisposeCalls);
}

static async Task TransitionPreparedDurationAtOrBelowLimitIsDeliverable()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-15T17:00:00Z");
    foreach (TimeSpan duration in new[] { TimeSpan.FromSeconds(24.999), TimeSpan.FromSeconds(25) })
    {
        RadioPreparationOpportunity opportunity = TestTransitionPreparation(now.AddSeconds(-20));
        var speech = new FakePreparedSpeech(
            new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
            duration);
        var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(opportunity))
        {
            Speech = speech,
        };
        var pipeline = new TransitionRadioPipeline(
            RadioBehaviorConfiguration.Default,
            new FakeTransitionCommentProvider(),
            host,
            new FakeRadioClock(now),
            new FakeLog());

        TransitionPreparationResult result = await pipeline.PrepareAsync(
            opportunity,
            true,
            CancellationToken.None);

        AssertEqual(TransitionPreparationStatus.Ready, result.Status);
        AssertEqual(0, host.DuckedDeliveryCalls);
        result.Prepared!.Dispose();
    }
}

static async Task TransitionOverDurationPreparedSpeechIsDiscardedBeforeDucking()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-15T17:00:00Z");
    RadioPreparationOpportunity opportunity = TestTransitionPreparation(now.AddSeconds(-20));
    var speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(25.001));
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(opportunity))
    {
        Speech = speech,
    };
    var log = new FakeLog();
    var pipeline = new TransitionRadioPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionCommentProvider(),
        host,
        new FakeRadioClock(now),
        log);

    TransitionPreparationResult result = await pipeline.PrepareAsync(
        opportunity,
        true,
        CancellationToken.None);

    AssertEqual(TransitionPreparationStatus.Abandoned, result.Status);
    AssertTrue(result.Prepared is null);
    AssertEqual(0, host.DuckedDeliveryCalls);
    AssertEqual(0, speech.SpeakCalls);
    AssertEqual(1, speech.DisposeCalls);
    AssertTrue(log.Contains("PREPARED SPEECH DISCARDED"));
    AssertTrue(log.Contains("No Core Audio target will be enumerated"));
    AssertTrue(log.Contains("No catch-up comment will be queued"));
}

static async Task TransitionOverDurationPreparedSpeechDoesNotBlockLaterOpportunity()
{
    DateTimeOffset now = DateTimeOffset.Parse("2026-08-15T17:00:00Z");
    RadioPreparationOpportunity first = TestTransitionPreparation(now.AddSeconds(-20));
    var host = new FakeTransitionRadioHost(ValidTransitionSnapshot(first))
    {
        Speech = new FakePreparedSpeech(
            new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
            TimeSpan.FromSeconds(26)),
    };
    var pipeline = new TransitionRadioPipeline(
        RadioBehaviorConfiguration.Default,
        new FakeTransitionCommentProvider(),
        host,
        new FakeRadioClock(now),
        new FakeLog());

    TransitionPreparationResult rejected = await pipeline.PrepareAsync(
        first,
        true,
        CancellationToken.None);
    AssertEqual(TransitionPreparationStatus.Abandoned, rejected.Status);

    RadioPreparationOpportunity later = first with
    {
        Id = first.Id + 1,
        Context = first.Context with { SubjectTrackStartedAt = now.AddSeconds(-21) },
    };
    host.Snapshot = ValidTransitionSnapshot(later);
    host.Speech = new FakePreparedSpeech(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed),
        TimeSpan.FromSeconds(10));

    TransitionPreparationResult laterResult = await pipeline.PrepareAsync(
        later,
        true,
        CancellationToken.None);

    AssertEqual(TransitionPreparationStatus.Ready, laterResult.Status);
    AssertEqual(0, host.DuckedDeliveryCalls);
    laterResult.Prepared!.Dispose();
}

static RadioPreparationOpportunity TestTransitionPreparation(DateTimeOffset subjectStartedAt)
{
    return new RadioPreparationOpportunity(
        700,
        new RadioPreparationContext(
            Track("Track A", "Artist A"),
            subjectStartedAt,
            [],
            []));
}

static RadioTransitionDelivery TestTransitionDelivery(
    RadioPreparationOpportunity opportunity,
    DateTimeOffset transitionObservedAt)
{
    return new RadioTransitionDelivery(
        opportunity.Id,
        opportunity.Context.SubjectTrack,
        Track("Track B", "Artist B"),
        transitionObservedAt);
}

static RadioOpportunitySnapshot ValidTransitionSnapshot(
    RadioPreparationOpportunity opportunity,
    MediaSnapshot? currentTrack = null)
{
    return new RadioOpportunitySnapshot(
        Armed: true,
        SilentMode: false,
        OpportunityStillPending: true,
        SessionListReadSucceeded: true,
        MatchingFirefoxSessionCount: 1,
        SameSelectedSession: true,
        SourceId: "firefox.exe",
        SourceReadSucceeded: true,
        CurrentMedia: currentTrack ?? opportunity.Context.SubjectTrack,
        MediaReadSucceeded: true,
        PlaybackStatus: GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
        PlaybackReadSucceeded: true);
}

static PreparedTransitionOpportunity FakePreparedTransition(
    RadioPreparationOpportunity opportunity,
    DateTimeOffset readyAt)
{
    return new PreparedTransitionOpportunity(
        opportunity,
        FakeTransitionCommentProvider.DefaultSelection,
        new FakePreparedSpeech(new SpeechPlaybackResult(SpeechPlaybackStatus.Completed)),
        readyAt);
}

static MediaSnapshot Track(string title, string artist)
{
    return new MediaSnapshot(title, artist, null);
}

static FirefoxSessionContinuityEvidence EligibleFirefoxEvidence(
    string title,
    string artist)
{
    return FirefoxSessionContinuityEvidence.FromDetails(
        new RadioSessionDetails(
            null!,
            FirefoxSessionSelector.VerifiedSourceId,
            true,
            Track(title, artist),
            true,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
            true));
}

static RadioCommentOpportunity TestRadioOpportunity(DateTimeOffset currentTrackStartedAt)
{
    var context = new RadioCommentContext(
        Track("Previous Track", "Previous Artist"),
        Track("Current Track", "Current Artist"),
        currentTrackStartedAt,
        [],
        []);
    return new RadioCommentOpportunity(1, context);
}

static RadioOpportunitySnapshot ValidRadioSnapshot(RadioCommentOpportunity opportunity)
{
    return new RadioOpportunitySnapshot(
        Armed: true,
        SilentMode: false,
        OpportunityStillPending: true,
        SessionListReadSucceeded: true,
        MatchingFirefoxSessionCount: 1,
        SameSelectedSession: true,
        SourceId: "firefox.exe",
        SourceReadSucceeded: true,
        CurrentMedia: opportunity.Context.CurrentTrack,
        MediaReadSucceeded: true,
        PlaybackStatus: GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
        PlaybackReadSucceeded: true);
}

static async Task AssertGenerationFailureUsesFallback(OllamaGenerationResult generation)
{
    var generator = new FakeCommentGenerator { GenerationResult = generation };
    var service = new AiCommentService(generator, AiCommentConfiguration.Default, new FakeLog());
    CommentSelection result = await service.SelectCommentAsync(TestMedia(), true, false, CancellationToken.None);

    AssertEqual(CommentTextSource.FixedFallback, result.Source);
    AssertEqual("That was Cochise by Audioslave.", result.Text);
    AssertEqual(1, generator.GenerateCalls);
    AssertEqual(1, generator.UnloadCalls);
    AssertEqual(1, generator.PsCalls);
}

static MediaSnapshot TestMedia()
{
    return new MediaSnapshot("Cochise", "Audioslave", null);
}

static AnnouncementSequenceTimings FastTimings()
{
    return new AnnouncementSequenceTimings(
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(1),
        TimeSpan.FromMilliseconds(1),
        3);
}

static AudioDuckingConfiguration FastAudioDuckingConfiguration()
{
    return new AudioDuckingConfiguration(
        DuckRatio: 0.25f,
        MinimumUsableStartingVolume: 0.05f,
        RampDuration: TimeSpan.Zero,
        HoldDuration: TimeSpan.Zero,
        RampSteps: 2,
        VerificationTolerance: 0.0001f);
}

static AudioSessionDescriptor AudioDescriptor(FakeAudioSessionTarget target)
{
    return new AudioSessionDescriptor(target, target.ReadState());
}

static AudioSessionEnumeration FakeAudioEnumeration(
    params FakeAudioSessionTarget[] targets)
{
    return new AudioSessionEnumeration(
        "FAKE-ENDPOINT",
        targets.Select(AudioDescriptor).ToArray());
}

static Task RunSynchronous(Action action)
{
    action();
    return Task.CompletedTask;
}

static IReadOnlyList<FakeSession> Select(IEnumerable<FakeSession> sessions)
{
    return FirefoxSessionSelector.FindExactMatches(sessions, session => session.SourceId);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(actual, expected))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void AssertSame(object expected, object actual)
{
    if (!ReferenceEquals(expected, actual))
    {
        throw new InvalidOperationException("The selected session object reference changed.");
    }
}

static void AssertApproximately(float expected, float actual, float tolerance = 0.0001f)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"Expected approximately {expected}, got {actual}.");
    }
}

static void AssertContains(string actual, string expected)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Expected text to contain: {expected}");
    }
}

static void AssertTrue(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true.");
    }
}

static void AssertFalse(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false.");
    }
}

internal sealed record FakeSession(string? SourceId);

internal sealed class FakeAudioSessionTarget : IAudioSessionVolumeTarget
{
    private bool _disposed;
    private int _successfulReads;

    public FakeAudioSessionTarget(
        string? processName,
        int? processId,
        string stableSessionTag,
        float volumeScalar,
        bool muted = false,
        AudioSessionActivity activity = AudioSessionActivity.Active)
    {
        ProcessName = processName;
        ProcessId = processId;
        StableSessionTag = stableSessionTag;
        VolumeScalar = volumeScalar;
        MinimumRequestedVolume = volumeScalar;
        Muted = muted;
        Activity = activity;
    }

    public string StableSessionTag { get; }

    public int? ProcessId { get; }

    public string? ProcessName { get; }

    public float VolumeScalar { get; private set; }

    public float MinimumRequestedVolume { get; private set; }

    public bool Muted { get; private set; }

    public AudioSessionActivity Activity { get; set; }

    public int SetVolumeCalls { get; private set; }

    public int SetMuteCalls { get; private set; }

    public int? DisappearAfterSuccessfulReads { get; init; }

    public Action<float>? OnVolumeSet { get; init; }

    public void ChangeExternally(float volumeScalar, bool? muted = null)
    {
        VolumeScalar = volumeScalar;
        MinimumRequestedVolume = volumeScalar;

        if (muted is not null)
        {
            Muted = muted.Value;
        }
    }

    public AudioSessionVolumeState ReadState()
    {
        if (_disposed)
        {
            return new AudioSessionVolumeState(false, AudioSessionActivity.Unknown, 0f, false, "disposed");
        }

        if (DisappearAfterSuccessfulReads is not null
            && _successfulReads >= DisappearAfterSuccessfulReads.Value)
        {
            return new AudioSessionVolumeState(false, AudioSessionActivity.Expired, 0f, false, "session disappeared");
        }

        _successfulReads++;
        return new AudioSessionVolumeState(true, Activity, VolumeScalar, Muted);
    }

    public void SetVolume(float volumeScalar, Guid eventContext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (DisappearAfterSuccessfulReads is not null
            && _successfulReads >= DisappearAfterSuccessfulReads.Value)
        {
            throw new InvalidOperationException("The original session disappeared.");
        }

        SetVolumeCalls++;
        VolumeScalar = volumeScalar;
        MinimumRequestedVolume = Math.Min(MinimumRequestedVolume, volumeScalar);
        OnVolumeSet?.Invoke(volumeScalar);
    }

    public void SetMute(bool muted, Guid eventContext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (DisappearAfterSuccessfulReads is not null
            && _successfulReads >= DisappearAfterSuccessfulReads.Value)
        {
            throw new InvalidOperationException("The original session disappeared.");
        }

        SetMuteCalls++;
        Muted = muted;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

internal sealed class FakePlaybackTarget : IAnnouncementPlaybackTarget
{
    public string SourceId => FirefoxSessionSelector.VerifiedSourceId;

    public int PauseCalls { get; private set; }

    public int PlayCalls { get; private set; }

    public GlobalSystemMediaTransportControlsSessionPlaybackStatus Status { get; private set; } =
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

    public Task<bool> RequestPauseAsync()
    {
        PauseCalls++;
        Status = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused;
        return Task.FromResult(true);
    }

    public Task<bool> RequestPlayAsync()
    {
        PlayCalls++;
        Status = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        return Task.FromResult(true);
    }

    public PlaybackStatusRead ReadPlaybackStatus()
    {
        return new PlaybackStatusRead(true, Status);
    }
}

internal sealed class FakePreparedSpeech : IPreparedAnnouncementSpeech
{
    private readonly SpeechPlaybackResult? _result;
    private readonly bool _throwCancellation;
    private readonly TimeSpan _measuredDuration;

    public FakePreparedSpeech(
        SpeechPlaybackResult result,
        TimeSpan? measuredDuration = null)
    {
        _result = result;
        _measuredDuration = measuredDuration ?? TimeSpan.FromSeconds(10);
    }

    public FakePreparedSpeech(bool throwCancellation)
    {
        _throwCancellation = throwCancellation;
        _measuredDuration = TimeSpan.FromSeconds(10);
    }

    public Action<int>? OnMeasuredDurationRead { get; init; }

    public Exception? DisposeException { get; init; }

    public int MeasuredDurationReads { get; private set; }

    public TimeSpan MeasuredDuration
    {
        get
        {
            MeasuredDurationReads++;
            OnMeasuredDurationRead?.Invoke(MeasuredDurationReads);
            return _measuredDuration;
        }
    }

    public int SpeakCalls { get; private set; }

    public TimeSpan? LastTimeout { get; private set; }

    public int DisposeCalls { get; private set; }

    public Task<SpeechPlaybackResult> SpeakAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        SpeakCalls++;
        LastTimeout = timeout;

        if (_throwCancellation)
        {
            throw new OperationCanceledException();
        }

        return Task.FromResult(_result!);
    }

    public void Dispose()
    {
        DisposeCalls++;
        if (DisposeException is not null)
        {
            throw DisposeException;
        }
    }
}

internal sealed class FakeSpeechService : IAnnouncementSpeechService
{
    public FakeSpeechService(string providerName)
    {
        ProviderName = providerName;
    }

    public string ProviderName { get; }

    public Exception? ExceptionToThrow { get; init; }

    public int PrepareCalls { get; private set; }

    public int DisposeCalls { get; private set; }

    public string? LastText { get; private set; }

    public Task<IPreparedAnnouncementSpeech> PrepareAsync(
        string text,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PrepareCalls++;
        LastText = text;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult<IPreparedAnnouncementSpeech>(new FakePreparedSpeech(
            new SpeechPlaybackResult(SpeechPlaybackStatus.Completed)));
    }

    public void Dispose()
    {
        DisposeCalls++;
    }
}

internal sealed class FakeTransitionTargetSource : ITransitionTargetSource
{
    private readonly Queue<int> _values;
    private int _lastValue;

    public FakeTransitionTargetSource(params int[] values)
    {
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one transition target is required.", nameof(values));
        }

        _values = new Queue<int>(values);
        _lastValue = values[^1];
    }

    public List<(int Minimum, int Maximum)> Requests { get; } = [];

    public int NextInclusive(int minimum, int maximum)
    {
        Requests.Add((minimum, maximum));
        if (_values.Count > 0)
        {
            _lastValue = _values.Dequeue();
        }

        return _lastValue;
    }
}

internal sealed class FakeRadioClock : IRadioClock
{
    public FakeRadioClock(DateTimeOffset now)
    {
        Now = now;
    }

    public DateTimeOffset Now { get; private set; }

    public List<TimeSpan> Delays { get; } = [];

    public Action? OnDelay { get; init; }

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Delays.Add(delay);
        Now += delay;
        OnDelay?.Invoke();
        return Task.CompletedTask;
    }
}

internal sealed class FakeRadioCommentProvider : IRadioCommentProvider
{
    public int Calls { get; private set; }

    public Action? OnSelect { get; init; }

    public CommentSelection Selection { get; init; } = new(
        "That title has arrived with enough confidence to rearrange the furniture, Steve, and frankly I admire the paperwork-free commitment.",
        CommentTextSource.Ai,
        19,
        "fake accepted AI comment",
        null,
        false);

    public Task<CommentSelection> SelectAsync(
        RadioCommentContext context,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        OnSelect?.Invoke();
        return Task.FromResult(Selection);
    }
}

internal sealed class FakeTransitionCommentProvider : ITransitionCommentProvider
{
    private readonly List<string>? _events;

    public FakeTransitionCommentProvider(List<string>? events = null)
    {
        _events = events;
    }

    public static CommentSelection DefaultSelection { get; } = new(
        "That title has arrived with enough confidence to rearrange the furniture, Steve, and frankly I admire the paperwork-free commitment.",
        CommentTextSource.Ai,
        19,
        "fake accepted AI comment",
        null,
        false);

    public int Calls { get; private set; }

    public bool? LastAiEnabled { get; private set; }

    public Task<CommentSelection> SelectAsync(
        RadioPreparationContext context,
        bool aiEnabled,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        LastAiEnabled = aiEnabled;
        _events?.Add("ai");
        return Task.FromResult(DefaultSelection);
    }
}

internal sealed class FakeTransitionRadioHost : ITransitionRadioHost
{
    private readonly List<string>? _events;

    public FakeTransitionRadioHost(
        RadioOpportunitySnapshot snapshot,
        List<string>? events = null)
    {
        Snapshot = snapshot;
        _events = events;
    }

    public RadioOpportunitySnapshot Snapshot { get; set; }

    public FakePreparedSpeech Speech { get; set; } = new(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));

    public int SnapshotReads { get; private set; }

    public int PrepareCalls { get; private set; }

    public int DuckedDeliveryCalls { get; private set; }

    public int PauseCalls { get; private set; }

    public int PlayCalls { get; private set; }

    public TransitionDuckingResult? ResultOverride { get; init; }

    public Task<RadioOpportunitySnapshot> ReadSnapshotAsync(
        long opportunityId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SnapshotReads++;
        return Task.FromResult(Snapshot);
    }

    public Task<IPreparedAnnouncementSpeech> PrepareSpeechAsync(
        string text,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PrepareCalls++;
        _events?.Add("tts");
        return Task.FromResult<IPreparedAnnouncementSpeech>(Speech);
    }

    public async Task<TransitionDuckingResult> RunDuckedAnnouncementAsync(
        RadioTransitionDelivery transition,
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        DateTimeOffset speechStartDeadline,
        bool simulatePostDuckFailure,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DuckedDeliveryCalls++;
        _events?.Add("duck");

        if (ResultOverride is not null)
        {
            return ResultOverride;
        }

        SpeechPlaybackResult speech = await preparedSpeech.SpeakAsync(
            maximumSpeechDuration,
            cancellationToken);
        return speech.Status == SpeechPlaybackStatus.Completed
            ? new TransitionDuckingResult(
                TransitionDuckingStatus.Completed,
                "fake duck, speech, and restoration completed",
                null)
            : new TransitionDuckingResult(
                TransitionDuckingStatus.Failed,
                "fake speech failed",
                null);
    }
    public async Task<TransitionDuckingResult> RunDuckedLinerAsync(
        ReusableLinerTransition transition,
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        DateTimeOffset speechStartDeadline,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DuckedDeliveryCalls++;
        _events?.Add("duck");
        if (ResultOverride is not null)
        {
            return ResultOverride;
        }

        SpeechPlaybackResult speech = await preparedSpeech.SpeakAsync(
            maximumSpeechDuration,
            cancellationToken);
        return speech.Status == SpeechPlaybackStatus.Completed
            ? new TransitionDuckingResult(
                TransitionDuckingStatus.Completed,
                "fake liner duck, speech, and restoration completed",
                null)
            : new TransitionDuckingResult(
                TransitionDuckingStatus.Failed,
                "fake liner speech failed",
                null);
    }
}

internal sealed class FakeAudioSessionEnumerationProvider : IAudioSessionEnumerationProvider
{
    private readonly IReadOnlyList<AudioSessionDescriptor> _sessions;

    public FakeAudioSessionEnumerationProvider(params AudioSessionDescriptor[] sessions)
    {
        _sessions = sessions;
    }

    public AudioSessionEnumeration Enumerate()
    {
        return new AudioSessionEnumeration("FAKE-ENDPOINT", _sessions);
    }
}

internal sealed class SequencedAudioSessionEnumerationProvider : IAudioSessionEnumerationProvider
{
    private readonly Queue<Func<AudioSessionEnumeration>> _enumerations;
    private Func<AudioSessionEnumeration> _lastEnumeration;

    public SequencedAudioSessionEnumerationProvider(
        params Func<AudioSessionEnumeration>[] enumerations)
    {
        if (enumerations.Length == 0)
        {
            throw new ArgumentException(
                "At least one fake enumeration is required.",
                nameof(enumerations));
        }

        _enumerations = new Queue<Func<AudioSessionEnumeration>>(enumerations);
        _lastEnumeration = enumerations[^1];
    }

    public int EnumerationCalls { get; private set; }

    public AudioSessionEnumeration Enumerate()
    {
        EnumerationCalls++;
        if (_enumerations.Count > 0)
        {
            _lastEnumeration = _enumerations.Dequeue();
        }

        return _lastEnumeration();
    }
}

internal sealed class FakeRadioOpportunityHost : IRadioOpportunityHost
{
    private readonly FakePreparedSpeech _speech = new(
        new SpeechPlaybackResult(SpeechPlaybackStatus.Completed));

    public FakeRadioOpportunityHost(RadioOpportunitySnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public RadioOpportunitySnapshot Snapshot { get; set; }

    public int SnapshotReads { get; private set; }

    public int PrepareCalls { get; private set; }

    public int AnnouncementCalls { get; private set; }

    public int PreparedDisposeCalls => _speech.DisposeCalls;

    public List<string> Events { get; } = [];

    public Action? OnPrepare { get; set; }

    public TimeSpan? MaximumSpeechDuration { get; private set; }

    public DateTimeOffset? AnnouncementObservedAt { get; private set; }

    public FakeRadioClock? Clock { get; set; }

    public Task<RadioOpportunitySnapshot> ReadSnapshotAsync(
        long opportunityId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SnapshotReads++;
        return Task.FromResult(Snapshot);
    }

    public Task<IPreparedAnnouncementSpeech> PrepareSpeechAsync(
        string text,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PrepareCalls++;
        Events.Add("prepare");
        OnPrepare?.Invoke();
        return Task.FromResult<IPreparedAnnouncementSpeech>(_speech);
    }

    public Task<AnnouncementSequenceResult> RunAnnouncementAsync(
        IPreparedAnnouncementSpeech preparedSpeech,
        TimeSpan maximumSpeechDuration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AnnouncementCalls++;
        Events.Add("announce");
        MaximumSpeechDuration = maximumSpeechDuration;
        AnnouncementObservedAt = Clock?.Now;
        return Task.FromResult(new AnnouncementSequenceResult(
            TestPassed: true,
            PauseVerified: true,
            PlaybackRestored: true,
            RecoveryNeeded: false,
            SimulatedFailure: false));
    }
}

internal sealed class FakeLog : IAnnouncementLog
{
    private readonly List<string> _messages = new();

    public void Write(string message)
    {
        _messages.Add(message);
    }

    public void Warning(string context, Exception exception)
    {
        _messages.Add($"{context}: {exception.Message}");
    }

    public bool Contains(string text)
    {
        return _messages.Any(message => message.Contains(text, StringComparison.Ordinal));
    }
}

internal sealed class FakeCommentGenerator : ILocalCommentGenerator
{
    public string ModelName => "qwen3:4b-instruct";

    public Uri Endpoint => new("http://localhost:11434/api/");

    public int GenerateCalls { get; private set; }

    public int UnloadCalls { get; private set; }

    public int PsCalls { get; private set; }

    public Exception? ExceptionToThrow { get; init; }

    public int InstalledModelCalls { get; private set; }

    public CommentPrompt? LastPrompt { get; private set; }

    public OllamaGenerationResult GenerationResult { get; init; } = new(
        OllamaGenerationStatus.Success,
        "With a title like Cochise, subtlety has clearly taken the evening off, and honestly that sounds wonderfully committed.",
        null,
        null);

    public Task<OllamaGenerationResult> GenerateAsync(
        CommentPrompt prompt,
        string keepAlive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        GenerateCalls++;
        LastPrompt = prompt;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(GenerationResult);
    }

    public Task<OllamaOperationResult> UnloadAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        UnloadCalls++;
        return Task.FromResult(new OllamaOperationResult(true, "fake unload accepted"));
    }

    public Task<OllamaRunningModelsResult> GetRunningModelsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        PsCalls++;
        return Task.FromResult(new OllamaRunningModelsResult(true, [], "fake ps succeeded"));
    }

    public Task<OllamaInstalledModelsResult> GetInstalledModelsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        InstalledModelCalls++;
        return Task.FromResult(
            new OllamaInstalledModelsResult(
                true,
                [new OllamaInstalledModel(
                    ModelName,
                    ModelName,
                    2_500_000_000,
                    new OllamaModelDetails("4.0B", "Q4_K_M"))],
                "fake tags succeeded"));
    }
}

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    public RecordingHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    public Uri? RequestUri { get; private set; }

    public string? RequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestUri = request.RequestUri;
        RequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody),
        };
    }
}
