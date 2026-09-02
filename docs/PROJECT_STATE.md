# Project state

## Accepted snapshot

| Field | Current value |
| --- | --- |
| Branch | `master` |
| Prior accepted I1 baseline commit | `79480a1a7f164dd6076eab6526be9a60f8d0a0ef`; accepted I2/I2.1 source/tests/launcher and documentation are part of the repository snapshot containing this text; the future commit hash is intentionally not named |
| Current passed milestone | M12D - Selected Quick-Reaction Contextual Pilot; Steve accepted D3.2 and VAR1 with limitations on August 27, 2026. |
| Manual acceptance date | 2026-08-27 |
| Latest accepted deterministic diagnostic evidence | 452 passed, 0 failed for the accepted fixture-only M13A-R2-I1 foundation; historical M13A-I2/I2.1 evidence was 407/407 and the prior I1 foundation was 402/402 |
| Accepted internal packaged baseline | `0.8.0-alpha.1` |
| Deferred distribution validation | Clean Windows 10/11 validation remains incomplete. Invited testing has begun and produced a concrete Firefox GSMTC identity compatibility defect; it is not a blocker to internal development. |
| Current concluded exploration | Milestone 9, closed as prototype/design evidence; M9B has no final runtime acceptance claim. |
| M12C outcome | Straight handoff retained as no-model reference; quick reaction selected for later bounded work; session-aware observation deferred for revision/retest. |
| Approved post-M9 direction | M12D is passed: quick reaction is an accepted default-off runtime pilot; all further work requires a separate bounded authorization. |
| Current accepted foundation | M12A supplies pure factual context; accepted M12B and M12C are isolated consumers. M13A-R2-I1 adds an accepted provider-neutral, entity-centric Artist/Track claim foundation; it and prior M13A I1/I2.1 evidence remain isolated from normal runtime and do not pass M13A. |
| Current accepted implementation | M12D D3.2 - a default-off, opt-in prepared quick-reaction pilot with accepted topic isolation; normal launches preserve accepted M11B behavior. |
| Next project step | Steve decides the next separately bounded roadmap task; no later M13A stage is authorized. The D3.1 recent-comment topic-carryover issue was resolved by accepted D3.2; VAR1's repeated-form limitation remains active and deferred. |
| M12D-VAR1 outcome | Steve accepted the generate-only simulated mixed-song format-variety evaluation with limitations on August 27, 2026; repeated title-first and `Oh ...` forms remain an expected quality/product limitation, not an established runtime defect. |
| Deferred milestone | Milestone 6 |

## Accepted M13A-I1 foundation

Steve accepted M13A-I1 on August 28, 2026 as an isolated, fixture-only, non-network music-knowledge and identity-ownership foundation. It provides typed knowledge/provenance/outcome models, deterministic fixture parsing, root-only identity resolution and reconciliation, and bounded fake cache/retry/report diagnostics. The 18-row matrix and complete fake-driven suite passed 402/402; three Release builds were clean and final Warden read-only review found no findings. The accepted M13A-I1 implementation/documentation snapshot is commit `79480a1a7f164dd6076eab6526be9a60f8d0a0ef`. This is historical accepted evidence, not a Scribe rerun.

I1 is unreachable from normal runtime and adds no provider, network, persistence, cache-on-disk, dependency, model, scheduling, context, generation, speech, playback, UI, or runtime behavior. M12D remains the current passed runtime milestone and accepted runtime implementation; M13A as a whole is not passed. No later M13 stage is authorized, and any next step requires a separately bounded authorization. No desktop/manual test was required or performed.

## Accepted M13A-I2/I2.1 diagnostic evidence

Steve accepted I2/I2.1 only as isolated provider-diagnostic evidence with limitations. It does not activate a provider, connect normal runtime, add persistence, or pass M13A. The accepted I2/I2.1 source/tests/launcher and documentation are part of the repository snapshot containing this text; owner-run live artifact directories remain intentionally ignored. The prior accepted I1 baseline is commit `79480a1a7f164dd6076eab6526be9a60f8d0a0ef`.

Historical deterministic evidence: three serial Release builds passed with shared compilation disabled; the complete fake-driven suite passed 407/407; the existing M13A launcher passed 407/407; the fixture-only I2 launcher made zero provider requests and wrote no artifacts; final Warden review had no findings. These are historical results, not a rerun.

Final owner-run live artifact `20260829T011223006Z` completed safely: six identities attempted, nine logical requests, fourteen attempts, six HTTP 200 and eight HTTP 503. Outcomes were Wake Up/Rage Against The Machine `AmbiguousIdentity` (2/3), Ouro De Tolo/Raul Seixas `AmbiguousIdentity` (2/3), T.N.T./AC/DC `RateLimited` (1/2), Leraine/Secede & Kettel `Unknown` (1/1), Sweet Home Alabama/Lynyrd Skynyrd `RateLimited` (1/2), and Chop Suey!/System of a Down `RateLimited` (2/3). Two fixtures were not attempted after the circuit breaker stopped on two consecutive final rate-limited identities. `incompleteRateLimited=yes`; no cancellation; zero canonical identities; zero Wikidata progression; no final-run schema failure; contact data and raw response bodies absent. The 503s, conservative outcomes, circuit breaking, and no Wikidata progression are accepted evidence limitations. Do not weaken matching or identity policy.

The earlier restricted-sandbox run `20260829T002932605Z` was inconclusive (8 logical/16 attempts, all status 0 `ProviderOutage`); escalated run `20260829T003339942Z` recorded 9 logical/16 attempts, 3 HTTP 200, 13 HTTP 503, 1 `AmbiguousIdentity`, 1 `SchemaFailure`, 6 `ProviderOutage`, and zero canonical/Wikidata progression. I2.1 added 503 -> `RateLimited`, 1.5-second pacing, `Retry-After` or at least five-second fallback cooldown, and circuit stop after two consecutive final rate-limited identities.

The conservative global resolver remains unchanged. The later R2-I1 foundation accepts fact-scope-specific, provider-neutral identity and claim modeling additively; it does not weaken or replace I1/I2.1 behavior, authorize extra provider requests, or change normal runtime.

## Accepted M13A-R2-I1 entity/claim foundation

Steve accepted M13A-R2-I1 on September 1, 2026. It separates provisional observed grouping from positively established Artist/Track subjects and external provider references. One Artist may be reused by many Tracks; facts are stored once on their actual entity with per-claim provenance, scope, state, conflict, freshness, withdrawal, and replacement.

Historical acceptance evidence: Release source/test builds completed with zero warnings/errors, the complete suite passed **452/452**, accepted I1/I2.1 tests remained green, the fixture diagnostic made zero provider requests and wrote zero artifacts, and Warden accepted the repaired result with no remaining findings. Recording/Release claim promotion is refused, temporary projection does not copy durable facts, and production composition cannot reach the foundation.

M13A remains incomplete. R2-E1 is authorized for design only and must return to Steve before any live provider call. No provider/search activation, persistence, runtime or Creative Context integration, lyrics, M13B, or model change is authorized.

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
- Clean Windows 10/11 validation remains pending and deferred as distribution evidence. Invited testing has begun but is incomplete; Mortorro's machine exposed a concrete Firefox GSMTC identity compatibility defect recorded in [KNOWN_ISSUES.md](KNOWN_ISSUES.md). The accepted internal package is not clean-machine certified, broadly compatible, publicly released, or validated for third-party distribution.
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
- Genuine clean Windows 10 22H2 and clean Windows 11 remain pending/deferred distribution evidence. Invited testing has begun but remains incomplete and has produced the recorded Firefox GSMTC identity compatibility defect. See [MILESTONE_8B_TEST.md](MILESTONE_8B_TEST.md).

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

- Milestone 8 remains the accepted live-runtime/package baseline at `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- M9A was rejected, M9C supplied successful experimental evidence, and M9B supplied bounded integration/safety evidence without final runtime acceptance. Steve concluded M9 as a prototype phase; no further M9 testing or feature work is authorized. The accepted M8 safety baseline remains controlling. See [MILESTONE_9_CLOSURE.md](MILESTONE_9_CLOSURE.md).
- Steve approved the capability-first post-M9 roadmap on August 21, 2026. The detailed phases, dependency map, shared-capability map, format-maturity map, and memory/knowledge/voice/delivery boundaries are in [POST_M9_CAPABILITY_ROADMAP.md](POST_M9_CAPABILITY_ROADMAP.md).
- Steve approved the [M10A contract](MILESTONE_10A_CONTRACT.md), authorized M10A-only implementation, and accepted the automatically verified result on August 22, 2026. M10A is passed at implementation commit `93ece64bf5cb21e67378d9ae024c6f45568fe86e`.
- M10A remains deliberately pure and unreachable: deterministic M8 decision reproduction and 14 parity/golden traces only, with no runtime connection, new format, persistence, creative system, knowledge work, voice work, or audio behavior.
- Steve approved the [M10B contract](MILESTONE_10B_CONTRACT.md) and authorized M10B-only implementation on August 22, 2026. The resulting pure prepared-material lifecycle remained unreachable from runtime until M10C; Steve accepted it after Release builds passed with 0 warnings/errors and the complete suite passed 308/308, including 16 focused M10B traces.
- Steve approved and authorized [M10C](MILESTONE_10C_CONTRACT.md), then accepted it on August 23, 2026. The live one-comment runtime now routes through the M10A kernel and M10B lifecycle with strict M8 parity. Release verification passed with 0 warnings/errors and the complete suite passed 314/314. Steve confirmed preparation-stop and delivery-stop both return the UI immediately to ready; the delivery-stop log records Firefox-only 100% -> 25% -> 100% exact restoration.
- Steve approved and authorized [M11A](MILESTONE_11A_CONTRACT.md), then accepted it on August 23, 2026. The pure Editorial Arbiter now gates the existing comment at preparation and airtime with typed reasons, direct music continuation, hard eligibility, treatment compatibility, policy priority, and deterministic bounded ties. Release builds passed with 0 warnings/errors and the complete suite passed 320/320. The accepted live log recorded Prepare and ReserveForAirtime for one existing comment, exact Firefox 100% -> 25% -> 100% restoration, Chrome isolation, no playback control, and clean shutdown. No liner or second format was added.
- Steve approved the [M11B contract](MILESTONE_11B_CONTRACT.md), authorized M11B-only implementation on August 23, 2026, and accepted it on August 24, 2026. One fixed session-reusable `Zoya Radio.` liner now shares candidate-specific cadence/spacing gates, comment priority, music as a valid winner, M10B reserve/release/return-to-inventory, and protected Firefox ducking/restoration. Three Release builds passed with 0 warnings/errors and the complete fake suite passed 337/337. Steve's listening evidence confirmed one correct liner at transition 10 after the 20-minute gate, repeated earlier music wins, ordinary-comment priority, exact Firefox 100% -> 25% -> 100% restoration, Ready inventory return, safe Silent Mode, and clean Stop. He found 20 minutes acceptable for the proof but slow for timely manual verification; it remains a provisional experimental default. M12 and a commit remain unauthorized.
- Steve approved the post-M11 roadmap refinements on August 24, 2026: generation remains format-aware, Creative Direction stays conceptually separate from exact script generation, voice/renderer/cast/locale capabilities mature in stages, and narrow production experiments may precede the later shared scene-production foundation.
- Steve approved and authorized [M12A](MILESTONE_12A_CONTRACT.md), then accepted it on August 24, 2026 after reviewing representative normal, cleared, oversized, Unicode, hostile-metadata, and future-producer-use behavior. Three Release builds passed with 0 warnings/errors and the complete suite passed 348/348, including 11 focused M12A checks. The pure context assembler remains unreachable from normal runtime and changes no audible behavior.
- Steve approved the bounded [M12B contract](MILESTONE_12B_CONTRACT.md) and authorized M12B-only implementation on August 24, 2026. The isolated implementation separates direction from exact script, dispatches exact format/version producers and validators, bounds and discards alternatives, and permits only explicit available M12A facts. MediaProbe Tests and ControlApp Release builds passed with 0 warnings/errors; the complete suite passed 360/360, including 12 focused M12B checks. It calls no real model or prompt and remains unreachable from normal runtime. Steve reviewed the representative fixtures and accepted M12B on August 26, 2026. M12B is passed.
- Steve accepted [M12C Host Format Laboratory, Batch 1](MILESTONE_12C_CONTRACT.md) on August 26, 2026. Its deterministic handoff remains the no-model reference; quick reaction was selected for later bounded work, and session-aware observation remains deferred.
- Steve then approved D3 under [M12D](MILESTONE_12D_CONTRACT.md), accepted D3.1 on August 26, 2026, and accepted the bounded D3.2 topic-isolation repair and comparison on August 27, 2026. The default-off `ZOYA_QUICK_REACTION_PILOT=1` route prepares a Track-A quick reaction through accepted lifecycle/arbitration and delivers only through the existing Firefox ducked overlay. D3.1 versioned its pilot-only prompt, removed Markdown/emoji presentation characters before TTS, and added bounded recent-comment anti-repetition context without changing M11B normal launches. D3.2 replaced that prompt boundary with abstract form measurements only, removing prior words/topics/facts and preserving current Track-A title/artist grounding. Release ControlApp build passed with 0 warnings/errors; the historical D3.1 suite passed 371/371, and later current-tree verification passed 390/390 including D3.2 and VAR1 coverage. The D3.1 topic-carryover incident is resolved by D3.2; VAR1 remains accepted with limitations and its repeated-form issue remains active. These accepted M10B-M12D implementations are recorded in commit `fdc1b6528acc694b3c15db7e50902e80a1b8453d`.
- Steve accepted the generate-only M12D-VAR1 simulated mixed-song format-variety evaluation with limitations on August 27, 2026. Later VAR1 focused checks passed 7/7 and full fake-driven regression passed 390/390; repeated title-first and `Oh ...` forms remain an expected quality/product limitation. VAR1 was not live, audible, or user-visible; default-off/opt-in behavior and accepted runtime/safety behavior remain unchanged. Broader research and later formats remain deferred and unauthorized.
- Clean-machine checks remain deferred compatibility/distribution evidence. Invited testing has begun and already produced one recorded Firefox GSMTC identity compatibility defect; incomplete tester coverage and this unapproved repair do not reopen M8 or block internal development.
- The earlier 20:49 observer stall remains intermittent, unresolved, and unreproduced by the accepted Firefox-plus-Chrome run.
- Wrapper-refresh diagnostics are excessively repetitive; this is a deferred non-blocking logging-quality issue.
- Other accepted limitations and deferred improvements are maintained in [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Navigation

- Completed milestones and approved direction: [ROADMAP.md](ROADMAP.md)
- Detailed approved post-M9 capability sequence: [POST_M9_CAPABILITY_ROADMAP.md](POST_M9_CAPABILITY_ROADMAP.md)
- Approved M10A contract and acceptance record: [MILESTONE_10A_CONTRACT.md](MILESTONE_10A_CONTRACT.md)
- Approved M10B contract and acceptance record: [MILESTONE_10B_CONTRACT.md](MILESTONE_10B_CONTRACT.md)
- Passed M10C runtime-migration contract and acceptance record: [MILESTONE_10C_CONTRACT.md](MILESTONE_10C_CONTRACT.md)
- Passed M11A Editorial Arbiter contract and acceptance record: [MILESTONE_11A_CONTRACT.md](MILESTONE_11A_CONTRACT.md)
- Passed M11B commentary/reusable-liner contract and acceptance record: [MILESTONE_11B_CONTRACT.md](MILESTONE_11B_CONTRACT.md)
- Passed M12A Creative Context Assembler v1 contract and acceptance record: [MILESTONE_12A_CONTRACT.md](MILESTONE_12A_CONTRACT.md)
- Passed M12B Creative Direction and Content Generation contract and acceptance record: [MILESTONE_12B_CONTRACT.md](MILESTONE_12B_CONTRACT.md)
- Passed M12C Host Format Laboratory, Batch 1 contract: [MILESTONE_12C_CONTRACT.md](MILESTONE_12C_CONTRACT.md)
- Passed M12D Selected Quick-Reaction Contextual Pilot contract and acceptance record: [MILESTONE_12D_CONTRACT.md](MILESTONE_12D_CONTRACT.md)
- Accepted M13A-I1 isolated Music Knowledge foundation: [MILESTONE_13A_CONTRACT.md](MILESTONE_13A_CONTRACT.md)
- Durable accepted choices: [DECISIONS.md](DECISIONS.md)
- Research provenance and non-authority boundary: [RESEARCH_INDEX.md](RESEARCH_INDEX.md)
