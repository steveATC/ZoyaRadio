# Milestone 11A contract - Pure Editorial Arbiter

## Contract status

**Passed.** Steve accepted M11A on August 23, 2026 after automated verification and focused desktop parity/reason-trace review. M11B remains unauthorized, and no commit is authorized.

M10A, M10B, and M10C are passed foundations. M11A is the next narrow step: a pure, deterministic way to choose one eligible prepared item or direct music continuation. It does not introduce a second on-air format. The reusable micro-liner remains the separate M11B proof.

## Purpose

Establish a pure **Editorial Arbiter** that makes an explainable deterministic choice at two points:

1. **Preparation arbitration:** whether a presently eligible opportunity may claim bounded preparation capacity.
2. **Airtime arbitration:** whether one eligible ready item should be reserved for delivery or direct music continuation (silence from Zoya) wins.

The arbiter is editorial policy, not a broadcast controller. It consumes typed facts and returns a typed decision with typed reasons. M10A remains authority for opportunity timing and validity; M10B for material state, binding, reservation, terminal disposition, and disposal; the runtime for real work, final safety checks, delivery, and protected cleanup.

## Why this milestone comes now

M10C proved the accepted single M8 comment can use M10A/M10B without listener-visible change. Before adding a liner or richer format, Zoya needs a coherent clock that can explain why content was prepared, why one item won, and why music continued.

M11A builds that decision seam before a second-format client makes policy harder to isolate. It is not a claim that present evidence can model future creative relevance, lore, music knowledge, or program breaks.

## Governing accepted baseline

- Accepted M8 runtime/package baseline: implementation commit `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- Passed M10A Deterministic Opportunity Kernel: implementation commit `93ece64bf5cb21e67378d9ae024c6f45568fe86e`.
- Passed M10B Prepared Material Lifecycle and Binding: accepted August 22, 2026; no M10B implementation commit exists.
- Passed M10C Accepted M8 Runtime Migration: Steve accepted August 23, 2026; 314 fake-driven tests passed in the accepted working tree; no M10C implementation commit exists.
- `CURRENT_ARCHITECTURE.md`, `DECISIONS.md`, `POST_M9_CAPABILITY_ROADMAP.md`, and passed M10 contracts govern where this contract is silent.

If this contract conflicts with accepted M8 safety or M10A/M10B authority, stop and return to Steve. The arbiter must not reconcile a conflict by inventing policy or weakening a refusal.

## Hard dependencies

- Passed M10A kernel, including M8 opportunity/parity traces.
- Passed M10B lifecycle/binding, including typed reservation and one-time terminal disposal.
- Passed M10C live migration and accepted single-comment safety baseline.
- Existing fake-driven harness and deterministic clocks/targets.

M11A has no dependency on M11B, Music Knowledge, Listening Intelligence, Station Lore, open threads, Creative Context, Creative Direction, Content Generation, voice/cast work, localization, a new model, audio production, program breaks, Firefox distribution repair, or configuration.

## Authorized implementation surface after approval

If Steve approves this contract and separately authorizes M11A-only implementation, edits are limited to:

1. Pure typed Editorial Arbiter inputs, decisions, reason categories, policy profile, and deterministic selection logic under `src/ZoyaRadio.MediaProbe/`.
2. Focused composition seams feeding already-available M10A/M10B facts to the arbiter and consuming its typed preparation/airtime decisions for the existing single-comment path only.
3. Focused fake-driven M11A tests and runner registration, plus only existing-test changes needed to preserve M10C parity.
4. Candidate/acceptance documentation updates required by this contract.

Readable diagnostics may be exposed through existing logs, but logs remain observations of typed decisions, never an alternate policy engine. Do not create a parallel runtime, second media watcher, generic plug-in scoring system, or new prepared-content scheduler.

## M11A responsibility boundary

### Inputs and opportunity classification

The arbiter receives only typed deterministic facts available at M11A:

- M10A opportunity identity, stage, deadlines/readiness, and logical target binding;
- M10B material identity, lifecycle state, binding, freshness/expiry, and reservation eligibility;
- existing comment format descriptor and known duration/treatment requirements;
- immediate M8/M10C history needed for already-accepted speech spacing and music-first pressure; and
- a bounded M11A policy profile plus explicit tie-break seed/input.

M11A may classify only what the existing comment path requires. Its sole supported treatment is accepted duck-over-continuing-Firefox delivery. It must reject an item whose required/compatible treatment is unavailable. It must not invent hold, pause, resume, program break, intro/outro overlay, long-form-safe boundary, or duration-based pause rule.

### Hard eligibility first

Before comparison, refuse candidates that are unsafe/not usable: mismatched or stale binding, incorrect opportunity/material state, expired/not-ready/already-terminal material, reservation conflict, unavailable treatment, missed timing, or violated applicable cooldown/spacing preconditions.

Hard refusal is never repaired by priority, relevance, or randomness. The result must distinguish an ineligible candidate from one that was eligible but lost to music continuation.

### Preparation arbitration

For the one existing comment, preparation arbitration decides whether M10A's accepted preparation request may claim the one bounded existing preparation path. It preserves M8/M10C one-ahead timing, one active path, cancellation, and no-catch-up behavior. Its record says whether preparation was allowed, refused by hard eligibility/capacity, or left unused by music-first/spacing policy.

It does not introduce speculative pre-generation, inventory fill, or parallel preparation for future formats.

### Airtime arbitration

For the one existing comment, airtime arbitration chooses exactly one:

- reserve the exact eligible M10B material for existing ducked delivery; or
- direct music continuation.

It considers in this responsibility order:

1. opportunity/treatment compatibility;
2. hard eligibility and reservation safety;
3. currently available music-first pressure, immediate speech density, and applicable spacing/cooldown;
4. the existing comment's bounded policy priority and only relevance facts genuinely present at M11A; and
5. bounded seeded tie-breaking only among editorially equivalent eligible alternatives.

Direct continuation is a real winner, not a failure/default. Randomness may never manufacture the reason to interrupt music. Reserve no more than one material. The live runtime still performs existing final pre-air target/safety revalidation; later refusal remains silence with normal M10A/M10B resolution and protected cleanup.

### Reasons, policy, and future relevance

Every decision returns stable typed reason categories answering: what was considered; which hard gate accepted/refused it; whether music pressure/spacing or policy priority affected the outcome; whether silence won; and whether seeded tie-breaking was used.

M11A may use a small explicit named experimental profile. Any cadence, cooldown, or density values are conservative M11A test defaults, parameterized in tests and subject to real listening evaluation. They are not durable station-clock policy and must not hide in aggregate scores.

Types may reserve narrowly named places for future deterministic contributors, but M11A evaluates only the facts above. Later milestones add each input and reason deliberately. M11A must not introduce a universal relevance score, arbitrary score plug-ins, Music Knowledge/Lore placeholders, or a framework forcing future context domains into its shape.

## Required accepted behavior preserved

Unless a later separately accepted M11B policy experiment changes them, M11A preserves:

1. M8 commentary as the only on-air format and candidate client.
2. M10A authority for Track-A preparation/Track-B delivery timing, invalidation, identity, and accepted 2-5 transition behavior.
3. M10B authority for binding, ready/reserved/terminal state, exact material identity, disposal, and stale-work refusal.
4. At most one comment opportunity and one automatic live path; no catch-up, backlog, or inventory.
5. Track-A preparation while Firefox plays; existing ducked speech over music only.
6. No automatic pause, resume, toggle, seek, other playback control, endpoint-volume, or master-volume change.
7. Exact Firefox targeting, Chrome isolation, delivery-time revalidation, ducking/restoration, cancellation, Stop, Silent Mode, Clear History, rebind, shutdown, and safe silence.
8. Existing generation/fallback, Nicole/Windows-TTS, model policy, duration limit, privacy boundary, UI/controller, and M9 isolation except factual arbiter diagnostics.

For a single eligible comment under an equivalent M8 policy profile, M11A reproduces the accepted M10C decision path. The arbiter is a policy seam, not permission to retune the station by accident.

## Explicit non-goals

M11A must not:

- add M11B's reusable micro-liner, ID, commercial, caller, rant, callback, chain, arc, second host format, or reusable on-air inventory;
- add a format clock, permanent cadence values, generalized multi-format ranking, arbitrary numeric relevance/utility scoring, or uncontrolled random selection;
- add Music Knowledge/source adapters, Listening Intelligence, durable memory, Station Lore/open threads, Creative Context, Creative Direction, premise/script generation, or AI alternatives;
- alter prompts, grounding, models, Ollama, voice/renderer/cast, language/locale, production/audio assets, or sonic identity;
- add delivery-treatment generalization, pause/hold/resume, program break, targeting change, or duration-triggered playback control;
- add packages, services, network access, persistence, caches, migrations, distribution work, configuration UI, or station builder; or
- delete tests or weaken M8/M10 acceptance checks.

## Main risks and containment

| Risk | Required containment |
| --- | --- |
| New arbiter changes accepted M8 behavior | Characterization/golden traces prove the equivalent one-comment profile remains M10C-equivalent. |
| Silence becomes an afterthought and Zoya talks too often | Model direct continuation explicitly; table-test it as winner under pressure/spacing cases. |
| Randomness causes interruption | Allow seeded tie-breaking only after hard eligibility and editorial equivalence; emit the tie reason. |
| Opaque policy or magic cadence values | Named inputs, typed reasons, small parameterized experimental profile. |
| Future relevance overbuilt before evidence | Accept M11A facts only; later typed inputs require contracts. |
| Unsafe/wrong material reserved | M10A/M10B identity checks, one exact reservation, existing final pre-air validation. |
| Treatment abstraction creates program breaks | Support existing ducked treatment only; refuse incompatible/unavailable treatment. |
| Runtime diverges from pure decision | Keep arbiter fake-driven and pure; runtime maps typed decisions and retains M10C safety paths. |

## Required automated verification for an implementation candidate

Current - not historical - evidence must show:

1. Release source/test builds with 0 warnings and 0 errors.
2. Complete fake-driven suite passing, including all M8, M9, M10A/B/C tests.
3. Pure table-driven preparation traces: accepted Track-A preparation, unavailable capacity, stale/mismatched opportunity, Silent Mode/cancellation-equivalent input, no catch-up.
4. Pure airtime traces: valid comment reservation, each hard refusal, treatment mismatch, expired/not-ready/already-reserved material, direct continuation.
5. Music-pressure/spacing traces proving direct continuation can win without a must-speak clock.
6. Stable typed reasons: considered candidates, hard refusals, silence, policy gates, reservation, and seeded true-tie handling.
7. Identity/reservation traces: wrong/stale material cannot reserve; at most one exact item reserves; final runtime revalidation turns changed/unsafe target into silence/cleanup.
8. M10C parity: equivalent one-comment scheduling, preparation, delivery, Stop/cancellation, target refusal, Firefox-only ducking, exact restoration, Chrome isolation, and no playback-control command.
9. `git diff --check` and complete final-diff review.

Tests use fakes for media, audio, speech, clocks, targets, tie input, and disposal. They must not open the GUI, contact Ollama/external services, speak audibly, control Firefox, or enumerate real Core Audio.

## Implementation-candidate verification - August 23, 2026

- Release builds of the media-probe source, fake-driven test project, and desktop control app passed with 0 warnings and 0 errors.
- The complete fake-driven suite passed **320 passed, 0 failed**. This retains all 314 accepted M10C tests and adds six focused M11A tests for preparation eligibility, airtime hard refusals, music/spacing/cooldown pressure, priority and deterministic bounded ties, M10C-equivalent existing-comment composition, and policy suppression without reservation.
- Production review confirmed that the existing comment alone passes through preparation arbitration before claiming the existing pipeline and through airtime arbitration before M10B reservation. M10A still creates/invalidates opportunities, M10B still owns material reservation/disposal, and the existing runtime still performs final pre-air target validation and protected Firefox-only delivery cleanup.
- The live profile is explicitly M10C-equivalent: it adds typed reason diagnostics without adding a format, changing the accepted 2-5 transition schedule, or creating new music-pressure/cooldown values.
- Automated tests used fakes and did not open the GUI, contact Ollama or external services, produce audible speech, control Firefox, or enumerate real Core Audio.
- git diff --check reported no whitespace errors. No commit was created.

Steve completed and accepted the manual reason-trace and desktop parity evaluation recorded below.

## Steve/manual evaluation after automated verification

M11A changes decision architecture but no audible format. Steve therefore performs a **reason-trace and parity review**, not creative-quality or station-clock acceptance:

1. Confirm readable explanation for an existing comment win, hard refusal, and direct continuation/silence.
2. Confirm the desktop experience remains M10C-recognizable: preparation while Firefox plays, normal ducked delivery when eligible, no liner/other segment.
3. With Chrome playing, confirm only Firefox ducks/restores; no pause/resume/toggle or endpoint/master-volume change.
4. Exercise Silent Mode, Clear History, rapid track change, Stop during preparation/delivery, and unsafe/ambiguous targeting; confirm accepted refusal, cleanup, ready-state.
5. Review provisional profile/reasons for plausibility, knowing it establishes no permanent cadence values. Hidden policy or increased chatter is a rejection/adjustment reason, not something to shrug off because tests pass.

No micro-liner, multi-format listening trial, commercial/caller review, Creative Director evaluation, voice audition, or program-break test belongs in M11A.

## Steve acceptance record - August 23, 2026

- Steve accepted M11A after the focused live run recorded in radio-behavior-20260823-231623-742.txt.
- The log recorded EDITORIAL PREPARATION: Prepare and EDITORIAL AIRTIME: ReserveForAirtime for the same existing-comment opportunity.
- Delivery revalidated the exact Playing Firefox media target and Core Audio session, ducked that session from 100% to 25%, completed speech, and restored the same session exactly to 100% unmuted.
- Steve confirmed the audible experience remained equivalent to accepted M10C and that Chrome was playing and remained unaffected.
- No pause/resume command, new format, cadence change, safety refusal, or cleanup failure occurred. The run stopped cleanly.
- Current automated evidence remains 320 passed, 0 failed with all Release builds at 0 warnings and 0 errors.
- No commit was authorized or created. M11B was not begun.

## Completion and acceptance

Automated verification creates an M11A implementation candidate; it does not pass the milestone automatically. M11A passes only after:

1. implementation stayed within this contract;
2. required current automated evidence passed;
3. Steve completed and accepted focused reason-trace/desktop parity review; and
4. canonical documentation records acceptance.

All four acceptance conditions are satisfied. M11A is passed.

No implementation commit is automatic. M11B, a liner, all format work, and later capabilities remain unauthorized until separately approved.

## Next action type

**Next action type - Documentation (Codex/Work)**

Prepare the bounded M11B Commentary and Reusable Liner Proof contract for Steve review. Do not implement M11B, add a liner, alter policy, or create a commit without separate authorization.
