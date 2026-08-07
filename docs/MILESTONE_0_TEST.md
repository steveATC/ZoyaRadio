# Milestone 0 manual test

This test checks what Windows receives from Firefox. The Zoya Radio probe is read only and must not change playback.

1. Open YouTube Music in Firefox and start playing a song.
2. Open the `ZoyaRadio` folder in File Explorer.
3. Click File Explorer's address bar, type `powershell`, and press Enter.
4. In the window that opens, type `.\run-media-probe.cmd` and press Enter.
5. Confirm the startup text says `READ ONLY`.
6. Find the session whose raw `SourceAppUserModelId` refers to Firefox. Confirm:
   - the title matches YouTube Music;
   - the artist matches YouTube Music;
   - the album matches if Windows provides it;
   - an absent album says `<not provided>` instead of showing a guess.
7. Pause and resume the song using YouTube Music itself. Confirm the probe prints readable `PLAYBACK` changes. The probe must not affect playback.
8. Wait for or manually choose at least **three different song transitions**. Confirm each new title/artist/album combination produces one clear `TRACK CHANGED` section with the new metadata.
9. Click the probe window and press **Ctrl+C** once. Confirm it reports a clean shutdown.

## If something fails

Do not include Firefox cookies, browser history, account details, or screenshots of private browser pages.

Copy or show the Work chat:

1. all text visible in the probe window from startup through the failure;
2. the newest `media-probe-*.txt` file from the `logs` folder;
3. which numbered step failed;
4. what you expected and what actually happened;
5. whether the music kept playing normally or the probe unexpectedly affected it.
