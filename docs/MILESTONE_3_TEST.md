# Milestone 3 manual test

Milestone 3 creates one short comment with a local Ollama model. Music keeps playing while the comment is generated. Only after the finished text is displayed and you answer `y` may the tester pause the exact `firefox.exe` session, speak once, and restore it.

Every run writes a Unicode log under `logs` named `ai-comment-test-YYYYMMDD-HHMMSS-fff.txt`.

## Before testing

1. Open YouTube Music in Firefox and play a song.
2. Open the `ZoyaRadio` folder in File Explorer.
3. Click the File Explorer address bar, type `powershell`, and press Enter.

The model is local: `qwen3:4b-instruct` through `http://localhost:11434/api/`. No API key or cloud service is used.

## Test A - generate-only quality

Run this on at least five different tracks:

```powershell
.\run-ai-comment-test.cmd --generate-only
```

The tester may take up to about 20 seconds. It must print a comment but never pause music or speak. For every comment, check that it:

- is 15-40 words;
- is concise and contains no generic radio cliche;
- contains no song lyric;
- makes no claim about lyrics, story, genre, release, album, awards, biography, geography, or trivia;
- does not explain that it is an AI.

Use at least one song the model might know. If convenient, use `Amos Moses` by `Jerry Reed`. With only that title and artist supplied, the comment must not mention alligators, Louisiana, plot, lyrics, release facts, or genre. If it does, factual grounding fails even if the claim is true.

## Test B - normal spoken AI comment

Repeat on at least three tracks:

```powershell
.\run-ai-comment-test.cmd
```

1. Confirm music continues while `AI GENERATION STARTED` is shown.
2. Check the displayed title, artist, final text, and `FINAL TEXT SOURCE: AI`.
3. At `Speak this comment and temporarily pause Firefox? [y/N]`, type `y` and press Enter.
4. Confirm Firefox pauses only now, the displayed comment is spoken, and Firefox resumes afterward.
5. Confirm the log contains `PAUSE VERIFIED`, `SPEECH COMPLETED`, `RESUME VERIFIED`, and `AI COMMENT TEST RESULT: PASS`.

## Test C - safe refusal

1. Run `.\run-ai-comment-test.cmd` with music Playing.
2. At the confirmation prompt, press Enter.
3. Confirm generation may already have happened, but music never pauses and no speech occurs.
4. Confirm the log says `User confirmation: NO` and that no playback or speech commands were issued.

## Test D - simulated AI failure and fallback

```powershell
.\run-ai-comment-test.cmd --simulate-ai-failure
```

1. Confirm the log says `SIMULATED AI FAILURE` and `FINAL TEXT SOURCE: FIXED FALLBACK`.
2. Confirm the displayed fallback is `That was [title] by [artist].`
3. Type `y` at the prompt.
4. Confirm the fallback is spoken and Firefox pauses/resumes normally.
5. Confirm `RESUME VERIFIED` and `AI COMMENT TEST RESULT: PASS` appear.

This mode intentionally makes no Ollama request, so it cannot accidentally load a model while testing fallback behavior.

## Test E - no-AI mode

```powershell
.\run-ai-comment-test.cmd --no-ai
```

1. Confirm the fixed fallback is displayed and `AI DISABLED: no Ollama request was made` appears.
2. Type `y` and confirm the proven pause/speech/resume sequence still works.
3. Confirm no model is loaded by this mode.

## Test F - performance and unload

Run once:

```powershell
.\run-ai-comment-test.cmd --measure-ai
```

This mode never pauses or speaks. It generates once, temporarily keeps only `qwen3:4b-instruct` loaded, queries Ollama's loaded-model status, records available measured size/VRAM/context/timing/token values, then requests immediate unload and verifies the selected model disappeared. Values Ollama does not expose are labeled `UNAVAILABLE`; they are not guessed.

## Test G - Ctrl+C recovery

1. Run `.\run-ai-comment-test.cmd` with Firefox Playing.
2. After the final text appears, type `y`.
3. Once speech begins, press Ctrl+C once.
4. Confirm speech cancels and Firefox is not left Paused.
5. Confirm the log records `SPEECH CANCELLED`, `RESUME VERIFIED`, and `PLAYBACK RECOVERY: PASS`.

If the speech finishes too quickly, try a track with a longer title or artist and report the timing honestly.

## What to bring back to the Work chat

Bring these files from `logs`:

- all five or more generate-only `ai-comment-test-*.txt` logs;
- all three normal spoken-test logs;
- the simulated-failure log;
- the no-AI log;
- the performance log;
- the Ctrl+C log.

If anything fails, also bring a screenshot of the full console and say:

- which test letter failed;
- the displayed title, artist, model, and final text source;
- whether music kept playing during generation;
- whether Firefox paused and whether it ended Playing or Paused;
- what speech you heard;
- any large warning or exact error shown.

If Firefox remains Paused, resume YouTube Music manually first. Do not share cookies, account details, browser URLs, passwords, or tokens.
