# Zoya Radio

Milestones 0, 1, and 2 have passed Steve's manual tests. Milestone 3 local-AI work has not started at this checkpoint.

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

Each run creates a timestamped text file in the repository's `logs` folder. Read-only runs use `media-probe-*.txt`, control-test runs use `playback-control-test-*.txt`, and spoken tests use `spoken-announcement-test-*.txt`. The console and log contain the same useful diagnostics. These files remain on this computer, are excluded from Git, and may be deleted whenever they are no longer needed.

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

## Developer build

The approved Microsoft .NET 8 SDK is installed locally in `.dotnet`, and the approved Microsoft Windows SDK reference pack is restored locally in `.packages`. Both folders are excluded from Git and require no administrator privileges.

From the repository root:

```powershell
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj --configuration Release
.\.dotnet\dotnet.exe run --project .\tests\ZoyaRadio.MediaProbe.Tests\ZoyaRadio.MediaProbe.Tests.csproj --configuration Release
```

The project uses Microsoft's supported `Windows.Media.Control`, `Windows.Media.SpeechSynthesis`, and `Windows.Media.Playback` APIs. It has no third-party runtime dependencies.
