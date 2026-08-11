# Current architecture

This document describes only the implementation present at accepted commit `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc`. Future ideas and research recommendations are excluded.

## Platform and local dependencies

- C#/.NET 8 Windows applications targeting `net8.0-windows10.0.19041.0`: an accepted WinForms control frontend plus preserved console diagnostics. The supported Windows API baseline is Windows 10 version 1809 (`10.0.17763.0`).
- Windows GSMTC through `Windows.Media.Control` for media-session discovery, metadata, playback state, and event observation.
- Windows Core Audio session APIs for per-application Firefox volume capture, proportional ducking, and exact restoration.
- Ollama's loopback HTTP API at `http://localhost:11434/api/` with `qwen3:4b-instruct` for short local comments.
- KokoroSharp.CPU 0.8.0 with the local Kokoro model and `af_nicole` as the preferred voice.
- Windows `SpeechSynthesizer`/`MediaPlayer` as the local speech fallback.
- Timestamped UTF-8 shared-runtime logs under `%LOCALAPPDATA%\Zoya Radio\logs`; explicit diagnostic logs remain under the ignored repository `logs/` folder. There is no telemetry or cloud provider.

The project-local Microsoft .NET SDK 8.0.423 is in ignored `.dotnet/`. Microsoft's Windows SDK reference pack and other restored packages are in ignored `.packages/`, and the approved Kokoro model is in ignored `.tools/kokoro/`. None requires administrator privileges.

## Accepted versions and local artifacts

- Ollama `0.32.6` and `qwen3:4b-instruct`: 4.0B parameters, Q4_K_M quantization, 2,497,293,803 bytes on disk (about 2.50 GB decimal). It was the only installed model and was downloaded after Steve's approval.
- Normal Ollama requests use .NET `HttpClient`, `stream: false`, `think: false`, temperature `0.7`, a 20-second generation timeout, and `keep_alive: 0`. Performance mode alone uses a temporary `30s` keep-alive and `/api/ps`, then unloads only the selected model.
- `KokoroSharp.CPU` 0.8.0 with Microsoft ONNX Runtime 1.22.0. The FP32 model is `.tools/kokoro/kokoro.onnx`, 325,532,387 bytes (310.5 MiB), SHA-256 `7D5DF8ECF7D4B1878015A32686053FD0EEBE2BC377234608764CC0EF3636A6C5`.
- The restored package closure is about 559.69 MiB. The model, package cache, and one built application output require about 1.20 GiB, excluding additional ignored audition build output.
- KokoroSharp is MIT licensed; the Kokoro model and voices are Apache-2.0; the bundled eSpeak NG phonemization component is GPLv3. Distribution review remains outside the current private implementation.
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
| `run-radio-behavior-test.cmd` | Arm transition-aware automatic preparation and ducked delivery. |
| `run-audio-ducking-test.cmd` | Enumerate or explicitly test isolated Firefox Core Audio duck/restoration. |
| `run-neural-tts-diagnostic.cmd` and related launchers | Safely exercise Nicole preparation, fallback, and resource diagnostics. |
| `run-voice-audition.cmd` | Produce local voice-audition material. |

## Component responsibilities

| Component | Responsibility |
| --- | --- |
| `RadioRuntimeController` | Own the authoritative asynchronous Start/Stop lifecycle, immutable status snapshots, stale-update suppression, and protected cleanup boundary shared by frontends. |
| `RadioControlForm` | Present controls and deterministic read-only runtime state; it does not infer playback facts or own radio behavior. |
| `MediaSessionWatcher` and `SessionObserver` | Request GSMTC, enumerate sessions, subscribe to session/media/playback changes, and refetch changed metadata. |
| `MediaSnapshot` | Hold title, artist, and optional album without inventing absent fields. |
| `RadioSessionAccess` | Read and validate the selected Firefox session and its current metadata/playback state. |
| `FirefoxSessionLifecycle` | Classify a stable session, a harmless wrapper refresh, an unsafe target state, or a genuine logical replacement. |
| `RadioBehaviorState` and `RadioHistory` | Deduplicate transitions, choose the 2-5-transition schedule, track one opportunity, and retain bounded in-memory history. |
| `TransitionRadioPipeline` | Separate Track-A preparation from Track-B delivery and abandon stale, late, or unsafe work. |
| `AiCommentService`, `OllamaClient`, `CommentPrompt`, and `CommentValidator` | Build title/artist-only prompts, call loopback Ollama, validate mechanical output rules, choose fixed fallback, and unload the model. |
| `KokoroSpeechService`, `WindowsSpeechService`, and `SpeechServiceSelection` | Prepare Nicole audio locally and select Windows speech fallback when required. |
| `WindowsAudioSessionCatalog` and `AudioSessionSelector` | Enumerate the default multimedia render endpoint and select exactly one active Firefox application session. |
| `WindowsTransitionAudioDucker` and `AudioDuckingSequence` | Revalidate the delivery-time audio target, duck only that session, run prepared speech, and restore the exact captured state in protected cleanup. |
| `LocalLog` and `LocalAppDataLogFolderProvider` | Write readable timestamped UTF-8 diagnostics and resolve the accepted shared-runtime log folder under LocalAppData. |

## Control application and lifecycle

- `ZoyaRadio.ControlApp` is a same-process WinForms frontend over the accepted automatic-radio runtime. It does not contain a second scheduling, targeting, speech, or ducking implementation.
- `RadioRuntimeController` exposes one authoritative asynchronous lifecycle. Start is idempotent, Stop is safely awaited, window close waits for the same cleanup path, and stale callbacks from an ended generation are suppressed.
- The Form remains thin. It exposes Start, Stop, Silent Mode, Clear History, Open Logs Folder, title, artist, Firefox target status, ordinary status, and read-only Radio activity derived from immutable runtime snapshots/events.
- Radio activity reports deterministic work such as the transition countdown, stability wait, local generation, Nicole preparation, prepared waiting, target/audio validation, ducking, speech, restoration, safe abandonment, target loss, stopping, and faults. Harmless Firefox wrapper refreshes remain detailed-log-only rather than churning the GUI state.
- Stop, window close, cancellation, and faults retain the accepted restoration ownership. The frontend cannot close past protected cleanup or retarget another media/audio session.
- Shared-runtime logs are written under `%LOCALAPPDATA%\Zoya Radio\logs`, and the button opens that folder. Existing console launchers remain usable diagnostic frontends to the same runtime.

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

- The only trusted music facts supplied to the model are the exact title and artist plus deterministic recent exact-string context.
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

The implementation has no browser scraping, cookies, account access, unofficial YouTube API, cloud AI/TTS, telemetry, tray icon, global media keys, endpoint/master-volume manipulation, persistent listening-history database, track-position tracking, duration tracking, installer, publish/package output, clean-machine distribution evidence, or M8B behavior. Milestone 8 as a whole is not passed.

## Windows API basis

- GSMTC observation uses `GlobalSystemMediaTransportControlsSessionManager` and `GlobalSystemMediaTransportControlsSession`.
- Core Audio uses `IMMDeviceEnumerator::GetDefaultAudioEndpoint`, `IAudioSessionManager2::GetSessionEnumerator`, `IAudioSessionControl2::GetProcessId`, and `ISimpleAudioVolume`; endpoint/master-volume APIs are not called.
- Microsoft references: [IAudioSessionEnumerator](https://learn.microsoft.com/windows/win32/api/audiopolicy/nn-audiopolicy-iaudiosessionenumerator), [IAudioSessionControl2::GetProcessId](https://learn.microsoft.com/windows/win32/api/audiopolicy/nf-audiopolicy-iaudiosessioncontrol2-getprocessid), and [session volume controls](https://learn.microsoft.com/windows/win32/coreaudio/session-volume-controls).
- Neural performance evidence remains local in ignored `logs/neural-tts-diagnostic-20260808-001817-018.txt`.
