# Current architecture

This document describes the accepted M12D default-off quick-reaction runtime pilot, M12B Creative Direction/Content Generation laboratory boundary, M12A Creative Context Assembler, and M11B commentary/reusable-liner runtime over the accepted M11A Editorial Arbiter, M10C one-comment runtime, M10A deterministic opportunity kernel, and M10B prepared-material lifecycle, plus the accepted isolated M13A-I1 fixture foundation, the implemented-but-unaccepted M9B grounded-context candidate, and preserved isolated M9A/M9C diagnostics. M9 was concluded without final M9B runtime acceptance; accepted M8 playback safety invariants remain controlling.

## Accepted M13A-I1 isolated foundation

- I1 is fixture-only, local, fake-driven, non-network, and unreachable from normal runtime. It contains typed knowledge/provenance/outcome models, a MusicBrainz fixture parser, linked Wikidata fixture behavior, root-only identity resolution, deterministic reconciliation, and bounded fake cache/retry/report diagnostics.
- Top-level/root artists own identity candidacy; nested/direct credits are references bound only to a declared root. Credit-only identity acceptance is prohibited. Exact canonical object binding is required; MBID remains `EntityId`, while QID is provenance only. Malformed/schema, ambiguous, temporary, and negative factual outcomes remain distinct, and parsers are total/no-throw.
- The accepted 18-row data-driven matrix covers the fixture contract. There is no network client/provider activation, persistence, cache-on-disk, normal-runtime/controller/UI/scheduling/context/prompt/speech/playback binding, or dependency addition from I1. Accepted M12D/M11 behavior is unchanged.

## Accepted M10A deterministic foundation

- `DeterministicOpportunityKernel` is a pure immutable reducer over explicit state, typed inputs, typed decisions/reasons, supplied timestamps, and supplied transition targets.
- It reproduces accepted M8 scheduling/state decisions and is covered by 14 focused parity/golden traces. Steve accepted M10A on August 22, 2026 after the complete suite passed 292 tests with 0 failures.
- Before M10C, no production launcher, controller, host, runtime, UI, playback, or audio path referenced the kernel. M10C now uses it as opportunity authority for the existing one-comment runtime; M11A does not change that ownership.
- The kernel performs no clock, random, Windows, file, network, AI, speech, audio, playback, or persistence access.

## Accepted M10B prepared-material lifecycle

- Steve approved the bounded M10B contract, authorized M10B-only implementation, and accepted the result on August 22, 2026. M10B is passed.
- `PreparedMaterialLifecycle` is an internal immutable reducer over explicit state and typed inputs. It tracks Preparing, Ready, Reserved, and Terminal material without reading a clock or performing side effects.
- Caller-supplied bindings may constrain one M10A opportunity, normalized-exact subject track, opaque logical session, and absolute expiry. Single-opportunity material and reusable-within-binding material remain distinct policies.
- The lifecycle owns only opaque payload handles. Typed decisions explicitly accept or refuse ownership and request one disposal when owned material becomes terminal; the reducer never holds or disposes a real resource.
- Reservations are exclusive but do not authorize airtime. Reusable material may return to Ready only through explicit release or completion disposition while still valid; single-opportunity material cannot return to inventory.
- Terminal records clear their payload handle and make repeated cleanup idempotent. State collections are exposed through read-only wrappers.
- Before M10C, no production launcher, controller, host, runtime, UI, playback, audio, M9 candidate, or diagnostic referenced the lifecycle. M10C introduced it for comments; accepted M11B now exercises `ReusableWithinBinding` for one session-bound liner fixture.
- Release builds pass with 0 warnings and 0 errors. The complete fake-driven suite passes 308 tests with 0 failures, including 16 focused lifecycle and composition traces.

## Accepted M10C live runtime migration

- Steve accepted M10C on August 23, 2026. `M10CRuntimeMigrationState` is the live adapter for the existing one-comment path: M10A owns deterministic opportunity/schedule decisions; M10B owns prepared-material lifecycle, binding, reservation, terminal disposition, and typed disposal; the adapter maps those decisions to existing M8 pipeline resources.
- The migration adds no format, editorial policy, playback control, persistence, or new audio behavior. Existing M8 preparation, delivery, Firefox-only ducking, exact restoration, silence/refusal, Silent Mode, Clear History, and bounded memory behavior are preserved.
- Current Release verification passed with 0 warnings/errors and 314/314 fake-driven tests. Steve confirmed Firefox-only delivery restoration, Chrome isolation, preparation-stop with no ducking, delivery-stop with exact restoration, and immediate control-form return to ready.

## Accepted M11A Editorial Arbiter

- Steve accepted M11A on August 23, 2026. EditorialArbiter is a pure deterministic decision layer with separate preparation and airtime inputs, one selected content candidate or direct music continuation, and stable typed decision traces.
- The arbiter evaluates hard opportunity/material eligibility, exact binding, prepared state, expiry, treatment availability, bounded preparation capacity, current music/speech pressure inputs, spacing/cooldown inputs, explicit policy priority, and deterministic bounded tie-breaking. Randomness cannot create the reason to interrupt music.
- The live M10C-equivalent profile supports only the existing comment and DuckOverMusic treatment. It adds no liner, second format, cadence value, universal relevance score, knowledge, memory, creative system, program break, or playback control.
- RadioBehaviorTest asks the arbiter before claiming the existing preparation path and before M10B reservation. M10A remains opportunity authority, M10B remains material/reservation/disposal authority, and the existing runtime retains final pre-air Firefox validation and protected cleanup.
- Current Release builds pass with 0 warnings/errors and the complete fake-driven suite passes 320/320. Steve's accepted live run recorded typed Prepare and ReserveForAirtime decisions, exact Firefox 100% -> 25% -> 100% restoration on the same session, Chrome isolation, no playback control, and clean shutdown.

## Accepted M11B commentary and reusable-liner proof

- Steve approved M11B-only implementation on August 23, 2026 and accepted it on August 24, 2026. The accepted runtime adds exactly one fixed local copy fixture, `Zoya Radio.`, prepared through the accepted Nicole/local fallback speech boundary and retained only in memory for the current logical runtime session.
- `ReusableLinerInventory` composes the accepted M10B reducer: Preparing -> Ready -> one Reserved transition -> explicit Release or completed ReturnToInventory. Session rebind, Stop, shutdown, or invalidation disposes the owned prepared speech exactly once. Concurrent reuse is refused.
- The accepted existing comment is `Timely`; the reusable liner is `Routine`. Comment preparation and airtime win conflicts. A comment-preparation boundary or missed/unready comment cannot be filled by the liner, and final refusal never triggers second-choice chatter.
- Candidate-specific arbiter pressure keeps the initial experimental liner policy separate from comment behavior: both six genuine transitions and twenty elapsed minutes since arm/last completed liner, plus two transitions since completed host speech. These values are provisional listening-test defaults, not permanent station policy.
- Ready material has no right to airtime. Silent Mode, cooldown, spacing, protected comment boundaries, unavailable delivery capacity, unsafe targeting, or stale timing select direct music/silence with typed reasons.
- Both comments and the liner use the same `WindowsRadioOpportunityHost` duck/speech/restoration implementation. Liner delivery revalidates the current track, the exact logical Firefox GSMTC target, one Firefox Core Audio session, Playing state, and the existing two-second window; it never pauses, resumes, toggles, seeks, changes endpoint/master volume, or targets Chrome.
- Clear History leaves liner inventory and cooldown alone because they are bounded operational state, not listening history or Station Lore. Nothing persists across process exit.
- Release builds for media probe, tests, and control app pass with 0 warnings/errors. The complete fake suite passes 337/337, including 17 focused M11B tests. Steve's accepted desktop trial confirmed one liner at transition 10 after more than 20 minutes, repeated earlier direct-music selections, comment priority, exact Firefox 100% -> 25% -> 100% restoration, Ready inventory return, safe Silent Mode, and clean Stop. The 20-minute value remains a provisional experimental default because Steve found it acceptable for the proof but slow for timely manual verification.

## Accepted M12A Creative Context Assembler

- Steve accepted M12A on August 24, 2026. `CreativeContextAssembler` is a pure deterministic, versioned packet assembler over explicitly supplied typed state. It copies bounded values and has no clock, network, model, prompt, provider, storage, audio, browser, playback, scheduling, reservation, or runtime dependency.
- Version 1 represents exact bounded current title/artist values, opportunity class/reason, deterministic session facts, recent tracks, accepted comments, and recent-use/policy facts with explicit provenance.
- Null facts remain unknown. Collections distinguish unknown, known empty, complete, and complete-with-omissions. Injected limits cannot exceed the accepted 10-track and 6-comment caps; newest retained items preserve deterministic order.
- String limits count Unicode text elements and report omitted elements. Instruction-shaped metadata remains inert typed data. Output collections are copied and read-only, and no state persists between assemblies.
- No normal production file references the assembler or packet. M12A therefore causes no audible, operator-visible, scheduling, lifecycle, or playback change.
- Three Release builds pass with 0 warnings/errors and the complete suite passes 348/348, including 11 focused M12A checks. Steve accepted the representative packet views.

## Accepted M12B Creative Direction and Content Generation boundary

- The pure `FormatLaboratory` composes injected Creative Direction, exact format/version Content Generation, and exact validator boundaries over an explicitly supplied M12A context packet. It does not call a real model or construct a prompt.
- `CreativeDirectionBrief` describes editorial objective, speaker role, situation, structure, binding, stable personality identity, explicit context permissions/scopes, prohibitions, and optional cast/performance/lore intent. It has no script field. Exact words exist only in `GeneratedContentCandidate`.
- Format producers dispatch by exact format identity/version. Validators additionally dispatch by exact validator identity/version. Contract, version, personality, scope, and availability mismatches produce typed refusal outcomes.
- Context permission grants the option to use one supplied fact; it does not require fact use. Declared use may be empty and is rejected for unknown, unavailable, omitted, unpermitted, or wrong-scope facts.
- Alternative count and all strings are bounded and copied. A run retains only one selected valid candidate; rejected, excess, unselected, failed, or cancelled output has no persistent ownership.
- No normal runtime source references M12B. It has no network, prompt, model, storage, audio, browser, playback, scheduling, reservation, or airtime capability and causes no audible or operator-visible change.
- Release builds passed with 0 warnings/errors and the complete fake-driven suite passed 360/360, including 12 focused M12B checks. Steve reviewed the fixtures and accepted M12B on August 26, 2026. M12B is passed.

## Accepted M12D quick-reaction contextual pilot

- Steve accepted M12D D3.1 on August 26, 2026 and accepted the D3.2 topic-isolation repair and comparison on August 27, 2026. `ZOYA_QUICK_REACTION_PILOT=1` is an explicit local experimental setting, default off; without it the accepted M11B generic-comment route remains selected.
- When enabled, `QuickReactionTransitionCommentProvider` selects only quick reaction through the same M10A opportunity, M10B lifecycle/binding/reservation, M11A arbitration, Track-A preparation, Track-B revalidation, and `DuckOverMusic` delivery as the accepted comment. It adds no cadence rule, persistent state, program break, or playback-control capability.
- The pilot uses versioned `quick-reaction-v3-topic-isolation` copy. Its prompt receives only the current Track-A title/artist as subject matter. Up to three recent accepted lines are reduced to abstract form measurements (opening shape, length band, sentence count, ending shape, and dash/semicolon use); their words, topics, facts, premises, entities, jokes, scenes, and continuations are absent. It does not receive session observations, enrichment, Music Knowledge, listener preference, or Station Lore.
- Before generic mechanical validation and duplicate comparison, pilot-only cleanup removes Markdown emphasis/underscores and emoji/presentation symbols from model text. It is not a factual assertion filter and does not add a semantic regeneration loop or restrict accepted creative latitude.
- D3.1 Release ControlApp verification passed with 0 warnings/errors and the full fake-driven suite passed 371/371. The accepted D3.2 repair preserves current-track grounding, lifecycle/arbitration/binding/cancellation/targeting/ducking/restoration, and M11B pilot-off behavior while isolating prior-topic subject matter. Its exactly 32-request comparison retained three mechanical under-15-word rejections and succeeded in selected-model unload. Later current-tree verification passed 390/390 including D3.2 and VAR1 coverage. The historical D3.1 Otava Yo/Tommy the Cat carryover was a quality defect, not a lifecycle or targeting defect, and is resolved by D3.2.

## Platform and local dependencies

- C#/.NET 8 Windows applications targeting `net8.0-windows10.0.19041.0`: an accepted WinForms control frontend plus preserved console diagnostics. The API target remains unchanged; the M8B private-alpha package support baseline is Windows 10 22H2 x64 (build 19045) or newer.
- M8B publishes the WinForms application as unsigned self-contained `win-x64` with Microsoft .NET/WindowsDesktop runtime 8.0.29, a manifest, hashes, private symbols outside the ZIP, and no installer.
- Windows GSMTC through `Windows.Media.Control` for media-session discovery, metadata, playback state, and event observation.
- Windows Core Audio session APIs for per-application Firefox volume capture, proportional ducking, and exact restoration.
- Ollama's loopback HTTP API at `http://localhost:11434/api/` with accepted `qwen3:4b-instruct` fallback comments and an optional M9B candidate route through owner-installed `qwen3:14b-q4_K_M`.
- KokoroSharp.CPU 0.8.0 with the local Kokoro model and `af_nicole` as the preferred voice.
- Windows `SpeechSynthesizer`/`MediaPlayer` as the local speech fallback.
- Timestamped UTF-8 shared-runtime logs under `%LOCALAPPDATA%\Zoya Radio\logs`; explicit diagnostic logs remain under the ignored repository `logs/` folder. There is no telemetry or cloud provider.

The project-local Microsoft .NET SDK 8.0.423 is in ignored `.dotnet/`. Microsoft's Windows SDK reference pack, approved .NET/WindowsDesktop 8.0.29 Win-x64 runtime packs, and other restored packages are in ignored `.packages/`; the approved Kokoro model is in ignored `.tools/kokoro/`. These repository-local build inputs require no administrator privileges and are not committed.

## Accepted versions and local artifacts

- Ollama `0.32.6` and accepted `qwen3:4b-instruct`: 4.0B parameters, Q4_K_M quantization, 2,497,293,803 bytes on disk (about 2.50 GB decimal).
- The M9B candidate additionally uses owner-installed `qwen3:14b-q4_K_M`: 14.8B parameters, Q4_K_M quantization, approximately 9.3 GB on Steve's machine, Apache License 2.0, with materially higher cold-load and RAM/VRAM cost. It is neither bundled nor automatically downloaded.
- Normal Ollama requests use .NET `HttpClient`, `stream: false`, `think: false`, temperature `0.7`, a 20-second generation timeout, and `keep_alive: 0`. Explicit performance and M9A benchmark diagnostics use a temporary `30s` keep-alive and `/api/ps`, then unload only the selected model.
- `KokoroSharp.CPU` 0.8.0 with Microsoft ONNX Runtime 1.22.0. The FP32 model is `.tools/kokoro/kokoro.onnx`, 325,532,387 bytes (310.5 MiB), SHA-256 `7D5DF8ECF7D4B1878015A32686053FD0EEBE2BC377234608764CC0EF3636A6C5`.
- The restored package closure is about 559.69 MiB. The model, package cache, and one built application output require about 1.20 GiB, excluding additional ignored audition build output.
- KokoroSharp is MIT licensed; MisakiSharp and the Kokoro model/voices are Apache-2.0. Restored KokoroSharp build output contains eSpeak NG, but the M8B staging audit removes and hash-audits every eSpeak file. Ordinary Nicole/en-US preparation works without it; unusual/OOV input may safely use Windows TTS fallback before any delivery control.
- Microsoft David, Zira, and Mark were discovered as enabled en-US Windows voices; the accepted Windows fallback prototype selected David.
- `.tools/`, `.packages/`, `bin/`, `obj/`, `artifacts/`, generated audio, and logs are excluded from Git.

## Executable modes

`Program.cs` dispatches explicit launcher modes. The original watcher remains read only.

| Launcher | Current responsibility |
| --- | --- |
| `run-zoya-radio-control.cmd` | Launch the accepted WinForms frontend over the shared automatic-radio runtime and controller. |
| `run-media-probe.cmd` | Enumerate and observe all GSMTC sessions; `--once` takes a snapshot and exits. |
| `run-playback-control-test.cmd` | Explicitly confirm and test targeted Firefox pause/resume. |
| `run-spoken-announcement-test.cmd` | Explicitly confirm one prepared title/artist announcement with protected same-session recovery. |
| `run-ai-comment-test.cmd` | Generate, validate, measure, or speak one local AI comment; includes generate-only, no-AI, and failure diagnostics. |
| `run-commentary-benchmark.cmd` | Run the isolated M9A synthetic, generate-only, blinded prompt comparison; no media, audio, GUI, or speech path is reachable. |
| `run-source-backed-commentary-benchmark.cmd` | Run the isolated M9C MusicBrainz/Wikidata plus loopback-Ollama comparison; no media, audio, GUI, or speech path is reachable. |
| `run-owner-curated-commentary-benchmark.cmd` | Run the isolated M9C.3 curated-confirmed, blinded quality benchmark; no media, audio, GUI, or speech path is reachable. |
| `run-deterministic-context-benchmark.cmd` | Run the isolated M9C.4 deterministic-anchor benchmark; no media, audio, GUI, speech, or web-enrichment path is reachable. |
| `run-m9c5-model-comparison.cmd` | Run the isolated M9C.5 4B-versus-14B comparison; normal radio configuration remains unchanged. |
| `run-m9c6-whole-comment-benchmark.cmd` | Run the isolated M9C.6 grounded-versus-training-augmented whole-comment comparison with reviewed context packs and local 14B. |
| `run-m9b-generate-only-evaluation.cmd` | Run the M9B 16-track/48-request owner-quality artifact generation. It does not reach Firefox, media, audio, GUI, speech, or normal radio state. |
| `run-m9b-gate3-silent.cmd` | Historical explicit Gate 3 preparation-only Firefox observation. It uses no speech, ducking, playback command, or volume operation and reports a typed operational evidence verdict after 12 transitions. Gate 3 is accepted from cumulative evidence; no further Gate 3 desktop trial is required. |
| `run-m9b-gate3-smoke.cmd` | Explicit local reviewed-catalog/loopback-Ollama diagnostic. It records raw local rejected-candidate evidence and selected-model unload without reaching Firefox, GSMTC, audio, speech, ducking, playback, volume, GUI, cache, or live enrichment. |
| `run-m13a-music-knowledge-evaluation.cmd` | Run the accepted M13A-I1 fake-driven 18-row music-knowledge matrix; local fixtures only, with no provider, network, persistence, or normal-runtime path. |
| `run-radio-behavior-test.cmd` | Arm transition-aware automatic preparation and ducked delivery. |
| `run-audio-ducking-test.cmd` | Enumerate or explicitly test isolated Firefox Core Audio duck/restoration. |
| `run-neural-tts-diagnostic.cmd` and related launchers | Safely exercise Nicole preparation, fallback, and resource diagnostics. |
| `run-voice-audition.cmd` | Produce local voice-audition material. |

## Component responsibilities

| Component | Responsibility |
| --- | --- |
| `RadioRuntimeController` | Own the authoritative asynchronous Start/Stop lifecycle, immutable status snapshots, stale-update suppression, and protected cleanup boundary shared by frontends. |
| `RadioControlForm` | Present controls, deterministic runtime state, and thin M8B readiness/setup actions; it does not infer playback facts or own radio behavior. |
| `MediaSessionWatcher` and `SessionObserver` | Request GSMTC, enumerate sessions, subscribe to session/media/playback changes, and refetch changed metadata. |
| `MediaSnapshot` | Hold title, artist, and optional album without inventing absent fields. |
| `RadioSessionAccess` | Read and validate the selected Firefox session and its current metadata/playback state. |
| `FirefoxSessionLifecycle` | Classify a stable session, a harmless wrapper refresh, an unsafe target state, or a genuine logical replacement. |
| `RadioBehaviorState` and `RadioHistory` | Deduplicate transitions, choose the 2-5-transition schedule, track one opportunity, and retain bounded in-memory history. |
| `TransitionRadioPipeline` | Separate Track-A preparation from Track-B delivery and abandon stale, late, or unsafe work. |
| `AiCommentService`, `OllamaClient`, `CommentPrompt`, and `CommentValidator` | Build title/artist-only prompts, call loopback Ollama, validate mechanical output rules, choose fixed fallback, and unload the model. |
| `CommentaryBrief`, `StructuredCommentPromptFactory`, and `DeterministicAnnouncementRenderer` | Represent the isolated M9A candidate context, prompt boundary, and exact-metadata announcement candidates; normal runtime does not compose them. |
| `CommentaryBenchmarkRunner` and `CommentaryBenchmarkArtifacts` | Run the fixed loopback-only synthetic comparison and write blinded ignored artifacts without desktop-media dependencies. |
| `KokoroSpeechService`, `WindowsSpeechService`, and `SpeechServiceSelection` | Prepare Nicole audio locally and select Windows speech fallback when required. |
| `WindowsAudioSessionCatalog` and `AudioSessionSelector` | Enumerate the default multimedia render endpoint and select exactly one active Firefox application session. |
| `WindowsTransitionAudioDucker` and `AudioDuckingSequence` | Revalidate the delivery-time audio target, duck only that session, run prepared speech, and restore the exact captured state in protected cleanup. |
| `LocalLog` and `LocalAppDataLogFolderProvider` | Write readable timestamped UTF-8 diagnostics and resolve the accepted shared-runtime log folder under LocalAppData. |
| `PrivateAlphaPackageIntegrity` and `PrivateAlphaSelfTest` | Verify the packaged manifest/hashes, refuse unsafe startup, audit prohibited content, and provide safe non-media extracted-package diagnostics. |
| `PrivateAlphaReadinessPresenter`, `PrivateAlphaSetupController`, and `OllamaSetupClient` | Present package/Windows/disk/Firefox/Visual C++/loopback-Ollama/Qwen readiness and perform only explicit, consented local model setup. |

## Isolated Milestone 9C diagnostic candidate

- M9C uses a strict MusicBrainz artist/recording identity gate, then only a linked Wikidata structured description with a direct source URL. It caches bounded diagnostic outcomes under ignored `artifacts/m9c/cache/` and treats all uncertain/failed source chains as no enrichment.
- Its source-backed prompt and benchmark command run before ordinary logs, GSMTC, Firefox, Core Audio, speech, ducking, GUI, and normal runtime state. Normal radio never composes M9C types or makes web requests.
- M9C.3 was a useful source-backed direction-finding experiment, but human review found unsupported cultural/geographic expansion and invented persona history.
- M9C.4 adds a separate 20-condition deterministic-anchor corpus. Its code renders each curated verified fact; its loopback-only model may generate only a separately validated subjective link using at most one selected `FictionalPersonaFact`.
- M9C.5 compares the accepted 4B diagnostic model with local 14B over fixed prompt-equal inputs. M9C.6 supplies 16 reviewed multi-fact context packs and lets 14B generate the complete comment in grounded and training-augmented modes.
- Steve selected the grounded M9C.6 direction as successful experimental evidence. All M9C commands remain generate-only, request selected-model unload, and leave normal radio unchanged.
- M9C is closed as an experiment. M9B reimplements only the reviewed runtime-safe responsibilities under its separate approved contract; M9C benchmark commands remain diagnostic-only.

## Milestone 9B grounded-context candidate

- `M9BContextCatalog` loads a versioned, reviewed 16-card local catalog. Each card keeps exact Unicode title/artist, any individually reviewed exact artist-specific title aliases, one to three concise facts, source name/direct HTTPS provenance, applicability, and usefulness. It performs no fuzzy matching or generic suffix removal. Those cited pages are never fetched at runtime.
- `M9BContextProvider` selects an exact local card or individually reviewed exact artist-specific alias before consulting its cache. Unknown tracks can use strict MusicBrainz artist/recording confirmation: an exact normalized title plus a nested artist-credit canonical ID/name match; multiple exact recording editions are song-level evidence only. Official MusicBrainz release data may add one fact only when exactly one distinct soundtrack release group remains. Linked Wikidata facts remain available under existing rules; absent, ambiguous, malformed, rate-limited, redirected, timed-out, cancelled, or weak context is discarded. Redirect following is disabled for the default source client.
- `M9BContextCache` stores only bounded typed context outcomes under `%LOCALAPPDATA%\ZoyaRadio\cache\music-context\`: SHA-256 normalized identity keys, 100 entries maximum, 24-hour confirmed and two-hour negative TTLs, corrupt-entry refusal, atomic writes, and a repair schema marker that ignores pre-repair negative entries while preserving reusable confirmed entries. It stores no page content, prompts, generated prose, browser data, or listening sequence.
- `M9BGroundedCommentService` asks only selected local `qwen3:14b-q4_K_M` for one complete grounded comment. Its prompt treats exact metadata and the typed card as the factual/editorial anchor while permitting familiar low-risk music knowledge and natural host opinion. Hard discard is limited to high-confidence technical/safety failures; advisory/repetition candidates receive at most one regeneration, and a remaining advisory candidate is retained for delivery. Rejected attempts retain raw/normalized local diagnostics with typed reasons for owner inspection. The selected model is unloaded in protected cleanup.
- `M9BTransitionCommentProvider` is an optional Track-A preparation wrapper around the accepted M8 provider. It prepares enrichment and 14B text before speech preparation and before Track-B delivery/ducking. M8 4B/fixed fallback remains eligible when M9B is missing, disabled, late, invalid, stale, or unavailable. No M9B web request or model operation starts after ducking. Private runtime logs distinguish selected, prepared, delivered, and discarded M9B text without retaining prompts or raw source content.
- Prepared audio carries its measured WAV duration. A duration strictly above the configured automatic maximum is discarded and disposed before Core Audio enumeration, ducking, or speech; the exact limit is permitted. Delivery-time timeout/restoration remains a defense in depth for unexpected stalls or measurement disagreement.
- An enriched fingerprint/fact identity enters M9B's six-entry ephemeral state only after completed delivery. Clear History clears that state and the context cache; shutdown clears only the ephemeral state. Gates 2 and 3 are owner-accepted. Gate 4 did not meet its final requirement, and M9 was concluded without M9B runtime acceptance; no further M9 work is authorized.
## Control application and lifecycle

## Isolated Milestone 9A diagnostic candidate

- The candidate derives exact-track/artist occurrence counts and transition distances from at most 10 prior usable tracks. The subject is excluded from those counts and identity normalization matches `RadioHistory`.
- Typed reason precedence is same-artist transition, exact-track repeat, repeated artist, then the explicitly supplied ordinary/continuity/no-pattern reason. Additional applicable reasons remain secondary observations.
- At most 6 recent accepted comments carry exact normalized text plus shallow opening, listener-name, question, comparison-marker, and word-count fingerprints.
- The candidate prompt is a separate embedded resource with inert JSON metadata, visible trusted/creative/prohibited sections, and exact `FALLBACK`. The accepted Milestone 4 prompt remains unchanged.
- The version 1.0 manifest contains 12 synthetic scenario classes and 2 samples for each blinded arm, for 48 requests. Generated artifacts remain ignored under `artifacts/m9a/`.
- The M9A command returns before ordinary logging, speech setup, GSMTC, Firefox, Core Audio, ducking, GUI, or runtime-state construction. Only loopback Ollama is available to this command.
- Steve's completed blinded review rejected the M9A StructuredCandidate for runtime consideration. It remains isolated and is not integrated into the control application or automatic radio.

- `ZoyaRadio.ControlApp` is a same-process WinForms frontend over the accepted automatic-radio runtime. It does not contain a second scheduling, targeting, speech, or ducking implementation.
- `RadioRuntimeController` exposes one authoritative asynchronous lifecycle. Start is idempotent, Stop is safely awaited, window close waits for the same cleanup path, and stale callbacks from an ended generation are suppressed.
- The Form remains thin. It exposes Start, Stop, Silent Mode, Clear History, Open Logs Folder, title, artist, Firefox target status, ordinary status, and read-only Radio activity derived from immutable runtime snapshots/events.
- Radio activity reports deterministic work such as the transition countdown, stability wait, local generation, Nicole preparation, prepared waiting, target/audio validation, ducking, speech, restoration, safe abandonment, target loss, stopping, and faults. Harmless Firefox wrapper refreshes remain detailed-log-only rather than churning the GUI state.
- Stop, window close, cancellation, and faults retain the accepted restoration ownership. The frontend cannot close past protected cleanup or retarget another media/audio session.
- Shared-runtime logs are written under `%LOCALAPPDATA%\Zoya Radio\logs`, and the button opens that folder. Existing console launchers remain usable diagnostic frontends to the same runtime.

## M8B packaging and readiness

- Version `0.8.0-alpha.1` is the accepted internal packaged baseline: a self-contained unsigned Windows x64 portable ZIP. The package resolves model/voice assets from `AppContext.BaseDirectory`, independent of the process working directory. Clean-machine and invited-tester validation remain deferred, so this is not a certified or public distribution.
- `package-manifest.json` records every staged file, size, SHA-256, critical status, version/RID, source commit, and dirty-source state. Packaged startup verifies critical assets before creating the runtime and refuses unsafe/corrupt packages rather than guessing.
- The deterministic packaging script stages only the Win-x64 publish closure, Kokoro FP32 model, `af_nicole.npy`, tester documents, licences, and notices. It excludes PDBs from the ZIP while retaining them privately, and rejects eSpeak, extra voices, non-x64 native assets, logs, generated audio, and bundled Firefox/Ollama/Qwen content.
- Safe self-test modes run before the GUI/media runtime. They never contact Ollama, open external applications, produce audible speech, or control desktop media/volume. The neural mode silently prepares audio in memory from the extracted candidate and verifies cancellation and Windows fallback.
- Readiness distinguishes valid/corrupt package assets, supported Windows, disk guidance, Firefox absent/idle/active/unsafe targeting, Visual C++ availability, Ollama absent/stopped/running/unreachable/incompatible, Qwen absent/present, and ready operation.
- Firefox and Ollama remain external. The application may open the official Ollama page only after a labelled click. It does not install or start Ollama. Obtaining `qwen3:4b-instruct` through a running local Ollama requires an explicit default-No confirmation, approximate-size disclosure, at least 8 GiB confirmed free space, streamed progress, cancellation, and a bounded two-hour operation.
- The Ollama API remains loopback-only. Capability checks use `/api/version` and `/api/tags`; the exact Qwen name, digest, and size are shown when available. No terms are accepted automatically and no PATH, firewall, service, driver, or system configuration is changed.
- The external Microsoft Visual C++ x64 runtime is detected, not bundled or silently installed. Missing runtime status directs testers to Microsoft's official redistributable.
- Generated package artifacts remain ignored under `artifacts/private-alpha/`; source-controlled scripts, tests, docs, templates, and required licence/notice material reproduce the candidate.

## Observation and explicit diagnostic behavior

- The watcher enumerates every GSMTC session and keeps the raw `SourceAppUserModelId` visible. It subscribes to session-list, media-property, and playback-info changes.
- Track identity is the normalized title/artist/optional-album combination. Changed events refetch properties; identical identity is deduplicated.
- The explicit Milestone 1 control path requires exactly one `firefox.exe` session, explicit confirmation, and a Playing starting state. It calls `TryPauseAsync()` and `TryPlayAsync()` on the same stored GSMTC session object.
- Pause/resume cleanup is protected by `finally`, ignores cancellation long enough for at most three same-session resume attempts, and prints an obvious manual-resume warning if Playing cannot be confirmed.
- The explicit Milestone 2 path initializes local speech before control, requires title/artist, rechecks the same session and metadata after confirmation, applies a 20-second speech timeout, and owns same-session resume after any pause request begins.
- Windows fallback uses `Windows.Media.SpeechSynthesis.SpeechSynthesizer`; `Windows.Media.Playback.MediaPlayer` sends the stream to the default audio device with media-command integration disabled.

## Normal transition-aware runtime flow

1. Request GSMTC and require exactly one eligible, Playing session whose source identifier matches `firefox.exe` case-insensitively.
2. Observe title, artist, optional album, playback state, and genuine deduplicated track changes. The startup track does not count as a transition.
3. Choose a fresh inclusive random target of 2-5 genuine transitions. One transition before delivery, create one Track-A preparation opportunity.
4. Require Track A to be at least eight seconds old. Generate a local comment while Firefox keeps playing, validate it, and select the fixed title/artist fallback if AI is disabled, unavailable, timed out, or mechanically invalid.
5. Use `keep_alive: 0` for normal Ollama generation. Prepare Nicole audio on CPU, dispose the model, and hold only the prepared material in memory. Windows speech is the fallback provider.
6. When Track B is observed, require the prepared opportunity, same logical Firefox target, Playing state, current Track B, Silent Mode off, and a delivery start within the two-second window.
7. Re-enumerate Core Audio. Require exactly one active Firefox application session. A transient missing/inactive Firefox target receives only bounded deterministic retry inside the existing delivery window; zero or ambiguous candidates otherwise produce silence.
8. Capture that Firefox session's current scalar and mute state. Smoothly lower it over ten steps/600 ms to 25% of the captured scalar, speak over the still-playing music, and restore the exact captured values in protected cleanup.
9. Dispose prepared material and begin a fresh 2-5-transition interval. There is no catch-up queue.

Normal automatic delivery contains no GSMTC pause, play, or toggle command. `TryPauseAsync()` and `TryPlayAsync()` remain reachable only in the earlier explicit playback/announcement diagnostic paths.

## Firefox identity and Chrome isolation

- GSMTC matching uses the verified exact source identifier `firefox.exe`; zero or multiple candidates are unsafe.
- The accepted identity boundary is Firefox application-level media ownership, not a browser tab or page. Ordinary YouTube may become the observed session when it holds Firefox media ownership; Zoya does not inspect the page or distinguish it from YouTube Music.
- Windows may replace a GSMTC wrapper object during unrelated global session churn. A different wrapper is treated as the same logical Firefox session only when exactly one eligible Firefox candidate remains Playing with the same normalized title and artist.
- Harmless wrapper refreshes move observation to the new wrapper without cancelling generation, synthesis, prepared material, active speech, or the current schedule.
- Invalid, missing, ambiguous, or genuinely different Firefox candidates trigger protected cancellation before any reacquisition. A fresh interval begins only after safe recovery.
- Core Audio selection accepts only an active exact Firefox process match. Chrome and unrelated application sessions are never fallback targets, and endpoint/master volume is never changed.

## Comment generation and factual boundary

- The only trusted music facts supplied to the model are exact title and artist. The accepted M12D v3 pilot supplies recent lines only as abstract form measurements; their subject matter is absent.
- Album, genre, sound, lyrics, biography, history, and remembered model knowledge are not trusted facts.
- Prompt text permits personality, literal wordplay, and clearly imaginary reactions without presenting outside knowledge as fact.
- AI comments are mechanically checked for one paragraph, 15-40 words, model commentary, control text, and banned radio cliches. Mechanical checks do not prove factual grounding.
- Exact `FALLBACK`, errors, timeouts, invalid output, simulated failure, and `--no-ai` select the fixed title/artist announcement. The sentinel itself is never spoken.
- Normal generation unloads only the selected Ollama model. No API key or remote endpoint is supported.
- `HostPersonaConfiguration.cs` supplies `Zoya` and `Steve`; the embedded, source-editable template is `Prompts/Milestone4HostPersonaPrompt.txt`.

## Speech, restoration, and cancellation

- Nicole (`af_nicole`) is the accepted primary CPU voice; `af_heart` is the recorded runner-up. Windows TTS remains the fallback.
- Neural defaults are 24 kHz mono, normal speed (`1.0`), and a 20-second preparation timeout. Each preparation loads the FP32 CPU model, validates finite non-silent output, creates an in-memory 16-bit mono WAV, and disposes the model; no temporary audio file is created.
- Cancellation during CPU inference waits for the in-flight work to finish safely, discards its output, and completes before any delivery/control step.
- Automatic preparation completes before delivery. The current speech bound is 25 seconds.
- Once ducking starts, protected cleanup owns exact restoration for success, cancellation, speech failure, simulated post-duck failure, stale state discovered after duck, and unexpected exceptions.
- Ctrl+C participates in normal cancellation and waits for restoration before clean shutdown.
- Earlier explicit pause/resume diagnostics retain their separate same-GSMTC-session recovery and never serve as fallback for automatic delivery.

## Runtime state

- At most 10 recent tracks and 6 accepted AI comments are retained in memory for the current process.
- **S** toggles Silent Mode. Entering or leaving it invalidates pending work and starts safely from a fresh interval as appropriate.
- **C** clears bounded in-memory history without deleting diagnostic logs.
- State, prepared audio, and recent history are not persisted across application restarts.
- Duplicate media-property events do not count as track transitions.

## Current boundaries

The implementation has no browser scraping, cookies, account access, unofficial YouTube API, cloud AI/TTS, telemetry, tray icon, global media keys, endpoint/master-volume manipulation, persistent listening-history database, track-position tracking, duration tracking, installer, code signing, public release, clean-machine certification, or invited-tester validation. Milestone 8 is closed only for continued internal development; compatibility and third-party distribution claims remain deferred. The rejected M9A and diagnostic M9C candidates remain isolated; only the bounded M9B candidate is integrated, and it remains unaccepted runtime behavior after M9's conclusion. The accepted M10C runtime routes the existing one-comment path through M10A/M10B while retaining the M8 pipeline, targeting, ducking, and cleanup behavior. Accepted M11A supplies typed arbitration; accepted M11B adds one fixed reusable liner through the same protected overlay path. Accepted M12A remains a pure context assembler with no normal-runtime reference; accepted M12B remains its isolated approved consumer. Accepted M12D is the separate, default-off quick-reaction runtime pilot; it does not route through M12A/M12B laboratory types.

## Windows API basis

- GSMTC observation uses `GlobalSystemMediaTransportControlsSessionManager` and `GlobalSystemMediaTransportControlsSession`.
- Core Audio uses `IMMDeviceEnumerator::GetDefaultAudioEndpoint`, `IAudioSessionManager2::GetSessionEnumerator`, `IAudioSessionControl2::GetProcessId`, and `ISimpleAudioVolume`; endpoint/master-volume APIs are not called.
- Microsoft references: [IAudioSessionEnumerator](https://learn.microsoft.com/windows/win32/api/audiopolicy/nn-audiopolicy-iaudiosessionenumerator), [IAudioSessionControl2::GetProcessId](https://learn.microsoft.com/windows/win32/api/audiopolicy/nf-audiopolicy-iaudiosessioncontrol2-getprocessid), and [session volume controls](https://learn.microsoft.com/windows/win32/coreaudio/session-volume-controls).
- Neural performance evidence remains local in ignored `logs/neural-tts-diagnostic-20260808-001817-018.txt`.
