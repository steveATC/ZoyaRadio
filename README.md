# Zoya Radio

Zoya Radio is a private, local-first Windows console application that observes YouTube Music in Firefox, prepares short locally generated comments, speaks with the accepted Nicole voice, and can deliver prepared comments over proportionally ducked Firefox audio.

Milestones 0 through 5, Milestone 7, and Milestones 7.5A-7.5B have passed Steve's manual tests. Milestone 6 is deliberately deferred. Milestone 8 is planned but unstarted. The latest accepted commit is `63662342a374b323b639325f360ae7c17ace1432` with 124 automated tests passed and 0 failed.

Start with [docs/PROJECT_INDEX.md](docs/PROJECT_INDEX.md) for the canonical documentation map. Current status is in [docs/PROJECT_STATE.md](docs/PROJECT_STATE.md), and implemented behavior is in [docs/CURRENT_ARCHITECTURE.md](docs/CURRENT_ARCHITECTURE.md).

## Safety and privacy

- The original media watcher is **READ ONLY** and cannot control playback or volume.
- Normal Milestone 7.5B automatic delivery never pauses Firefox. It changes only the selected Firefox Core Audio application-session volume, speaks, and restores the exact captured state.
- Explicit playback, spoken-announcement, and volume diagnostic modes display their target and default to **NO** before real control.
- Zoya Radio does not inspect Firefox pages, URLs, cookies, history, credentials, tokens, or Google account data.
- Ollama and speech run locally. There is no paid/cloud API, API key, telemetry, or song download.
- Missing or ambiguous Firefox targets cause refusal and silence; Chrome and Windows master volume are never fallback targets.

## Open a command window

1. Open this `ZoyaRadio` folder in File Explorer.
2. Click the address bar, type `powershell`, and press Enter.
3. Run one of the commands below.
4. Press **Ctrl+C** once to stop a long-running mode safely.

## Read-only media watcher

Watch every Windows media session and its available title, artist, optional album, and playback state:

```powershell
.\run-media-probe.cmd
```

Take one snapshot and exit:

```powershell
.\run-media-probe.cmd --once
```

Missing fields display as `<not provided>` rather than being guessed. The preserved acceptance procedure is [docs/MILESTONE_0_TEST.md](docs/MILESTONE_0_TEST.md).

## Transition-aware automatic radio

Run the accepted Milestone 7.5B behavior:

```powershell
.\run-radio-behavior-test.cmd
```

After confirming the displayed `firefox.exe` target with **Y**, Zoya schedules a comment every 2-5 genuine transitions. It prepares local AI text and Nicole audio one transition early while music plays. At the next valid transition it ducks only Firefox to 25% of the volume captured at delivery time, speaks over the still-playing music, and restores the exact captured volume/mute state.

While armed:

- press **S** to toggle Silent Mode;
- press **C** to clear the small in-memory recent-track/comment history;
- press **Ctrl+C** to restore safely and stop.

Run the same schedule without contacting Ollama:

```powershell
.\run-radio-behavior-test.cmd --no-ai
```

Exercise the protected post-duck restoration path:

```powershell
.\run-radio-behavior-test.cmd --simulate-transition-failure
```

Accepted evidence and the complete regression procedure are in [docs/MILESTONE_7_5B_TEST.md](docs/MILESTONE_7_5B_TEST.md). The earlier automatic pause/speak/resume procedure is preserved in [docs/MILESTONE_5_TEST.md](docs/MILESTONE_5_TEST.md).

## Explicit Firefox playback-control test

This separate diagnostic targets exactly one Playing `firefox.exe` session, asks before control, pauses that same session, waits about three seconds, resumes it, and verifies both states:

```powershell
.\run-playback-control-test.cmd
```

Enter/default NO issues no playback command. See [docs/MILESTONE_1_TEST.md](docs/MILESTONE_1_TEST.md).

## One spoken announcement

Prepare and speak one title/artist announcement using Nicole with Windows local TTS fallback:

```powershell
.\run-spoken-announcement-test.cmd
```

Force Windows TTS for troubleshooting:

```powershell
.\run-spoken-announcement-test.cmd --windows-tts
```

Exercise the protected failure-recovery path without speech:

```powershell
.\run-simulated-speech-failure-test.cmd
```

These explicit tests may pause/resume only the confirmed Firefox GSMTC session. See [docs/MILESTONE_2_TEST.md](docs/MILESTONE_2_TEST.md).

## Local AI/persona diagnostics

Generate one Zoya comment and optionally confirm its spoken explicit test:

```powershell
.\run-ai-comment-test.cmd
```

Safe diagnostic modes:

```powershell
.\run-ai-comment-test.cmd --generate-only
.\run-ai-comment-test.cmd --measure-ai
.\run-ai-comment-test.cmd --no-ai
.\run-ai-comment-test.cmd --simulate-ai-failure
```

Generate-only and measurement modes do not speak or control playback. No-AI does not contact Ollama. See [docs/MILESTONE_3_TEST.md](docs/MILESTONE_3_TEST.md) and [docs/MILESTONE_4_TEST.md](docs/MILESTONE_4_TEST.md).

## Isolated Firefox audio-ducking diagnostic

List current Core Audio sessions without changing anything:

```powershell
.\run-audio-ducking-test.cmd --enumerate-only
```

Explicitly confirm a three-second Firefox-only duck/restoration test:

```powershell
.\run-audio-ducking-test.cmd
```

Exercise protected volume restoration after a deliberate failure:

```powershell
.\run-audio-ducking-test.cmd --simulate-duck-failure
```

This mode never changes Windows master volume or playback state. See [docs/MILESTONE_7_5A_TEST.md](docs/MILESTONE_7_5A_TEST.md).

## Nicole voice diagnostics

Force the Windows fallback path:

```powershell
.\run-neural-tts-fallback-test.cmd
```

Prepare three in-memory Nicole samples without playback or media control:

```powershell
.\run-neural-tts-diagnostic.cmd
```

Create local audition material:

```powershell
.\run-voice-audition.cmd
```

Steve selected Kokoro `af_nicole` as primary and `af_heart` as runner-up. See [docs/MILESTONE_7_TEST.md](docs/MILESTONE_7_TEST.md) and [docs/MILESTONE_7_VOICE_AUDITION.md](docs/MILESTONE_7_VOICE_AUDITION.md).

## Logs

Every run writes a timestamped UTF-8 text file under `logs\`. Common names are:

- `media-probe-*.txt`
- `playback-control-test-*.txt`
- `spoken-announcement-test-*.txt`
- `ai-comment-test-*.txt`
- `radio-behavior-test-*.txt`
- `audio-ducking-test-*.txt`
- `neural-tts-diagnostic-*.txt`

Logs stay on this computer, are excluded from Git, and may be deleted when no longer needed. Clearing in-memory history with **C** does not delete logs.

## Developer build and tests

The approved Microsoft .NET 8 SDK is local in `.dotnet\`, and restored packages are local in `.packages\`. Both are ignored by Git and require no administrator privileges.

From the repository root:

```powershell
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj --configuration Release
.\.dotnet\dotnet.exe build .\tests\ZoyaRadio.MediaProbe.Tests\ZoyaRadio.MediaProbe.Tests.csproj --configuration Release
.\.dotnet\dotnet.exe run --project .\tests\ZoyaRadio.MediaProbe.Tests\ZoyaRadio.MediaProbe.Tests.csproj --configuration Release --no-build
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.VoiceAudition\ZoyaRadio.VoiceAudition.csproj --configuration Release
```

The application uses Microsoft's Windows media, speech, playback, and Core Audio session APIs; Ollama's loopback-only local HTTP API; and the approved `KokoroSharp.CPU` 0.8.0 package. No cloud speech service or API key is used.

## Canonical project documents

- [Documentation index](docs/PROJECT_INDEX.md)
- [Current project state](docs/PROJECT_STATE.md)
- [Current architecture](docs/CURRENT_ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Durable decisions](docs/DECISIONS.md)
- [Known issues](docs/KNOWN_ISSUES.md)
- [Research index](docs/RESEARCH_INDEX.md)
