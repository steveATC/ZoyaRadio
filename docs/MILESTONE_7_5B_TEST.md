# Milestone 7.5B manual acceptance - transition-aware delivery

Milestone 7.5B **PASSED Steve's real-desktop manual acceptance on August 10, 2026**. The normal path prepares a Track-A comment while music plays, then ducks Firefox and speaks over Track B. It never hard-pauses the song.

## Accepted desktop evidence

The focused Firefox-plus-Chrome run is recorded locally in `logs\radio-behavior-test-20260810-131308-866.txt`.

- Firefox transitions remained observable while Chrome/YouTube activity caused global GSMTC churn.
- Fifty-nine harmless Firefox wrapper refresh diagnostics were classified as the same logical session. They did not cancel AI generation, Nicole synthesis, prepared material, active speech, or the current scheduling interval; no false replacement or reacquisition was logged.
- Two prepared announcements completed successfully, including wrapper refreshes during both active announcements.
- Firefox Core Audio PID 11568 / session `7D62879DB482` was the sole duck target. Both deliveries captured 100%, ducked to 25%, and restored exactly to 100%.
- Chrome was not ducked or controlled, normal delivery did not pause Firefox, and Ctrl+C produced a clean shutdown.
- The earlier 20:49 observer stall was not reproduced. It remains an intermittent known observation rather than a confirmed fixed defect.
- Wrapper-refresh diagnostics are excessively repetitive. That is a non-blocking deferred logging-quality issue; logging behavior was deliberately unchanged for acceptance.

The checks below remain as the regression procedure for future changes.

Before starting, play YouTube Music in Firefox. Open the `ZoyaRadio` folder in File Explorer, click its address bar, type `powershell`, and press Enter.

## Test 1 - three normal transitions

1. Run:

   ```powershell
   .\run-radio-behavior-test.cmd
   ```

2. Check that the displayed target is `firefox.exe`, then type **Y** and press Enter. Enter by itself means NO and changes nothing.
3. Let tracks change normally until Zoya has made at least three automatic comments. The random interval is 2-5 genuine transitions, so this can take a while.
4. For each comment, confirm:
   - Track A keeps playing while AI and Nicole audio are prepared;
   - Nicole begins at or very near the Track A -> Track B change;
   - Track B keeps playing quietly underneath her and is never paused;
   - Firefox returns smoothly to exactly its previous volume;
   - the timing feels materially less abrupt than the old mid-song pause.
5. In the console/log, a successful opportunity should include `AI COMMENT READY`, `NICOLE AUDIO READY`, `TRACK A -> TRACK B TRANSITION OBSERVED`, `DUCK VERIFIED`, `SPEECH COMPLETED`, `RESTORATION VERIFIED`, and `AUTOMATIC TRANSITION RESULT: COMPLETED`.

The delivery window is deliberately only two seconds after Track B is observed. If preparation or safety checks are late, silence is correct; the log should explain why the opportunity was abandoned.

## Test 2 - non-100% Firefox volume

1. Open Windows **Volume mixer** and set Firefox to approximately 50%.
2. Keep the automatic test running until a scheduled comment occurs.
3. Confirm Firefox ducks proportionally to about 12.5% and returns to about 50%, never 100%.
4. Check that the log's `ORIGINAL STATE CAPTURED` and `RESTORATION VERIFIED` values match the delivery-time level.

## Test 3 - rapid skip and stale material

1. Run and arm the normal test.
2. When the log says `OPPORTUNITY SCHEDULED` or preparation has started, rapidly skip Track A to B and then C.
3. Confirm no stale Track-A comment barges in several seconds later or on the wrong track.
4. Look for `PENDING COMMENT ABANDONED`, `PREPARATION ABANDONED`, or `TRANSITION DELIVERY ABANDONED` with a readable reason.

## Test 4 - Silent Mode

1. Run and arm the normal test.
2. After `OPPORTUNITY SCHEDULED` appears but before the next transition delivery, press **S** once.
3. Confirm the console says `SILENT MODE: ON`.
4. Confirm there is no delayed surprise comment and Firefox volume remains unchanged. If ducking had already started, it must restore before silence.
5. Press **S** again only if you want to continue; a fresh 2-5-transition interval begins.

## Test 5 - unrelated application audio

1. If convenient, play audible media in Chrome or another separate application while Firefox also plays.
2. Allow one automatic Firefox comment.
3. Confirm only Firefox becomes quieter. Chrome/the other application and Windows master volume must not change.

Windows combines multiple audible Firefox tabs into the same Firefox application session on Steve's PC. Those Firefox tabs ducking together is expected and accepted; it is not a failed test. Zoya never changes YouTube Music's in-page volume control.

## Test 6 - Ctrl+C restoration

1. Run and arm the normal test.
2. When `DUCK VERIFIED` or `SPEECH STARTED` appears, press **Ctrl+C** once.
3. Confirm Nicole stops and Firefox returns to the exact captured volume before the program exits.
4. Look for cancellation plus `RESTORATION VERIFIED` and `AUTOMATIC RADIO: STOPPED CLEANLY`.

The timing window is short. Repeat if speech finishes before Ctrl+C is registered.

## Test 7 - deliberate post-duck failure

1. Start a fresh run with:

   ```powershell
   .\run-radio-behavior-test.cmd --simulate-transition-failure
   ```

2. Verify the target, type **Y**, and wait for the first scheduled delivery.
3. The first delivery deliberately fails after the duck is verified and produces no speech for that opportunity.
4. Confirm Firefox still restores exactly.
5. Look for `SIMULATED FAILURE`, `EXPECTED SIMULATED FAILURE`, and `RESTORATION VERIFIED`. The automatic result may say failed because the announcement intentionally failed; restoration must pass.

## Optional zero-AI regression

Run:

```powershell
.\run-radio-behavior-test.cmd --no-ai
```

It should use the fixed title/artist announcement without contacting Ollama, while keeping the same transition duck/speak/restore behavior.

## Stop and bring back evidence

Press **Ctrl+C** once to stop a normal run. Bring the newest relevant files from:

```text
logs\radio-behavior-test-*.txt
```

Please report:

- whether at least three normal comments landed close to real transitions;
- whether Track B kept playing underneath Nicole;
- whether Nicole was clearly audible and the timing felt natural;
- the captured/ducked/restored values from the 50% test;
- whether rapid skips stayed silent rather than producing a late comment;
- whether Silent Mode prevented delayed delivery;
- whether Chrome/other audio and Windows master volume were unchanged;
- whether Ctrl+C and simulated failure restored Firefox exactly.

If anything fails, also include a screenshot of the console, the exact track transition involved, whether Firefox ever paused, and the Firefox volume shown in Windows Volume mixer afterward.
