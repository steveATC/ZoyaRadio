# Project state

## Current milestone

**Milestone 5 - radio behavior and minimal listening state**

Implementation is ready for Steve's manual automatic-behavior test but is **AWAITING MANUAL ACCEPTANCE**.

- `run-radio-behavior-test.cmd` is a separate explicit launcher. It asks once and defaults to not armed.
- A testable random source chooses an inclusive 2-5 transition target. The startup track does not count; deduplicated title/artist changes count once.
- AI generation and model unloading happen while music remains Playing. An eight-second minimum current-track age and a final same-session/track/Playing recheck precede every automatic pause.
- Track or session changes, Silent Mode, clearing history, cancellation, and shutdown invalidate pending work. Missed comments are discarded rather than queued.
- Only one comment pipeline may run. Automatic speech is limited to 25 seconds and reuses protected same-session recovery.
- **S** toggles Silent Mode; leaving Silent Mode resets scheduling. **C** clears bounded behavioral history without deleting diagnostic logs.
- Recent history is in memory only: 10 exact title/artist tracks and 6 accepted AI comments. It is discarded on process exit.
- Exact artist repetition uses trimmed/collapsed, case-insensitive string comparison only. No fuzzy artist matching or preference inference is performed.
- Recent comments are compact prompt context for wording variety. Exact normalized duplicates use the fixed fallback without a regeneration loop.
- `--no-ai` preserves automatic scheduling and fixed pause/speech/resume behavior while making no Ollama request.
- Genre/style-change detection is intentionally deferred because M5 has no trusted genre or audio-analysis data.

## Completed milestones

**Milestone 0 - PASSED manual acceptance on Steve's Windows PC.**

- Firefox exposed the intended session as `firefox.exe`.
- YouTube Music title, artist, Playing/Paused state, and live track changes were detected correctly.
- Album was consistently unavailable and remains optional; it must never be guessed.
- Repeated unchanged `MediaPropertiesChanged` events did not create duplicate `TRACK CHANGED` reports.
- The read-only probe shut down cleanly and issued no playback controls.

**Milestone 1 - PASSED manual acceptance on Steve's Windows PC.**

- The tester targeted exactly one `firefox.exe` session, even while an unrelated `chrome.exe` session was present.
- At least five normal targeted pause/resume tests passed using `TryPauseAsync()` and `TryPlayAsync()` on the same Firefox session.
- Confirmation refusal and the already-Paused starting-state check issued no playback commands.
- Ctrl+C after pausing used protected cleanup and restored the same Firefox session to Playing.

**Milestone 2 - PASSED manual acceptance on Steve's Windows PC.**

- Microsoft David, Zira, and Mark were discovered as enabled en-US Windows voices; the prototype selected Microsoft David.
- Three normal spoken-announcement tests were fully audible and restored Firefox playback reliably.
- Simulated TTS-failure recovery and Ctrl+C-during-speech recovery both passed.
- Unicode metadata remained correct through the application and UTF-8 logs.

**Milestone 3 - PASSED manual acceptance on Steve's Windows PC.**

- Ollama `0.32.6` is installed locally and its loopback API is available.
- The selected model is `qwen3:4b-instruct`: 4.0B parameters, Q4_K_M quantization, 2,497,293,803 bytes on disk (about 2.50 GB decimal). It was downloaded only after Steve approved it.
- It was selected as the only installed model and as a small instruction-tuned option appropriate for one 15-40 word comment.
- Normal generation uses `http://localhost:11434/api/`, a 20-second timeout, non-streaming output, thinking disabled, and `keep_alive: 0`.
- The deterministic validator and fixed fallback are implemented. Generate-only, no-AI, simulated-AI-failure, and performance-measurement modes are separate explicit options.
- Manual review verified local generation, generate-before-pause ordering, targeted speech/playback recovery, timeout fallback, no-AI mode, simulated failure, performance measurement, and model unload.
- The first grounding review found unsupported claims; the tightened literal-metadata prompt, exact safe `FALLBACK` contract, and follow-up manual retest passed.

**Milestone 4 - PASSED manual acceptance on Steve's Windows PC.**

- The host is `Zoya` and the listener is `Steve`, configured in `HostPersonaConfiguration.cs`.
- The editable persona lives in `Prompts/Milestone4HostPersonaPrompt.txt`.
- Manual personality review accepted Zoya's warm, dry, clever, conversational style and the preserved title/artist-only grounding contract.
- The known repeated `Borrowed Umbrella` invention on unrelated Cyrillic metadata is a model/prompt grounding defect, not Unicode corruption. Steve accepted the current persona and deferred that tuning.
- Ollama sampling remained at temperature `0.7`; no model, voice, playback-control, GUI, scheduling, history, or retrieval change was part of Milestone 4.

## Known issues and limits

- Windows or Firefox may omit album information. The probe displays `<not provided>` and does not treat that as an error.
- Windows may expose zero sessions when no compatible media is active. The probe reports that without crashing.
- The automated `--once` run under the non-interactive Codex sandbox account was denied access to the Windows media-session manager. Steve's manual test confirmed the normal signed-in desktop account works correctly.
- Real playback-control calls remain excluded from automated test runs. Steve's Milestone 1 manual acceptance supplied that evidence.
- PowerShell may cosmetically display Cyrillic metadata as question marks. The UTF-8 logs preserve the original Unicode, and the announcement sentence is built directly from the in-memory Unicode title and artist.
- The current en-US Microsoft David voice speaks the English parts of an announcement but may skip Cyrillic title and artist text. Multilingual TTS remains outside Milestone 5.
- Mechanical validation cannot prove factual grounding or personality quality; those remain manual review concerns for AI-generated prose.
- Milestone 5 supplies up to six recent accepted AI comments for repetition avoidance, but it intentionally performs no semantic-similarity analysis or regeneration loop.
- Manual Milestone 4 testing found that `qwen3:4b-instruct` sometimes invented `Borrowed Umbrella` for unrelated Cyrillic metadata. This is a known model/prompt grounding defect, not Unicode corruption, and Steve deferred it rather than adding a brittle blacklist.
- Natural track endings and manual skips are indistinguishable with the current metadata. M5 calls both a track transition and never claims the previous track finished naturally.
- Dramatic genre/style-change detection is intentionally deferred; inferring it from title, artist name, or model memory would violate grounding.
- Steve's performance run measured 4.262 seconds wall-clock, 2.191 seconds Ollama total duration, 1.999 seconds model load, 2.68 GiB loaded size/VRAM, and context length 2048. The selected-model unload verification passed. These are measurements from that run, not estimates.

## Important decisions and evidence

- The probe is a C#/.NET 8 console application using only `GlobalSystemMediaTransportControlsSessionManager` and `GlobalSystemMediaTransportControlsSession` for runtime media observation.
- It targets Windows 10 and uses the supported Windows Runtime API introduced in Windows 10 version 1809.
- It enumerates all sessions and keeps the raw `SourceAppUserModelId` visible. Firefox recognition is only a case-insensitive hint based on the raw identifier returned by Windows.
- It subscribes to session-list, media-property, and playback-info changes. Track identity is exactly the normalized title/artist/album combination, which prevents duplicate `TRACK CHANGED` messages for an identical state.
- The original `run-media-probe.cmd` path remains read only. Playback control is reachable only through the explicit `--control-test` mode used by `run-playback-control-test.cmd`.
- Milestone 1 matches exactly one `firefox.exe` session case-insensitively, requires an explicit confirmation and a Playing starting state, then calls `TryPauseAsync()` and `TryPlayAsync()` on that same stored session object.
- Resume cleanup is protected by `finally`, ignores cancellation long enough to make at most three same-session resume attempts, and prints a prominent manual-resume warning if Playing cannot be confirmed.
- Milestone 2 uses Microsoft's local `Windows.Media.SpeechSynthesis.SpeechSynthesizer` with its default usable voice. `Windows.Media.Playback.MediaPlayer` sends the synthesized stream to the normal/default Windows audio device and has its media-command integration disabled.
- Milestone 2 validates speech and metadata before control, rechecks the same session after confirmation, uses a 20-second speech timeout, and owns same-session resume in protected `finally` cleanup after a pause request begins.
- The explicit simulated-failure mode skips audible speech after Paused is verified and exercises the same real resume/retry path used by genuine speech failures and cancellation.
- No new package or dependency was added for speech; the APIs are already in the existing Microsoft Windows SDK reference pack and Windows itself.
- Milestone 3 calls Ollama directly through .NET `HttpClient`; no Ollama SDK or other NuGet dependency was added. The endpoint policy permits only HTTP loopback hosts (`localhost`, `127.0.0.1`, or `::1`) at `/api`.
- The model prompt contains only title and artist JSON as song ground truth, treats those values as opaque untrusted labels, explicitly discards remembered knowledge, and forbids lyrics and unsupported facts.
- AI output must be one paragraph and 15-40 words, with no obvious model commentary, malformed control output, or banned commercial-radio cliches. The separate exact token `FALLBACK` requests the fixed announcement and is successful safe behavior; it is never sent to speech. Rejected/error/timeout output also uses the fixed announcement without retrying.
- `--no-ai` bypasses Ollama entirely. `--generate-only` performs no playback or speech operations. Normal spoken mode generates and unloads before confirmation or pause, rechecks the same session and track, then reuses Milestone 2's protected same-session pause/speech/resume sequence.
- The performance mode temporarily uses `keep_alive: 30s`, queries `/api/ps`, labels measured and unavailable fields, sends `keep_alive: 0` for the selected model only, and verifies that model is no longer loaded. It never unloads unrelated models.
- Milestone 4 renders the host and listener names from `HostPersonaConfiguration.cs` into the embedded `Prompts/Milestone4HostPersonaPrompt.txt` template. This is deliberately small and source-editable.
- The persona prompt permits title/name wordplay, subjective reactions, clearly framed imaginary scenes, affectionate teasing, dry humor, and occasional mild swearing while preserving the Milestone 3 factual boundary.
- Temperature remains the existing `0.7`; `stream: false`, `think: false`, the 20-second timeout, and normal `keep_alive: 0` are unchanged.
- Milestone 5 adds only bounded automatic transition scheduling and ephemeral recent state. It adds no global media keys, fallback session, browser automation, browser data access, cloud AI, API keys, online speech, volume control, GUI, Five Queens content, persistent history, retrieval, or factual song research.
- `RadioBehaviorConfiguration.cs` contains validated, readable defaults: 2-5 transitions, 8-second minimum track age, 25-second speech limit, 10 recent tracks, and 6 recent accepted AI comments.
- `RadioBehaviorState` performs deterministic deduplication, scheduling, silent/reset behavior, and bounded-history updates. `RadioOpportunityPipeline` serializes generation, age waiting, revalidation, speech, and recovery.
- Automatic prompt context contains previous/current title and artist, explicitly derived exact-string observations, and recent accepted comments. No album, genre, audio description, biography, or remembered trivia is added.
- If the intended track changes during generation or age waiting, the cancellation token abandons that interruption and the state chooses a fresh future interval. There is no missed-comment queue.
- The known local Ollama model, endpoint, 20-second generation timeout, `keep_alive: 0`, 15-40-word validation, exact `FALLBACK`, Windows voice, and same-session recovery mechanisms remain unchanged.
- Build tooling is project-local: Microsoft .NET SDK 8.0.423 and Microsoft's `Microsoft.Windows.SDK.NET.Ref` targeting pack. Neither requires administrator privileges.
