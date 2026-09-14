# Cancellation-source lifetime repair contract

## Contract status

**REVISED-CANDIDATE REPAIR 1/1 COMPLETE; WARDEN PASS ON SEPTEMBER 14, 2026.**

Steve first authorized this documentation-only revision on September 10, 2026 after Warden blocked the original implementation candidate and Advisor recommended a bounded redesign. After reviewing the revised contract, Steve explicitly approved it for implementation on September 10, 2026. The approval authorizes only the revised implementation and verification surface stated below; it does not authorize a repair round, desktop or external runtime activity, staging, a commit, or a push.

The original contract was approved on September 9, 2026 for one implementation attempt with no preauthorized repair. That attempt has ended. The September 10 approval authorizes one separate revised implementation attempt, not a repair round for the blocked candidate. No automatic repair follows a Warden finding.

The revised candidate was declared **COMPLETE** by Imp with fresh Release builds and 490/490. Warden independently reran the Release builds for MediaProbe, tests, and ControlApp clean, and independently reran the safe deterministic suite at 490/490. Warden nevertheless **BLOCKED** the candidate after confirming an idle/shutdown cleanup exception defect: a throwing provider `Discard` skips speech and CTS cleanup, and a throwing speech `Dispose` skips CTS cleanup after coordinator state has already been cleared. An additional `Preparing -> Ready` seam sequence was reproduced through an internal seam, but full production-caller reachability was not established; this remains an evidence question, not a confirmed live regression.

On September 14, 2026, Steve authorized one bounded revised-candidate repair round (1/1): protect idle/shutdown cleanup so every terminal action is attempted while preserving the first failure; add deterministic throwing provider/speech tests; determine bounded production-path reachability of the `Preparing -> Ready` seam, correcting only within the approved holder/phase design or returning `BLOCKED`; then run fresh builds, the full safe suite, and Warden review. No automatic further repair is authorized.

Imp completed repair 1/1 on September 14, 2026. Idle/shutdown cleanup now clears coordinator ownership first, then independently attempts provider discard, prepared-speech disposal, and terminal lifetime disposal while retaining the first failure for the existing outer error path. Three deterministic production-routine tests cover throwing provider discard, throwing speech disposal, and combined failures; the prior successful-cleanup test remains green. A fourth deterministic production-routine test establishes that the reported `Preparing -> Ready` seam cannot occur through the examined real invalidation caller: preparation holds `_stateLock` through Ready publication, while invalidation must acquire that same lock and invalidate deterministic state before it can select the operation under `_pipelineLock`. Therefore invalidation either follows a completed Ready publication and detaches it, or wins the state lock and makes readiness publication fail; it cannot select `Preparing` between accepted state readiness and coordinator publication. Source review found the same state-before-pipeline ordering in track invalidation, Silent Mode, clear history, shutdown, session-safety invalidation, and delivery-transition cancellation paths. No holder/phase redesign was required. Fresh Release builds for MediaProbe, tests, and ControlApp passed with zero warnings and errors, and the full safe deterministic suite passed 494/494. At handoff, this was Imp evidence for a `COMPLETE` repaired candidate; Warden subsequently independently passed it as recorded below.

Warden independently reviewed the repair and **PASSED** it with no remaining findings in the approved scope. Imp's repair 1/1 was verified with fresh clean Release builds for MediaProbe, tests, and ControlApp, the full safe deterministic suite at 494/494, and scoped integrity checks. Warden independently reran all three Release builds clean and the full suite at 494/494. Independent production cleanup probes covering success, provider throw, speech throw, and combined throw each attempted speech disposal once and CTS disposal once while leaving the coordinator empty; when both actions threw, the first provider failure was preserved; repeated cleanup caused no additional disposal.

Warden's production readiness/publication-versus-invalidation contention and reverse-order probes support that the prior seam-only `Preparing -> Ready` sequence is not reachable through the examined production ordering. This resolves the evidence question for the examined paths, but is not proof of a desktop occurrence or of every possible production schedule. All nine contract areas pass. No manual desktop acceptance was required.

This closes only the cancellation-source lifetime issue as verified internal cancellation/disposal correctness. It does not establish desktop frequency, audible effects, memory impact, a milestone change, or resolution of any other issue. Warden changed no files. Steve later authorized a selective commit; no push was authorized or performed.

## History and evidence

This work remains separate from the prepared-liner ownership/disposal correction completed in commit `4ba375b`. That accepted correction closed a Ready-speech handoff and cleanup gap; this draft does not reopen it.

Auditor confirmed a source-level cancellation-token-source (CTS) lifetime race in the low-priority liner coordinator and the analogous ordinary automatic preparation/delivery coordinator:

1. A canceller can select the current CTS under its coordinator lock and release the lock.
2. The worker can then clear and dispose that same CTS.
3. A later `Cancel()` on the selected source can throw `ObjectDisposedException`.

Imp's original approved attempt moved cancellation under the coordinator locks and extracted production seams for deterministic tests. Fresh Release builds passed, and the full safe deterministic suite passed 487/487. Those results establish that the candidate compiled and passed its then-current tests; they do not establish that its lifetime design was sound.

Warden blocked the candidate because:

- cancellation callbacks execute synchronously, so a callback that throws can escape while coordinator state is locked and skip cleanup of detached prepared speech;
- preparation and delivery can overlap such that nullable-slot inference leaves both paths believing they own disposal of the same CTS;
- the tests did not force the contested lock timing or the preparation-to-delivery ownership overlap; and
- `_pipelineTask` could be read outside the lock while other pipeline state was selected under the lock, creating a mixed-snapshot concern.

Advisor then performed a read-only first-principles consultation and classified the candidate as **REDESIGN / OWNER DECISION**. Advisor recommended surgically superseding lock-held `Cancel()` and nullable-slot ownership inference with a private task-specific lifetime holder, a short cancellation lease, and an explicit preparation-to-delivery ownership phase. That recommendation is the basis of this revised draft; it did not itself authorize implementation.

No desktop failure, audible issue, unsafe targeting, restoration failure, crash, practical frequency, or native-memory cause has been established. The 487/487 candidate result is historical automated evidence for a blocked implementation, not acceptance evidence for this revised design.

## Contract

The revised repair closes the CTS selection-versus-disposal race without executing cancellation callbacks while coordinator locks are held and without inferring ownership from nullable fields.

Each approved operation uses one private operation-specific lifetime object. The object owns one CTS and a cached cancellation token. It supports a short cancellation lease: cancellation selects the exact lifetime and acquires the lease while holding the existing coordinator lock, but logging and `Cancel()` run after that lock is released. Terminal completion requests disposal; the CTS is actually disposed exactly once only after all cancellation leases have released. Lease release must occur in `finally`, including when logging or a cancellation callback throws.

The ordinary coordinator additionally uses explicit phases for one operation:

`Preparing -> Ready -> Delivering -> terminal`

Publication of prepared material and transfer to delivery occur atomically with the associated phase and identity changes under `_pipelineLock`. A preparation finalizer must not dispose a lifetime that has reached `Ready` or `Delivering`; after transfer, delivery is the sole terminal owner. Code performing asynchronous preparation or delivery obtains the cached token before beginning that work and never asks a possibly disposed CTS for its token.

Cancellation atomically snapshots the exact lifetime, prepared payload, and pipeline task under `_pipelineLock`. If a `Ready` operation is idle, cancellation may terminally claim it and detach its payload. If preparation or delivery is active, that worker remains the terminal owner. Reference identity must ensure that stale completion or cancellation work cannot clear, cancel, dispose, or otherwise alter a replacement operation.

Cleanup for a detached ordinary prepared payload stays inside the production cancellation routine. Protected or nested cleanup must attempt every applicable action even if an earlier action fails: provider `Discard`, prepared-speech `Dispose`, lifetime terminal completion, and cancellation-lease release. The liner path uses the same lease and deferred-disposal rule without importing ordinary phase machinery; the liner worker remains its terminal owner.

The implementation must surgically supersede the blocked lock-held-cancellation candidate. It may retain only factoring and tests that genuinely serve the revised design. It must not wholesale revert dirty files, stack more tests around the unsound design, or absorb unrelated work.

## In simple terms

The radio needs to be able to press an operation's stop button without destroying that button while someone is pressing it—and without holding its internal state lock while user or library cancellation callbacks run.

The small lifetime holder works like a temporary claim ticket. Cancellation briefly claims the exact live stop signal, releases the coordinator lock, and then performs logging and cancellation. Final cleanup can be requested at the same time, but disposal waits until that claim ticket is returned. For ordinary prepared material, explicit `Preparing`, `Ready`, and `Delivering` phases make it unambiguous whether preparation, cancellation, or delivery owns final cleanup.

## Why it matters

The original race can produce an avoidable exception during cancellation. The blocked fix removed that particular timing window but introduced more serious cleanup ambiguity: synchronous callbacks could escape while shared state was locked, and overlapping preparation and delivery could both take disposal responsibility.

The revised boundary is intended to make cancellation, cleanup, and replacement identity deterministic while preserving Zoya's current safe-silence behavior. This is internal lifetime correctness only. It is not a new feature, an audible change, or a claim that a desktop incident has occurred.

## Approval effect

Conductor may route one separate revised attempt to Imp. Imp may replace the blocked candidate only within the allowed surface below, run the specified deterministic verification, and submit a `COMPLETE` candidate to Warden. Evidence-based updates to this contract and the relevant `KNOWN_ISSUES.md` and `PROJECT_INDEX.md` entries may follow the verified outcome.

If implementation reveals that the design needs a broader or general lifetime framework, an observable behavior change, consequential architecture expansion, or files outside the justified surface, Imp must return `BLOCKED`. If Warden finds a defect, it returns to Conductor and Steve because no repair round is preauthorized.

## Steve approved

- One separate revised implementation attempt covering only the low-priority liner CTS path and the analogous ordinary automatic preparation/delivery CTS path.
- The smallest private task-specific lifetime holder, cached token, cancellation lease, ordinary operation phase, and narrow internal production gates or seams necessary to implement and deterministically verify the revised boundary.
- Necessary changes in `src/ZoyaRadio.MediaProbe/RadioBehaviorLiner.cs` and `src/ZoyaRadio.MediaProbe/RadioBehaviorTest.cs`.
- Focused deterministic tests in `tests/ZoyaRadio.MediaProbe.Tests/Program.cs` or a narrow existing test file when separation is genuinely clearer.
- The smallest relevant Release builds, the full safe deterministic suite, independent Warden review, and evidence-based status-document updates.
- No manual desktop acceptance by default because the intended result has no observable behavior change. If implementation changes observable behavior, Imp must return `BLOCKED` rather than treating this contract as desktop-test authority.

## Steve did NOT approve

- Reopening or changing the prepared-speech correction in commit `4ba375b`.
- Keeping the blocked lock-held `Cancel()` design or treating its 487/487 result as proof of the revised design.
- A generic CTS, cancellation, operation, task, or lifecycle framework; persistence, recovery machinery, a broad coordinator refactor, or speculative reuse.
- Any other audit finding, including restoration-warning/GUI observability work, retained-working-set investigation, M9B work, or unrelated cancellation behavior.
- Changes to GUI or status policy, liner/comment text, timing, cadence, cooldown, scheduling, arbitration, delivery semantics, reuse policy, Firefox targeting, ducking/restoration, endpoint volume, or master volume.
- Memory-management work, milestone advancement, roadmap or durable-decision changes, provider/model/dependency/package changes, or external services.
- Desktop application launches, browser/media control, audio or speech synthesis, provider calls, network activity, or manual desktop testing under this contract.
- Changes to other files unless Imp first establishes that they are strictly necessary and returns `BLOCKED` for owner scope approval.
- Any repair round, commit, push, release, or distribution action.

## Risks and unknowns

- The defect remains confirmed by static source reasoning rather than a reproduced desktop incident; its practical frequency and visible effect are unknown.
- The private holder and phase must remain task-specific. If implementation starts becoming a reusable lifecycle subsystem, that is evidence the work has exceeded this contract.
- Callback, provider-discard, and speech-disposal failures can combine. The revised implementation must preserve the primary failure while still attempting every required cleanup action; exact exception-reporting mechanics remain an ordinary implementation choice so long as existing observable status policy is unchanged.
- Exact phase and lease representation are low-level implementation choices, but ownership, lock boundaries, identity protection, and cleanup guarantees are contract requirements.
- Passing tests cannot prove every possible thread schedule. The required tests therefore must force the exact production handoff points rather than infer them from task startup or timing.

## How we know it worked

Focused deterministic tests must use the real production routines and only narrow internal gates when needed. They must not use sleeps. Together they must establish:

1. **Lease versus worker finalization:** after cancellation selects the exact lifetime and acquires its lease, a gate pauses it; the worker finalizer requests disposal; the source remains usable through cancellation; disposal occurs exactly once only after the lease releases.
2. **Real lock contention:** tests expose explicit `contender-before-lock` and `lock-acquired/state-transition` signals. Starting a `Task.Run` is not accepted as evidence that a contender reached the lock.
3. **Publication-to-delivery transfer:** publication reaches `Ready`, delivery atomically transfers it to `Delivering` before preparation cleanup runs, delivery receives the cached token, preparation does not dispose the lifetime, and delivery is the sole terminal owner.
4. **Cancellation between publication and transfer:** cancellation detaches the prepared payload, attempts provider `Discard` and speech `Dispose`, prevents delivery, and completes terminal lifetime disposal correctly.
5. **Throwing cancellation callback:** the complete production outer cancellation routine encounters a throwing callback yet still attempts detached-payload cleanup, lifetime terminal completion, and lease release.
6. **Replacement safety:** if a replacement is installed before stale work resumes, the replacement lifetime, source, task, phase, and prepared payload remain untouched.
7. **Production terminal cleanup:** shutdown and idle cleanup exercise their actual production routines. Tests do not prove disposal by manually disposing values returned from a helper.
8. **Liner overlap:** proportionate production-path evidence forces the liner cancellation-lease/finalizer overlap and proves cancellation remains usable and final disposal occurs exactly once.
9. **Baseline preservation:** live cancellation reaches the intended worker; completed or stale cancellation is a safe no-op; no stale work is published or delivered; all existing safe deterministic tests continue to pass.

Tests use deterministic coordination and fakes only. They must not load Kokoro, synthesize or play audio, call Qwen/Ollama, access Firefox, control browser/media, contact a provider or network, or launch the desktop application.

After implementation, Imp runs the smallest relevant Release builds and focused tests, then the full safe deterministic suite because shared coordinator lifetime behavior changes. Imp must account for every requirement above and declare exactly `COMPLETE`, `BLOCKED`, or `INCOMPLETE`. Only `COMPLETE` is Warden-ready. Warden independently reviews the design, production interleavings, cleanup behavior, scope, and fresh evidence.

## Next owner gate

The authorized repair round is limited to repair 1/1 of the Warden-blocked revised candidate. The route is:

`Conductor -> Imp -> Warden -> Scribe -> Conductor`

Warden may hand directly to Scribe if the repaired candidate passes and no owner test is required. The repair does not authorize automatic further repair, a commit or push, desktop/provider work, broader design, observable behavior changes, prepared-speech reopening, or unrelated findings. Any broader/general lifetime framework, observable behavior change, architecture expansion, unjustified file, unexpected Warden defect, commit, or push returns to Conductor and Steve as a separate decision.
