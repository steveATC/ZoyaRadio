# Zoya Radio

Zoya Radio is a private, local-first Windows application with an accepted WinForms control frontend and preserved console diagnostics. It observes Firefox media, prepares short locally generated comments, speaks with the accepted Nicole voice, and can deliver prepared comments over proportionally ducked Firefox audio.

Milestones 0 through 5, Milestone 7, Milestones 7.5A-7.5B, and Milestone 8 have passed their required internal-development acceptance. Milestone 6 is deliberately deferred. Steve accepted packaged version `0.8.0-alpha.1` on August 11, 2026, making commit `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf` the latest accepted internal implementation with 178 automated tests passed and 0 failed. Milestone 9A was rejected for runtime consideration. Milestone 9C is closed as successful experimental evidence. Steve then explicitly authorized the bounded M9B implementation. M9B Gates 2 and 3 are owner-accepted. The first authorized Gate 4 run safely delivered five M8 fallback comments with Firefox ducking/restoration and no playback pause, but delivered zero M9B enriched comments because live MusicBrainz context failed before 14B generation. Gate 4 remains authorized but paused pending the bounded parser/cache repair and Steve's resumed test; M9B is not yet accepted runtime behavior. Clean Windows 10/11 and invited-tester validation remain deferred distribution evidence, so the package is not a clean-machine-certified or public release.

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

## Milestone 8A control application

Start the development control application:

```powershell
.\run-zoya-radio-control.cmd
```

The window provides Start, safely awaited Stop, Silent Mode, Clear History, and Open Logs Folder controls. It displays the current title, artist, concise runtime status, Firefox targeting state, and a read-only **Radio activity** line. That activity line shows the transition countdown and preparation/delivery phase, so normal testing does not require a second PowerShell window tailing the log. Closing the window waits for the same protected cleanup path used by Stop.

Steve accepted this development control application on August 11, 2026. See [docs/MILESTONE_8A_TEST.md](docs/MILESTONE_8A_TEST.md) for the accepted evidence and preserved regression procedure.

## Milestone 8B internal packaged baseline

The local candidate is an unsigned, portable, self-contained Windows x64 ZIP for Windows 10 22H2 or newer. Reproduce it from the repository with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\New-PrivateAlphaCandidate.ps1
```

The generated tester files remain under the ignored `artifacts\private-alpha\0.8.0-alpha.1\` folder. The ZIP contains the application, .NET desktop runtime, Kokoro FP32 model, Nicole only, and Windows local TTS fallback. It excludes eSpeak, PDBs, Firefox, Ollama, Qwen, logs, and user data. Firefox and Ollama are separate installations; Qwen can be obtained through an installed local Ollama only after an explicit size/disk/consent prompt.

The packaged window adds thin readiness actions for package integrity, Windows/disk, Firefox, Visual C++, Ollama, and `qwen3:4b-instruct`. It never silently installs or starts third-party software. Some unusual or out-of-vocabulary names may make the eSpeak-free Nicole path fall back to a local Windows voice before any ducking or speech begins.

Steve accepted the extracted package as the internal-development baseline on August 11, 2026. Follow [docs/MILESTONE_8B_TEST.md](docs/MILESTONE_8B_TEST.md) and the packaged tester guide for the recorded evidence and deferred compatibility checks. The ZIP remains an unsigned private-alpha candidate: do not describe it as clean-machine certified, broadly compatible, publicly released, or validated for third-party distribution.

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

## Milestone 9A structured-commentary benchmark

Run the explicit generate-only comparison:

```powershell
.\run-commentary-benchmark.cmd
```

This fixed benchmark compares the accepted prompt with a structured candidate over 12 synthetic scenarios and 2 samples per arm (48 local requests). It contacts only the already-configured loopback Ollama service. It does not request Windows media sessions, observe or control Firefox, change volume, open the GUI, or produce speech.

Outputs are written under the ignored `artifacts\m9a\<timestamp>\` folder. Score `M9A_EVALUATION.md` before opening the separate answer key. See [docs/MILESTONE_9A_TEST.md](docs/MILESTONE_9A_TEST.md) for the exact review procedure.

The rejected M9A candidate remains unreachable from normal automatic radio. M9C supplied the evidence used by the bounded M9B candidate; M9B still requires its staged owner gates before acceptance.

## Milestone 9C source-backed benchmark

```powershell
.\run-source-backed-commentary-benchmark.cmd
```

This optional diagnostic sends only its fixed title/artist scenarios to approved MusicBrainz and, after strict identity confirmation, linked Wikidata. It writes ignored artifacts under `artifacts\m9c\<timestamp>\`, never accesses Firefox or audio, and cannot enter normal radio. The later M9C.3-M9C.6 launchers preserve the curated-context, deterministic-anchor, model-comparison, and whole-comment experiments; see [docs/MILESTONE_9C_TEST.md](docs/MILESTONE_9C_TEST.md) for the completed record.

## Milestone 9B grounded-context candidate evaluation

The one authorized 48-request local run is complete. Review:

```text
artifacts\m9b\20260815-095443-189\M9B_EVALUATION.md
```

It did not start the radio, inspect or control Firefox, produce speech, or change any
volume. Steve completed and accepted Gate 2 with the narrow same-card repetition exception recorded in [docs/MILESTONE_9B_TEST.md](docs/MILESTONE_9B_TEST.md). Gate 3 is owner-accepted. Gate 4 is authorized but paused after its first safe fallback-only attempt exposed the bounded live MusicBrainz parser/cache repair; do not rerun a radio launcher until Steve resumes that test.

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

The Milestone 8A control application and automatic radio runtime write timestamped UTF-8 logs under:

```text
%LOCALAPPDATA%\Zoya Radio\logs
```

The control app's **Open Logs Folder** button opens that directory. The automatic radio console uses the same runtime-log location. Other diagnostic modes continue writing under this repository's `logs\` folder. Existing logs were not moved, changed, or deleted.

Common names include:

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
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.ControlApp\ZoyaRadio.ControlApp.csproj --configuration Release
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

### Preserved M9C quality benchmarks

```powershell
.\run-deterministic-context-benchmark.cmd
.\run-m9c5-model-comparison.cmd
.\run-m9c6-whole-comment-benchmark.cmd
```

These generate-only experiments preserve the rejected deterministic-anchor design, the 4B-versus-14B comparison, and the successful M9C.6 whole-comment comparison. They never touch Firefox, audio, speech, volume, ducking, the GUI, or ordinary radio state. M9C.6 selected grounded 14B whole-comment synthesis as future design evidence only; see [docs/MILESTONE_9C_TEST.md](docs/MILESTONE_9C_TEST.md).
