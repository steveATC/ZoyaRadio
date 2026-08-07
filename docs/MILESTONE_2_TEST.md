# Milestone 2 manual test

Milestone 2 is a one-announcement mechanism test. It uses an installed local Windows voice, temporarily pauses only the single `firefox.exe` media session, speaks the displayed title and artist once, and restores that same session. It does not use AI or the internet.

Each run writes a Unicode text log under `logs` named `spoken-announcement-test-YYYYMMDD-HHMMSS-fff.txt`.

## Before testing

1. Open YouTube Music in Firefox.
2. Start playing a song.
3. Open the `ZoyaRadio` folder in File Explorer.
4. Click the address bar, type `powershell`, and press Enter.

The tester will list installed Windows voices, all available media sessions, the selected `firefox.exe` session, and the exact proposed sentence. It must refuse to continue if there is no usable voice, no Firefox session, more than one Firefox session, missing title or artist, or Firefox is not Playing.

## Test A - safe refusal

1. With YouTube Music playing, enter:

   ```powershell
   .\run-spoken-announcement-test.cmd
   ```

2. Check that the displayed title and artist match the song.
3. At `Speak this announcement and temporarily pause Firefox? [y/N]`, press Enter.
4. Confirm the music never pauses and no speech occurs.
5. Confirm the log says `User confirmation: NO` and `no playback or speech commands were issued`.

## Test B - normal spoken announcement

Repeat this test on at least three different tracks.

1. Start with YouTube Music Playing.
2. Run `.\run-spoken-announcement-test.cmd`.
3. Check that `SourceAppUserModelId` is `firefox.exe` and the title and artist are correct. Album may be `<not provided>`.
4. Check the proposed sentence, then type `y` and press Enter.
5. Confirm Firefox music pauses.
6. Confirm the selected Windows voice audibly says `That was [title] by [artist].`
7. Confirm Firefox resumes automatically after speech ends.
8. Confirm the output and log contain `PAUSE VERIFIED`, `SPEECH COMPLETED`, `RESUME VERIFIED`, and `ANNOUNCEMENT TEST RESULT: PASS`.

The ordinary Windows voice does not need to sound natural or like the future Zoya persona.

## Test C - starting Paused

1. Manually pause YouTube Music in Firefox.
2. Run `.\run-spoken-announcement-test.cmd`.
3. Confirm it reports that Firefox is Paused and refuses to continue.
4. Confirm it does not speak and does not start the music.

## Test D - simulated speech failure

This mandatory test deliberately skips speech so recovery can be proven safely.

1. Start YouTube Music Playing.
2. Enter:

   ```powershell
   .\run-simulated-speech-failure-test.cmd
   ```

3. Check the displayed Firefox title and artist, type `y`, and press Enter.
4. Confirm Firefox pauses, the intentional failure is reported, and Firefox resumes automatically.
5. Confirm no announcement is spoken.
6. Confirm the log contains `SIMULATED SPEECH FAILURE`, `Protected recovery needed: YES`, `RESUME VERIFIED`, `PLAYBACK RECOVERY: PASS`, and `SIMULATED SPEECH-FAILURE TEST RESULT: PASS`.

## Test E - Ctrl+C during speech

1. Start YouTube Music Playing and run `.\run-spoken-announcement-test.cmd`.
2. Check the title and artist, then type `y` and press Enter.
3. As soon as speech begins, press Ctrl+C once.
4. Confirm speech stops and Firefox is not left Paused.
5. Confirm the log records `SPEECH CANCELLED`, `RESUME VERIFIED`, and `PLAYBACK RECOVERY: PASS`.

The sentence may finish too quickly to catch. If that happens, try again on a track with a longer title or artist. Report that timing issue honestly; it is not necessary to alter Windows settings.

## Test F - Unicode observation, if convenient

If a track with Cyrillic or other non-Latin title or artist happens to play:

1. Run the normal announcement test.
2. Open its text file in `logs` and confirm the real characters are present.
3. Note whether the Windows voice pronounces them sensibly.

Poor pronunciation by an English-language voice is not a failure. Question marks replacing the real characters in the log or spoken input are a failure. Question marks visible only in the PowerShell window may be a cosmetic console-rendering issue.

## If anything fails

Manually resume YouTube Music immediately if it is still Paused. Then bring these items back to the Work chat:

- the newest `spoken-announcement-test-*.txt` file from `logs`;
- a screenshot of the full console window, especially any large resume warning;
- which test letter failed;
- what the music did, what speech you heard, and whether Firefox ended Playing or Paused;
- the song title and artist shown by the tester;
- for voice trouble, the `AVAILABLE ENABLED WINDOWS VOICES` and `Selected voice` lines.

Do not include browser cookies, account details, URLs, or credentials.
