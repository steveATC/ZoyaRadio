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
| Current concluded exploration | Milestone 9, closed as prototype/design evidence; M9B has no final runtime acceptance claim. |
| Next project step | Project-planning discussion before any future implementation authorization. |
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
- M9C through M9C.6 are separate explicit diagnostics only. M9C.6 uses fixed reviewed context packs and loopback `qwen3:14b-q4_K_M`; all M9C candidates remain unreachable from normal radio, Firefox, audio, speech, GUI, and normal logs/state.
- Recent state remains process-local and bounded to 10 tracks and 6 accepted AI comments. There is no persistent listening history or catch-up queue.

Full implemented behavior is in [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md).

## Milestone 8 gate status

- Steve approved the bounded [Milestone 8 contract](MILESTONE_8_CONTRACT.md) on August 10, 2026.
- The original M8A candidate was implemented at `f123ddd9de484de590ba596abf812314b265390c`; the accepted Radio-activity correction is `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc`.
- Steve manually accepted M8A on August 11, 2026. The accepted evidence and preserved regression procedure are in [MILESTONE_8A_TEST.md](MILESTONE_8A_TEST.md).
- Steve accepted the extracted M8B `0.8.0-alpha.1` package on August 11, 2026. M8B and Milestone 8 are closed for continued internal development at implementation commit `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- Clean Windows 10/11 and invited-tester validation remain pending and deferred as distribution evidence. The accepted internal package is not clean-machine certified, broadly compatible, publicly released, or validated for third-party distribution.
- M8 acceptance did not itself authorize post-M8 work. Steve subsequently approved the bounded M9A and M9C experiments, then explicitly authorized the bounded M9B implementation on August 15, 2026. M9B is now a candidate, not an accepted replacement for the M8 baseline.

## Milestone 9A candidate status

- Steve approved M9A as a structured-context experiment after the post-M8 project-management review.
- The implementation adds a typed deterministic brief, shallow recent-comment fingerprints, a separate candidate prompt, deterministic announcement candidates, and an explicit generate-only comparison benchmark.
- Normal radio continues to use the accepted Milestone 4 prompt/context, validator, fixed fallback, scheduler, delivery path, voice, targeting, ducking, and restoration behavior.
- The fake-driven suite currently passes 206 tests with 0 failures: all 178 accepted baseline regressions plus 28 focused M9A tests. This is current candidate verification, not a replacement for the accepted 178/0 baseline at `b56c03d`.
- The one authorized Codex live benchmark completed 48/48 loopback generations and verified model unload. A post-run audit found mojibake in its Unicode fixture; the fixture and tests were corrected, the first artifact set was labelled invalid for acceptance, and no second Codex benchmark was run.
- Steve's corrected manual benchmark at `artifacts/m9a/20260812-215927-424/` completed 48/48 requests, with 42 valid non-fallback comments, 2 mechanical validator rejections, 4 safe `FALLBACK` responses, and selected-model unload verified. The blind review was complete: all 48 samples were scored or explicitly rejected.
- The answer key identifies Arm Cedar as the StructuredCandidate and Arm Lark as the AcceptedBaseline. Cedar received fewer qualitative rejections (10 of 24 samples, versus 20 of 24) but was still not acceptable: the review identified repetitive construction, awkward output, unsupported listener/session claims, and a Unicode-metadata failure where neither arm reliably preserved the exact supplied title and artist.
- M9A does not pass. The StructuredCandidate is rejected for runtime consideration, is not integrated, and the `Borrowed Umbrella` grounding issue is not fixed. It supplied no integration authorization; later work is governed separately by the M9C record and approved M9B contract.

## Milestone 9C candidate status

- Steve approved M9C as a bounded source-backed enrichment experiment after M9A was rejected.
- M9C through M9C.4 established strict identity/source boundaries and showed that useful source-backed context can improve commentary, but deterministic anchor-plus-reaction composition remained awkward, repetitive, and over-rejected.
- M9C.5 compared identical prompts on `qwen3:4b-instruct` and `qwen3:14b-q4_K_M`. The 14B model produced 16 accepted anchor-plus-link candidates versus 2 for 4B and materially improved diversity, so it became the selected model for the final isolated experiment.
- M9C.6 supplied 16 owner-familiar tracks with reviewed three-to-five-fact context packs and let 14B generate complete comments in grounded and training-augmented modes. The 96-request run completed without generation failure and verified model unload.
- Steve scored 83 displayed outputs at a combined mean overall desire of 4.86/5; 73 received 5/5. The run contained 86 mechanically clean candidates and 10 mechanical hard rejects; all 10 rejects were also judged acceptable.
- Grounded and training-augmented quality was effectively tied (4.85 versus 4.86 overall), while grounded factual acceptability was higher (5.00 versus 4.84). Grounded 14B whole-comment synthesis is therefore the selected M9B candidate direction.
- M9C is closed as successful experimental evidence, not accepted runtime behavior. Repetition, metadata-mention policy, semantic exaggeration, and unsupported sound/lyric interpretation remain integration risks.
- Steve explicitly authorized the bounded [Milestone 9B contract](MILESTONE_9B_CONTRACT.md) on August 15, 2026. The candidate implementation adds pre-delivery reviewed context, an optional local 14B whole-comment route, bounded context cache, and M8 fallback selection; it is not accepted runtime behavior.
- The accepted M8 safety baseline remains controlling. M9B preserves schedule, delivery-time revalidation, Firefox-only ducking/exact restoration, and safe silence. The latest authorized Gate 4 run prepared four M9B comments, delivered three enriched M9B comments and three M8/fixed fallbacks, and safely timed out one known 26.3-second M9B preparation after entering ducking. Every attempt restored Firefox exactly, never paused playback, and stopped cleanly. Live MusicBrainz enrichment worked for `Ouro De Tolo` and `T.N.T.` after the nested artist-credit, multiple-recording, soundtrack-mapping, and stale-negative-cache repair.
- Current M9B engineering verification is recorded separately from the accepted 178/0 M8 baseline. The M9B generate-only owner review procedure is [MILESTONE_9B_TEST.md](MILESTONE_9B_TEST.md).

## Milestone 9B candidate status

- M9B is implemented as a candidate only, using the M9C.6 reviewed familiar-track catalog and a local `qwen3:14b-q4_K_M` whole-comment service before the existing Track-A preparation/delivery boundary.
- It keeps the M8 4B candidate/fallback available when the 14B model, confirmed context, timing, validation, or safe opportunity is unavailable. The 14B service is local-only, bounded, selected-model-unloading, and does not start after ducking.
- Live enrichment remains restricted to exact MusicBrainz and linked Wikidata; catalog source URLs are provenance and are never fetched at runtime. The bounded LocalAppData context cache is not listening history and Clear History clears it plus M9B's ephemeral repetition state.
- Current M9B Gate 3 policy engineering verification is recorded separately from the accepted historical M8 178/0 result: all four Release builds passed with 0 warnings and 0 errors, all four formatting checks and `git diff --check` passed, and the complete fake-driven suite passed 259 tests with 0 failures. It preserves exact observed-title aliases, reviewed-catalog precedence over stale negative cache outcomes, typed rejected-candidate diagnostics, the owner-approved distinction between natural musical characterization and hard technical/safety failures, bounded advisory regeneration, and selected-model unload.
- The bounded Gate 4 live-enrichment repair was then verified separately: all four Release builds passed with 0 warnings and 0 errors, all four formatting checks and `git diff --check` passed, and the complete fake-driven suite passed 266 tests with 0 failures. The additional coverage proves nested MusicBrainz artist-credit parsing, accepting multiple exact song-level recordings, one-official-soundtrack-group deduplication, safe invalidation of legacy negative caches, unsupported collaboration refusal, and enrichment-path desktop-media isolation. The subsequent observability/pre-delivery repair passed the same checks with 278 fake-driven tests and adds private M9B text-lifecycle logging, definitive pre-duck measured-duration rejection, and typed MusicBrainz/Wikidata failure diagnostics.
- The one authorized generate-only 14B artifact is `artifacts/m9b/20260815-095443-189/`: 48/48 requests, zero generation failures, 33 displayable candidates, 15 mechanical hard discards, 68.75% availability, 624.9 ms median/819.7 ms p95/3328.0 ms maximum latency, and selected-model unload verified.
- Steve accepted Gate 2 on August 15, 2026 with a narrow same-card repetition exception. Of 32 scored displayable candidates, factual acceptability was 5.00, context integration 4.72, personality 4.44, spoken naturalness 4.38, variety 3.72, and overall desire 3.84. The 10 scored candidates without a repeated-template flag averaged 4.90 overall and variety; Raul Seixas candidate 3 was intentionally unscored as a near-copy. The full decision is in [MILESTONE_9B_TEST.md](MILESTONE_9B_TEST.md).
- Four silent Gate 3 attempts recorded 48 genuine transitions with zero forbidden desktop-media activity. The post-repair evidence includes one ready candidate before a transition, safe suppression of prepared delivery, cancellation/stale-work disposal, and selected-model unload. The reviewed-context smoke's former 7/12 (58%) validator-ready figure remains diagnostic; Steve accepted all 12 final outputs and Gate 3 under the revised operational criteria.
- Steve concluded Milestone 9 on August 15, 2026 as a successful exploration/prototype phase. M9B's original Gate 4 requirement was not met, so M9B is not accepted runtime behavior and no further M9 Gate 4 test is authorized. The complete outcome and deferred work are in [MILESTONE_9_CLOSURE.md](MILESTONE_9_CLOSURE.md); the implemented candidate remains unchanged pending future product planning.

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
- M9A was rejected, M9C supplied successful experimental evidence, and M9B supplied bounded integration/safety evidence without final runtime acceptance. Steve concluded M9 as a prototype phase; no further M9 testing or feature work is authorized. The accepted M8 safety baseline remains controlling. See [MILESTONE_9_CLOSURE.md](MILESTONE_9_CLOSURE.md).
- The next action is project planning: define product areas, roadmap sequencing, and future milestone contracts before code changes begin.
- Clean-machine and invited-tester checks remain deferred compatibility/distribution evidence. A later defect may require a bounded repair, but missing tester feedback does not block internal development.
- The earlier 20:49 observer stall remains intermittent, unresolved, and unreproduced by the accepted Firefox-plus-Chrome run.
- Wrapper-refresh diagnostics are excessively repetitive; this is a deferred non-blocking logging-quality issue.
- Other accepted limitations and deferred improvements are maintained in [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Navigation

- Completed milestones and approved direction: [ROADMAP.md](ROADMAP.md)
- Durable accepted choices: [DECISIONS.md](DECISIONS.md)
- Research provenance and non-authority boundary: [RESEARCH_INDEX.md](RESEARCH_INDEX.md)
