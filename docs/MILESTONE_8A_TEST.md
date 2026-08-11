# Milestone 8A manual acceptance - Windows control application

## Status

Steve manually accepted Milestone 8A on August 11, 2026. The accepted implementation is commit `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc`; the original candidate was `f123ddd9de484de590ba596abf812314b265390c`, and `41b7824` supplied the accepted activity-status correction. The accepted automated result is 162 passed and 0 failed.

Milestone 8 as a whole is **not passed**. Milestone 8B is the next gate, remains unstarted, and requires a separate explicit implementation request.

## Accepted checkpoint

- Original implementation candidate: `f123ddd9de484de590ba596abf812314b265390c`
- Activity correction and accepted implementation: `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc`
- Accepted automated suite: 162 passed, 0 failed
- Accepted Release builds: production, test, and WinForms control projects each completed with 0 warnings and 0 errors
- Formatting and complete-diff verification: passed
- This acceptance-record update did not rerun builds or automated tests; it records the already accepted evidence.

## Accepted desktop evidence

- The WinForms application launched without administrator privileges. Start, Stop, Silent Mode, Clear History, and Open Logs Folder worked.
- Current title, artist, Firefox targeting state, and radio activity were readable without a PowerShell console.
- Activity reported the transition countdown, stability wait, local generation, Nicole preparation, prepared state, delivery, restoration, and resumed observation.
- Stop and window close awaited the authoritative protected cleanup path.
- Firefox ducking captured and restored the exact application-session volume and mute state. Chrome and Windows master volume remained untouched.
- Missing, stale, or unsafe targeting remained silent.
- Preserved console diagnostics remained usable.
- Firefox media ownership remains application-level: ordinary YouTube can become the tracked `firefox.exe` media session when it owns Firefox media. Zoya does not inspect pages or distinguish browser tabs.

## Accepted log evidence

- `radio-behavior-20260811-091603-715.txt` records three complete deliveries inside the two-second transition window, each with exact Firefox restoration from 100% to 25% and back to 100%, unmuted; it also records clean shutdown and no automatic pause/resume command.
- `radio-behavior-20260811-095014-238.txt` records the visible control-window sequence through a transition, the eight-second stability wait, local generation, Nicole preparation, and prepared material waiting for delivery. It does not establish a completed delivery or shutdown.

The procedure below is retained as the accepted regression procedure.

## Preserved regression procedure

### Before starting

1. Open YouTube Music in Firefox and start a song.
2. Close any older Zoya Radio window or console run.
3. Open the `ZoyaRadio` folder in File Explorer.
4. Click the address bar, type `powershell`, and press Enter.
5. Run:

```powershell
.\run-zoya-radio-control.cmd
```

The application should open without administrator privileges. It must not pause, resume, or toggle Firefox during normal automatic delivery.

### Test A - startup, status, and repeated Start

1. Confirm the window shows the current title, artist, a readable Firefox targeting state, and a **Radio activity** line.
2. Click **Start** once.
3. Confirm the status becomes Running, Start is no longer available while running, and Radio activity shows an observing countdown such as `next comment in 3 transitions`.
4. Click rapidly around the Start/Stop area only as the enabled controls allow. Confirm no duplicate windows, observers, announcements, or error loops appear.
5. Skip or wait through genuine track transitions. Confirm the countdown changes and, when preparation is due, Radio activity progresses through the applicable stability wait, local generation, Nicole preparation, and ready-for-transition phases.
6. Confirm music continues while Zoya prepares the comment and Nicole audio.
7. At delivery, confirm Radio activity reports target/audio validation, lowering Firefox volume, speaking, and restoring Firefox volume as those phases occur.
8. Confirm the announcement is delivered over ducked Firefox audio without pausing Firefox.
9. Confirm Firefox returns to exactly its previous volume and mute state afterward.

Detailed timestamped logs are still available through **Open Logs Folder**, but the normal scheduling/preparation check should now be visible directly in the window.

### Test B - Silent Mode and Clear History

1. While running, turn **Silent Mode** on before a scheduled delivery if practical.
2. Confirm Radio activity says Silent Mode, observation can continue, and no pending or stale announcement plays.
3. Turn Silent Mode off and confirm scheduling starts fresh with no catch-up announcement.
4. Click **Clear History**.
5. Confirm the app stays running and logs are not deleted. This button clears only the small process-local recent-track/comment context.

### Test C - Stop and protected cleanup

Repeat Stop in several representative phases when timing permits: during local AI generation, Nicole preparation, target acquisition, Firefox ducking, speech, and restoration. For each attempt:

1. Click **Stop**.
2. Confirm the window visibly says it is stopping or restoring and conflicting controls are disabled.
3. Confirm Stop completes without freezing the window.
4. If Firefox had already been ducked, confirm its exact previous volume and mute state are restored before the app reports Stopped.
5. Confirm Chrome, another application's volume, and Windows master volume never change.

It is fine if the short timing windows make a particular phase difficult to catch. Report which phase could not be exercised rather than pretending it passed.

### Test D - safe window close

1. Start the app again.
2. Close the window during ordinary observation.
3. Repeat during preparation, ducking, or speech if practical.
4. Confirm the window shows cleanup in progress and closes only after safe cleanup completes.
5. Confirm Firefox is never left ducked or muted.

If the app warns that Firefox volume may be uncertain, restore Firefox manually and keep the log for review.

### Test E - target safety and isolation

1. If convenient, play media in Chrome while Firefox music remains active.
2. Confirm Zoya continues to target only Firefox and never ducks or controls Chrome.
3. If Firefox is missing, not Playing, or Windows exposes an ambiguous Firefox target, confirm the app reports the condition and remains safely silent.
4. Confirm normal delivery never changes Windows master volume and never issues Firefox pause, play, resume, or toggle commands.

### Test F - logs and preserved console diagnostics

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

### What to bring back to the Work chat

Provide:

- the newest `radio-behavior-test-*.txt` file from `%LOCALAPPDATA%\Zoya Radio\logs`;
- a screenshot of the control window showing Radio activity while Running and, if possible, while preparing, speaking, or Stopping;
- which Stop/close phases you successfully exercised;
- whether Firefox's volume and mute state were restored exactly each time;
- confirmation that Chrome and Windows master volume stayed unchanged;
- any unreadable status, delay, duplicate action, freeze, unexpected pause, stale announcement, or crash;
- the exact time of any failure and what you clicked immediately before it.

This procedure supplied the accepted M8A desktop evidence. It does not authorize Milestone 8B; M8B requires a separate explicit implementation request.
