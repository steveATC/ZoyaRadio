# Milestone 10A contract - Deterministic Opportunity Kernel

## Contract status

**Passed. Steve accepted the automatically verified M10A implementation on
August 22, 2026. The accepted implementation is commit `93ece64bf5cb21e67378d9ae024c6f45568fe86e`.
M10B was subsequently authorized, implemented, and accepted as a separate passed foundation.**

This contract is the first bounded implementation proposal under the approved
[post-M9 capability roadmap](POST_M9_CAPABILITY_ROADMAP.md). Steve approved this
contract and explicitly authorized its implementation on August 22, 2026. Steve
accepted the resulting implementation later that day.
Those approvals did not extend to M10B, a commit, or a push. Steve separately
authorized the accepted M10A commit later on August 22, 2026.

## Purpose

Create a pure, deterministic, directly testable opportunity kernel that reproduces
the accepted Milestone 8 scheduling/state decisions. M10A is intentionally an
unreachable parallel foundation: it does not replace, call, or alter the live
radio runtime.

The milestone answers one engineering question:

> Can accepted M8 opportunity behavior be expressed as deterministic inputs,
> state, decisions, and typed reasons before lifecycle extraction or runtime
> migration begins?

## Why this milestone comes first

The current `RadioBehaviorState` already contains much of the accepted deterministic
behavior, but its public result shape mixes observation, preparation, delivery,
history context, and boolean reason flags. Later prepared-material lifecycle and
multi-format arbitration need an explicit decision boundary that can be exercised
without Windows media sessions, audio, AI, network access, threads, or clocks.

M10A characterizes and reproduces that behavior before M10B adds lifecycle concepts
and before M10C moves the live runtime. It does not treat the retained M9B candidate
as the new baseline.

## Governing accepted baseline

- Accepted runtime implementation: Milestone 8 commit
  `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- Accepted automated evidence: 178 passed, 0 failed at that implementation.
- Current repository includes later M9 prototype code, but M9 is closed without
  final runtime acceptance.
- Current safety and behavior remain governed by
  [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md),
  [DECISIONS.md](DECISIONS.md), and
  [MILESTONE_9_CLOSURE.md](MILESTONE_9_CLOSURE.md).

## Hard dependencies

- Accepted M8 `RadioBehaviorState` behavior and regression suite.
- Existing deterministic `ITransitionTargetSource` seam or an equivalently bounded
  injected transition-target sequence.
- Existing immutable `MediaSnapshot` identity input. M10A may adapt it at its pure
  boundary but must not introduce a second media-observation implementation.

There is no dependency on M9 enrichment, provider access, Ollama, Kokoro, Core
Audio, GSMTC, WinForms, persistence, or a new package.

## Authorized implementation surface after approval

The implementation is limited to:

1. One small internal pure-kernel source file, plus a second small file only if
   separating types materially improves readability, under
   `src/ZoyaRadio.MediaProbe/`.
2. Focused tests and golden/characterization fixtures in the existing
   `tests/ZoyaRadio.MediaProbe.Tests` project.
3. Milestone documentation updates needed to record implementation verification
   and later acceptance state.

The expected source concepts are:

- an internal immutable kernel state;
- typed inputs/events;
- typed decisions/effects;
- a typed reason enum or similarly closed reason category;
- a pure reducer/evaluator that returns next state plus decisions;
- an injected bounded transition-target value/sequence.

Exact type names are implementation detail. They must make the state transition
contract obvious and must not expose a new public API.

No existing production launcher, runtime host, controller, pipeline, or UI may
reference the M10A kernel. Existing production source should remain unchanged
unless a minimal `InternalsVisibleTo`/test-access adjustment is genuinely required;
the current test project already exercises internal types, so such a change is
not expected.

## Pure input boundary

The kernel must accept explicit values rather than read the environment. The
bounded input/event set must cover the accepted state transitions represented by:

- arm with one usable initial track and observed time;
- observe a track with explicit observed time;
- mark a specific preparation opportunity ready;
- resolve a specific opportunity as completed, abandoned, or failed, retaining
  only the M8 accepted-comment effect needed for parity;
- enable or disable Silent Mode;
- clear recent in-memory history;
- cancel the current opportunity and request a fresh schedule;
- rebind the current logical track after safe runtime reacquisition;
- disarm.

If the implementation uses methods rather than a single event union, the same
boundary applies. No input method may query system time, random global state,
Windows state, files, network services, models, renderers, or playback.

## Kernel-owned state

Only state required to reproduce accepted M8 decisions is in scope:

- armed and Silent Mode flags;
- current usable track identity and its explicitly supplied start time;
- transition counter and total observed transitions;
- currently selected inclusive transition threshold;
- monotonic in-process opportunity identity;
- at most one pending opportunity;
- whether its preparation is ready;
- whether its delivery transition has already been observed;
- bounded recent tracks and accepted AI comments only to the extent necessary to
  reproduce current M8 context/clear-history results.

This remains in-memory candidate state. No serialization, database, LocalAppData
store, cache, migration, listening affinity, or Station Lore is permitted.

## Typed decisions and reasons

Each evaluation must return enough typed information for tests and a future
adapter to distinguish at least:

- unusable observation ignored;
- duplicate identity ignored;
- genuine transition observed;
- transition counted toward the current schedule;
- preparation opportunity created;
- preparation missed its delivery transition and was invalidated;
- prepared opportunity became eligible for transition delivery;
- rapid following transition invalidated an unresolved delivery;
- Silent Mode suppressed scheduling and invalidated pending work where applicable;
- clear, rebind, explicit cancellation, disarm, or resolution invalidated/closed
  pending work where applicable;
- fresh transition schedule selected;
- stale/wrong opportunity identity refused.

The result may use a small ordered list of typed effects or one typed decision
record containing optional fields. Free-form log strings are not the decision
contract. Human-readable logging remains a future adapter concern.

M10A does not define Editorial Arbiter reason taxonomy. Its reasons describe only
accepted opportunity-kernel transitions and should remain small enough to change
or map during M10C if parity evidence demands it.

## Required accepted-M8 parity

The candidate must reproduce these current behaviors exactly:

1. `Arm` requires usable title and artist, clears bounded history, records the
   initial track, resets counters, and selects one inclusive threshold from 2-5
   under the current default configuration. The startup track is not a transition.
2. Duplicate normalized title/artist observations do not count as transitions.
3. Observations without a usable title and artist do not count and do not replace
   the current usable track.
4. Each genuine transition increments the total observed-transition count and
   updates bounded recent-track state.
5. While armed and not silent, the scheduling counter increments only on genuine
   transitions with no pending opportunity.
6. Preparation is requested one transition before delivery: when the counter
   reaches `scheduled threshold - 1`, exactly one opportunity is created and bound
   to the current track.
7. A pending opportunity that is not ready when the next genuine transition
   arrives is invalidated as missed; it does not create catch-up work, and a fresh
   schedule is selected.
8. A ready opportunity produces one delivery decision on the next genuine
   transition, bound to the commented track and newly observed track.
9. Another genuine transition before that opportunity is resolved invalidates it
   and selects a fresh schedule; no late delivery is created.
10. Resolving the matching opportunity closes it and selects a fresh schedule.
    A completed accepted AI comment enters the existing bounded recent-comment
    history; abandoned/failed or absent text does not.
11. Resolving or marking ready with a stale/wrong opportunity identity is refused
    without mutating the valid opportunity.
12. Entering Silent Mode resets the scheduling counter, invalidates pending work,
    and creates no fresh active opportunity. Leaving Silent Mode selects a fresh
    schedule. Setting the already-current mode is idempotent.
13. Clear History clears bounded tracks/comments, invalidates pending work, and
    selects a fresh schedule using current accepted semantics.
14. Rebind requires a usable track, invalidates pending work, makes that track
    current, adds it to bounded recent tracks, and selects a fresh schedule without
    counting a new observed transition.
15. Explicit cancellation closes the current opportunity and selects a fresh
    schedule only when an opportunity exists.
16. Disarm invalidates pending work and suppresses future scheduling until another
    arm. It does not issue playback or audio actions.
17. Every new threshold must be within the configured inclusive bounds; an injected
    out-of-range value fails deterministically.

Where current source and current tests expose an edge case not listed here, the
characterization trace governs unless it conflicts with a canonical accepted
invariant. Any genuine conflict must stop implementation and return to Steve for a
contract decision; it must not be silently rationalized.

## Golden and characterization trace set

Focused tests must exercise the candidate with explicit tracks, timestamps, and
transition-target values. At minimum:

1. Threshold 2 happy path: arm, prepare on first transition, ready, deliver on
   second, resolve complete, fresh threshold.
2. Threshold 5 happy path with no early preparation.
3. Duplicate and unusable observations interleaved with genuine transitions.
4. Missed preparation followed by a new full schedule.
5. Ready delivery followed by a rapid extra transition before resolution.
6. Stale ready/resolve identities.
7. Silent Mode entered with and without pending work, repeated idempotent set, and
   resumed fresh schedule.
8. Clear History with pending work and empty history.
9. Safe rebind with pending work and without pending work.
10. Explicit cancellation and repeated no-op cancellation.
11. Disarm and re-arm.
12. Bounded recent-track and accepted-comment behavior.
13. Minimum and maximum inclusive target values plus an out-of-range injected
    value.

For behaviors represented by `RadioBehaviorState`, tests should either run the
same scripted trace against legacy and candidate implementations and compare a
normalized result, or lock both to equivalent explicit expectations. A shared
trace adapter is preferred when it remains smaller and clearer than duplicated
assertions.

Tests must not bless incidental free-form log wording, object `ToString()` output,
thread scheduling, or wall-clock timing as golden behavior.

## Explicit non-goals

M10A must not:

- connect the kernel to `RadioBehaviorRuntime`, `RadioBehaviorTest`,
  `M9BGate3SilentRunner`, `RadioRuntimeController`, WinForms, console launchers, or
  any live host;
- replace or delete `RadioBehaviorState`;
- change the current 2-5 transition policy or any exact cadence value;
- add prepared-material inventory, reservation, expiry, release, or disposal
  beyond current single-opportunity parity (those belong to M10B);
- add the Editorial Arbiter, opportunity classification, speech-pressure policy,
  multi-format selection, liner, commercial, call, rant, ID, or callback;
- add Creative Context, Creative Direction, Content Generation, new prompts, or AI
  model work;
- add Music Knowledge, source adapters, Listening Intelligence, Station Lore,
  open threads, cache, or persistence;
- change Nicole, renderer behavior, cast roles, language, locale, or speech assets;
- add delivery-treatment abstraction, hold/pause/resume behavior, audio changes,
  Core Audio work, or Firefox targeting changes;
- add packages, services, network endpoints, models, or distribution work;
- alter retained M9 code or claim M9B acceptance.

## Accepted behavior that must remain unchanged

Because the M10A kernel is unreachable, all observable application behavior must
remain byte-for-byte or functionally unchanged except for test-only compilation of
new internal types. In particular:

- normal automatic delivery does not pause, resume, or toggle playback;
- exact Firefox GSMTC/Core Audio targeting and safe refusal remain unchanged;
- proportional ducking and exact scalar/mute restoration remain unchanged;
- user playback intent, cancellation, Silent Mode, stale opportunities, target
  replacement, Stop, and shutdown retain their current priority;
- zero, ambiguous, missing, late, or unsafe targeting remains silence;
- M8 commentary/prompt/model/voice/fallback and in-memory limits remain unchanged;
- M9 diagnostic/candidate paths remain retained and unreachable as currently
  documented;
- no new logs, files, cache, persistence, GUI control, configuration, or audio are
  introduced.

## What M10A unlocks

- M10B can add a general prepared-material lifecycle against a stable pure kernel.
- Later parity tests can identify whether M10C runtime migration changes a decision
  rather than mixing that question with live Windows timing.
- M11A can build arbitration above typed opportunities instead of extracting
  scheduling while also adding a second format.

M10A does not authorize or partially implement any of those milestones.

## Main risks and containment

| Risk | Required containment |
| --- | --- |
| Candidate accidentally enters live runtime | No production references; source/diff review and reference search |
| Golden tests merely duplicate a new bug | Compare with legacy state or explicit accepted traces |
| Incidental implementation details become contract | Assert typed state/decisions, not logs or wall-clock timing |
| Current edge case conflicts with roadmap language | Stop and report; do not silently change current behavior |
| M10B lifecycle leaks into M10A | Keep only current one-opportunity parity state |
| Global randomness/time makes tests flaky | Inject exact target values and timestamps |
| Refactor expands into M9 or runtime code | Strict file/symbol diff boundary and full regression run |

## Automated verification required for an implementation candidate

Run and report these as current evidence:

1. Release build of `src/ZoyaRadio.MediaProbe/ZoyaRadio.MediaProbe.csproj`.
2. Release build of `tests/ZoyaRadio.MediaProbe.Tests/ZoyaRadio.MediaProbe.Tests.csproj`.
3. Complete existing fake-driven test executable with all prior tests plus focused
   M10A traces passing.
4. Formatting verification for changed C# files using the repository's existing
   local SDK/tooling.
5. `git diff --check`.
6. Reference search proving normal runtime/controller/launcher/UI entry points do
   not instantiate or call the M10A kernel.
7. Complete final diff review limited to the approved files and contract scope.

Automated verification must not open the GUI, contact Ollama or external sources,
produce speech, control Firefox, enumerate real Core Audio, pause/resume playback,
or change volume.

The exact test count is not predetermined. Existing tests must not be deleted or
weakened to obtain a pass.

## Steve/manual evaluation

No audible or real-desktop test is required for M10A because the candidate must be
unreachable from live behavior. Steve's acceptance is an engineering-review gate:

- confirm the diff remained pure and bounded;
- confirm parity traces are understandable;
- confirm all automated verification passed;
- confirm no application behavior or milestone status was changed.

If any live behavior changes or a desktop test becomes necessary, the candidate
has exceeded this contract and must return for scope review.

## Implementation and acceptance record

- Implemented on August 22, 2026 in one new internal source file and one focused
  test file, plus test registration. Existing runtime/state/host/controller/UI
  production files were not changed.
- Accepted implementation commit: `93ece64bf5cb21e67378d9ae024c6f45568fe86e`.
- The kernel is an immutable reducer over explicit state, typed inputs, typed
  decisions/reasons, supplied timestamps, and supplied inclusive transition
  targets. It performs no time, random, Windows, file, network, AI, speech, audio,
  playback, or persistence access.
- A production-source reference search found no use of the kernel outside its own
  source file. Normal launch paths therefore cannot instantiate or call it.
- Release builds of the source and test projects passed with 0 warnings and 0
  errors.
- The complete fake-driven suite passed 292 tests with 0 failures, including 14
  focused M10A parity/golden traces.
- The normal formatter was applied to the three changed C# files. Full formatting
  verification passed for the source file, and whitespace verification passed for
  both changed test files. Full semantic `dotnet format --verify-no-changes` for
  the test project crashed inside Roslyn's nullable-flow analysis; compilation
  with warnings-as-errors and the full test suite both remained clean.
- Automated verification opened no GUI, contacted no model or external source,
  produced no speech, controlled no browser/playback, enumerated no real Core
  Audio session, and changed no volume.

## Completion and milestone status

Steve reviewed and accepted the implementation on August 22, 2026. M10A is
passed. The accepted M8 runtime remains controlling because this pure kernel is
unreachable from live application paths.

No M10B work was authorized by this acceptance. Steve later authorized and
recorded the accepted M10A implementation commit as
`93ece64bf5cb21e67378d9ae024c6f45568fe86e`, and subsequently approved the
separate M10B contract and M10B-only implementation.

## Next action type

**Decision - Steve**

M10B is passed. The proposed [M10C contract](MILESTONE_10C_CONTRACT.md) is ready for Steve review only; M10C implementation and a commit remain unauthorized.
