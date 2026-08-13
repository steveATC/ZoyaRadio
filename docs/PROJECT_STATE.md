# Project state

## Accepted snapshot

| Field | Current value |
| --- | --- |
| Branch | `master` |
| Latest accepted implementation commit | `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf` |
| Current passed milestone | Milestone 8, closed for continued internal development |
| Manual acceptance date | 2026-08-11 |
| Latest accepted automated result | 178 passed, 0 failed at accepted internal implementation `b56c03d` |
| Accepted internal packaged baseline | `0.8.0-alpha.1` |
| Deferred distribution validation | Clean Windows 10/11 and invited private-alpha testing; not blockers to internal development |
| Next project step | Steve must complete the M9A blinded qualitative evaluation; M9A is not passed and M9B is not authorized |
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
- The accepted M8B internal baseline adds a self-contained Windows x64 package, application-relative packaged assets, startup integrity checks, and thin prerequisite/readiness actions without changing the accepted radio runtime.
- The M9A structured-commentary prompt, typed brief, deterministic announcement candidates, and 48-request benchmark are isolated diagnostics and are unreachable from the accepted automatic-radio composition.
- Recent state remains process-local and bounded to 10 tracks and 6 accepted AI comments. There is no persistent listening history or catch-up queue.

Full implemented behavior is in [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md).

## Milestone 8 gate status

- Steve approved the bounded [Milestone 8 contract](MILESTONE_8_CONTRACT.md) on August 10, 2026.
- The original M8A candidate was implemented at `f123ddd9de484de590ba596abf812314b265390c`; the accepted Radio-activity correction is `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc`.
- Steve manually accepted M8A on August 11, 2026. The accepted evidence and preserved regression procedure are in [MILESTONE_8A_TEST.md](MILESTONE_8A_TEST.md).
- Steve accepted the extracted M8B `0.8.0-alpha.1` package on August 11, 2026. M8B and Milestone 8 are closed for continued internal development at implementation commit `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- Clean Windows 10/11 and invited-tester validation remain pending and deferred as distribution evidence. The accepted internal package is not clean-machine certified, broadly compatible, publicly released, or validated for third-party distribution.
- M8 acceptance did not authorize post-M8 work. Steve subsequently approved only the bounded M9A experiment in [MILESTONE_9A_CONTRACT.md](MILESTONE_9A_CONTRACT.md); M9B and every other post-M8 implementation remain unauthorized.

## Milestone 9A candidate status

- Steve approved M9A as a structured-context experiment after the post-M8 project-management review.
- The implementation adds a typed deterministic brief, shallow recent-comment fingerprints, a separate candidate prompt, deterministic announcement candidates, and an explicit generate-only comparison benchmark.
- Normal radio continues to use the accepted Milestone 4 prompt/context, validator, fixed fallback, scheduler, delivery path, voice, targeting, ducking, and restoration behavior.
- The fake-driven suite currently passes 206 tests with 0 failures: all 178 accepted baseline regressions plus 28 focused M9A tests. This is current candidate verification, not a replacement for the accepted 178/0 baseline at `b56c03d`.
- The one authorized Codex live benchmark completed 48/48 loopback generations and verified model unload. A post-run audit found mojibake in its Unicode fixture; the fixture and tests were corrected, the first artifact set was labelled invalid for acceptance, and no second Codex benchmark was run. Steve's documented manual run must produce the qualitative evaluation set.
- M9A awaits Steve's blinded qualitative evaluation. It is not passed, the candidate is not integrated, the `Borrowed Umbrella` issue is not claimed fixed, and M9B is not authorized.

## M8B accepted engineering verification

- Release production and test-project builds pass with 0 warnings and 0 errors.
- Complete fake-driven suite: 178 passed, 0 failed, preserving all 162 accepted M8A-era tests plus 16 focused package, integrity, readiness, Ollama/Qwen setup, documentation, and ZIP tests.
- The versioned self-contained Win-x64 ZIP is built, hashed, extracted, audited, and safe-self-tested from an unrelated working directory.
- The neural candidate contains Nicole only and no eSpeak files. Offline preparation passes for ordinary English, punctuation/numbers, repeated preparation/disposal, pre-cancellation, and deliberate Windows fallback. An unusual/OOV sample safely selects Windows TTS when KokoroSharp attempts its absent eSpeak helper.
- PDBs are retained in ignored private-symbol output and excluded from the tester ZIP. Firefox, Ollama, Qwen, logs, generated audio, user data, and non-x64 native assets are excluded.
- Automated and package checks produced no audible speech, contacted no real Ollama service, and controlled no browser, media session, playback, or volume.
- Genuine clean Windows 10 22H2, clean Windows 11, and invited testing remain pending/deferred distribution evidence. See [MILESTONE_8B_TEST.md](MILESTONE_8B_TEST.md).

## M8B owner packaged-desktop acceptance

- Steve extracted the package outside the repository to `C:\Users\Steve\Desktop\ZoyaRadio-0.8.0-alpha.1-win-x64`.
- He ran `.\ZoyaRadio.ControlApp.exe --package-self-test` and `.\ZoyaRadio.ControlApp.exe --neural-package-self-test`. The WinForms executable wrote reports under `%LOCALAPPDATA%\Zoya Radio\logs`; no PowerShell output was expected.
- `private-alpha-self-test-20260811-173929-328.txt` records version `0.8.0-alpha.1`, target `win-x64`, execution from the extracted Desktop package, LocalAppData log resolution, integrity verification of all 515 manifest-listed package files, and `SELF-TEST RESULT: PASS`.
- `private-alpha-self-test-20260811-174209-610.txt` records full package integrity verification; silent offline Nicole preparation for ordinary English, punctuation/numbers, unusual/OOV-looking names, and repeated preparation/disposal; pre-cancellation; deliberate neural failure; successful Windows-TTS fallback preparation; no loaded eSpeak module; and `SELF-TEST RESULT: PASS`.
- Steve directly confirmed readiness presentation, normal Nicole speech, automatic radio operation, Firefox-only ducking/restoration, Chrome isolation, Silent Mode, Stop, safe window close, and LocalAppData logging.
- Steve did not separately report manually recomputing the outer ZIP hash during acceptance. The candidate build/package audit verified the published ZIP digest; his extracted-package self-test verified the internal 515-file manifest.
- This evidence is owner packaged-desktop acceptance only. It does not establish clean Windows 10, clean Windows 11, invited-tester, broad hardware, SmartScreen-reputation, code-signing, public-release, or general-distribution validation.

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

## Next step and deferred observations

- Milestone 8 is closed for continued internal development, and `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf` is the latest accepted internal implementation snapshot.
- The post-M8 review selected only the bounded M9A structured-commentary experiment. The next step is Steve's blinded evaluation under [MILESTONE_9A_TEST.md](MILESTONE_9A_TEST.md), not runtime integration.
- Clean-machine and invited-tester checks remain deferred compatibility/distribution evidence. A later defect may require a bounded repair, but missing tester feedback does not block internal development.
- The earlier 20:49 observer stall remains intermittent, unresolved, and unreproduced by the accepted Firefox-plus-Chrome run.
- Wrapper-refresh diagnostics are excessively repetitive; this is a deferred non-blocking logging-quality issue.
- Other accepted limitations and deferred improvements are maintained in [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Navigation

- Completed milestones and approved direction: [ROADMAP.md](ROADMAP.md)
- Durable accepted choices: [DECISIONS.md](DECISIONS.md)
- Research provenance and non-authority boundary: [RESEARCH_INDEX.md](RESEARCH_INDEX.md)
