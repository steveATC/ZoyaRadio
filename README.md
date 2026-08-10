# Zoya Radio

Milestones 0 through 5, Milestone 7, and Milestone 7.5A through 7.5B have passed Steve's manual tests. Milestone 6 is deliberately deferred. Milestone 8 is planned but has not started.

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

Each run creates a timestamped text file in the repository's `logs` folder. Read-only runs use `media-probe-*.txt`, control-test runs use `playback-control-test-*.txt`, spoken tests use `spoken-announcement-test-*.txt`, AI-comment tests use `ai-comment-test-*.txt`, automatic behavior runs use `radio-behavior-test-*.txt`, neural resource checks use `neural-tts-diagnostic-*.txt`, and audio-ducking checks use `audio-ducking-test-*.txt`. The console and log contain the same useful diagnostics. These files remain on this computer, are excluded from Git, and may be deleted whenever they are no longer needed.

The completed Milestone 0 procedure is preserved in [docs/MILESTONE_0_TEST.md](docs/MILESTONE_0_TEST.md).

## Run the explicit playback-control test

This is separate from the read-only probe. It lists every Windows media session, refuses zero or multiple `firefox.exe` matches, requires Firefox to already be Playing, and asks for confirmation before issuing any control request.

```powershell
.\run-playback-control-test.cmd
```

Only an explicit `y` or `yes` confirms the test; Enter and every other answer mean no. If confirmed, the tester requests pause on the one selected Firefox session, verifies Paused, waits about three seconds, then requests and verifies Playing on that same session. Follow [docs/MILESTONE_1_TEST.md](docs/MILESTONE_1_TEST.md) for the complete acceptance test.

## Run the explicit spoken-announcement test

This separate test now prefers the selected local Kokoro `af_nicole` neural voice and automatically falls back to the existing Windows voice if neural preparation fails. It announces the exact title and artist supplied by the selected `firefox.exe` media session. It does not use an online speech service, YouTube APIs, browser data, or guessed album information.

```powershell
.\run-spoken-announcement-test.cmd
```

It validates local speech before any playback command, requires Firefox to be Playing, shows the proposed sentence, and defaults to no at the confirmation prompt. If confirmed, it prepares Nicole's audio while music still plays, rechecks the same session and metadata, pauses that Firefox session, speaks once, and restores that same session in protected cleanup.

To force the old Windows voice for troubleshooting:

```powershell
.\run-spoken-announcement-test.cmd --windows-tts
```

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

## Run the transition-aware automatic radio test

Milestone 7.5B watches genuine deduplicated Firefox track changes and schedules one automatic comment at a new random interval of 2-5 transitions. One transition before delivery, it prepares a comment about Track A and synthesizes Nicole's audio while Track A keeps playing. When Track B appears, a valid prepared comment has a strict two-second window to begin: Firefox is smoothly ducked to 25% of its current application-session volume, Nicole speaks over the still-playing music, and the exact volume/mute state captured at delivery time is restored.

Milestone 7.5B passed Steve's focused Firefox-plus-Chrome desktop retest on August 10, 2026. Repeated harmless Firefox GSMTC wrapper refreshes did not interrupt preparation or active speech; both accepted deliveries used the same Firefox Core Audio target, restored 100% -> 25% -> 100% exactly, left Chrome untouched, kept observing track changes, and shut down cleanly.

The test asks once before arming and defaults to **NO**. Normal automatic delivery does **not** pause or resume Firefox. Unsafe, unfinished, stale, late, ambiguous, or rapidly skipped opportunities produce silence instead of a catch-up comment.

```powershell
.\run-radio-behavior-test.cmd
```

While armed:

- press **S** to turn Silent Mode on or off;
- press **C** to clear the small in-memory recent-track/comment history;
- press **Ctrl+C** to stop safely.

Track A must be at least eight seconds old before preparation begins. Immediately before delivery, the program rechecks the original `firefox.exe` media session, current Track B and Playing state, the two-second window, and the active Firefox Core Audio target. The volume captured then is the value protected restoration owns—even if Steve changed Firefox's mixer volume during Track A.

Recent behavioral history is deliberately temporary: at most 10 tracks and 6 accepted AI comments, discarded when the application closes. Diagnostic text logs are separate and are not deleted by **C**.

The zero-GPU automatic mode keeps the same 2-5 scheduling and fixed announcement:

```powershell
.\run-radio-behavior-test.cmd --no-ai
```

The deliberate first-delivery recovery check is:

```powershell
.\run-radio-behavior-test.cmd --simulate-transition-failure
```

It deliberately fails after Firefox is ducked and must still restore the captured volume. The completed acceptance evidence and preserved regression procedure are in [docs/MILESTONE_7_5B_TEST.md](docs/MILESTONE_7_5B_TEST.md). The completed Milestone 5 procedure remains preserved in [docs/MILESTONE_5_TEST.md](docs/MILESTONE_5_TEST.md).

## Milestone 7 local neural voice

Steve selected `af_nicole`; `af_heart` was the runner-up. Nicole is now the normal local speech provider, running on the CPU. The model loads only while preparing one announcement and is disposed before Firefox is paused. Prepared WAV audio stays in memory and is disposed after use, so temporary audio files do not accumulate.

If Nicole cannot initialize or synthesize, Zoya automatically prepares the same text with Microsoft Windows TTS before pausing music. If playback itself fails after pause, the protected same-Firefox-session resume path runs immediately. Force the safe fallback test with:

```powershell
.\run-neural-tts-fallback-test.cmd
```

The safe resource diagnostic creates three in-memory Nicole preparations but plays nothing and controls no media:

```powershell
.\run-neural-tts-diagnostic.cmd
```

The approved model and packages are project-local, require no administrator access, and remain excluded from Git. See [docs/MILESTONE_7_TEST.md](docs/MILESTONE_7_TEST.md) for the completed manual acceptance test and [docs/MILESTONE_7_VOICE_AUDITION.md](docs/MILESTONE_7_VOICE_AUDITION.md) for the original samples.

## Run the isolated Firefox audio-ducking test

Milestone 7.5A passed Steve's manual acceptance and remains available as a separate diagnostic. It uses Windows Core Audio to enumerate the current default output device's application sessions and considers only **active**, exact `firefox`/`firefox.exe` process matches. It refuses zero or multiple active Firefox candidates and never guesses at a browser tab.

Start with the completely read-only inventory:

```powershell
.\run-audio-ducking-test.cmd --enumerate-only
```

The real duck-and-restore test is:

```powershell
.\run-audio-ducking-test.cmd
```

It clearly displays the selected session and defaults to **NO**. After an explicit **Y**, it captures Firefox's exact per-session volume and mute state, smoothly lowers that captured volume to 25% of itself for about three seconds, and restores the exact captured values in protected cleanup. It does not change Windows master volume, pause or resume playback, generate AI text, or speak.

The deliberate restoration check is:

```powershell
.\run-audio-ducking-test.cmd --simulate-duck-failure
```

The preserved procedure and accepted observations are in [docs/MILESTONE_7_5A_TEST.md](docs/MILESTONE_7_5A_TEST.md). Windows aggregates multiple audible Firefox tabs into the same application session on Steve's PC, so they duck together; this application-level behavior is accepted for Phase 1.

## Developer build

The approved Microsoft .NET 8 SDK is installed locally in `.dotnet`, and the approved Microsoft Windows SDK reference pack is restored locally in `.packages`. Both folders are excluded from Git and require no administrator privileges.

From the repository root:

```powershell
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj --configuration Release
.\.dotnet\dotnet.exe run --project .\tests\ZoyaRadio.MediaProbe.Tests\ZoyaRadio.MediaProbe.Tests.csproj --configuration Release
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.VoiceAudition\ZoyaRadio.VoiceAudition.csproj --configuration Release
```

The radio project uses Microsoft's supported `Windows.Media.Control`, `Windows.Media.SpeechSynthesis`, `Windows.Media.Playback`, and Core Audio session APIs, Ollama's loopback-only local HTTP API, and the approved `KokoroSharp.CPU` 0.8.0 package with CPU ONNX Runtime. No cloud speech service or API key is used.
