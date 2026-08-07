# Project state

## Current milestone

**Milestone 3 - local AI comment generation with Ollama**

Implementation is ready for manual testing but is **AWAITING MANUAL ACCEPTANCE**.

- Ollama `0.32.6` is installed locally and its loopback API is available.
- The selected model is `qwen3:4b-instruct`: 4.0B parameters, Q4_K_M quantization, 2,497,293,803 bytes on disk (about 2.50 GB decimal). It was downloaded only after Steve approved it.
- It was selected as the only installed model and as a small instruction-tuned option appropriate for one 15-40 word comment.
- Normal generation uses `http://localhost:11434/api/`, a 20-second timeout, non-streaming output, thinking disabled, and `keep_alive: 0`.
- The deterministic validator and fixed fallback are implemented. Generate-only, no-AI, simulated-AI-failure, and performance-measurement modes are separate explicit options.
- Actual comment quality, latency, VRAM use, audible speech, Firefox control, and selected-model unload after inference still require Steve's manual tests.

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

## Known issues and limits

- Windows or Firefox may omit album information. The probe displays `<not provided>` and does not treat that as an error.
- Windows may expose zero sessions when no compatible media is active. The probe reports that without crashing.
- The automated `--once` run under the non-interactive Codex sandbox account was denied access to the Windows media-session manager. Steve's manual test confirmed the normal signed-in desktop account works correctly.
- Real playback-control calls remain excluded from automated test runs. Steve's Milestone 1 manual acceptance supplied that evidence.
- PowerShell may cosmetically display Cyrillic metadata as question marks. The UTF-8 logs preserve the original Unicode, and the announcement sentence is built directly from the in-memory Unicode title and artist.
- The current en-US Microsoft David voice speaks the English parts of an announcement but may skip Cyrillic title and artist text. Multilingual TTS remains outside Milestone 3.
- Mechanical validation cannot prove factual grounding. Steve must review generate-only comments for unsupported facts, especially on well-known songs.
- No generation or VRAM measurement has been run automatically. The explicit `--measure-ai` test will report measured `/api/ps` values when Ollama exposes them, then unload only the selected model.

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
- The model prompt contains only title and artist JSON as song ground truth, treats those values as untrusted data, forbids lyrics and unsupported facts, and is stored in `Prompts/Milestone3CommentPrompt.txt`.
- AI output must be one paragraph and 15-40 words, with no obvious model commentary, malformed control output, or banned commercial-radio cliches. Rejected/error/timeout output uses the fixed announcement instead without retrying.
- `--no-ai` bypasses Ollama entirely. `--generate-only` performs no playback or speech operations. Normal spoken mode generates and unloads before confirmation or pause, rechecks the same session and track, then reuses Milestone 2's protected same-session pause/speech/resume sequence.
- The performance mode temporarily uses `keep_alive: 30s`, queries `/api/ps`, labels measured and unavailable fields, sends `keep_alive: 0` for the selected model only, and verifies that model is no longer loaded. It never unloads unrelated models.
- No global media keys, fallback session, browser automation, browser data access, cloud AI, API keys, online speech, volume control, GUI, persona prompting, or automatic radio behavior is included.
- Build tooling is project-local: Microsoft .NET SDK 8.0.423 and Microsoft's `Microsoft.Windows.SDK.NET.Ref` targeting pack. Neither requires administrator privileges.
