# Zoya Radio

Milestone 0 has passed Steve's manual test. The repository now also contains the explicit Milestone 1 Firefox pause/resume test harness, which is ready for manual testing but has not yet passed.

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

Each run creates a timestamped text file in the repository's `logs` folder. Read-only runs use `media-probe-*.txt`; control-test runs use `playback-control-test-*.txt`. The console and log contain the same useful diagnostics. These files remain on this computer, are excluded from Git, and may be deleted whenever they are no longer needed.

The completed Milestone 0 procedure is preserved in [docs/MILESTONE_0_TEST.md](docs/MILESTONE_0_TEST.md).

## Run the explicit playback-control test

This is separate from the read-only probe. It lists every Windows media session, refuses zero or multiple `firefox.exe` matches, requires Firefox to already be Playing, and asks for confirmation before issuing any control request.

```powershell
.\run-playback-control-test.cmd
```

Only an explicit `y` or `yes` confirms the test; Enter and every other answer mean no. If confirmed, the tester requests pause on the one selected Firefox session, verifies Paused, waits about three seconds, then requests and verifies Playing on that same session. Follow [docs/MILESTONE_1_TEST.md](docs/MILESTONE_1_TEST.md) for the complete acceptance test.

## Developer build

The approved Microsoft .NET 8 SDK is installed locally in `.dotnet`, and the approved Microsoft Windows SDK reference pack is restored locally in `.packages`. Both folders are excluded from Git and require no administrator privileges.

From the repository root:

```powershell
.\.dotnet\dotnet.exe build .\src\ZoyaRadio.MediaProbe\ZoyaRadio.MediaProbe.csproj --configuration Release
.\.dotnet\dotnet.exe run --project .\tests\ZoyaRadio.MediaProbe.Tests\ZoyaRadio.MediaProbe.Tests.csproj --configuration Release
```

The project uses Microsoft's supported [`Windows.Media.Control`](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager) APIs. It has no third-party runtime dependencies.
