# Project state

## Current milestone

**Milestone 2 - basic Windows spoken announcement**

Implementation status: ready for Steve's manual acceptance test.

**Milestone 2 is NOT PASSED.** Audible Windows speech and real Firefox recovery require Steve's normal interactive desktop test.

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

## Known issues and limits

- Windows or Firefox may omit album information. The probe displays `<not provided>` and does not treat that as an error.
- Windows may expose zero sessions when no compatible media is active. The probe reports that without crashing.
- The automated `--once` run under the non-interactive Codex sandbox account was denied access to the Windows media-session manager. Steve's manual test confirmed the normal signed-in desktop account works correctly.
- Real playback-control calls remain excluded from automated test runs. Steve's Milestone 1 manual acceptance supplied that evidence.
- The silent Windows speech check in Codex's non-interactive execution context returned `Internal Speech Error` before WinRT voice enumeration. System registration shows Microsoft David, Mark, and Zira OneCore voices, plus David Desktop and Zira Desktop. The Milestone 2 test safely refuses control unless the speech API works and selects a usable default voice in Steve's normal desktop session.
- PowerShell may cosmetically display Cyrillic metadata as question marks. The UTF-8 logs preserve the original Unicode, and the announcement sentence is built directly from the in-memory Unicode title and artist.

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
- No global media keys, fallback session, browser automation, browser data access, AI, online speech, volume control, GUI, or runtime networking is included.
- Build tooling is project-local: Microsoft .NET SDK 8.0.423 and Microsoft's `Microsoft.Windows.SDK.NET.Ref` targeting pack. Neither requires administrator privileges.
