# Milestone 11B contract - Commentary and Reusable Liner Proof

## Contract status

**Passed.** Steve accepted M11B on August 24, 2026 after automated verification and the required desktop listening trial. M12 and a commit remain unauthorized.

M11A is passed. M11B is the first deliberately tiny second-format vertical proof: the accepted existing comment plus one fixed, reusable spoken liner, with direct music continuation still allowed to beat both.

## Purpose

Prove that the accepted Editorial Arbiter and prepared-material lifecycle can safely coordinate two live content clients:

1. the existing track-bound M8 comment; and
2. one short, fixed, session-reusable liner fixture.

M11B answers one question:

> Can Zoya introduce a second eligible content type without turning eligibility into airtime, disrupting accepted commentary, over-speaking, creating stale reservations, or duplicating the protected audio path?

This is an arbitration and lifecycle proof, not a station-imaging milestone.

## Why this milestone comes now

M11A established typed preparation and airtime decisions, hard eligibility, direct music continuation, policy priority, treatment compatibility, and deterministic bounded ties. Its live composition intentionally retained one format.

A reusable micro-liner is the smallest client that exposes the next real requirements:

- format-specific cooldown cannot be represented as one global comment/liner gate;
- reusable material must return safely to inventory after use or a missed opportunity;
- track-bound comments and evergreen liners require different bindings;
- two eligible formats require explicit priority;
- silence must remain common even when reusable material is ready.

These should be proved before commercials, calls, Creative Direction, lore, longer formats, or richer production make mistakes expensive.

## Governing accepted baseline

- Passed M10A Deterministic Opportunity Kernel.
- Passed M10B Prepared Material Lifecycle and Binding.
- Passed M10C accepted one-comment runtime migration.
- Passed M11A Pure Editorial Arbiter, accepted August 23, 2026 with 320/320 fake-driven tests and focused desktop evidence.
- Accepted M8 playback safety, targeting, ducking, restoration, controls, privacy, and local-first behavior remain controlling.
- [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md), [DECISIONS.md](DECISIONS.md), [POST_M9_CAPABILITY_ROADMAP.md](POST_M9_CAPABILITY_ROADMAP.md), and the passed M10/M11A contracts govern where this contract is silent.

A conflict with accepted comment behavior or playback safety must stop implementation and return to Steve. A liner never justifies weakening a refusal.

## Hard dependencies

- Passed M11A arbiter and typed decision trace.
- Passed M10B reusable-within-binding lifecycle.
- Passed M10C exact Firefox delivery, cancellation, cleanup, and controls.
- Existing accepted local speech-service boundary and replayable prepared-speech behavior.
- Existing deterministic clocks, target/session identity, and fake-driven tests.

M11B has no dependency on Music Knowledge, Listening Intelligence, Station Lore, Creative Context, Creative Direction, Content Generation, new AI models, voice abstraction, cast roles, localization, program-break hold, Firefox distribution repair, sonic identity, or configuration extraction.

## Proposed fixed fixture

M11B introduces exactly one copy fixture:

> Zoya Radio.

The fixture is fixed, not AI-generated; uses the accepted Nicole/local fallback path; is prepared locally and held only in memory; is reusable only in the current logical runtime session; has a 4-second maximum prepared duration; and uses only DuckOverMusic.

It is not a jingle, ID package, sting, bumper, advertisement, lore entity, or configurable asset. Steve may change the exact text while reviewing this contract. After approval, implementation may not casually add variants.

## Authorized implementation surface after approval

If Steve approves this contract and separately authorizes M11B-only implementation, edits are limited to:

1. Minimal M11A refinement demonstrated by the second client:
   - add ReusableLiner as the second format;
   - distinguish only the opportunity classes needed for comment preparation/delivery and liner inventory/transition consideration;
   - move format-specific cooldown/music-pressure facts into explicit per-candidate typed gates while retaining session-wide facts separately;
   - add only the typed reasons needed to explain those gates.
2. One small in-memory reusable-liner inventory/coordinator under src/ZoyaRadio.MediaProbe.
3. Minimal M10B/M10C composition to register, bind, reserve, release, complete, return, invalidate, and dispose the liner without changing M10B reducer semantics.
4. A minimal shared overlay-delivery seam routing the liner through the existing exact-Firefox DuckOverMusic host, safety checks, activity, cancellation, and restoration. A parallel audio pipeline is forbidden.
5. Focused M11B fake-driven tests and only necessary regression-test adjustments.
6. Candidate and acceptance documentation.

No package, external service, network dependency, model, voice, audio asset, persistent store, executable, or configuration UI is authorized.

## Architectural responsibilities

### Editorial opportunity classes

M11B may add only narrowly demonstrated typed classes:

- existing comment preparation;
- existing comment delivery;
- reusable liner inventory preparation;
- ordinary transition overlay consideration.

Comments retain M10A opportunity identities. A liner airtime candidate uses the genuine transition sequence plus current logical runtime-session identity and must not pretend to be a track-bound M10A comment.

This does not authorize a universal program clock, arbitrary slots, long-form boundaries, or speculative future opportunity classes.

### Candidate-specific policy gates

M11A's one-format live path can use one pressure snapshot. M11B proves two clients need some facts to differ per candidate.

The arbiter may therefore receive named typed candidate facts for:

- format cooldown;
- speech spacing;
- candidate music-pressure permission;
- preparation capacity;
- required/compatible treatment;
- policy priority.

These are gates, not a numeric utility score. Existing comment cadence cannot be reinterpreted through liner policy. Knowledge, lore, relevance, and salience remain outside M11B.

### Preparation arbitration

The liner contains no generation step. Preparation synthesizes the fixed text through the existing local speech service and registers the opaque payload through M10B.

Rules:

1. Liner preparation is low-priority inventory work.
2. It must not delay Start, pause/duck music, contact Ollama, or occupy an accepted comment opportunity.
3. Runtime may request it after arming or at a later safe transition using a current-session inventory-preparation identity.
4. If comment and liner preparation compete for one bounded capacity, the expiring comment wins.
5. Cancelled, refused, late, failed, or over-4-second liner work creates no airtime debt and disposes caller-owned resources.
6. Retry occurs only at a later explicit safe preparation opportunity with bounded backoff, never a tight loop.
7. M10B ownership acceptance is required for readiness.

Implementation must prove current prepared speech is safely replayable serially. Concurrent playback of one prepared resource is forbidden.

### Reusable lifecycle and binding

The liner is one M10B ReusableWithinBinding item:

- current logical-session binding only;
- no permanent track or comment-opportunity binding;
- in-memory only;
- one exact transition reservation at a time;
- explicit ReturnToInventory after completed use;
- release to Ready after missed/refused pre-air use only while session binding remains valid;
- exact one-time invalidation/disposal on rebind, Stop, shutdown, replacement session, terminal failure, or discard.

Silent Mode suppresses eligibility without needlessly destroying valid session inventory. Clear History must not turn the liner into listening history or lore; its exact inventory effect must be harmless and explicit. Nothing survives process exit.

A lost reservation cannot remain silently Reserved. M10B must release, invalidate, or terminalize it according to current evidence.

### Airtime arbitration

At each genuine transition, runtime may submit:

- existing comment when M10A reports delivery due and M10B reports Ready;
- liner when inventory and experimental policy gates pass;
- direct music continuation.

Rules:

1. Hard eligibility precedes comparison.
2. Valid track-bound comment has Timely priority over Routine liner.
3. Liner is ineligible where new comment preparation begins.
4. A missed/unready comment is not replaced by a liner.
5. If selected comment later fails final safety, liner is not an immediate fallback; silence wins.
6. When only liner remains, direct music may still win through explicit pressure/spacing policy.
7. Bounded tie-breaking applies only after every named gate; initial live fixture should need no random tie.
8. Exactly one result may be reserved.
9. Final pre-air revalidation remains mandatory.
10. Only complete successful airtime updates normal liner cooldown. Bounded operational failure backoff may prevent immediate retry without becoming canonical memory.

A ready liner may remain unused for the whole session.

## Provisional M11B experimental policy

The initial policy is deliberately conservative. A liner is eligible only when all are true:

- at least 6 genuine transitions since arm or the last completed liner;
- at least 20 minutes since arm or the last completed liner;
- at least 2 genuine transitions since any completed host speech;
- no comment preparation begins on this boundary;
- no comment delivery was missed on this boundary;
- liner is Ready, session-valid, unexpired, and unreserved;
- DuckOverMusic is available;
- exact current Firefox transition target remains safe.

Both liner transition and elapsed-time gates are required. These are named M11B experimental defaults, parameterized in fake tests and evaluated through listening—not permanent policy or generalized user settings.

Any operational failure backoff must be an explicit typed bounded rule, never hidden in a score.

## Delivery treatment and safety

The liner uses only accepted DuckOverMusic:

- current track and exact selected Firefox session positively revalidated;
- exactly one eligible current Firefox Core Audio session;
- existing 2-second transition window;
- proportional 25% application-session duck;
- exact scalar/mute restoration on the same session;
- no endpoint/master-volume change;
- no pause, resume, toggle, seek, or blind playback command;
- user intent overrides Zoya;
- unsafe, late, missing, ambiguous, replaced, or inactive ownership yields silence.

The liner may refine/share existing transition safety and ducking. It must not copy Windows targeting/restoration into a format-specific pipeline.

## Successful-airtime and history effects

M11B adds only memory-only operational state:

- last completed liner transition/time;
- last completed host-speech transition/time if needed for spacing;
- bounded failure/retry state if implemented.

This is neither Listening Intelligence nor Station Lore. Refused, cancelled, failed, partial, or unaired liner attempts create no canon. A partial attempt may affect only bounded operational anti-retry pressure. There is no lore, thread, profile, diary, or durable store.

## Accepted behavior that must remain unchanged

M11B preserves:

1. Existing comment generation, Track-A preparation, Track-B delivery, 2-5 transition scheduling, history, and fallback.
2. M10A opportunity and M10B lifecycle/reservation/disposal authority.
3. M11A hard eligibility, typed reasons, direct music continuation, and randomness boundary.
4. One active delivery and one protected audio target at a time.
5. Firefox-only targeting, Chrome isolation, ducking, restoration, and safe refusal.
6. Silent Mode, Clear History, rapid invalidation, rebind, Stop, shutdown, controller status, and ready-state behavior.
7. No catch-up queue and no automatic playback controls.
8. Prompts, models, M9 isolation, Nicole/fallback, privacy, logs, packaging, and distribution status except factual M11B diagnostics.

The liner may add an occasional audible event only within this contract. It cannot shift the comment schedule or consume a comment slot.

## Explicit non-goals

M11B must not add:

- AI-generated/contextual liners;
- multiple liner texts, copy rotation, localization, selectable voice, or configuration UI;
- IDs, jingles, stings, beds, effects, imaging, or sonic identity;
- commercials, calls, rants, callbacks, segues, chains, arcs, characters, or lore;
- Creative Context/Direction/Generation, Music Knowledge, Listening Intelligence, Station Lore, open threads, or persistence;
- program-break hold, playback control, treatment generalization, or duration-based pause;
- generalized station clock, permanent cadence, relevance score, plug-in system, or arbitrary numeric priority;
- dependencies, distribution repair, station builder, broad refactors, or weakened tests.

## Main risks and containment

| Risk | Required containment |
| --- | --- |
| Liner becomes annoying | Both-gates cadence, shared speech spacing, silence winner, Steve evaluation. |
| Ready inventory forces airtime | Readiness is eligibility only; music remains valid. |
| Liner steals comment slot | Timely comment priority, protected preparation/missed boundaries, one reservation. |
| Resource leaks/disposes early | Session binding, explicit release/return/invalidation, serial replay, one-time disposal. |
| Final refusal causes fallback chatter | No second-choice catch-up. |
| Policy becomes universal scoring | Named typed gates only. |
| Audio safety is duplicated | One shared DuckOverMusic path. |
| Guesses become permanent | M11B values remain experimental and reviewed. |
| Stop/rebind leaves inventory alive | Session invalidation and one-time disposal. |

## Required automated verification for an implementation candidate

Current evidence must include:

1. Release builds of media probe, tests, and desktop app with 0 warnings/errors.
2. Complete fake suite with all M8-M11A regressions.
3. Two-format matrices: comment wins conflict; liner wins only with all gates; music wins cooldown/time/spacing/treatment/policy cases; missed comment is not filled; deterministic ties cannot create eligibility.
4. Preparation tests: comment outranks liner, singular capacity, failed/cancelled/over-duration ownership, bounded retry.
5. Lifecycle tests: reserve, release, ReturnToInventory, serial replay, session invalidation, exact one-time disposal.
6. Delivery tests: exact track/session, 2-second window, Firefox-only 25% duck, exact restoration, Chrome isolation, no playback command.
7. Silent Mode, Clear History, rapid transition, unsafe targets, inactive audio, Stop during preparation/delivery/restoration, window close, shutdown.
8. Exact just-before/inclusive boundary tests for 6 transitions, 20 minutes, and 2 transitions using fake clocks.
9. Only completed liner resets normal cooldown; failed/partial attempts create no canon.
10. Production review: one fixed fixture only, no AI/network generation, no parallel Windows audio path.
11. git diff --check and final-diff review.

Tests use fake media, audio, speech, clocks, targets, transition/session identities, lifecycle payloads, and tie inputs. They do not open GUI, contact services, speak, control Firefox, or enumerate Core Audio.

## Implementation candidate evidence

Candidate verification completed August 24, 2026:

- Release builds for `ZoyaRadio.MediaProbe`, `ZoyaRadio.MediaProbe.Tests`, and `ZoyaRadio.ControlApp` each passed with 0 warnings and 0 errors.
- The complete fake-driven suite passed 337/337, including 17 focused M11B cases for comment priority, no liner fallback on protected boundaries, exact provisional cooldown/spacing boundaries, Silent Mode/music selection, reusable reserve/release/complete, serial replay, concurrent-use refusal, cancellation, over-duration disposal, fixed copy, shared duck-host delivery, and exact-target safety.
- Production review found exactly one fixed liner copy (`Zoya Radio.`), no liner AI/network generation, no new playback command, and one shared protected Core Audio duck/speech/restoration implementation for comments and the liner.
- `git diff --check` and final scope review passed; only expected M11B files plus preserved earlier uncommitted milestone work remain.

This is automated candidate evidence only. No real liner was spoken by Codex, no GUI was opened, and no Firefox, Chrome, Core Audio session, Ollama service, or local speech device was controlled during verification.
## Steve/manual evaluation after automated verification

Steve's acceptance trial is conservative real listening, not a forced demo:

1. Run normal Zoya with Firefox and Chrome playing.
2. Confirm comments retain accepted cadence/behavior.
3. Confirm liner is exactly "Zoya Radio.", very short, accepted voice/fallback, and ducks only Firefox.
4. Confirm no liner before both 6-transition and 20-minute initial gates.
5. After a completed liner, confirm no second liner before both cooldown gates and shared speech spacing.
6. Confirm ordinary transitions frequently remain uninterrupted.
7. Review logs explaining comment, liner, or music and lifecycle return/release.
8. Exercise Silent Mode and Stop; confirm no late liner and safe ready-state/cleanup.
9. Judge flow, annoyance, voice fit, and whether policy needs adjustment.

Automated matrices carry impractical race/failure cases; Steve need not manufacture them unless evidence requires investigation.

## Steve acceptance record - August 24, 2026

- Steve completed the bounded listening trial represented by `radio-behavior-20260824-002307-890.txt`, `radio-behavior-20260824-174033-498.txt`, and `radio-behavior-20260824-180501-977.txt`, then explicitly accepted M11B.
- The accepted run delivered the fixed `Zoya Radio.` liner once at transition 10, more than 20 minutes after arming. Earlier eligible-looking boundaries correctly selected direct music because the provisional liner gates had not all passed.
- The liner used the shared protected overlay path: Firefox was ducked from 100% to 25%, speech completed, the same Firefox session was restored to exactly 100%, and the reusable payload returned to Ready inventory.
- Existing commentary retained priority at its protected preparation/delivery boundaries, while ordinary transitions frequently continued music. Silent Mode cancelled pending commentary without late speech, and Stop returned cleanly.
- Steve judged the 20-minute value acceptable for this proof but potentially too slow for timely manual verification. It remains a provisional experimental default, not permanent station policy; no policy adjustment was requested or made in M11B.
- The observed enriched-comment quality concern belongs to later format-specific Creative Direction and Content Generation work. It does not change the accepted M11B arbitration/lifecycle result.
- No build or automated test was rerun for this documentation-only acceptance update. The accepted automated evidence remains the three zero-warning Release builds and 337/337 fake-driven tests recorded above.
- No commit was authorized or created. M12 was not begun.

## Completion and acceptance

Automated verification creates an M11B candidate; it does not pass automatically. M11B passes only after:

1. implementation stays in contract;
2. required current automated evidence passes;
3. Steve accepts the listening trial;
4. any policy adjustment is explicitly recorded and reverified;
5. canonical documentation records acceptance.

No implementation commit is automatic. M12, AI liners, other formats, and later capabilities remain unauthorized.

All five acceptance conditions are satisfied. M11B is passed.

## What M11B unlocks

Accepted M11B proves:

- one arbiter handles two live formats plus music;
- track-bound and reusable material coexist;
- candidate-specific gates remain explainable;
- one prepared payload safely returns to inventory;
- later clients can use shared seams instead of format-specific schedulers.

It would not prove liners are creatively mature or that every future format deserves runtime.

## Next action type

**Next action type - Documentation/Planning (Codex/Work)**

Prepare the bounded M12A Creative Context Assembler v1 contract for Steve review. Do not implement M12A, begin later M12 work, or create a commit without separate authorization.
