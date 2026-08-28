# Zoya Radio roadmap

This document records accepted milestone status and approved direction. It does not turn research recommendations, brainstorming, or a milestone number into implementation scope.

## Milestone status

| Milestone | Status | Accepted result |
| --- | --- | --- |
| 0 - Windows media observation | Passed | Firefox `firefox.exe` metadata, playback state, and deduplicated transitions verified. |
| 1 - Targeted playback control | Passed | Exact-session pause/resume, refusal, ambiguity, isolation, and recovery verified. |
| 2 - Basic spoken announcement | Passed | Local Windows speech and protected playback recovery verified. |
| 3 - Local AI comment generation | Passed | Loopback Ollama, grounding contract, validation/fallback, no-AI, timing, and unload verified after correction. |
| 4 - Host persona | Passed | Zoya/Steve persona accepted while preserving title/artist factual grounding. |
| 5 - Radio behavior and minimal listening state | Passed | Inclusive 2-5 transition scheduling, bounded in-memory context, Silent Mode, and stale-work safety verified. |
| 6 | Deferred | Deliberately deferred; no implementation or approved scope is recorded here. |
| 7 - Better local voice | Passed | Kokoro `af_nicole` accepted as primary CPU voice; `af_heart` runner-up; Windows speech retained as fallback. |
| 7.5A - Isolated Firefox ducking | Passed | Proportional application-session ducking and exact restoration verified without affecting Chrome or master volume. |
| 7.5B - Transition-aware delivery | Passed | One-transition-ahead preparation, ducked delivery over playing music, wrapper-churn continuity, Chrome isolation, and clean restoration accepted on August 10, 2026. |
| 8A - Control application | Passed | Shared controller/runtime, WinForms frontend, read-only activity status, LocalAppData logs, safe controls, and protected shutdown accepted on August 11, 2026. |
| 8B - Private-alpha distribution | Passed - internal-development gate | Version `0.8.0-alpha.1`, package integrity/neural self-tests, and Steve's packaged-desktop behavior accepted; clean-machine and invited-tester distribution validation deferred. |
| 9A - Structured host commentary experiment | Completed - candidate rejected | Blinded qualitative review found the structured candidate preferable to the baseline in aggregate but still unsuitable for runtime; normal radio remains unchanged. |
| 9B - Grounded context integration | Concluded without final runtime acceptance | Gates 2 and 3 were accepted; Gate 4 did not meet its five-enriched-delivery requirement. The candidate and evidence are retained for future planning, but no further M9 testing or promotion is authorized. |
| 9C - Source-backed commentary experiments | Completed - direction selected | M9C.6 selected grounded 14B whole-comment synthesis as integration evidence; diagnostic paths remain outside normal radio. |
| 10A - Deterministic Opportunity Kernel | Passed | Steve accepted the pure immutable reducer on August 22, 2026. It remains unreachable from runtime; source/test builds pass and the complete suite passes 292/292. |
| 10B - Prepared Material Lifecycle and Binding | Passed | Steve accepted the pure lifecycle on August 22, 2026. Release builds and the complete 308-test suite passed. |
| 10C - Accepted M8 Runtime Migration | Passed | Steve accepted the live M10A/M10B migration on August 23, 2026. Release builds passed with 0 warnings/errors; the complete suite passed 314/314; focused desktop parity confirmed preparation-stop and delivery-stop return the UI to ready, with Firefox-only exact restoration during delivery. |
| 11A - Pure Editorial Arbiter | Passed | Steve accepted the pure preparation/airtime arbiter on August 23, 2026. Release builds passed with 0 warnings/errors; the complete suite passed 320/320; focused desktop evidence confirmed typed Prepare/ReserveForAirtime decisions, M10C-equivalent audio behavior, Chrome isolation, and exact Firefox restoration. |
| 11B - Commentary and Reusable Liner Proof | Passed | Steve accepted the two-format proof on August 24, 2026. Three Release builds passed with 0 warnings/errors and the complete suite passed 337/337; desktop evidence confirmed one correctly gated liner, comment priority, frequent music continuation, exact Firefox restoration, reusable inventory return, safe Silent Mode, and clean Stop. |
| 13A-I1 - Music Knowledge and Identity Foundation | Accepted isolated foundation | Steve accepted the fixture-only, non-network foundation on August 28, 2026. Three Release builds were clean, the complete fake-driven suite passed 402/402, and final Warden review found no issues. M13A as a whole is not passed. |

Milestone 8 is closed for continued internal development after M8A and M8B owner acceptance. Clean Windows 10/11 and invited-tester evidence remain deferred compatibility/distribution gates and do not certify this package for broad or public distribution.

The latest accepted implementation snapshot is `fdc1b6528acc694b3c15db7e50902e80a1b8453d`, containing the accepted M10B-M12D implementations. The earlier M10A implementation remains historically identified by commit 93ece64bf5cb21e67378d9ae024c6f45568fe86e. A future commit still requires separate authorization.

On August 28, 2026 Steve accepted [M13A-I1](MILESTONE_13A_CONTRACT.md) as an isolated first foundation only: typed music-knowledge/provenance/outcome models, fixture parsing, deterministic identity ownership and reconciliation, and fake cache/retry/report diagnostics. It is not M13A completion and does not authorize adapters, live providers, network, runtime consumption, M13A-I2, M13B, commit, push, release, or distribution. Any next M13A stage requires a separately bounded authorization.

## Milestone 7.5B repair provenance

- `e0bb294` introduced transition-aware ducked delivery.
- `cdca733` repaired Firefox GSMTC/Core Audio recovery, and `4870cd8` preserved logical work across harmless GSMTC wrapper churn.
- A power outage left only `tests/ZoyaRadio.MediaProbe.Tests/Program.cs` as an all-zero file. That file alone was restored exactly from `e0bb294`; six surviving repair files were preserved, focused tests were recreated, and repository integrity was rechecked.
- `63662342` recorded Steve's accepted Firefox-plus-Chrome evidence. The final automated suite passed 124/124.

## Milestone 8A provenance

- `f123ddd9de484de590ba596abf812314b265390c` implemented the original control-application candidate.
- `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc` added the deterministic Radio-activity presentation and is the accepted M8A implementation snapshot.
- Steve accepted M8A on August 11, 2026 after the 162-test suite, clean Release builds, and renewed real-desktop testing.

## Milestone 8B provenance

- `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf` implemented the self-contained `0.8.0-alpha.1` package, integrity/readiness/setup behavior, safe self-tests, distribution audit, and 178-test result.
- Steve accepted the extracted package on August 11, 2026 after both LocalAppData self-test reports passed and the requested packaged desktop behavior worked normally.
- Steve amended project sequencing so clean Windows 10/11 and invited-tester validation remain deferred distribution evidence rather than blockers to internal development.
- The package remains an unsigned private-alpha candidate, not a clean-machine-certified, broadly compatible, public, or third-party-distribution release.

## Completed capability arc

- **Observation:** read-only Windows media-session inventory, metadata, state changes, and deduplicated track transitions.
- **Explicit safety diagnostics:** exact Firefox selection, confirmation, targeted playback control, local speech, simulated failure, and cancellation recovery.
- **Local generation:** loopback-only Ollama comments constrained to trusted title/artist metadata with deterministic fallback and prompt-level grounding rules.
- **Host delivery:** accepted Zoya persona, Nicole CPU speech, and Windows speech fallback.
- **Automatic behavior:** bounded ephemeral history, inclusive 2-5 transition scheduling, Silent Mode, stale-work abandonment, and no catch-up queue.
- **Transition presentation:** one-transition-ahead generation/synthesis, strict delivery revalidation, Firefox-only proportional ducking, exact restoration, and safe continuity through harmless GSMTC wrapper churn.
- **Control application:** same-process WinForms frontend, UI-independent controller, one authoritative lifecycle, deterministic read-only activity/status, LocalAppData logs, and preserved console diagnostics.

## Accepted manual evidence summary

- Milestone 0 observed correct title/artist and Playing/Paused state across at least three transitions, treated album as optional, deduplicated repeated events, and remained read only.
- Milestone 1 passed at least five targeted Firefox pause/resume runs, refused default-NO and starting-Paused cases, recovered after Ctrl+C, and ignored a simultaneous Chrome media session.
- Milestone 2 passed three fully audible announcements plus simulated-speech-failure and Ctrl+C recovery; Unicode remained intact.
- Milestone 3 passed local generation, corrected literal-metadata grounding, timeout/fallback, no-AI, performance measurement, and selected-model unload.
- Milestone 4 accepted Zoya's warm, dry, conversational persona while retaining the title/artist-only factual boundary.
- Milestone 5 accepted automatic frequency, minimum-age/stale-track safety, Silent Mode, bounded in-memory state, and clean no-AI/shutdown behavior.
- Milestone 7 accepted Nicole as the primary local voice and Windows TTS as fallback; Cyrillic sounded natural to Steve, but Russian accuracy was not claimed.
- Milestone 7.5A verified 50.0% -> 12.5% -> 50.0% proportional restoration, normal/Ctrl+C/simulated-failure cleanup, a natural ramp, and Chrome/master-volume isolation.
- Milestone 7.5B verified two prepared deliveries through Chrome-driven GSMTC churn, exact 100% -> 25% -> 100% restoration, continued transition observation, Chrome isolation, and clean shutdown.
- Milestone 8A accepted the WinForms control application, readable Radio activity, safe Stop/window-close cleanup, Firefox-only duck/restoration, Chrome/master isolation, target refusal, LocalAppData logs, and preserved console diagnostics.
- M10C accepted strict live migration of the one-comment path onto M10A/M10B. Steve reconfirmed Chrome isolation, preparation-stop without Firefox ducking, delivery-stop with exact 100% -> 25% -> 100% Firefox restoration, and immediate UI return to ready.
- M11A accepted pure preparation and airtime arbitration for the existing comment. The accepted live run produced typed Prepare and ReserveForAirtime decisions, preserved music-first safe silence as a valid outcome, left Chrome unaffected, and retained exact Firefox 100% -> 25% -> 100% restoration without playback control.

Milestone-specific evidence and preserved regression instructions remain in the existing `MILESTONE_*_TEST.md` documents.

## Milestone 9 conclusion

Steve approved the bounded [Milestone 8 contract](MILESTONE_8_CONTRACT.md) on August 10, 2026. Its historical approval-time sequence used two authorization gates:

1. **M8A - Control application:** a small WinForms frontend, same-process hosting of the accepted runtime, a UI-independent controller, one authoritative asynchronous lifecycle, safe controls and status, LocalAppData logs, preserved console diagnostics, and fake-driven controller/UI-state tests.
2. **M8B - Private-alpha distribution:** a versioned unsigned self-contained Windows x64 portable ZIP for approximately 2-3 invited testers, subject to M8A acceptance, a separate authorization, eSpeak exclusion/compliance evidence, clean-machine testing, and final manual acceptance.

Steve accepted M8A, separately authorized M8B, and accepted the extracted M8B package on August 11, 2026. His dated contract amendment closes Milestone 8 for continued internal development while deferring clean-machine and invited-tester distribution validation.

- Steve's post-M8 review approved the bounded [Milestone 9A contract](MILESTONE_9A_CONTRACT.md) for a structured-commentary experiment only.
- M9A's corrected manual benchmark and blinded review are recorded in [MILESTONE_9A_TEST.md](MILESTONE_9A_TEST.md). The StructuredCandidate was rejected for runtime consideration: it was less often rejected than the baseline, but still produced unacceptable repetition, awkward output, unsupported listener/session claims, and Unicode-metadata failures.
- M9A did not pass; its structured candidate remains rejected.
- Steve completed the separate bounded [Milestone 9C contract](MILESTONE_9C_CONTRACT.md). M9C.6 validated grounded whole-comment synthesis with local `qwen3:14b-q4_K_M`; its owner review is recorded in [MILESTONE_9C_TEST.md](MILESTONE_9C_TEST.md).
- M9C is closed as successful experimental evidence. Its selected direction is not normal radio behavior.
- Steve explicitly authorized the bounded [Milestone 9B contract](MILESTONE_9B_CONTRACT.md) on August 15, 2026 and accepted generate-only Gate 2 with the recorded narrow repetition exception in [MILESTONE_9B_TEST.md](MILESTONE_9B_TEST.md). He later accepted Gate 3 under its revised operational criteria. Gate 4 did not meet its five-enriched-delivery requirement: it established safe live enrichment, safe fallback, and protected playback behavior, but remote source availability remained variable. Steve therefore concluded M9 as an exploration/prototype phase rather than continuing its audible test loop. M9B is not accepted runtime behavior. See [MILESTONE_9_CLOSURE.md](MILESTONE_9_CLOSURE.md).
- Research remains evidence only and proposal history remains non-authoritative except for exact decisions Steve separately promoted into canonical milestone contracts.
- Milestone 6 remains deferred unless Steve separately promotes it.

## Approved post-M9 direction

Steve approved a capability-first post-M9 roadmap on August 21, 2026. The full
architecture, milestone responsibilities/non-goals, dependency map, shared-capability
map, format-maturity map, Music Knowledge flow, memory boundaries, and safety gates
are governed by [POST_M9_CAPABILITY_ROADMAP.md](POST_M9_CAPABILITY_ROADMAP.md).

On August 24, 2026, Steve approved the post-M11 refinements that keep generation
format-aware, distinguish Creative Direction from exact script generation, stage
voice/renderer/cast/locale capabilities, and separate narrow format experiments
from the later shared production foundation. This is roadmap approval only; each
milestone still requires its own bounded contract and implementation authorization.

The approved phase sequence is:

1. **Deterministic broadcast foundation:** M10A pure opportunity kernel, M10B
   prepared-material lifecycle/binding, then M10C strict M8 runtime parity.
2. **Editorial arbitration:** preparation/airtime arbitration, music-first
   pressure, format spacing, treatment compatibility, explainable decisions,
   bounded tie-breaking, and silence; commentary plus one reusable micro-liner is
   the first two-format proof.
3. **Creative content foundation:** deterministic Creative Context, conceptual
   separation of Creative Direction from exact Content Generation, bounded Format
   Laboratory batches, and only evidence-selected host-format integration.
4. **Knowledge, listening intelligence, memory, and continuity:** source-agnostic
   Music Knowledge, session/recent-session Listening Intelligence before any
   durable-affinity experiment, bounded Station Lore/open threads, and minimal
   complete-airtime effects.
5. **Voice and localization foundation:** host voice/renderer abstraction first,
   provider-neutral expressive-renderer evaluation as a separate evidence gate,
   then cast/role capability and separate language/locale work. Independent
   auditions and call/commercial/telephone/PT-BR experiments may happen earlier;
   naming a backend such as Chatterbox does not approve it.
6. **Delivery grammar and program-space safety:** treatment compatibility remains
   independent from format and duration. Exact-target hold is an isolated major
   diagnostic gate; opt-in `HoldAndReplace` requires separate later approval.
7. **Procedural format expansion:** fixed commercial only as a plumbing fixture,
   procedural commercial and caller lab batches, shared prepared-format pipeline,
   and optional editorially earned cross-format continuity.
8. **Rich production and sonic identity:** deterministic prepared-scene
   representation/compilation first, production-grade voice-renderer integration
   second, shared mixing/FX third, then a coherent Zoya sound pack and imaging
   grammar that progressively upgrade proven formats. Narrow format experiments
   may precede this phase, but proven needs are consolidated here rather than
   preserved as format-specific hacks.
9. **Experience-ready private alpha:** compatibility and packaging when Steve
   decides the experience is worth distributing; opportunistic Firefox evidence
   may be collected earlier.
10. **Configuration extraction:** expose only dimensions proven by the mature,
    opinionated Zoya station.

Formats are clients of shared capabilities, not feature silos. Exact cadence,
cooldown, retention, promotion, and decay values remain conservative experimental
defaults until listening evidence supports durable policy. Music Knowledge remains
optional to safe scheduling. Generated content has no right to airtime. Raw
creative alternatives are ephemeral, and unaired or partially heard material does
not become canonical Station Lore.

The [M10A - Deterministic Opportunity Kernel](MILESTONE_10A_CONTRACT.md) is passed.
Steve accepted the automatically verified, runtime-unreachable implementation on
August 22, 2026, and the implementation is committed as `93ece64bf5cb21e67378d9ae024c6f45568fe86e`.
Steve approved the [M10B bounded contract](MILESTONE_10B_CONTRACT.md) and
authorized M10B-only implementation on August 22, 2026. The pure lifecycle
passed Release builds and the full 308-test suite; Steve accepted M10B the same
day. Steve then approved, authorized, and accepted the [M10C contract](MILESTONE_10C_CONTRACT.md)
on August 23, 2026. The existing one-comment runtime now uses M10A/M10B with
strict M8 parity. Steve then approved, authorized, and accepted the
[M11A contract](MILESTONE_11A_CONTRACT.md) on August 23, 2026. The pure
Editorial Arbiter gates the existing comment at preparation and airtime with
typed reasons while preserving M10C behavior; current verification is 320/320.
Steve then accepted [M11B](MILESTONE_11B_CONTRACT.md) on August 24, 2026 after the 337/337 automated result and bounded desktop listening trial. M11B proves the accepted comment, one session-reusable liner, and direct music can share typed arbitration, lifecycle, and protected overlay delivery. Steve approved, authorized, and accepted [M12A](MILESTONE_12A_CONTRACT.md) on August 24, 2026 after three zero-warning Release builds, 348/348 passing tests, and representative-packet review. The accepted pure context assembler remains unreachable from normal runtime. Steve then approved the bounded [M12B contract](MILESTONE_12B_CONTRACT.md), authorized M12B-only implementation, reviewed its representative fixtures, and accepted it on August 26, 2026. The isolated direction/generation/laboratory boundary passed 360/360 tests and zero-warning Release builds while remaining unreachable from normal runtime. Steve accepted the [M12C Host Format Laboratory, Batch 1](MILESTONE_12C_CONTRACT.md) on August 26, 2026 after its blinded qualitative review. The deterministic handoff remains the fixed no-model reference; quick reaction is selected only for later bounded work; session-aware observation is deferred for revision/retest. M12C remains local-only and unreachable from normal runtime. Steve then authorized the bounded [M12D Selected Quick-Reaction Contextual Pilot](MILESTONE_12D_CONTRACT.md), accepted D3.1 on August 26, 2026, and accepted the bounded D3.2 comparison on August 27, 2026. The selected quick-reaction client remains a default-off local pilot using the accepted prepared lifecycle, Editorial Arbiter, and existing ducked-overlay delivery; normal M11B launches remain unchanged. D3.2 found no `Oh [...]` repetition in its batch, but the same music can receive the same comment format; this is accepted observation, not a defect or scope change. The later VAR1 simulated mixed-song evaluation was accepted with limitations, and its repeated-form quality limitation remains active. The earlier D3.1 topic-carryover issue is resolved by D3.2. M12D is passed; no commit or next milestone is authorized.

M12D's accepted D3.2 topic-isolation repair replaced prior recent-line subject matter with abstract form measurements only; its exactly 32-request comparison retained three mechanical under-15-word rejections and succeeded in selected-model unload. Steve accepted that comparison on August 27, 2026. The later M12D-VAR1 generate-only simulated mixed-song evaluation was also accepted with limitations: individual comments can be good, but repeated title-first and `Oh ...` forms become recognizable and repetitive song after song. This is an expected quality/product limitation, not an established runtime defect. The earlier D3.1 topic-carryover issue is resolved by D3.2. Broader research and later formats remain deferred and require separate authorization; no new milestone or sequence is implied.

## Deferred and future boundaries

- Milestone 6 remains deliberately deferred without an approved implementation scope in the repository.
- Known limitations and possible quality improvements are tracked in [KNOWN_ISSUES.md](KNOWN_ISSUES.md), not silently promoted into milestone work.
- External production analyses and repository research are indexed in [RESEARCH_INDEX.md](RESEARCH_INDEX.md). Their recommendations require an explicit decision before entering this roadmap.
- Product-vision discussions outside Git remain brainstorming unless Steve promotes them into an approved milestone.
- The approved post-M9 roadmap governs direction, but each implementation milestone still requires a separately approved bounded contract and explicit request.

## Milestone gate policy

1. Work on one explicitly requested milestone or repair at a time.
2. Preserve all previously accepted behavior and regression coverage.
3. Complete safe automated verification before asking for desktop acceptance.
4. Mark a milestone passed only after Steve supplies its required real-desktop evidence.
5. Record the accepted result in Git before advancing to another milestone.
