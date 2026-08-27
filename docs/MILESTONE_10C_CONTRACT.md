# Milestone 10C contract - Accepted M8 Runtime Migration

## Contract status

**Passed. Steve approved M10C-only implementation and accepted the completed migration on August 23, 2026. M10C does not authorize M11, a new format, or a commit.**

M10A and M10B are passed pure foundations. M10C is the first proposed live
migration: it must reproduce accepted M8 behavior before Editorial Arbitration,
liners, commercials, callers, program breaks, knowledge, memory, voice, or
production work begins.

## Purpose

Move the existing single-comment M8 runtime path onto the accepted M10A
Deterministic Opportunity Kernel and M10B Prepared Material Lifecycle with strict
observable parity.

M10C answers one engineering question:

> Can the live runtime use the accepted deterministic opportunity and lifecycle
> foundations while retaining the same music-first M8 schedule, safety refusals,
> preparation timing, ducked delivery, protected cleanup, controls, and user
> experience?

This is a migration, not a feature milestone. The listener should hear the same
station behavior as the accepted M8 runtime.

## Governing accepted baseline

- Accepted live runtime/package baseline: Milestone 8 implementation commit
  `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- Accepted M10A pure opportunity kernel: implementation commit
  `93ece64bf5cb21e67378d9ae024c6f45568fe86e`.
- Accepted M10B prepared-material lifecycle: accepted on August 22, 2026, with
  308 fake-driven tests passed and no implementation commit yet created.
- The accepted M8 safety baseline, [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md),
  [DECISIONS.md](DECISIONS.md), [MILESTONE_10A_CONTRACT.md](MILESTONE_10A_CONTRACT.md),
  and [MILESTONE_10B_CONTRACT.md](MILESTONE_10B_CONTRACT.md) govern where this
  contract is silent.
- M9 remains concluded prototype/design evidence. M9B is not accepted runtime
  behavior and M10C must not promote, remove, or redesign it.

A conflict between accepted M8 behavior and the M10 foundations must stop work and
return to Steve. M10C may not silently invent new radio policy to reconcile them.

## Why this milestone comes now

M10A isolated accepted scheduling decisions from Windows timing. M10B isolated
prepared-material ownership, binding, reservation, terminal disposition, and
one-time disposal decisions from real resources. The current M8 live path still
uses `RadioBehaviorState`, `PreparedTransitionOpportunity`, mutable pending fields,
and dispersed cancellation/disposal paths.

M10C connects the already accepted foundations to that one existing comment path.
Doing so now confines migration risk before a second format or an Editorial Arbiter
can multiply the number of live resource and scheduling paths.

## Hard dependencies

- Passed M10A kernel and its accepted M8 parity traces.
- Passed M10B lifecycle and its ownership, binding, expiry, reservation, and
  disposal traces.
- Accepted M8 preparation, delivery, Firefox selection, Core Audio ducking, and
  protected cleanup tests.
- Existing fake-driven test project and current manually accepted M8 desktop
  procedure.

M10C has no dependency on Music Knowledge, Listening Intelligence, Station Lore,
Creative Context, Creative Direction, Content Generation, new models, new voices,
localization, imaging, program breaks, or distribution work.

## Authorized implementation surface after approval

If Steve approves this contract and separately authorizes M10C-only
implementation, edits are limited to:

1. A small live adapter/coordinator under `src/ZoyaRadio.MediaProbe/` that owns
   the mapping between real M8 pipeline resources and M10A/M10B typed state.
2. Minimal changes to the current M8 runtime/coordinator and focused test seams
   needed to route the one existing comment path through that adapter.
3. Focused M10C migration/parity tests and registration in the existing test
   runner, plus only the existing test adjustments required by the new accepted
   composition.
4. Documentation updates required to record candidate verification and later
   acceptance.

The adapter may use the existing `RadioBehaviorTest`, `TransitionRadioPipeline`,
`WindowsRadioOpportunityHost`, `PreparedTransitionOpportunity`, clock, target
source, cancellation, logging, activity, and status boundaries. It must not create
a parallel second runtime, a second media watcher, or a general prepared-content
scheduler.

`RadioBehaviorState` may be retired from the live path only after focused parity
coverage proves the M10A adapter is its replacement. It may remain temporarily as
test characterization evidence during implementation. No broad cleanup or
unrelated renaming is authorized.

## Migration architecture

M10C has three deliberately separate layers:

1. **M10A kernel** remains the deterministic authority for arm, track observation,
   schedule target, M8 opportunity identity, readiness, delivery due, invalidation,
   resolution, and bounded current M8 history/comment state.
2. **M10B lifecycle** remains the deterministic authority for the corresponding
   prepared item's binding, Preparing/Ready/Reserved/Terminal state, payload
   ownership, completion disposition, and typed one-time disposal request.
3. **M10C runtime adapter** observes Windows and real asynchronous work, supplies
   explicit inputs to both foundations, maps a lifecycle payload handle to the
   existing `PreparedTransitionOpportunity`, executes a requested disposal through
   the existing protected resource cleanup, and drives the existing M8 pipeline.

The runtime adapter must not reimplement transition cadence, stale-work policy,
material reuse, expiry policy, editorial priority, or a new content-selection rule.
It interprets accepted typed decisions; it does not replace them with booleans or
ad-hoc mutable scheduling fields.

### Required single-comment mapping

For the existing one-comment path only:

| M8 event | M10A action | M10B action | Runtime adapter obligation |
| --- | --- | --- | --- |
| Start with a usable Firefox track | Arm with explicit time and target | None | Retain current safe subscription/start behavior. |
| Genuine Track-A transition creates preparation | Observe track and read `PreparationRequested` | Register single-opportunity Preparing material bound to opportunity, Track A, logical runtime session, and any existing delivery-validity boundary | Start one existing preparation task; no pause, duck, or new queue. |
| Existing preparation succeeds | Mark matching M10A opportunity ready only after lifecycle acceptance | Attach one opaque payload handle and move to Ready | Retain the existing `PreparedTransitionOpportunity` only after ownership is accepted; dispose refused/late resources through existing cleanup. |
| Track-B delivery due | Observe track and read `DeliveryDue` | Reserve the exact matching Ready material | Start the existing delivery pipeline only after reservation succeeds. |
| Completed delivery | Resolve matching opportunity Completed, preserving existing accepted-AI-comment behavior | Complete with RetireOrConsume | Execute the one typed disposal request exactly once, then retain the existing next-schedule/activity/status behavior. |
| Failed, abandoned, cancelled, missed, rapid, stale, unsafe, Stop, shutdown, Silent Mode, Clear History, rebind, or explicit cancellation | Apply the corresponding accepted M10A input | Terminate or invalidate the exact material as required | Cancel current work when existing M8 does, execute any typed disposal exactly once, and leave no catch-up work. |

The handle-to-resource map is private runtime adapter state. It must be keyed by
the deterministic material identity and must be removed before or while executing
the one corresponding disposal decision so repeated terminal decisions cannot
release the same real speech resource twice.

### Ordering and locking rules

- The current media-event serialization and protected runtime lifecycle ordering
  remain in force; M10C must not introduce concurrent observation, preparation,
  delivery, or disposal for one opportunity.
- A real prepared resource is never made eligible for delivery merely because a
  task completed. Lifecycle ownership must first accept its opaque handle, and the
  M10A opportunity must still be the current matching opportunity.
- If M10A or M10B refuses an event as stale, invalid, mismatched, terminal, or
  unsafe, the safe outcome is existing M8 silence plus protected caller/resource
  cleanup.
- A typed M10B disposal decision is authoritative for an adapter-owned resource.
  A refused payload remains caller-owned and follows the existing preparation
  cleanup path rather than the lifecycle disposal path.
- Runtime logs and UI activity may map typed decisions to the existing readable
  messages/states, but free-form log text is not the new source of truth.

## Required accepted-M8 parity

M10C must preserve all current observable behavior for the one comment format:

1. Arm requires one safely selected, Playing `firefox.exe` media session; startup
   track is not a transition.
2. Duplicate normalized track identity and unusable metadata remain inert.
3. The inclusive configured 2-5 transition target behavior remains exactly as
   accepted, including one-ahead Track-A preparation and no early preparation.
4. At most one comment opportunity and one automatic pipeline exist at a time.
5. Track-A preparation happens while music continues; it performs current safety
   checks, minimum-age wait, local generation, speech preparation, maximum-duration
   rejection, and pre-delivery revalidation as before.
6. An unready Track-A opportunity is missed on Track B; a ready opportunity is
   eligible once on Track B; a further transition before resolution invalidates it.
   No case creates catch-up work.
7. Delivery uses the existing Track-B revalidation, exact Firefox ownership,
   Firefox-only Core Audio selection, proportional ducking, speech, and protected
   scalar/mute restoration.
8. Normal automatic behavior never pauses, resumes, toggles, seeks, or otherwise
   controls Firefox playback. User Play/Pause/track-change intent still wins.
9. Missing, zero, ambiguous, replaced, inactive, late, unsafe, or stale targets
   remain silence with cleanup, never a fallback target.
10. Silent Mode, Clear History, rebind/session safety, explicit cancellation,
    Stop, window close, shutdown, task cancellation, and pipeline failures retain
    their existing invalidation, cleanup, activity, status, and fresh-schedule
    semantics.
11. Existing M8 comment generation, deterministic fallback, Nicole/Windows-TTS
    behavior, maximum duration, M9 candidate isolation, in-memory limits, and
    accepted recent-comment recording behavior remain unchanged.
12. The accepted UI/controller/console behavior and log privacy boundary remain
    unchanged except for factual migration diagnostics if a focused test requires
    them.

## Explicit non-goals

M10C must not:

- add an Editorial Arbiter, preparation arbitration, airtime arbitration,
  relevance, salience, policy priority, format spacing, randomness, or a liner;
- add any commercial, caller, rant, ID, callback, chained bit, second host format,
  reusable on-air inventory, or multi-format behavior;
- add Music Knowledge, source adapters, Listening Intelligence, persistent
  memory, Station Lore, open threads, Creative Context, Creative Direction, or
  Content Generation;
- alter prompts, grounding, models, Ollama endpoint policy, M9 status, voice,
  renderer, cast, language, locale, audio assets, or production treatment;
- add a delivery-treatment model, pause/hold/resume behavior, program break,
  endpoint/master-volume change, or new Firefox/Core Audio targeting rule;
- add packages, services, network access, files, caches, persistence, migration
  stores, distribution work, configuration UI, or a general station builder;
- broaden M10B reusable-material behavior into current live radio;
- delete historical tests or weaken acceptance checks to make the migration pass.

## Main risks and containment

| Risk | Required containment |
| --- | --- |
| Kernel/lifecycle decisions drift from M8 runtime timing | Trace adapters compare the accepted M8 state path with the M10C path using explicit clocks and targets. |
| Real speech resource is leaked or disposed twice | One private handle map, typed M10B disposal effects, idempotence tests, and existing protected cleanup. |
| Late async work revives a stale opportunity | Require matching M10A identity and accepted M10B ownership before retention or delivery. |
| Runtime migration changes user-visible safety behavior | Retain current Firefox/session/audio host boundaries and full existing safety regression suite. |
| M10C accidentally becomes a new scheduler | One existing comment only; no new format, clock, ranking, or editorial decision. |
| Activity/status or controller behavior regresses | Preserve current state projection and add focused controller/activity traces. |
| M9 candidate behavior is accidentally promoted or removed | Retain the current optional composition and test its existing boundaries. |

## Required automated verification for an implementation candidate

The candidate must provide current—not historical—evidence for:

1. Release builds of the source and test projects with 0 warnings and 0 errors.
2. The complete fake-driven suite, with all prior M8, M9, M10A, and M10B tests
   still passing.
3. Focused M10C migration traces for threshold-two and threshold-five full paths:
   arm, Track A, preparation, ready, Track B, reservation, delivery completion,
   one disposal, resolution, and fresh target.
4. Focused parity traces covering duplicate/unusable observations, missed
   preparation, ready delivery, rapid Track-C invalidation, stale ready/resolve,
   Silent Mode, Clear History, rebind, explicit cancellation, disarm/re-arm, and
   bounded history/comment behavior.
5. Exact live-adapter ownership traces proving late/duplicate prepared resources
   remain caller-owned, lifecycle-owned resources dispose once, terminal replay
   cannot dispose twice, and wrong identities cannot touch another opportunity.
6. Preparation and delivery safety regressions: target refusal, stale Track A/B,
   minimum-age wait, max speech duration, cancellation, simulated preparation and
   post-duck failures, Firefox-only ducking, exact restoration, Chrome isolation,
   bounded inactive retry, and replacement-session refusal.
7. Runtime control/activity traces: Start/Stop serialization, Stop during each
   preparation/delivery/cleanup stage, window close, Silent Mode, Clear History,
   observable status, and no playback-control command in automatic activity.
8. A production-reference review showing the live comment path uses the M10A/M10B
   adapter and no longer uses `RadioBehaviorState` as active scheduling authority.
   Retained characterization-only uses must be explicitly named.
9. Formatting verification, `git diff --check`, and complete final-diff review.

The final test count is produced by implementation. Tests must use fakes for media,
audio, speech, clocks, target sources, and disposal; they must not open the GUI,
contact Ollama or external services, produce audible speech, control real Firefox,
or enumerate real Core Audio during automated verification.

## Implementation-candidate verification - August 23, 2026

- Release source and test-project builds passed with 0 warnings and 0 errors.
- The complete fake-driven suite passed **313 passed, 0 failed**. It includes all
  prior M8/M9/M10A/M10B coverage plus four focused M10C traces: M8-parity happy
  path through reservation, missed/rapid invalidation, one-time adapter-owned
  prepared-speech retirement, controls/rebind parity, and a preparation-stage Stop terminal-state regression.
- `git diff --check` passed. Automated checks used fakes and did not open the
  GUI, contact Ollama or external services, generate audible speech, control
  Firefox, or enumerate real Core Audio.
- Production review: `RadioBehaviorTest` now owns
  `M10CRuntimeMigrationState`, which supplies explicit targets/times to M10A,
  registers/binds/reserves/completes through M10B, and owns a private
  handle-to-`PreparedTransitionOpportunity` map for typed disposal. The former
  `RadioBehaviorState` remains only as characterization evidence in tests.

This implementation evidence was followed by Steve's accepted manual desktop parity evaluation, recorded below.

## Steve/manual evaluation after automated verification - completed

M10C requires a focused real-desktop parity check because it changes the live
runtime composition. Steve should use the existing accepted M8 desktop procedure,
not explore new features:

1. Start while exactly one eligible Firefox session is Playing; confirm ordinary
   Track-A preparation and a normal ducked comment at Track B.
2. With Chrome also playing, confirm Firefox ducks/restores while Chrome and master
   volume remain unchanged.
3. Turn Silent Mode on while preparation is pending; confirm silence and no later
   catch-up. Turn it off and confirm a fresh ordinary interval.
4. Use Clear History during pending preparation; confirm discard and fresh schedule.
5. Change Firefox track rapidly during or after readiness; confirm stale speech
   never plays.
6. Stop or close the window during preparation and during delivery; confirm clean
   shutdown and exact restoration where ducking began.
7. Exercise missing/ambiguous Firefox or unsafe replacement targeting; confirm
   refusal/silence rather than interference with another application.

No new cadence, new voice, new commercial/call/liner, pause/hold behavior, or
sonic treatment is part of this evaluation. A different audible experience is a
migration defect, not an acceptable M10C variation.

## Steve acceptance record - August 23, 2026

- Steve accepted M10C after focused real-desktop parity evaluation.
- Chrome remained unaffected while Firefox was the only eligible target.
- Stopping during Track-A preparation cancelled the pending work without ducking Firefox and returned the control UI immediately to ready.
- Stopping during delivery cancelled speech, restored the same Firefox audio session exactly from 25% to its captured 100% unmuted state, and returned the control UI immediately to ready. The corresponding runtime log is `radio-behavior-20260823-224429-608.txt` under `%LOCALAPPDATA%\Zoya Radio\logs`.
- The focused control-form repaint repair stays within M10C parity scope: it reads the authoritative stopped controller snapshot while stopping and does not alter opportunity policy, playback behavior, or audio cleanup.
- Final current verification: Release build succeeded with 0 warnings/errors; complete fake-driven suite passed 314/314.
- No commit was authorized or created. M11 remains unimplemented and requires a separately approved bounded contract.

## Completion and acceptance

Passing automated verification creates an M10C implementation candidate; it does
not pass the milestone automatically.

M10C passes only after:

1. the bounded migration stays within this contract;
2. required automated evidence passes;
3. Steve completes and accepts the focused desktop parity evaluation;
4. canonical documentation records the acceptance.

All four conditions are satisfied for M10C.

No implementation commit is automatic. No M11 work, Editorial Arbiter work, new
format, or later capability becomes authorized through M10C acceptance.

## Next action type

**Next action type - Documentation (Codex/Work)**

Prepare the bounded M11A Editorial Arbiter contract for Steve review. Do not implement M11, add a liner, alter cadence, or create a commit until Steve separately approves that contract and authorizes M11A-only implementation.