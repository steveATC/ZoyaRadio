# Project state

## Accepted snapshot

| Field | Current value |
| --- | --- |
| Branch | `master` |
| Latest accepted implementation commit | `63662342a374b323b639325f360ae7c17ace1432` |
| Current passed milestone | Milestone 7.5B |
| Manual acceptance date | 2026-08-10 |
| Latest automated result | 124 passed, 0 failed |
| Next gate | M8A implementation candidate awaits Steve's manual desktop acceptance; M8B remains blocked |
| Deferred milestone | Milestone 6 |

The local Git repository is authoritative. See [PROJECT_INDEX.md](PROJECT_INDEX.md) for the documentation authority and snapshot rules.

## Current operational state

- The original Windows media-session watcher remains read only.
- The verified target is exactly one eligible, Playing `firefox.exe` GSMTC session. Missing or ambiguous targeting produces silence.
- Automatic radio behavior schedules one comment every 2-5 genuine transitions. It prepares Track-A local AI text and Nicole audio one transition early while Firefox keeps playing.
- At Track B, the application revalidates the logical Firefox media target and exactly one active Firefox Core Audio session inside a two-second delivery window.
- Normal delivery never pauses Firefox. It captures the Firefox application-session scalar/mute state, ducks proportionally to 25%, speaks over continuing music, and restores the exact captured state in protected cleanup.
- Harmless Firefox GSMTC wrapper refreshes preserve preparation, active speech, and the current schedule. Unsafe or genuinely different target states cancel protected work before any reacquisition.
- Ollama remains loopback-only with `qwen3:4b-instruct`, deterministic validation/fallback, `--no-ai`, and normal `keep_alive: 0` unloading.
- Kokoro `af_nicole` is the accepted primary CPU voice; `af_heart` is runner-up and Windows local TTS is fallback.
- Recent state remains process-local and bounded to 10 tracks and 6 accepted AI comments. There is no persistent listening history or catch-up queue.

Full implemented behavior is in [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md).

## Milestone 8 candidate status

- Steve approved the bounded [Milestone 8 contract](MILESTONE_8_CONTRACT.md) on August 10, 2026.
- Steve then explicitly authorized M8A implementation. The small WinForms control-application candidate is implemented and its automated verification is complete.
- M8A is **not passed or accepted** until Steve completes the real-desktop procedure in [MILESTONE_8A_TEST.md](MILESTONE_8A_TEST.md).
- M8B, the private-alpha distribution gate, remains blocked until M8A is implemented, verified, manually accepted by Steve, recorded in Git, and separately authorized.
- No post-M8 proposal is approved by the M8 contract.

## M8A candidate verification

- Existing console/runtime Release build: passed with 0 warnings and 0 errors.
- M8A WinForms Release build: passed with 0 warnings and 0 errors.
- Test-project Release build: passed with 0 warnings and 0 errors.
- Complete automated suite: 151 passed, 0 failed, including all 124 previously accepted regression tests plus 27 M8A controller/UI-boundary tests.
- Shared runtime, WinForms, and test-project formatting verification: passed. `git diff --check` also passed.
- Automatic tests use fakes and did not open the GUI or Explorer, produce speech, contact Ollama, control desktop playback, or change real volume.
- The accepted-snapshot result remains the historical 124 passed, 0 failed recorded above; the 151-test candidate run does not accept M8A.

## Milestone 7.5B acceptance evidence

- Firefox transitions remained observable during Chrome/YouTube global GSMTC churn.
- Fifty-nine harmless wrapper refresh diagnostics were treated as logical continuity, with no false replacement/reacquisition and no interruption to generation, Nicole synthesis, prepared material, active speech, or the current interval.
- Two prepared announcements completed while wrapper refreshes continued during speech.
- Both deliveries used only Firefox Core Audio PID 11568 / session `7D62879DB482` and restored exactly from 100% to 25% to 100%.
- Chrome remained untouched, normal delivery did not pause Firefox, and Ctrl+C shut down cleanly.
- The accepted local evidence is preserved in `logs\radio-behavior-test-20260810-131308-866.txt` and summarized in [MILESTONE_7_5B_TEST.md](MILESTONE_7_5B_TEST.md).

## Immediate blockers and deferred observations

- There is no active M7.5B blocker. M8A now requires Steve's manual desktop acceptance; M8B remains blocked and unstarted.
- The earlier 20:49 observer stall remains intermittent, unresolved, and unreproduced by the accepted Firefox-plus-Chrome run.
- Wrapper-refresh diagnostics are excessively repetitive; this is a deferred non-blocking logging-quality issue.
- Other accepted limitations and deferred improvements are maintained in [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Navigation

- Completed milestones and approved direction: [ROADMAP.md](ROADMAP.md)
- Durable accepted choices: [DECISIONS.md](DECISIONS.md)
- Research provenance and non-authority boundary: [RESEARCH_INDEX.md](RESEARCH_INDEX.md)
