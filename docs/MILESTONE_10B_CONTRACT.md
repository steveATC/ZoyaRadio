# Milestone 10B contract - Prepared Material Lifecycle and Binding

## Contract status

**Approved by Steve on August 22, 2026. Steve separately authorized M10B-only
implementation. Steve accepted the automatically verified implementation on
August 22, 2026. M10B is passed. No commit or M10C work is authorized.**

M10A is passed at implementation commit
`93ece64bf5cb21e67378d9ae024c6f45568fe86e`. This contract translated the
approved M10B roadmap direction into one bounded implementation scope. Steve
approved it and authorized implementation on August 22, 2026.

## Purpose

Create a pure, deterministic, directly testable lifecycle for prepared material
that makes ownership, binding, reservation, release, expiry, completion, and
disposal decisions explicit.

M10B answers one engineering question:

> Can Zoya represent who owns prepared material, where it is valid, whether it
> may be reused, and when its payload must be released before live runtime
> migration or multi-format arbitration begins?

Prepared or generated material has no right to airtime. M10B manages potential
material; it does not decide that the station should interrupt music.

## Why this milestone comes now

M10A established deterministic opportunity decisions without touching the live
runtime. Current M8 preparation code still coordinates several different concepts
through mutable fields and cleanup paths:

- an M8 opportunity requests preparation;
- asynchronous work may or may not produce a prepared speech resource;
- the result may become stale before it is retained;
- one prepared result may be taken for delivery;
- cancellation, Silent Mode, rebind, rapid transition, failure, or shutdown must
  discard it;
- successful, abandoned, and failed delivery all require cleanup;
- `PreparedTransitionOpportunity.Dispose()` is idempotent, but ownership is
  distributed across current pipeline and runtime code.

That behavior is accepted for the single current comment path. It is not yet a
shared model suitable for reusable liners, commercials, calls, or produced scenes.
M10B extracts the lifecycle rules into pure state and typed effects before M10C
connects any foundation to live resources.

## Governing accepted baseline

- Accepted M10A implementation:
  `93ece64bf5cb21e67378d9ae024c6f45568fe86e`.
- Accepted live runtime/package baseline remains Milestone 8:
  `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- Latest accepted automated result: 292 passed, 0 failed at M10A.
- M9 remains closed without final M9B runtime acceptance.
- Safety and current behavior remain governed by
  [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md),
  [DECISIONS.md](DECISIONS.md),
  [MILESTONE_10A_CONTRACT.md](MILESTONE_10A_CONTRACT.md), and
  [POST_M9_CAPABILITY_ROADMAP.md](POST_M9_CAPABILITY_ROADMAP.md).

Where current runtime code and tests reveal an edge case not covered here, the
accepted M8 invariant and M10A characterization trace govern. A genuine conflict
must stop implementation and return to Steve; it must not be silently resolved by
inventing new lifecycle policy.

## Hard dependencies

- Accepted M10A opportunity state, identifiers, decisions, and golden traces.
- Existing immutable `MediaSnapshot` input and accepted normalized exact
  title/artist identity behavior.
- Current M8 prepared-resource ownership and disposal behavior as characterization
  evidence only.
- Existing fake-driven test project.

There is no dependency on M9 enrichment, Music Knowledge, Listening Intelligence,
Station Lore, Creative Direction, Ollama, Kokoro, Core Audio, GSMTC, WinForms,
persistence, new formats, or a live delivery adapter.

## Authorized implementation surface after approval

If Steve approves this contract and separately authorizes implementation, M10B is
limited to:

1. One internal pure lifecycle source file, plus one additional small source file
   only if separating state/types materially improves readability, under
   `src/ZoyaRadio.MediaProbe/`.
2. One focused lifecycle test file and registration in the existing
   `tests/ZoyaRadio.MediaProbe.Tests` runner.
3. Minimal M10A type-access or composition adjustments only when required to use
   accepted opportunity identifiers/decisions without duplicating them.
4. Milestone documentation updates needed to record candidate verification and
   later acceptance.

No existing production launcher, runtime, controller, host, pipeline, UI, M9
candidate, or diagnostic may reference the M10B lifecycle. Normal application
paths must remain unable to instantiate or call it.

M10B must not replace `PreparedTransitionOpportunity`, alter current disposal
code, or remove legacy state. Those are possible M10C migration concerns.

## Pure architecture boundary

M10B is a deterministic reducer over explicit state and typed inputs. It returns
next state plus typed lifecycle decisions/effects.

It must not:

- read system time;
- allocate random or GUID identity;
- run asynchronous work;
- start or cancel a task;
- own an `IDisposable`, stream, audio buffer, cancellation token, model result,
  or speech object;
- call `Dispose()`;
- inspect Windows, Firefox, playback, audio, files, network, models, or UI state.

A caller supplies deterministic identifiers, timestamps, bindings, and outcome
events. A future adapter may map an opaque payload handle to an actual resource.
The pure lifecycle may emit one typed request to dispose/release that handle; it
does not perform the side effect.

## Material identity and registration boundary

Each lifecycle item must have one stable, caller-supplied material identity. The
identity must be deterministic, non-empty, and testable. M10B does not choose a
global persistence scheme or generate random IDs.

Registration must declare, at minimum:

- material identity;
- reuse policy;
- validity binding;
- explicit registration time;
- optional explicit expiry time;
- optional opaque payload handle only when a prepared payload already exists.

A raw Creative Director alternative, unselected brainstorm, prompt, provider
payload, or arbitrary generated string does not automatically enter this
lifecycle. Registration is a deterministic promotion event. This contract does
not define the future creative policy that chooses to register material.

Duplicate registration of the same active identity must be refused without
replacing ownership. If registration offers a payload that is not accepted, the
caller retains ownership and must receive a typed refusal. Re-registration of a
terminal identity is outside M10B; callers must supply a new material identity.

Registration observed at or after its declared expiry is refused as already
expired. Any offered payload remains caller-owned.

## Reuse policy

M10B supports exactly two policy categories:

### Single-opportunity material

- permanently bound to one M10A opportunity;
- normally also bound to the subject track and current logical session when those
  identities are supplied;
- cannot return to general inventory after reservation, missed delivery,
  invalidation, or release;
- becomes terminal and requests payload disposal when its opportunity is lost,
  completed, abandoned, failed, or expired.

A specific comment about a song/artist is the primary example.

### Reusable-within-binding material

- is not permanently owned by one opportunity;
- may be reserved temporarily for one potential use;
- returns to Ready when a reservation is released and its base binding and expiry
  remain valid;
- after successful use, may either return to Ready or become terminal according to
  an explicit completion disposition supplied by the caller;
- becomes terminal when completed-and-retired, explicitly discarded, expired, or
  invalidated by its base binding;
- does not imply indefinite or cross-session validity.

A prebuilt liner, evergreen fictional commercial, or reusable produced call is a
future example. M10B proves lifecycle mechanics only; it creates none of them.

No third reuse category, replay count, inventory ranking, campaign policy, or
automatic reuse cadence is in scope.

## Typed validity binding

A material binding may contain only deterministic, caller-supplied constraints:

- optional permanent M10A opportunity identity;
- optional normalized-exact subject-track identity derived from the accepted
  `MediaSnapshot` boundary;
- optional opaque logical session identity;
- optional absolute expiry timestamp.

Binding semantics are conjunctive: every declared constraint must remain valid.
Unknown or missing required binding evidence is a refusal, never a guess.

Rules:

1. Single-opportunity material requires a permanent opportunity identity.
2. Reusable material must not claim permanent ownership of one opportunity;
   temporary opportunity ownership belongs to its reservation.
3. Track comparison uses the accepted normalized exact title/artist semantics.
   No fuzzy matching, source enrichment, alias inference, or title-suffix guessing
   is permitted.
4. Session identity is an opaque deterministic token. M10B does not discover,
   compare, or repair Firefox identities.
5. A binding invalidation is explicit input. The reducer does not observe runtime
   state itself.
6. Opportunity-bound or track-bound material invalidated by a stale/mismatched
   target is discarded, not silently generalized into reusable inventory.

## Lifecycle states

The bounded lifecycle states are:

- **Preparing:** a registered lifecycle item exists, but no prepared payload is
  owned yet.
- **Ready:** a prepared payload handle is owned and the item is available within
  its binding.
- **Reserved:** exactly one reservation owns the right to attempt use; reservation
  is not airtime authorization.
- **Terminal:** the item can never be selected again. A typed terminal reason
  records Completed, Abandoned, Failed, Expired, Discarded, or BindingInvalidated.

Exact type names may differ. The model must keep availability state separate from
terminal reason and from the one-time payload-disposal effect.

M10B does not add Proposed, Approved, Aired, Canonical, Pending Lore, Open Thread,
Dormant, or Retired Campaign states. Those belong to later creative/memory
capabilities.

## Required lifecycle transitions

| Current state | Input | Required result |
| --- | --- | --- |
| Absent | Register without payload | Preparing |
| Absent | Register with payload handle | Ready |
| Absent | Register already expired, with or without payload | Refusal; no ownership transfer |
| Preparing | Preparation succeeded with handle | Ready; ownership transfer accepted |
| Preparing | Preparation abandoned/failed/expired/invalidated | Terminal; no payload-disposal effect when no handle was acquired |
| Existing item | Duplicate registration with payload | Refusal; offered handle remains caller-owned |
| Ready, Reserved, or Terminal | Attach another or late payload handle | Refusal; offered handle remains caller-owned |
| Ready | Reserve with compatible binding | Reserved |
| Ready | Reserve with incompatible binding | Refusal; state unchanged |
| Ready | Expire/discard/invalidate | Terminal plus one payload-disposal request |
| Reserved | Release reusable material while still valid | Ready |
| Reserved | Release single-opportunity material | Terminal Abandoned plus one payload-disposal request |
| Reserved | Complete reusable material and return it while still valid | Ready plus completed-use decision; no disposal request |
| Reserved | Complete and retire/consume | Terminal Completed plus one payload-disposal request |
| Reserved | Request return-to-inventory for single-opportunity material | Refusal; a valid terminal outcome is still required |
| Reserved | Abandon/fail/expire/invalidate | Terminal reason plus one payload-disposal request |
| Terminal | Any non-inspection transition | Typed already-terminal/stale refusal; no state mutation and no repeated disposal request |

A Ready item cannot complete without first being reserved. Preparing material
cannot be reserved. A second reservation cannot replace the first. Wrong material
or reservation identity must not mutate the correct item.

Payload ownership transfer is explicit. Registration or preparation may offer an
opaque handle, but lifecycle ownership begins only when the result accepts it and
moves the item to Ready. A late, duplicate, stale, expired, or otherwise refused
handle remains caller-owned. The result must say so explicitly so a future adapter
can dispose that unaccepted resource through its own protected cleanup path.

The implementation may represent terminal records in state for deterministic
inspection and disposal idempotence. Persistence, pruning, tombstone retention,
and long-running inventory limits are deferred because M10B is unreachable from
runtime.

## Reservation contract

A reservation has one deterministic caller-supplied identity and may include the
opportunity identity it intends to serve.

M10B guarantees only:

- at most one active reservation per material;
- the reservation matches the material's current binding and validity;
- only the matching reservation may release, complete, abandon, or fail it;
- repeated or mismatched reservation commands are refused without mutation;
- reservation does not mean playback, airtime, editorial priority, or canonical
  completion.

M10B does not decide whether multiple different materials may be prepared for one
future opportunity. Preparation arbitration, airtime arbitration, and reservation
of the final editorial winner belong to M11. The M10B model must not accidentally
encode a one-format scheduler.

## Completion disposition

Completing a reserved item requires one explicit caller-supplied disposition:

- **Retire/consume:** terminal Completed and dispose the owned payload exactly
  once. Single-opportunity material requires this disposition.
- **Return to inventory:** record a completed use and return still-valid reusable
  material to Ready without disposal.

Return to inventory is not an instruction to schedule another airing. M10B does
not count airings, apply cooldown, rank inventory, or decide whether repetition is
editorially acceptable. Those are later Arbiter policies. Completed use also does
not commit listening history, Station Lore, or canon.

## Expiry contract

Expiry is deterministic and explicit:

- an optional absolute `ExpiresAt` is supplied at registration;
- every expiry evaluation receives an explicit observed time;
- `observed time >= ExpiresAt` means expired;
- no timer, wall clock, background sweep, or scheduling loop is added;
- expiry of Ready or Reserved material emits disposal exactly once;
- expiry before payload acquisition creates a terminal record without a disposal
  effect;
- a released reusable item returns to Ready only when it is not expired and its
  base binding remains valid.

Exact commercial/call/liner shelf life is not chosen here. Those values remain
format- and evidence-dependent policy supplied by future callers.

## Disposal and payload ownership contract

The lifecycle stores only an opaque payload handle. It emits a typed
`DisposePayload`-equivalent effect when ownership must end.

Required guarantees:

1. A handle is accepted into lifecycle ownership at most once.
2. An offered but unaccepted handle remains caller-owned and emits no lifecycle
   disposal request; a typed refusal preserves the caller's cleanup obligation.
3. A material that never acquired a handle emits no disposal request.
4. A material that acquired a handle emits exactly one disposal request across
   completion, abandonment, failure, expiry, invalidation, and repeated cleanup.
5. Releasing still-valid reusable material does not dispose its payload.
6. Completing still-valid reusable material with Return to inventory records the
   use and retains its payload without disposal.
7. Releasing single-opportunity material or completing any material with
   Retire/consume disposes it.
8. A stale or wrong identity never disposes another material.
9. Terminal replays are idempotent and emit no second disposal request.
10. The pure reducer does not claim that the real resource was disposed; it records
   only the deterministic ownership decision/effect.

M10C must later prove that a live adapter executes those effects through protected
cleanup. M10B neither implements nor authorizes that adapter.

## Composition with M10A

M10B must use M10A identifiers and decisions rather than reimplement scheduling.

Focused composition traces must demonstrate the intended mapping:

- M10A `PreparationRequested` may register one single-opportunity Preparing item
  bound to that opportunity and subject track.
- Successful preparation may attach one opaque handle and move it to Ready before
  M10A is marked preparation-ready.
- M10A `DeliveryDue` may reserve the matching Ready material.
- M10A pending-opportunity invalidation, missed preparation, cancellation, Silent
  Mode, clear history, rebind, rapid transition, disarm, abandoned resolution, or
  failed resolution invalidates the corresponding single-opportunity material.
- Completed resolution terminally completes and disposes the matching material.
- A stale M10A opportunity identity cannot reserve, complete, or dispose unrelated
  material.
- No lifecycle event creates a new M10A opportunity, changes its cadence, or
  changes its schedule target.

The composition test harness remains test-only. Production code must not reference
M10B until a separately approved M10C migration.

## Typed decisions and reason categories

Results must use closed typed reasons/effects sufficient to distinguish:

- registered Preparing;
- registered Ready;
- preparation became Ready and accepted payload ownership;
- offered payload refused with caller ownership retained;
- reservation acquired;
- reservation conflict;
- binding mismatch;
- reservation released to reusable inventory;
- single-opportunity release discarded;
- completed and returned to reusable inventory;
- completed and retired/consumed;
- abandoned;
- failed;
- expired;
- binding invalidated;
- payload disposal requested;
- duplicate registration refused;
- stale/wrong material refused;
- stale/wrong reservation refused;
- invalid transition refused;
- already terminal/idempotent no-op.

Exact names may differ. Free-form log messages are not the lifecycle contract.
Editorial Arbiter reasons, creative relevance, format category, and runtime safety
reasons must not be folded into this enum.

## Required automated traces

At minimum, focused tests must cover:

1. Register Preparing, attach payload, reserve, complete, and emit one disposal.
2. Register already prepared, reserve, abandon, and emit one disposal.
3. Preparation failure before a payload exists emits no disposal.
4. Late preparation success after invalidation refuses ownership; the offered
   handle remains caller-owned and no lifecycle disposal is emitted.
5. Duplicate registration with an offered payload refuses ownership without
   replacing or disposing the active payload.
6. Registration observed at or after expiry refuses ownership deterministically.
7. Ready material refuses a second reservation.
8. Wrong reservation identity cannot release, complete, fail, or dispose.
9. Reusable reservation releases to Ready without disposal and may be reserved
   again.
10. Reusable completion with Return to inventory records the use, stays Ready,
    emits no disposal, and may be reserved again.
11. Reusable completion with Retire/consume becomes terminal and disposes.
12. Single-opportunity release becomes terminal and disposes; Return to inventory
    is refused.
13. Expiry immediately before and exactly at the boundary.
14. Ready and Reserved expiry each dispose exactly once.
15. Opportunity, track, and session binding mismatch/invalidation.
16. Stale/wrong material identity cannot affect another item.
17. Repeated terminal cleanup emits no second disposal.
18. M10A happy-path composition: prepare, ready, delivery due, reserve, complete
    with Retire/consume.
19. M10A missed-preparation and rapid-transition invalidation composition.
20. M10A Silent Mode, Clear History, rebind, cancellation, and disarm invalidation
    composition.
21. Reusable material survives a released missed opportunity only while its base
    binding and expiry remain valid.
22. Existing complete fake-driven suite remains green.
23. Production-reference search proves the lifecycle is unreachable from normal
    launch paths.

Tests use explicit identities, tracks, session tokens, timestamps, payload handles,
and M10A transition targets. They must not use sleep, wall-clock time, Windows
media/audio APIs, models, network access, or real disposable speech objects.

## Explicit non-goals

M10B must not:

- connect M10A or M10B to `RadioBehaviorState`, `RadioBehaviorRuntime`,
  `RadioBehaviorTest`, `TransitionRadioPipeline`, controllers, WinForms, or
  launchers;
- replace current M8 lifecycle/resource cleanup;
- change opportunity cadence, commentary behavior, prompts, model, voice, fallback,
  delivery timing, or audio;
- add a second format, liner, commercial, caller, rant, ID, callback, or produced
  scene;
- add preparation or airtime arbitration, music pressure, format clocks,
  editorial priority, relevance, bounded randomness, or silence policy;
- add actual inventory capacity, ranking, eviction, persistence, cache files,
  serialization, migration, or UI;
- add Creative Context, Creative Direction, Content Generation, Music Knowledge,
  Listening Intelligence, Station Lore, open threads, or lore transactions;
- treat successful lifecycle completion as proof of airtime or canonical lore;
- add content scripts, audio assets, TTS voices, multi-voice rendering, FX, beds,
  telephone treatment, or sonic identity;
- add delivery-treatment modeling, program breaks, pause/hold/resume, Firefox
  targeting changes, or Core Audio changes;
- add packages, services, network endpoints, models, or configuration UI;
- alter retained M9 code or claim M9B acceptance;
- begin M10C migration.

## Accepted behavior that must remain unchanged

Because M10B remains unreachable:

- accepted M8 live behavior and M10A opportunity decisions remain unchanged;
- only one M8 comment opportunity exists in the current runtime;
- no missed work creates a catch-up queue;
- stale, cancelled, late, ambiguous, or unsafe work still becomes silence;
- current preparation cancellation and idempotent prepared-speech disposal remain
  unchanged;
- user playback intent and Silent Mode retain priority;
- normal automatic delivery never pauses, resumes, or toggles playback;
- Firefox-only proportional ducking and exact captured-state restoration remain
  unchanged;
- M8 prompt/model/voice/fallback and in-memory history remain unchanged;
- M9 diagnostic/candidate behavior and isolation remain unchanged;
- no new logs, files, persistence, UI, audio, speech, or desktop action occurs.

## What M10B unlocks

After implementation, automated verification, and explicit Steve acceptance:

- M10C can build a live adapter that maps current prepared speech ownership onto
  deterministic lifecycle effects while preserving strict M8 parity.
- M11 can arbitrate among multiple candidates without conflating eligibility,
  editorial choice, and resource ownership.
- Later liners, commercials, calls, and scenes can share one distinction between
  single-opportunity material and reusable-within-binding inventory.
- Future memory work can reason about aired/canonical effects without confusing
  them with generated, prepared, reserved, or discarded material.

M10B does not authorize any of those follow-on milestones.

## Main risks and required containment

| Risk | Required containment |
| --- | --- |
| Pure state owns real disposable resources | Store opaque handles only; emit typed disposal effects |
| Reusable content leaks across context | Conjunctive explicit bindings; unknown/mismatch refuses |
| Song-specific content returns to inventory | Single-opportunity policy cannot release to Ready |
| Reservation becomes airtime authority | Contract and types state that reservation is ownership only |
| Returned material becomes automatic repetition | Return changes availability only; M11 owns spacing and selection |
| Completion is confused with canon | No lore/history commit exists in M10B |
| Duplicate cleanup disposes twice | Terminal idempotence and exactly-once disposal-effect tests |
| Late preparation leaks an unaccepted payload | Explicit ownership-transfer result; caller retains refused handle |
| Stale identity disposes current material | Material and reservation IDs must both match |
| Expiry becomes a hidden clock | Caller supplies every observed timestamp |
| M10B grows into an inventory scheduler | No ranking, capacity, eviction, cadence, or format selection |
| Lifecycle duplicates M10A scheduling | Test-only composition consumes M10A decisions/IDs |
| Foundation enters live runtime early | Production-reference scan and no existing production edits |
| M8 cleanup semantics regress | Complete existing suite plus no runtime references |

## Automated verification after authorized implementation

Required evidence:

1. Release build of `src/ZoyaRadio.MediaProbe` with 0 warnings and 0 errors.
2. Release build of `tests/ZoyaRadio.MediaProbe.Tests` with 0 warnings and 0
   errors.
3. Complete fake-driven suite passes, including every existing M10A trace.
4. Focused M10B transition-table, binding, reservation, release, expiry, and
   disposal tests pass.
5. Test-only M10A/M10B composition traces pass.
6. Invalid transitions prove state and effects remain unchanged.
7. Disposal-effect counts prove exactly-once behavior.
8. Reference search proves no production source outside the lifecycle definition
   references M10B.
9. Forbidden-dependency scan finds no Windows, audio, playback, file, network,
   model, clock, random, task, or persistence access.
10. `git diff --check` and focused formatting verification pass.

The final test count is evidence produced by implementation; this contract does
not guess it in advance.

## Steve/manual evaluation

No audible or real-desktop test is required. M10B must be pure and unreachable.

Steve's acceptance review is an engineering review:

- lifecycle table matches the approved distinction between disposable
  opportunity-bound material and reusable material;
- reservation does not imply airtime;
- disposal effects are explainable and exactly once;
- no creative, scheduling, runtime, or persistence policy leaked into the model;
- automated evidence and the production-reference proof are complete.

If implementation makes a desktop test necessary, it has exceeded this contract
and must return for scope review.

## Implementation and acceptance record

- Steve accepted this implementation on August 22, 2026. M10B is passed; this acceptance does not authorize a commit or any M10C work.
- Implemented on August 22, 2026 as two new internal source files, one focused
  test file, and registration in the existing test runner. No existing runtime,
  controller, launcher, host, pipeline, UI, M9 candidate, or audio path references
  the lifecycle.
- The candidate is an immutable reducer over explicit state, caller-supplied
  identities and timestamps, typed bindings, reservation/completion inputs, and
  typed ownership/disposal decisions. It stores opaque payload handles only and
  performs no disposal or other side effect itself.
- It supports exactly SingleOpportunity and ReusableWithinBinding material,
  conjunctive opportunity/track/session/expiry bindings, one reservation per
  material, explicit release/completion disposition, terminal idempotence, and
  caller-retained ownership for refused payloads.
- Release builds of the source and test projects passed with 0 warnings and 0
  errors.
- The complete fake-driven suite passed 308 tests with 0 failures, including 16
  focused M10B lifecycle and M10A/M10B composition traces.
- Formatting was applied to all changed C# files. Production-reference and
  forbidden-dependency scans found no live integration or Windows, audio,
  playback, file, network, model, clock, random, task, or persistence access.
- Automated verification opened no GUI, contacted no model or external source,
  produced no speech, controlled no browser/playback, enumerated no real Core
  Audio session, and changed no volume.

## Completion and acceptance

Steve reviewed and accepted the automatically verified implementation on August 22, 2026. M10B is passed.

The accepted implementation remains unreachable from production paths and the accepted M8 live runtime remains controlling. No implementation commit was created. M10C remains unstarted and requires separate future authorization.

## Next action type

**Decision - Steve**

The proposed [M10C contract](MILESTONE_10C_CONTRACT.md) is ready for Steve review only. M10C implementation and a commit still require separate explicit authorization.
