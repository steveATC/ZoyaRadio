# Milestone 5 automatic radio-behavior test

Milestone 5 is the first long-running automatic test. It can pause and resume Firefox after you explicitly arm it. It is not yet the finished Zoya Radio application.

Every run writes a Unicode log under `logs` named `radio-behavior-test-YYYYMMDD-HHMMSS-fff.txt`.

## Before starting

1. Open YouTube Music in Firefox and start a song.
2. Open the `ZoyaRadio` folder in File Explorer.
3. Click the File Explorer address bar, type `powershell`, and press Enter.

## A. Default-NO safety

1. Run:

   ```powershell
   .\run-radio-behavior-test.cmd
   ```

2. At `Enable automatic Zoya Radio behavior? [y/N]`, press **Enter**.
3. Confirm the program reports `AUTOMATIC RADIO NOT ARMED`.
4. Confirm the music never pauses and no speech occurs.

## B. Arm and check automatic frequency

1. Run the same command again.
2. Verify the displayed `firefox.exe`, title, artist, and Playing state are correct.
3. Type `y` and press Enter.
4. The log will show a random target such as `Next comment target: after 4 observed track transitions.`
5. Let tracks change or manually skip tracks to make the test quicker. A manual skip is acceptable; M5 cannot distinguish it from a natural transition.
6. Verify each genuine title/artist change advances the counter once. Repeated unchanged Windows events must not advance it.
7. Verify each comment opportunity occurs only when the logged counter reaches its random target between 2 and 5.
8. After a completed or abandoned opportunity, verify a fresh 2-5 target appears.

## C. Track-age and stale-track safety

For every automatic comment, inspect the console or log:

- generation must start while Firefox is still Playing;
- Zoya may log a minimum-age wait;
- `PLAYBACK REVALIDATION: PASS` must appear before `PAUSE REQUESTED`;
- the current track must have reached the configured eight-second age;
- if another track starts during generation or waiting, the pending comment must say it was abandoned and must not pause the new track.

Do not perform difficult timing tricks. If a natural/quick skip happens during generation, simply save that log as useful evidence.

## D. Three normal automatic comments

Collect at least three successful automatic comments. For each one, verify:

1. music kept playing while Ollama generated the text;
2. the selected model was unloaded;
3. the expected Firefox session and current track were revalidated;
4. `PAUSE VERIFIED` appeared;
5. the comment was spoken;
6. `SPEECH COMPLETED` appeared;
7. `RESUME VERIFIED` appeared;
8. Firefox continued Playing afterward.

The comment should primarily react to the previously playing track. It may use the current track only as transition context.

## E. Silent Mode

1. While armed, press **S** once.
2. Confirm the log says `SILENT MODE: ON`.
3. Cross several track transitions.
4. Confirm there is no new AI generation, automatic pause, or speech.
5. Press **S** again.
6. Confirm `SILENT MODE: OFF` and a fresh 2-5 transition target appear.
7. Confirm Zoya does not immediately dump missed comments.

If you press **S** while Zoya has Firefox paused for speech, she should cancel speech and run protected playback recovery. Firefox must not remain Paused.

## F. Clear recent history

1. Preferably while no comment is speaking, press **C**.
2. Confirm `RECENT HISTORY CLEARED` appears.
3. Confirm the bounded history shows zero tracks and zero accepted AI comments until new transitions arrive.
4. Confirm a fresh target appears.
5. Confirm pressing **C** itself does not pause the music or start speech.
6. The current diagnostic log remains on disk; **C** does not delete logs.

## G. Repeated artist observation

If convenient, play two or more tracks whose Windows artist text matches exactly apart from capitalization or harmless whitespace.

Confirm a line such as this is mechanically correct:

```text
TRUSTED LISTENING OBSERVATION: The exact artist string 'Bee Gees' has appeared 2 times in the last 6 observed tracks.
```

`Artist` and `Artist feat. X` must not be merged. Zoya is not required to make a repeated-artist joke.

## H. No-AI mode and clean shutdown

Optionally test the zero-GPU mode:

```powershell
.\run-radio-behavior-test.cmd --no-ai
```

It should keep the same 2-5 scheduling and safe playback sequence but use `That was [title] by [artist].` without contacting Ollama.

For either mode, press **Ctrl+C** to stop. If shutdown occurs while Zoya has paused Firefox, protected recovery must restore Playing before exit.

## What to bring back

Bring the following to the Work chat:

- the default-NO `radio-behavior-test-*.txt` log;
- the main armed log containing at least three successful automatic comments;
- any log showing a stale-track abandonment;
- the Silent Mode and clear-history evidence;
- the optional no-AI log, if tested;
- a screenshot of the full console and a note saying whether Firefox ended Playing or Paused if anything fails.

Report a failure if any comment occurs before its 2-5 threshold, a new track is paused immediately, two pipelines overlap, Silent Mode still generates/speaks/pauses, history exceeds its bounds, an unrelated session is controlled, or Firefox is ever stranded Paused.

Do not share cookies, browser URLs, account details, passwords, tokens, or unrelated personal information.
