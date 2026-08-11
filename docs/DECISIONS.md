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

- The accepted application now includes the small .NET WinForms control frontend and the preserved console diagnostics. M8B private-alpha packaging remains unstarted and requires a separate explicit implementation request.
- The Windows SDK APIs, loopback Ollama client, and KokoroSharp.CPU are the currently accepted implementation dependencies.
- Project-local SDK, package cache, models, generated audio, builds, and logs remain excluded from Git.

## Approved Milestone 8 boundaries

- **Staged authorization is mandatory.** M8A fulfilled its separately authorized gate and is accepted. M8B remains unstarted and cannot begin without its own explicit implementation request. M8 as a whole is not passed.
- **M8A uses WinForms and same-process hosting.** The accepted control application uses one UI-independent runtime controller or facade and one authoritative asynchronous lifecycle and cleanup path. Existing console diagnostics remain frontends to the same accepted runtime.
- **M8 does not redesign radio behavior.** Existing scheduling, transition timing, safe-silence decisions, Firefox-only targeting, exact restoration, local-first privacy, and bounded ephemeral state remain controlling.
- **M8B's contracted primary distribution is portable.** If separately authorized, the approved route is a versioned, unsigned, self-contained Windows x64 ZIP for private-alpha testing, with application-relative assets, LocalAppData logs, hashes, a manifest, notices, clean-machine evidence, and manual side-by-side replacement and rollback.
- **Firefox and Ollama remain separate.** Neither installer is bundled. Zoya may direct a tester to the official Ollama source, and may start an installed Ollama application only after explicit tester action.
- **Qwen setup requires informed consent.** Invoking `ollama pull qwen3:4b-instruct` requires size disclosure, a free-space check, explicit consent, visible progress, and cancellation. Third-party terms are never accepted automatically.
- **eSpeak exclusion is a release gate.** The Nicole/en-US candidate must exclude eSpeak and pass representative and clean-machine tests. If eSpeak proves necessary, distribution pauses or uses the explicitly described Windows-TTS-only fallback until an approved compliance solution exists.
- **Windows TTS is a deliberate fallback route.** It omits the Kokoro model, Nicole voice, ONNX native closure, and eSpeak; it must be disclosed as a different voice experience and meet the same runtime-safety and clean-machine requirements.
- **M8 excludes broader distribution machinery.** No traditional installer, Store package, bootstrapper, automatic third-party installation, code signing, public release, automatic updater, tray/background service, or general future-scale packaging framework is approved.

The complete scope, non-goals, gates, evidence requirements, and unresolved risks are governed by [MILESTONE_8_CONTRACT.md](MILESTONE_8_CONTRACT.md).
