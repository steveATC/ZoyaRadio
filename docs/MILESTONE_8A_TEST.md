# Milestone 8A manual desktop test

## Status

Milestone 8A is an implementation candidate. Automated verification is complete, but Steve's real Windows desktop acceptance is still required. Do not mark M8A passed from this document alone. Milestone 7.5B remains the latest accepted implementation, and Milestone 8B remains blocked and unstarted.

## Before starting

1. Open YouTube Music in Firefox and start a song.
2. Close any older Zoya Radio window or console run.
3. Open the `ZoyaRadio` folder in File Explorer.
4. Click the address bar, type `powershell`, and press Enter.
5. Run:

```powershell
.\run-zoya-radio-control.cmd
```

The application should open without administrator privileges. It must not pause, resume, or toggle Firefox during normal automatic delivery.

## Test A - startup, status, and repeated Start

1. Confirm the window shows the current title, artist, and a readable Firefox targeting state.
2. Click **Start** once.
3. Confirm the status becomes Running and Start is no longer available while running.
4. Click rapidly around the Start/Stop area only as the enabled controls allow. Confirm no duplicate windows, observers, announcements, or error loops appear.
5. Leave it running across enough genuine track transitions for at least one scheduled announcement.
6. Confirm music continues while Zoya prepares the comment and Nicole audio.
7. Confirm the announcement is delivered over ducked Firefox audio without pausing Firefox.
8. Confirm Firefox returns to exactly its previous volume and mute state afterward.

## Test B - Silent Mode and Clear History

1. While running, turn **Silent Mode** on before a scheduled delivery if practical.
2. Confirm observation can continue but no pending or stale announcement plays.
3. Turn Silent Mode off and confirm scheduling starts fresh with no catch-up announcement.
4. Click **Clear History**.
5. Confirm the app stays running and logs are not deleted. This button clears only the small process-local recent-track/comment context.

## Test C - Stop and protected cleanup

Repeat Stop in several representative phases when timing permits: during local AI generation, Nicole preparation, target acquisition, Firefox ducking, speech, and restoration. For each attempt:

1. Click **Stop**.
2. Confirm the window visibly says it is stopping or restoring and conflicting controls are disabled.
3. Confirm Stop completes without freezing the window.
4. If Firefox had already been ducked, confirm its exact previous volume and mute state are restored before the app reports Stopped.
5. Confirm Chrome, another application's volume, and Windows master volume never change.

It is fine if the short timing windows make a particular phase difficult to catch. Report which phase could not be exercised rather than pretending it passed.

## Test D - safe window close

1. Start the app again.
2. Close the window during ordinary observation.
3. Repeat during preparation, ducking, or speech if practical.
4. Confirm the window shows cleanup in progress and closes only after safe cleanup completes.
5. Confirm Firefox is never left ducked or muted.

If the app warns that Firefox volume may be uncertain, restore Firefox manually and keep the log for review.

## Test E - target safety and isolation

1. If convenient, play media in Chrome while Firefox music remains active.
2. Confirm Zoya continues to target only Firefox and never ducks or controls Chrome.
3. If Firefox is missing, not Playing, or Windows exposes an ambiguous Firefox target, confirm the app reports the condition and remains safely silent.
4. Confirm normal delivery never changes Windows master volume and never issues Firefox pause, play, resume, or toggle commands.

## Test F - logs and preserved console diagnostics

1. Click **Open Logs Folder** and confirm File Explorer opens:

```text
%LOCALAPPDATA%\Zoya Radio\logs
```

2. Confirm a readable timestamped UTF-8 `radio-behavior-test-*.txt` log is present.
3. Run the preserved read-only snapshot:

```powershell
.\run-media-probe.cmd --once
```

4. When convenient, run the existing automatic-radio console launcher and confirm it still behaves as before:

```powershell
.\run-radio-behavior-test.cmd
```

Press **Ctrl+C** to stop the console run safely. Other explicit diagnostics do not need to be rerun unless troubleshooting requires them.

## What to bring back to the Work chat

Provide:

- the newest `radio-behavior-test-*.txt` file from `%LOCALAPPDATA%\Zoya Radio\logs`;
- a screenshot of the control window while Running and, if possible, while Stopping;
- which Stop/close phases you successfully exercised;
- whether Firefox's volume and mute state were restored exactly each time;
- confirmation that Chrome and Windows master volume stayed unchanged;
- any unreadable status, delay, duplicate action, freeze, unexpected pause, stale announcement, or crash;
- the exact time of any failure and what you clicked immediately before it.

Successful desktop evidence is required before M8A can be marked passed. Do not begin Milestone 8B from this test.
