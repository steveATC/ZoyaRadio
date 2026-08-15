# Durable decisions

These decisions summarize accepted project rules and rationale already evidenced by the repository and manual milestones. New ideas do not become decisions until Steve explicitly approves them.

## Authority and delivery discipline

- **The local Git repository is authoritative.** Exported ChatGPT Project-source copies are snapshots and may become stale.
- **One milestone at a time.** Passing automated checks makes work ready for manual acceptance; Steve's required real-desktop evidence is what passes a milestone.
- **Preserve proven paths.** Read-only observation, explicit control diagnostics, normal automatic behavior, and failure diagnostics remain separate so a safer mode cannot silently gain broader behavior.

## Local-first privacy and cost

- Song metadata, generated comments, speech preparation, state, and logs stay local.
- The application does not inspect browser pages, URLs, cookies, history, credentials, tokens, or account data and does not use unofficial YouTube APIs.
- AI uses Ollama's HTTP API only at loopback hosts (`localhost`, `127.0.0.1`, or `::1`); there is no API key or cloud-provider configuration.
- Normal model use requests immediate unload with `keep_alive: 0`. `--no-ai` is a first-class zero-model path.
- No large model or dependency is added without size, purpose, source, memory implications, alternatives, and Steve's approval.

## Media identity and metadata

- The intended Windows media session is the verified exact, case-insensitive `firefox.exe` GSMTC source. Zero or multiple candidates are unsafe; the application stays silent instead of guessing.
- Title and artist supplied by Windows are trusted metadata. Album is optional, is usually unavailable, and is never guessed.
- Duplicate media-property events with unchanged normalized identity do not become duplicate transitions.
- A fresh GSMTC wrapper is continuity only when exactly one eligible Playing Firefox candidate still has the same normalized title and artist. Unsafe or genuinely different targets require protected cancellation before reacquisition.

## Deterministic facts versus creative AI

- Exact title and artist, plus explicitly derived exact-string observations, are the only song/artist facts given authority in local generation.
- Pretrained knowledge, remembered trivia, lyrics, genre, sound, biography, release information, geography, and cultural associations are unavailable as factual evidence.
- Zoya may use literal wordplay, subjective reactions, and clearly imaginary or absurd framing without presenting them as facts.
- The deterministic validator catches mechanical problems; it does not claim to prove factual grounding or personality quality.
- Exact `FALLBACK`, invalid output, timeouts, AI failure, or disabled AI select the fixed title/artist announcement. Silence or fallback is preferable to unsupported prose.

## Scheduling and silence

- Automatic comments use an inclusive random interval of 2-5 genuine track transitions. The startup track does not count.
- Track A must be at least eight seconds old before preparation.
- For transition-aware delivery, generation and speech synthesis happen one transition ahead while music keeps playing. Prepared Track-A material may begin only inside the two-second Track-A-to-Track-B delivery window.
- There is only one pending/prepared opportunity and no catch-up queue. Late, unfinished, stale, rapidly skipped, silent, ambiguous, or unsafe opportunities are discarded.
- **Silence is valid behavior.** Safety and timing refusal are successful containment, not a reason to target another session or interrupt later.

## Voice choice

- Steve accepted Kokoro `af_nicole` as Zoya's primary local CPU voice and selected `af_heart` as runner-up.
- Windows local TTS remains the supported fallback and explicit troubleshooting path.
- Voice naturalness is separate from language accuracy. Steve found the Cyrillic sample natural-sounding but did not claim verified Russian pronunciation.

## Playback and volume safety

- The original media watcher remains completely read only.
- Earlier playback-control and one-announcement tests may use `TryPauseAsync()`/`TryPlayAsync()` only on the explicitly selected same Firefox GSMTC session with protected recovery.
- Normal automatic M7.5B delivery never pauses or resumes playback. It uses only the selected Firefox Core Audio application session.
- Ducking is proportional: 25% of the scalar captured immediately before delivery, not an absolute 25% volume. Protected cleanup restores the exact captured scalar and mute state.
- Chrome, unrelated applications, replacement sessions, global media keys, blind toggles, endpoint/master volume, and YouTube Music's in-page volume are never recovery fallbacks or control targets.
- Cancellation, speech failure, post-duck failure, stale state, and exceptions must all pass through restoration ownership before shutdown.

## Accepted isolation boundary

- Core Audio isolation is at the Firefox application-session level on Steve's PC. Multiple audible Firefox tabs may duck together.
- This application-level boundary is accepted for Phase 1. Chrome and Windows master volume remaining untouched are mandatory; browser-tab isolation is not a current requirement.
- GSMTC ownership is also Firefox application-level, not page-level. Ordinary YouTube may become the tracked Firefox media session when it owns Firefox media; Zoya does not inspect pages or distinguish it from YouTube Music.

## State and logging

- Recent listening context is deliberately ephemeral and bounded to 10 tracks and 6 accepted AI comments.
- Clearing state affects only in-memory context; timestamped UTF-8 diagnostics remain local until Steve deletes them. Shared-runtime logs use `%LOCALAPPDATA%\Zoya Radio\logs`; explicit diagnostic logs retain their repository-local location.
- Logs may contain media source, title, artist, optional album, state, timing, selected local model/voice, targeting, duck/restoration, and readable errors. They do not contain browser URLs, credentials, tokens, cookies, or unrelated process command lines.

## Accepted control application

- M8A is accepted at implementation commit `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc` after Steve's August 11, 2026 desktop test and the associated 162-test automated result.
- WinForms is a thin same-process frontend over the accepted radio runtime. It does not own factual playback state, scheduling, targeting, generation, speech, or restoration policy.
- `RadioRuntimeController` owns one authoritative asynchronous Start/Stop lifecycle and cleanup path. Stop and window close await protected cleanup; stale status after shutdown cannot revive the UI.
- Immutable status/activity snapshots or events are the presentation boundary. Radio activity is read only and reports deterministic runtime work; harmless wrapper refreshes remain detailed-log-only.
- Existing console launchers remain diagnostic frontends to the same runtime rather than a separate implementation.

## Tooling and dependencies

- The accepted application includes the WinForms control frontend, preserved console diagnostics, and the self-contained `0.8.0-alpha.1` Windows x64 package/readiness implementation at `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- The Windows SDK APIs, loopback Ollama client, and KokoroSharp.CPU are the currently accepted implementation dependencies.
- Project-local SDK, package cache, models, generated audio, builds, and logs remain excluded from Git.

## Internal acceptance and deferred distribution validation

- Steve accepted M8B and closed Milestone 8 for continued internal development on August 11, 2026.
- Clean Windows 10 22H2, clean Windows 11, and invited private-alpha testing remain useful compatibility and distribution evidence but are deliberately deferred and do not block internal feature development.
- The accepted internal baseline must not be described as clean-machine certified, broadly compatible, publicly released, code-signed, SmartScreen-reputable, or validated for third-party distribution.
- A later tester failure may require a bounded compatibility or packaging repair; the absence of tester feedback is not evidence of compatibility.
- M9A was rejected for runtime consideration. M9C is closed as successful experimental evidence. Steve explicitly authorized the bounded M9B implementation on August 15, 2026; M9B is now an implemented candidate awaiting staged owner acceptance, not accepted runtime behavior.

## Approved Milestone 9A experimental boundaries

- M9A is a generate-only comparison experiment, not an accepted change to ordinary radio behavior.
- The experimental brief may use exact title/artist, bounded exact-string history facts, an explicit typed reason for speaking, recent accepted comment text, and shallow deterministic style fingerprints. It may not add inferred music, artist, listener, or session facts.
- The accepted runtime prompt/context/fallback remains unchanged. The structured candidate prompt and deterministic announcement renderer cannot enter normal composition without a separately authorized M9B contract.
- Metadata that resembles instructions remains inert serialized data. Creative freedom is limited to subjective reaction, literal wordplay, playful exaggeration, clearly imaginary framing, and accepted-persona teasing without unsupported claims.
- The explicit benchmark uses fixed synthetic fixtures, two blinded arms, loopback-only `qwen3:4b-instruct`, bounded requests, ignored local artifacts, and selected-model unload. It cannot request GSMTC, use Core Audio, control playback/volume, produce speech, or open the GUI.
- Mechanical validation and latency do not determine creative quality. Steve's blinded grounding, relevance, personality, phrasing, variety, concision, and preference review is the M9A acceptance evidence.
- M9A does not claim the known `Borrowed Umbrella` grounding defect is fixed and does not authorize M9B.
- Steve's completed blinded review rejected the StructuredCandidate for runtime consideration. Although it drew fewer rejections than the accepted baseline, neither arm reliably met the required qualitative and exact-metadata grounding standard. The accepted runtime prompt, context, validator, and fallback remain controlling; a further experiment or any M9B work requires a new bounded contract.

## Approved Milestone 9C experimental boundaries

- M9C is a generate-only diagnostic. It may use exact title/artist only with approved MusicBrainz and linked Wikidata endpoints, plus loopback Ollama.
- Any ambiguous, unavailable, rate-limited, malformed, conflicting, unsupported, or failed source chain produces no enrichment and safe `FALLBACK`; no source fact can force speech.
- Every supplied external fact must be concise structured data with a direct approved HTTPS source URL. Raw page content, browser data, history, listener data, prompts, and generated prose never leave the machine.
- The bounded ignored cache is diagnostic-only, easy to clear, and is not listening history or runtime state.
- M9C diagnostic code cannot enter normal radio composition directly. Only the separately approved M9B contract may authorize a bounded reimplementation after Steve gives an explicit coding instruction.
- Steve's completed M9C.6 review selects local `qwen3:14b-q4_K_M` with reviewed multi-fact context packs and grounded whole-comment synthesis as the direction for any future integration proposal.
- The model must synthesize the entire comment; deterministic fact-plus-separate-reaction concatenation is rejected as unnatural and repetitive.
- Training-augmented factual freedom is not selected: it provided no material qualitative benefit and reduced factual acceptability. Externally verifiable claims in the selected candidate remain bounded to reviewed supplied facts.
- A future integration validator must not require every comment to repeat the current artist or title, but must guard against meaning-changing exaggeration and unsupported sound, lyric, cultural, or biographical claims.
- M9C is accepted only as experimental design evidence. The accepted M8 runtime remains controlling until M9B is implemented, verified, and explicitly accepted.

## Approved Milestone 9B integration boundaries

- M9B is the approved name and bounded contract for the first grounded-context runtime integration. It is implemented as a candidate after Steve's explicit August 15 instruction, but remains unaccepted until its staged engineering and owner gates are complete.
- Live enrichment is restricted to exact MusicBrainz identity and linked structured Wikidata data. Arbitrary browsing, live Wikipedia/page retrieval, raw-page prompting, cloud AI, and unrestricted training-knowledge claims are prohibited.
- A small versioned local catalog of reviewed, paraphrased, source-attributed context facts is allowed. Its cited pages are provenance only and are not fetched at runtime.
- The runtime context cache may persist under bounded LocalAppData storage: at most 100 entries, 24-hour confirmed TTL, two-hour negative TTL, and Clear History deletion. It is not durable listening history.
- `qwen3:14b-q4_K_M` is an optional owner-installed local prerequisite. It is not bundled or automatically downloaded. If absent, disabled, late, or failed, the accepted M8 4B behavior remains eligible and unsafe opportunities remain silent.
- The 14B model writes the whole comment from supplied metadata/context. It may paraphrase and synthesize but may not introduce externally verifiable claims from training knowledge.
- Omission of artist/title, questions, clichés, mild verbosity, imagined-scene phrasing, fact recitation, and formulaic construction are advisory quality signals unless they create another violation. They are not automatic hard rejections.
- Unsupported facts, new unsupported entities/numbers/dates, invented biography, stale/mismatched context, and material exaggeration or changed causality are hard discard conditions.
- M9B permits at most one bounded quality regeneration, six ephemeral enriched-comment fingerprints, and no catch-up or unbounded retry.
- All accepted M8 targeting, scheduling, cancellation, Silent Mode, speech, Firefox-only ducking, exact restoration, shutdown, local-first, and safe-silence invariants remain controlling.
- Steve accepted the recorded M9B Gate 2 generate-only review on August 15, 2026 with a narrow same-card repetition exception. The exception recognizes that three outputs from each identical song/card amplified repeated templates; it does not relax grounding, hard-discard, repetition-state, fallback, silence, silent-desktop, audible-desktop, or final-acceptance requirements.
- M9B must pass fake-driven engineering checks, a familiar-track generate-only review, a silent desktop preparation trial, a bounded audible trial, and explicit Steve acceptance. No intermediate gate alone makes it accepted runtime behavior.

## Approved Milestone 8 boundaries

- **The staged M8 authorizations were satisfied.** M8A and M8B were separately authorized and owner-accepted. The dated sequencing amendment closes M8 for internal development while preserving the original distribution-evidence requirements as deferred.
- **M8A uses WinForms and same-process hosting.** The accepted control application uses one UI-independent runtime controller or facade and one authoritative asynchronous lifecycle and cleanup path. Existing console diagnostics remain frontends to the same accepted runtime.
- **M8 does not redesign radio behavior.** Existing scheduling, transition timing, safe-silence decisions, Firefox-only targeting, exact restoration, local-first privacy, and bounded ephemeral state remain controlling.
- **M8B's implemented package is portable.** The internal baseline is a versioned, unsigned, self-contained Windows x64 ZIP with application-relative assets, LocalAppData logs, hashes, a manifest, notices, and manual side-by-side replacement/rollback. Clean-machine evidence remains deferred before broader distribution claims.
- **Firefox and Ollama remain separate.** Neither installer is bundled. Zoya may direct a tester to the official Ollama source, and may start an installed Ollama application only after explicit tester action.
- **Qwen setup requires informed consent.** Invoking `ollama pull qwen3:4b-instruct` requires size disclosure, a free-space check, explicit consent, visible progress, and cancellation. Third-party terms are never accepted automatically.
- **eSpeak exclusion remains a distribution gate.** The accepted internal Nicole/en-US package contains no eSpeak and passed representative owner-machine tests. Clean-machine validation remains deferred; if it reveals that eSpeak is necessary, broader distribution pauses or uses an explicitly approved fallback/compliance solution.
- **Windows TTS is a deliberate fallback route.** It omits the Kokoro model, Nicole voice, ONNX native closure, and eSpeak; it must be disclosed as a different voice experience and meet the same runtime-safety and clean-machine requirements.
- **M8 excludes broader distribution machinery.** No traditional installer, Store package, bootstrapper, automatic third-party installation, code signing, public release, automatic updater, tray/background service, or general future-scale packaging framework is approved.

The complete scope, non-goals, gates, evidence requirements, and unresolved risks are governed by [MILESTONE_8_CONTRACT.md](MILESTONE_8_CONTRACT.md).
