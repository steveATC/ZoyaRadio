# Project state

## Current milestone

**Milestone 7.5A - isolated Firefox per-session audio ducking**

Implementation is **READY FOR STEVE'S MANUAL ACCEPTANCE**, but neither Milestone 7.5A nor the complete Milestone 7.5 transition is passed yet.

- Milestone 6 remains deliberately deferred. Milestone 7 passed Steve's manual acceptance. Milestone 7.5 is the current Phase-1 gate; Milestone 8 follows it and is the Phase-1 usable-MVP completion gate.
- The dedicated `run-audio-ducking-test.cmd` probe is not integrated into automatic radio behavior. It performs no AI generation, speech, media pause, or media resume.
- The implementation uses Windows Core Audio `IAudioSessionManager2`, `IAudioSessionEnumerator`, `IAudioSessionControl2`, and `ISimpleAudioVolume` directly. No new dependency was added.
- It enumerates sessions on the default multimedia render endpoint, logs a short hash tag instead of raw session/device identifiers, and reads process ID, process name, activity, per-session scalar, and mute state.
- Only active sessions whose process name is exactly `firefox` or `firefox.exe`, case-insensitively, are candidates. Zero candidates cause a safe refusal; multiple active candidates are explicitly ambiguous and cause a safe refusal.
- Exactly one candidate still requires an explicit **Y**. Read-only enumeration, Enter/default NO, muted Firefox, volume below 5%, an inactive/expired session, or unreadable volume state issues no volume command.
- The fixed diagnostic target is 25% of the exact captured starting scalar, not 25% absolute. A 60% starting value therefore ducks to 15% and restores to exactly 60%.
- Ducking and restoration use ten 60 ms scalar steps. The audible hold is about three seconds. The captured mute state is restored as well.
- Protected cleanup retains the original session object and attempts exact restoration after normal completion, Ctrl+C, a simulated failure, or another exception. It never re-enumerates or redirects restoration to another session.
- If the original session disappears, the probe warns Steve to restore Firefox manually and does not guess at a replacement.
- A safe live `--enumerate-only` run found three sessions on Steve's default output: one active `firefox.exe` candidate at 100% and unmuted, inactive Steam at 17.7%, and one inactive system/unidentified session. It issued no volume command.
- Automated verification currently reports 91 passed and 0 failed. No automated run changed real volume or playback.
- M7.5B transition integration and Milestone 8 were not started.

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

**Milestone 5 - PASSED manual acceptance on Steve's Windows PC.**

- Automatic comments use an inclusive random interval of 2-5 genuine deduplicated Firefox track transitions; the startup track does not count.
- AI generation occurs while music remains Playing. The eight-second age guard and final same-session, same-track, and Playing recheck precede a targeted pause.
- Silent Mode, clearing bounded in-memory history, cancellation, speech timeout, and shutdown preserve the tested same-session recovery behavior.
- `--no-ai` keeps fixed-template automatic behavior without contacting Ollama. No persistent listening history, genre inference, GUI, or volume control was added.

**Milestone 7 - PASSED manual acceptance on Steve's Windows PC.**

- Steve selected Kokoro `af_nicole` as Zoya's primary local voice and `af_heart` as runner-up.
- Steve reported Milestone 7's manual acceptance passed with Nicole as the accepted primary local voice and Windows TTS retained as fallback.
- Steve described the Cyrillic sample as natural-sounding but did not claim verified Russian pronunciation.
- The application keeps Windows TTS as fallback, prepares neural audio while Firefox plays, disposes the CPU model before pause, and retains protected same-session resume behavior.

## Known issues and limits

- Windows or Firefox may omit album information. The probe displays `<not provided>` and does not treat that as an error.
- Windows may expose zero sessions when no compatible media is active. The probe reports that without crashing.
- The automated `--once` run under the non-interactive Codex sandbox account was denied access to the Windows media-session manager. Steve's manual test confirmed the normal signed-in desktop account works correctly.
- Real playback-control calls remain excluded from automated test runs. Steve's Milestone 1 manual acceptance supplied that evidence.
- PowerShell may cosmetically display Cyrillic metadata as question marks. The UTF-8 logs preserve the original Unicode, and the announcement sentence is built directly from the in-memory Unicode title and artist.
- The fallback en-US Microsoft David voice may skip Cyrillic title and artist text. Multilingual transliteration remains outside Milestone 7.
- Kokoro's documented language list does not include Russian. The Milestone 7 Cyrillic sample preserves the characters, but its audible pronunciation or omissions are a manual observation rather than claimed language support.
- Milestone 7 passed Steve's manual acceptance. Russian pronunciation accuracy remains unverified.
- Repeated CPU preparation disposes the ONNX model each time, but the long-running .NET/native process retained roughly 333 MiB working set after three preparations. No GPU provider is loaded.
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
- The known local Ollama model, endpoint, 20-second generation timeout, `keep_alive: 0`, 15-40-word validation, exact `FALLBACK`, and same-session recovery mechanisms remain unchanged. Windows TTS is retained as the speech fallback.
- Milestone 7 keeps the audition in `src/ZoyaRadio.VoiceAudition` and adds a modular `IAnnouncementSpeechService` boundary to the radio project. `KokoroSharp.CPU` 0.8.0 is preferred; Windows TTS is retained as fallback.
- `NeuralSpeechConfiguration.Default` selects `af_nicole`, 24 kHz mono output, normal speed, a 20-second preparation timeout, and the repository-local model. Changing the voice later is a single readable configuration edit.
- Each neural preparation explicitly loads the FP32 model from disk, performs CPU inference, validates nonempty finite audio, encodes a WAV in memory, and disposes the model before any pause request. Cancellation waits for in-flight CPU inference to finish safely and discards the result.
- `--windows-tts` bypasses neural initialization. `--simulate-neural-tts-failure` safely exercises Windows fallback, and `--measure-neural-tts` prepares three comments without sound or media control.
- Automatic radio ordering is comment generation, Ollama unload, neural preparation, minimum-age wait if needed, final safety recheck, targeted pause, prepared playback, and protected same-session resume.
- The model is stored at ignored `.tools/kokoro/kokoro.onnx`: 325,532,387 bytes (310.5 MiB), SHA-256 `7D5DF8ECF7D4B1878015A32686053FD0EEBE2BC377234608764CC0EF3636A6C5`.
- The restored package closure occupies about 559.69 MiB in the ignored NuGet cache; the model, package cache, and one built application output require about 1.20 GiB, with additional ignored audition build output if both projects are built.
- KokoroSharp is MIT licensed; the Kokoro model and voices are Apache-2.0; the bundled eSpeak NG phonemization component is GPLv3. Distribution remains a later review, not part of this private audition.
- `.tools/`, `.packages/`, `bin/`, `obj/`, and `artifacts/` were already excluded from Git, so the model, native runtime, generated WAV files, and reports cannot be committed accidentally.
- The performance evidence is in ignored `logs/neural-tts-diagnostic-20260808-001817-018.txt`.
- Build tooling is project-local: Microsoft .NET SDK 8.0.423 and Microsoft's `Microsoft.Windows.SDK.NET.Ref` targeting pack. Neither requires administrator privileges.
- Milestone 7.5A follows Microsoft's documented Core Audio route: `IMMDeviceEnumerator::GetDefaultAudioEndpoint` for the default multimedia render device, `IAudioSessionManager2::GetSessionEnumerator` to list its sessions, `IAudioSessionControl2::GetProcessId` for process association, and `ISimpleAudioVolume` for that session's scalar/mute only. It never calls endpoint/master-volume APIs.
- Microsoft documentation used for the decision: `https://learn.microsoft.com/windows/win32/api/audiopolicy/nn-audiopolicy-iaudiosessionenumerator`, `https://learn.microsoft.com/windows/win32/api/audiopolicy/nf-audiopolicy-iaudiosessioncontrol2-getprocessid`, and `https://learn.microsoft.com/windows/win32/coreaudio/session-volume-controls`.
