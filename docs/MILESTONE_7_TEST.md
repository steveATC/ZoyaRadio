# Milestone 7 manual acceptance - Nicole neural voice

Milestone 7 is implemented but not passed until these checks work on Steve's normal Windows desktop.

## Test 1 - one Nicole announcement

1. Play YouTube Music in Firefox.
2. Run:

   ```powershell
   .\run-spoken-announcement-test.cmd
   ```

3. Confirm the screen says the preferred provider is `Kokoro CPU (af_nicole)`.
4. Check the displayed title and artist, then answer **Y**.
5. Confirm music keeps playing during `NEURAL TTS SYNTHESIS STARTED`.
6. Confirm Firefox pauses only after the log says the neural audio is verified and the model dispose was requested.
7. Confirm Nicole speaks and Firefox resumes automatically.
8. Look for `PAUSE VERIFIED`, `SPEECH COMPLETED`, and `RESUME VERIFIED`.

## Test 2 - automatic radio behavior

1. From the ZoyaRadio folder, run:

   ```powershell
   .\run-radio-behavior-test.cmd
   ```

2. Arm it only after verifying it selected the correct `firefox.exe` session.
3. Let at least **three** automatic comments occur.
4. Confirm the existing random 2-5 genuine-track-transition scheduling still works.
5. For each comment, confirm AI and Nicole preparation happen while music keeps playing, followed by one pause, Nicole speech, and automatic resume.
6. Confirm no unrelated media session is controlled.

## Test 3 - Silent Mode

1. While the automatic test is armed, press **S**.
2. Change enough tracks that a comment would normally become due.
3. Confirm no AI generation, speech preparation, pause, or speech begins.
4. Press **S** again and confirm scheduling restarts with a fresh interval.

## Test 4 - automatic Windows fallback

1. Start Firefox music Playing.
2. Run:

   ```powershell
   .\run-neural-tts-fallback-test.cmd
   ```

3. This safely simulates Nicole being unavailable; it does not damage or rename the model.
4. Confirm the log says `SIMULATED NEURAL TTS INITIALIZATION FAILURE` and `AUTOMATIC SPEECH FALLBACK ACTIVE`.
5. Verify the displayed metadata and answer **Y**.
6. Confirm the familiar Windows voice speaks and Firefox resumes.

## Test 5 - explicit Windows troubleshooting mode

Run:

```powershell
.\run-spoken-announcement-test.cmd --windows-tts
```

Confirm no neural initialization occurs and the log says `explicit Windows TTS`.

## Test 6 - Ctrl+C recovery

1. Start a normal Nicole announcement and answer **Y**.
2. After Nicole actually begins speaking, press **Ctrl+C**.
3. Confirm Firefox is not left Paused and the log contains `RESUME VERIFIED`.

If Nicole finishes too quickly, repeat rather than claiming the test passed.

## Optional checks

- Run `.\run-radio-behavior-test.cmd --no-ai` to confirm fixed-template comments still use Nicole without contacting Ollama.
- If Cyrillic metadata appears naturally, confirm the log preserves it. Natural-sounding output is encouraging, but do not claim verified Russian accuracy without a Russian speaker.
- If another application exposes a media session, confirm only `firefox.exe` is paused and resumed.

## What to bring back to the Work chat

If everything passes, report:

- three Nicole automatic announcements completed;
- scheduling and Silent Mode still worked;
- the forced Windows fallback worked;
- Ctrl+C restored Firefox;
- whether Nicole is materially better than Windows TTS in the real radio.

If anything fails, provide:

1. the newest `logs\radio-behavior-test-*.txt` or `logs\spoken-announcement-test-*.txt`;
2. a screenshot of the obvious error or manual-resume warning;
3. the title and artist shown at the time;
4. whether Firefox ended Playing or Paused;
5. whether Nicole, Windows TTS, silence, or distorted audio was heard.

The automated resource log is `logs\neural-tts-diagnostic-20260808-001817-018.txt`.
