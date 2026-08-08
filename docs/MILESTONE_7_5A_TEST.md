# Milestone 7.5A manual acceptance - PASSED

Steve completed this isolated volume-safety test successfully. A 50.0% Firefox application volume ducked to 12.5% and restored exactly to 50.0%; normal, Ctrl+C, and simulated-failure restoration passed; Chrome and Windows master volume were unaffected; and Steve described the ramp as natural.

Windows grouped multiple audible Firefox tabs into the same application session, so they ducked together. That application-level isolation is accepted for Phase 1. The procedure is preserved below for troubleshooting and repeatability.

The probe changes only the selected Firefox **application-session** volume. It must never pause the music, change the Windows master volume, or affect another application.

## Start with the read-only check

1. Play YouTube Music in Firefox.
2. Open PowerShell in the `ZoyaRadio` folder.
3. Run:

   ```powershell
   .\run-audio-ducking-test.cmd --enumerate-only
   ```

4. Confirm it reports exactly one active `firefox.exe` candidate.
5. Confirm the final lines say `READ-ONLY ENUMERATION COMPLETE` and `VOLUME COMMAND ISSUED: NO`.

If it reports zero or multiple active Firefox candidates, do not proceed with the changing tests. Bring the new log back to the Work chat.

## Test A - normal duck

1. Keep YouTube Music playing in Firefox.
2. Run:

   ```powershell
   .\run-audio-ducking-test.cmd
   ```

3. Verify the target says `firefox.exe` and shows its current per-session volume.
4. At `Temporarily duck this exact Firefox audio session? [y/N]`, type **Y** and press Enter.
5. Firefox should smoothly become much quieter, remain quiet for about three seconds, and smoothly return to precisely the previous level.
6. The music must remain Playing throughout; it must never pause.
7. Look for `DUCK VERIFIED`, `RESTORATION VERIFIED`, and `AUDIO DUCKING TEST RESULT: PASS`.

Pressing Enter without typing Y is also safe: the test must issue no volume command.

## Test B - unrelated audio isolation

1. If convenient, make another application such as Chrome, VLC, Spotify, or a game produce audible sound at the same time.
2. Keep Firefox/YouTube Music playing too.
3. Run the normal command and answer **Y**.
4. Only Firefox should become quieter.
5. The other application and the Windows master-volume indicator must remain unchanged.

If Windows does not expose the other application as a separate session, report that honestly rather than forcing it.

## Test C - non-100% original Firefox volume

1. Open Windows **Volume mixer** from the speaker/sound settings.
2. Set Firefox's application volume to a clearly recognizable value below 100%, such as about 60%.
3. Start YouTube Music playing and run the normal duck test.
4. The log should capture approximately 60%, duck to approximately 15% (25% of 60%), then restore approximately 60%.
5. Check the Volume mixer afterward. Firefox must not be reset to 100% or left at the ducked value.

## Test D - Ctrl+C recovery

1. Start the normal test and answer **Y**.
2. As soon as `DUCK VERIFIED` appears and Firefox is quiet, press **Ctrl+C** once.
3. Firefox's volume must return to the exact captured level before the program exits.
4. Look for `AUDIO DUCKING CANCELLED`, `RESTORATION VERIFIED`, and `CANCELLATION RECOVERY: PASS`.

The quiet window is short. Repeat the test if it finishes before you can press Ctrl+C.

## Test E - simulated failure recovery

1. Start YouTube Music playing.
2. Run:

   ```powershell
   .\run-audio-ducking-test.cmd --simulate-duck-failure
   ```

3. Verify the Firefox target and answer **Y**.
4. The probe deliberately fails immediately after verifying the duck.
5. Firefox must still return to its captured volume.
6. Look for `EXPECTED SIMULATED FAILURE`, `RESTORATION VERIFIED`, and `SIMULATED-FAILURE RECOVERY: PASS`.

The deliberate operation failure is expected. Failure to restore the volume is not.

## Test F - ambiguous Firefox sessions, if practical

1. If convenient, make another Firefox source produce audio at the same time.
2. Run the read-only command again.
3. Record what Windows exposes.
4. If Windows exposes more than one active Firefox audio session, the probe must print `AMBIGUOUS FIREFOX AUDIO TARGET` and issue no volume command.

Firefox may combine multiple tabs or processes into one Windows audio session. This milestone does not claim tab-level isolation.

## What to bring back to the Work chat

If all applicable checks pass, report:

- normal duck was clearly audible and music never paused;
- the exact non-100% starting level returned correctly;
- unrelated application and Windows master volume were unchanged;
- Ctrl+C restored Firefox;
- simulated failure restored Firefox;
- what Windows exposed during the ambiguity attempt.

Provide the relevant newest files from:

```text
logs\audio-ducking-test-*.txt
```

If anything fails, also provide:

1. a screenshot of the warning or final result;
2. whether Firefox ended at the original, ducked, muted, or some other level;
3. whether Firefox music ever paused;
4. whether any unrelated application's sound changed;
5. whether the Windows master-volume indicator changed.
