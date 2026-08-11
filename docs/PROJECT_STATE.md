# Project state

## Accepted snapshot

| Field | Current value |
| --- | --- |
| Branch | `master` |
| Latest accepted implementation commit | `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc` |
| Current passed milestone | Milestone 8A |
| Manual acceptance date | 2026-08-11 |
| Latest automated result | 162 passed, 0 failed |
| Next gate | M8B is unstarted and requires a separate explicit implementation request; Milestone 8 as a whole is not passed |
| Deferred milestone | Milestone 6 |

The local Git repository is authoritative. See [PROJECT_INDEX.md](PROJECT_INDEX.md) for the documentation authority and snapshot rules.

## Current operational state

- The original Windows media-session watcher remains read only.
- The accepted WinForms frontend hosts the same radio runtime in-process through a UI-independent controller. It provides Start, safely awaited Stop, Silent Mode, Clear History, current metadata/target status, read-only Radio activity, and LocalAppData log access while preserving console diagnostics.
- The verified target is exactly one eligible, Playing `firefox.exe` GSMTC session. Missing or ambiguous targeting produces silence.
- Automatic radio behavior schedules one comment every 2-5 genuine transitions. It prepares Track-A local AI text and Nicole audio one transition early while Firefox keeps playing.
- At Track B, the application revalidates the logical Firefox media target and exactly one active Firefox Core Audio session inside a two-second delivery window.
- Normal delivery never pauses Firefox. It captures the Firefox application-session scalar/mute state, ducks proportionally to 25%, speaks over continuing music, and restores the exact captured state in protected cleanup.
- Harmless Firefox GSMTC wrapper refreshes preserve preparation, active speech, and the current schedule. Unsafe or genuinely different target states cancel protected work before any reacquisition.
- Ollama remains loopback-only with `qwen3:4b-instruct`, deterministic validation/fallback, `--no-ai`, and normal `keep_alive: 0` unloading.
- Kokoro `af_nicole` is the accepted primary CPU voice; `af_heart` is runner-up and Windows local TTS is fallback.
- Recent state remains process-local and bounded to 10 tracks and 6 accepted AI comments. There is no persistent listening history or catch-up queue.

Full implemented behavior is in [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md).

## Milestone 8 gate status

- Steve approved the bounded [Milestone 8 contract](MILESTONE_8_CONTRACT.md) on August 10, 2026.
- The original M8A candidate was implemented at `f123ddd9de484de590ba596abf812314b265390c`; the accepted Radio-activity correction and latest accepted implementation snapshot are `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc`.
- Steve manually accepted M8A on August 11, 2026. The accepted evidence and preserved regression procedure are in [MILESTONE_8A_TEST.md](MILESTONE_8A_TEST.md).
- Milestone 8 as a whole is **not passed or complete**. M8B private-alpha distribution is the next gate, remains unstarted, and requires a separate explicit implementation request.
- No post-M8 proposal is approved by the M8 contract.

## M8A accepted verification

- Existing console/runtime Release build: passed with 0 warnings and 0 errors.
- M8A WinForms Release build: passed with 0 warnings and 0 errors.
- Test-project Release build: passed with 0 warnings and 0 errors.
- Complete automated suite: 162 passed, 0 failed, including all 124 previously accepted regression tests plus 38 M8A controller/UI-boundary and activity-status tests.
- Shared runtime, WinForms, and test-project formatting verification: passed. `git diff --check` also passed.
- Automatic tests use fakes and did not open the GUI or Explorer, produce speech, contact Ollama, control desktop playback, or change real volume.
- This documentation-only acceptance record did not rerun builds or tests; the results above are the verified evidence associated with accepted implementation commit `41b7824`.

## M8A manual acceptance evidence

- The WinForms application launched successfully. Start, Stop, Silent Mode, Clear History, Open Logs Folder, current title/artist, and readable Firefox target status behaved correctly.
- Radio activity was visible without PowerShell and showed transition observation/countdown, the stability wait, local generation, Nicole preparation, prepared waiting, delivery, restoration, and ordinary observation.
- Stop and window close awaited protected cleanup. Firefox-only proportional ducking and exact scalar/mute restoration passed; Chrome and Windows master volume remained untouched, and normal delivery issued no pause/resume command.
- Missing or unsafe targeting remained silent, and the existing console diagnostics remained usable.
- Firefox targeting is intentionally application-level: ordinary YouTube in Firefox may become the observed media session when it holds Firefox media ownership. Zoya does not inspect pages or distinguish YouTube Music from ordinary YouTube.
- `radio-behavior-20260811-091603-715.txt` recorded three complete automatic deliveries, speech starting inside the two-second window, exact 100% -> 25% -> 100% Firefox restoration with the original unmuted state, clean shutdown, and no automatic playback pause/resume.
- `radio-behavior-20260811-095014-238.txt` recorded the corrected visible sequence through transition, eight-second stability wait, generation, Nicole preparation, and prepared waiting. It ended before a completed delivery or shutdown, so neither is claimed from that log.

## Milestone 7.5B acceptance evidence

- Firefox transitions remained observable during Chrome/YouTube global GSMTC churn.
- Fifty-nine harmless wrapper refresh diagnostics were treated as logical continuity, with no false replacement/reacquisition and no interruption to generation, Nicole synthesis, prepared material, active speech, or the current interval.
- Two prepared announcements completed while wrapper refreshes continued during speech.
- Both deliveries used only Firefox Core Audio PID 11568 / session `7D62879DB482` and restored exactly from 100% to 25% to 100%.
- Chrome remained untouched, normal delivery did not pause Firefox, and Ctrl+C shut down cleanly.
- The accepted local evidence is preserved in `logs\radio-behavior-test-20260810-131308-866.txt` and summarized in [MILESTONE_7_5B_TEST.md](MILESTONE_7_5B_TEST.md).

## Immediate blockers and deferred observations

- M8A is accepted. M8B is the next gate but remains unstarted and unauthorized until Steve supplies a separate explicit implementation request.
- The earlier 20:49 observer stall remains intermittent, unresolved, and unreproduced by the accepted Firefox-plus-Chrome run.
- Wrapper-refresh diagnostics are excessively repetitive; this is a deferred non-blocking logging-quality issue.
- Other accepted limitations and deferred improvements are maintained in [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Navigation

- Completed milestones and approved direction: [ROADMAP.md](ROADMAP.md)
- Durable accepted choices: [DECISIONS.md](DECISIONS.md)
- Research provenance and non-authority boundary: [RESEARCH_INDEX.md](RESEARCH_INDEX.md)
