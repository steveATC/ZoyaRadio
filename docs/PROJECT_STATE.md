# Project state

## Current milestone

**Milestone 0 - Windows media-session / Firefox track detection**

Implementation status: built successfully and ready for Steve's manual acceptance test.

**Milestone 0 is NOT MANUALLY VERIFIED.** Do not label it successful until Steve tests Firefox playing YouTube Music on this machine.

## Completed milestones

None. Milestone 0 remains open pending the manual Firefox and YouTube Music test.

## Known issues and limits

- Automated tests cannot prove that Firefox and YouTube Music publish correct metadata on Steve's interactive Windows desktop.
- Windows or Firefox may omit album information. The probe displays `<not provided>` and does not treat that as an error.
- Windows may expose zero sessions when no compatible media is active. The probe reports that without crashing.
- The automated `--once` run under the non-interactive Codex sandbox account was denied access to the Windows media-session manager. It exited cleanly with a readable warning and code 1. Steve's normal signed-in desktop test is required to determine whether this is only a sandbox restriction.

## Important decisions and evidence

- The probe is a C#/.NET 8 console application using only `GlobalSystemMediaTransportControlsSessionManager` and `GlobalSystemMediaTransportControlsSession` for runtime media observation.
- It targets Windows 10 and uses the supported Windows Runtime API introduced in Windows 10 version 1809.
- It enumerates all sessions and keeps the raw `SourceAppUserModelId` visible. Firefox recognition is only a case-insensitive hint based on the raw identifier returned by Windows.
- It subscribes to session-list, media-property, and playback-info changes. Track identity is exactly the normalized title/artist/album combination, which prevents duplicate `TRACK CHANGED` messages for an identical state.
- No playback-control API, browser automation, browser data access, AI, speech, or runtime networking is included.
- Build tooling is project-local: Microsoft .NET SDK 8.0.423 and Microsoft's `Microsoft.Windows.SDK.NET.Ref` targeting pack. Neither requires administrator privileges.
