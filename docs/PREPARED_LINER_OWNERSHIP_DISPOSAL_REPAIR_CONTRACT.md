# Prepared-liner ownership/disposal repair contract

## Contract status

**Implemented and independently Warden-passed on September 9, 2026 within the approved prepared-liner ownership/disposal scope.**

Steve approved the original contract on September 8, 2026 and one separately authorized revised implementation attempt on September 9, 2026. That approval covered only `RadioBehaviorLiner.cs`, the narrow `ReusableLiner.cs` cancellation-aware prepublication acceptance path, focused deterministic tests, Release builds, the full safe deterministic suite, and Warden review. Revised-candidate repair 1/1 is complete and Warden passed it; no further repair is automatic.

## History and exhausted original-repair boundary

- The first Warden review blocked the original candidate because the pre-check/acceptance race remained open and its test was insufficient.
- Steve authorized repair round 1/1 on September 8, 2026. It attempted post-accept rollback.
- The second Warden review blocked because `inventory.Invalidate()` correctly disposed speech but also cleared session/material/payload binding and cadence/history, causing later same-session liner preparations to fail.
- The original repair budget was exhausted. As Steve directed, Conductor consulted Advisor; Advisor was read-only, changed no files, and ran no tests.

The separate cancellation-source disposal race and every other audit finding remain out of scope.

## Revised-candidate review and authorized correction

Warden returned **NOT REVIEW-READY — known contract work remains**. The missing or ineffective cancellation-before-entry and cancellation-after-transfer tests, plus partial cadence/history evidence, are unfinished contract work and consume zero repair rounds.

Warden also found one unexpected implementation defect in a candidate Imp declared `COMPLETE`: the compatibility wrapper can attempt a second `Dispose` after a throwing `Dispose`.

Steve authorized repair 1/1 for the revised candidate on September 9, 2026. It was limited to correcting that wrapper defect, completing the already-required cancellation and cadence/history tests, rerunning the existing deterministic verification, and returning to Warden. No further automatic repair is authorized.

## Final verified outcome

Imp completed revised-candidate repair 1/1. Imp's fresh Release MediaProbe and deterministic-test builds each passed with 0 warnings and 0 errors, and Imp's full safe deterministic suite passed 481/481 with 0 failures.

Warden independently re-reviewed the completed repair and returned **PASS**, with no remaining findings within the approved repair scope. Warden independently ran the existing Release test binary: 481/481 passed with 0 failures. Scoped-diff and strict UTF-8 checks also passed. Warden did not rerun the builds.

This closes only the prepared-liner ownership/disposal issue as verified static cleanup correctness. Manual desktop acceptance was not required by this invisible-cleanup contract and was not used as evidence. The result establishes no audible, UI, or cadence-policy behavior change; it does not establish native-memory impact or resolve the separate cancellation-source disposal race or any other audit finding. Steve later authorized a selective commit; no push was authorized or performed.

## Contract

The approved revised approach repaired the confirmed prepared-liner ownership/disposal gap without publishing then rolling back an inventory handoff. It originated as Advisor's recommendation and was subsequently approved by Steve only for this bounded implementation; it is not a general lifecycle design decision.

Permit a narrow addition to `ReusableLiner.cs`: a cancellation-aware, non-disposing inventory acceptance path used only by the coordinator. Within the existing inventory lock, it must validate inventory/duration, compute the proposed lifecycle attachment using the existing pure reducer without publishing, determine admissibility, then perform one final cancellation read as the explicit cancellation-versus-acceptance linearization point. If cancelled, it must return `false` without changing inventory binding/state. Otherwise it publishes the already-computed state and speech reference using only non-fallible assignments, then returns `true`.

The coordinator retains local speech in `try`/`finally`, clears it only when that path returns `true`, and disposes any remaining local speech exactly once. The new path does not dispose on `false` or before throwing. Cancellation after the linearization point loses the handoff and follows the existing inventory lifecycle; this contract does not claim an impossible race-free physical method return.

The repair must preserve these rules:

1. If cancellation, inventory rejection, or an in-scope failure prevents acceptance, the locally owned speech is disposed exactly once.
2. If acceptance succeeds, inventory becomes the sole owner. The coordinator must not dispose it, and existing inventory lifecycle, release, invalidation, and disposal behavior remains unchanged.
3. Non-Ready, error, and cancelled preparation results remain safely disposed as they are today.
4. No generated or prepared material gains airtime rights. Existing cancellation, Silent Mode, shutdown, session invalidation, stale/unsafe-state, and music-first arbitration rules still suppress or discard it.

The approved and completed implementation surface was limited to:

- `src/ZoyaRadio.MediaProbe/RadioBehaviorLiner.cs`;
- `src/ZoyaRadio.MediaProbe/ReusableLiner.cs`, limited to the proposed cancellation-aware, non-disposing acceptance path and the smallest compatibility wrapper needed to preserve existing callers;
- focused tests in `tests/ZoyaRadio.MediaProbe.Tests/Program.cs`, or one narrowly scoped existing test file if that structure is cleaner; and
- status wording only after evidence exists.

The existing one-argument `AcceptPreparedSpeech` must retain its existing rejection-disposal behavior for current callers through the smallest wrapper/compatibility path. If prepublication is impossible without broader lifecycle change, Imp returns `BLOCKED`; this contract does not pre-authorize an ownership redesign.

## Evidence and problem statement

Static review established that `RunLinerPreparationAsync` awaits `ReusableLinerPipeline.PrepareAsync`. For a Ready result with non-null speech, it attempts inventory acceptance only when cancellation has not been requested. The existing disposal `else` applies only to non-Ready or missing-speech results. A Ready speech rejected because cancellation was already requested can therefore remain locally owned and undisposed.

`ReusableLinerPipeline` already transfers ownership to the Ready result by clearing its local speech variable before returning. This is a confirmed static ownership gap, not a reproduced desktop incident. It is not linked causally to retained native working set or unsafe speech.

The separate suspected cancellation-source disposal race in `RadioBehaviorLiner` and `RadioBehaviorTest` is outside this revision.

## In simple terms

Before accepting a prepared liner, inventory decides whether it can publish it while holding its existing lock. Cancellation has one final named decision point: before that point, the coordinator keeps and disposes the speech; after it, inventory owns the speech under its existing lifecycle.

## Why it matters

The failed rollback proved that publishing then undoing inventory state damages the same-session lifecycle. Preventing publication before the final decision is smaller and preserves cadence, history, binding, and existing disposal semantics.

## Authorized revised implementation scope

Imp's completed work was limited to the narrow coordinator/inventory change and focused deterministic tests. After verification, the candidate went to Warden and passed. Repair 1/1 for the revised candidate was limited to the wrapper defect and already-required cancellation/cadence-history coverage; no further automatic repair is authorized. A pure prepublication path that required broader lifecycle change would have returned `BLOCKED`.

## What Steve approved

- One separately authorized revised implementation attempt over only the listed narrow source/test surface, deterministic verification, and Warden review.

## What Steve did not approve

- Repair or investigation of the separate cancellation-source disposal race.
- Restoration-warning or GUI-observability changes.
- Retained-working-set remediation or a causation claim.
- Changes to liner text, timing, cadence, cooldown, scheduling, arbitration, reuse policy, delivery, Firefox targeting, ducking/restoration, speech provider/voice, M9B, M12D, M13, M16, prompts, dependencies, or packages.
- A general ownership framework, refactor, lifecycle redesign, persistence, telemetry platform, or new abstraction.
- A commit or push.

## Risks and unknowns

The gap is static evidence, not a reproduced desktop incident. The revised path must prove its linearization semantics deterministically; it does not establish native-memory impact, unsafe audible delivery, or whether the separate cancellation-source race is real. If the smallest path changes observable behavior or requires broader lifecycle architecture, Imp must return `BLOCKED`.

## How we know it worked

Focused deterministic tests must establish all of the following:

1. Coordinator-level interleaving cancels from inside duration/acceptance validation before the final decision; the cancelled handoff publishes no payload, leaves binding/material/payload identity and cadence/history unchanged, and disposes locally exactly once.
2. The same inventory, without an Arm call, accepts a second fresh speech after the cancelled handoff; the coordinator does not dispose that accepted speech.
3. Equivalent cooldown/spacing boundaries, reservation/release/reuse, and eventual invalidation/disposal remain correct.
4. Cancellation before acceptance entry, inventory rejection including preservation of any existing payload, duration-getter exception, successful transfer, and cancellation after transfer are covered.

Tests use fakes and deterministic task coordination only, with no sleeps. They must not load Kokoro, synthesize audio, call Qwen/Ollama, access Firefox, control browser/media, use a provider/network, or launch the desktop application.

The completed verification required the smallest relevant Release builds and focused tests, then the full safe deterministic suite because shared runtime resource ownership changes. Imp ran those builds and the full suite; Warden independently reviewed the complete candidate and independently ran the existing Release test binary, as recorded above.

## Next owner gate

The completed route was `Conductor -> Imp -> Warden -> Conductor`. Manual desktop acceptance was not required because the intended result is invisible cleanup correctness with no behavior or UX change. If the work had changed observable behavior or needed broader lifecycle architecture, Imp would have returned `BLOCKED` for Conductor and Steve.

## Explicit non-goals and stop boundary

The approved revised attempt was required to return `BLOCKED` rather than add a general ownership framework, refactor, lifecycle redesign, persistence, telemetry, proxy/wrapper ownership object, or new abstraction. Alternatives rejected for this attempt were extra coordinator checks (they merely move the race), locking cancellation (it wanders into excluded cancellation-source work), invalidate/re-arm (it changes cadence), proxy/wrapper ownership (it adds an ownership object), and payload rollback (it is more complex than preventing publication). The attempt also excluded restoration/GUI warnings, every other audit finding, behavior/cadence changes, M9B, M12D, M13, M16, providers, packages, models, commits, pushes, and the separate cancellation-source disposal race.
