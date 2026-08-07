# Milestone 1 manual test

This test temporarily pauses and resumes one exact `firefox.exe` Windows media session. The ordinary `run-media-probe.cmd` remains read only.

Open the `ZoyaRadio` folder in File Explorer, click the address bar, type `powershell`, and press Enter. Start each control-test run with:

```powershell
.\run-playback-control-test.cmd
```

## Test A - cancel before control

1. Play YouTube Music in Firefox.
2. Start the playback-control tester.
3. Confirm it displays `firefox.exe` and the correct title and artist.
4. At the `[y/N]` question, press Enter or type `n`.
5. Confirm the music is never interrupted and the tester says no playback commands were issued.

## Test B - normal pause and resume

1. Keep YouTube Music playing in Firefox and start the tester.
2. Confirm the displayed `firefox.exe`, title, and artist match the song actually playing. Album may say `<not provided>`.
3. Type `y` and press Enter.
4. Confirm Firefox pauses, stays paused for roughly three seconds, and resumes automatically.
5. Confirm the tester reports `PAUSE VERIFIED`, `RESUME VERIFIED`, and `CONTROL TEST RESULT: PASS`.
6. Confirm the music continues normally afterward.
7. Repeat this normal test at least **five times**, using at least two different songs if possible.

## Test C - refuse an already paused session

1. Manually pause YouTube Music yourself.
2. Start the tester.
3. Confirm it reports that Firefox is Paused and refuses to run the control sequence.
4. Confirm it does not start the music by itself.

## Test D - Ctrl+C recovery

1. Start YouTube Music playing, run the tester, verify the target, and answer `y`.
2. As soon as `PAUSE VERIFIED` appears, press Ctrl+C during the three-second pause.
3. Confirm the tester still attempts to resume that same Firefox session before exiting.
4. Confirm YouTube Music ends in Playing rather than being stranded Paused.
5. If the timing is difficult, repeat the test instead of changing the program.

## Test E - unrelated media isolation

If convenient, play media in another normal application such as Chrome, Edge, VLC, or Spotify so Windows may expose a second media session.

1. Keep YouTube Music playing in Firefox and start the tester.
2. Confirm it lists the available sessions and selects only `firefox.exe`.
3. Run the confirmed control test.
4. Confirm only Firefox pauses and resumes; the unrelated session must not be controlled.
5. If Windows does not expose the other application as a separate session, report that honestly and do not try to force it.

## If anything fails

If the tester says it could not confirm Firefox resumed, resume YouTube Music manually first.

Then provide the Work chat with:

1. all tester-window text from startup through the result;
2. the newest `playback-control-test-*.txt` file from the `logs` folder;
3. which test and step failed;
4. what you expected and what happened;
5. whether Firefox ended Playing or Paused;
6. whether any unrelated media session was affected.
