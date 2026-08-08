# Zoya Radio

Milestones 0 through 5 have passed Steve's manual tests. Milestone 6 is deliberately deferred. Milestone 7 Stage B has produced local Kokoro voice samples and is waiting for Steve to choose a voice before any radio integration begins.

The original media probe is **read only**. It lists and watches Windows media sessions, but it cannot pause, play, skip, change volume, or otherwise control playback. It does not inspect Firefox, webpage content, URLs, cookies, history, or Google account information.

## Run the watcher

1. Open this `ZoyaRadio` folder in File Explorer.
2. Click the address bar at the top of File Explorer, type `powershell`, and press Enter.
3. In the blue or black window that opens, type:

   ```powershell
   .\run-media-probe.cmd
   ```

4. Leave that window open while testing YouTube Music.
5. To stop the probe, click the probe window and press **Ctrl+C** once.

The startup message will explicitly say `READ ONLY`. The watcher reports every media session Windows exposes, including its raw `SourceAppUserModelId`, title, artist, album, and playback state. A missing field is printed as `<not provided>` rather than guessed.

## Take one snapshot and exit

Use this shorter check when you only want to list the sessions that exist right now:

```powershell
.\run-media-probe.cmd --once
```

## Logs

Each run creates a timestamped text file in the repository's `logs` folder. Read-only runs use `media-probe-*.txt`, control-test runs use `playback-control-test-*.txt`, spoken tests use `spoken-announcement-test-*.txt`, AI-comment tests use `ai-comment-test-*.txt`, and automatic behavior runs use `radio-behavior-test-*.txt`. The console and log contain the same useful diagnostics. These files remain on this computer, are excluded from Git, and may be deleted whenever they are no longer needed.

The completed Milestone 0 procedure is preserved in [docs/MILESTONE_0_TEST.md](docs/MILESTONE_0_TEST.md).

## Run the explicit playback-control test

This is separate from the read-only probe. It lists every Windows media session, refuses zero or multiple `firefox.exe` matches, requires Firefox to already be Playing, and asks for confirmation before issuing any control request.

```powershell
.\run-playback-control-test.cmd
```

Only an explicit `y` or `yes` confirms the test; Enter and every other answer mean no. If confirmed, the tester requests pause on the one selected Firefox session, verifies Paused, waits about three seconds, then requests and verifies Playing on that same session. Follow [docs/MILESTONE_1_TEST.md](docs/MILESTONE_1_TEST.md) for the complete acceptance test.

## Run the explicit spoken-announcement test

This separate Milestone 2 test uses Microsoft's local Windows speech system and the ordinary default installed voice. It announces the exact title and artist supplied by the selected `firefox.exe` media session. It does not use the internet, AI, YouTube APIs, browser data, or guessed album information.

```powershell
.\run-spoken-announcement-test.cmd
```

It validates and lists the installed voices before any playback command, requires Firefox to be Playing, shows the proposed sentence, and defaults to no at the confirmation prompt. If confirmed, it rechecks the same session and metadata, pauses that Firefox session, speaks once, and restores that same session in protected cleanup.

The deliberate recovery check has its own launcher and never speaks:

```powershell
.\run-simulated-speech-failure-test.cmd
```

Follow [docs/MILESTONE_2_TEST.md](docs/MILESTONE_2_TEST.md) for the complete manual acceptance test.

## Run the local Zoya-persona test

Milestone 4 keeps Milestone 3's local Ollama and grounding protections, then asks `qwen3:4b-instruct` to respond as Zoya: Steve's warm, dry, witty companion rather than a radio announcer. It still sends only the current Firefox title and artist as music facts. Exact `FALLBACK`, validation, timeout, unloading, and fixed-announcement behavior remain unchanged.

Start the normal one-comment test with:

```powershell
.\run-ai-comment-test.cmd
```

The test shows the final text before asking permission. Only `y` or `yes` allows speech and targeted pause/resume; Enter defaults to no. Other explicit modes are:

```powershell
.\run-ai-comment-test.cmd --generate-only
.\run-ai-comment-test.cmd --no-ai
.\run-ai-comment-test.cmd --simulate-ai-failure
.\run-ai-comment-test.cmd --measure-ai
```

Generate-only and measurement modes never speak or control playback. No-AI mode makes no Ollama request. Zoya's and Steve's names are defined in `HostPersonaConfiguration.cs`; her editable text persona is in `Prompts/Milestone4HostPersonaPrompt.txt`. Follow [docs/MILESTONE_4_TEST.md](docs/MILESTONE_4_TEST.md) for the personality review. The completed Milestone 3 procedure remains in [docs/MILESTONE_3_TEST.md](docs/MILESTONE_3_TEST.md).

## Run the automatic radio-behavior test

Milestone 5 watches genuine deduplicated Firefox track changes and schedules one automatic comment after a new random interval of 2-5 transitions. It asks once before arming and defaults to **NO**. It never treats launching the program as permission to control playback. The small validated defaults are collected in `RadioBehaviorConfiguration.cs`.

```powershell
.\run-radio-behavior-test.cmd
```

While armed:

- press **S** to turn Silent Mode on or off;
- press **C** to clear the small in-memory recent-track/comment history;
- press **Ctrl+C** to stop safely.

The current track must be at least eight seconds old before Zoya may pause it. AI generation happens while music remains Playing. Immediately before pause, the program rechecks that exactly the original `firefox.exe` session, expected track, Playing state, age guard, armed state, and Silent Mode are all safe. A changed track cancels the pending interruption instead of creating catch-up chatter.

Recent behavioral history is deliberately temporary: at most 10 tracks and 6 accepted AI comments, discarded when the application closes. Diagnostic text logs are separate and are not deleted by **C**.

The zero-GPU automatic mode keeps the same 2-5 scheduling and fixed announcement:

```powershell
.\run-radio-behavior-test.cmd --no-ai
```

The completed Milestone 5 procedure is preserved in [docs/MILESTONE_5_TEST.md](docs/MILESTONE_5_TEST.md).

## Compare the Milestone 7 voice samples

The separate voice-audition tool uses the approved CPU-only Kokoro model to create WAV files. It does **not** play them automatically, inspect Firefox, control music, call Ollama, or change the working Windows speech fallback.

The current samples are under `artifacts\voice-audition\20260807-235846-809`. Open that folder in File Explorer and double-click files `01` through `05` to compare the voices. Follow [docs/MILESTONE_7_VOICE_AUDITION.md](docs/MILESTONE_7_VOICE_AUDITION.md) and tell the Work chat which exact voice name you prefer.

To create a fresh set later:

```powershell
.\run-voice-audition.cmd
```

The approved model and packages are project-local, require no administrator access, and remain excluded from Git. Milestone 7 has not been integrated into the radio yet; that work waits for Steve's voice choice.

## Developer build

The approved Microsoft .NET 8 SDK is installed locally in `.dotnet`, and the approved Microsoft Windows SDK reference pack is restored locally in `.packages`. Both folders are excluded from Git and require no administrator privileges.

From the repository root:

```powershell
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj --configuration Release
.\.dotnet\dotnet.exe run --project .\tests\ZoyaRadio.MediaProbe.Tests\ZoyaRadio.MediaProbe.Tests.csproj --configuration Release
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.VoiceAudition\ZoyaRadio.VoiceAudition.csproj --configuration Release
```

The radio project uses Microsoft's supported `Windows.Media.Control`, `Windows.Media.SpeechSynthesis`, and `Windows.Media.Playback` APIs plus Ollama's loopback-only local HTTP API. The separate Milestone 7 audition project adds the approved `KokoroSharp.CPU` 0.8.0 package and CPU ONNX runtime locally. No cloud speech service or API key is used.
