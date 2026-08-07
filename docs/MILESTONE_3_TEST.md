# Milestone 3 grounding-correction manual test

Milestone 3 creates one short comment with a local Ollama model. Music keeps playing while the comment is generated. Only after the finished text is displayed and you answer `y` may the tester pause the exact `firefox.exe` session, speak once, and restore it.

The first manual review verified the engineering mechanisms but found unsupported facts in several AI comments. Milestone 3 has not passed. This retest concentrates on factual grounding; it does not add a Zoya persona.

Every run writes a Unicode log under `logs` named `ai-comment-test-YYYYMMDD-HHMMSS-fff.txt`.

## Before testing

1. Open YouTube Music in Firefox and play a song.
2. Open the `ZoyaRadio` folder in File Explorer.
3. Click the File Explorer address bar, type `powershell`, and press Enter.

The model is local: `qwen3:4b-instruct` through `http://localhost:11434/api/`. No API key or cloud service is used.

## Required retest - generate-only grounding

Run this command separately for each test track:

```powershell
.\run-ai-comment-test.cmd --generate-only
```

Retest these previously problematic title/artist pairs when available:

1. `Loituma - Ievan Polkka (cover) | Lorelai` - `LorelaiMusic`
2. `Cocaine Blues` - `Johnny Cash`
3. `Du Riechst so Gut` - `Russian Style Folkestra`
4. `Rasputin` - `Boney M.`
5. `Stayin Alive` - `Bee Gees`
6. `Dai Dai` - `Shakira & Burna Boy`
7. `Amos Moses` - `Jerry Reed`

If one is not conveniently available, substitute another well-known song likely to trigger remembered knowledge. The tester may take up to about 20 seconds. It must never pause music or speak in generate-only mode.

Two outcomes are safe:

- A 15-40 word AI comment that talks only about the literal title or artist-name wording.
- `FINAL TEXT SOURCE: FIXED FALLBACK` accompanied by `AI SAFE FALLBACK REQUESTED`. The displayed final text must be `That was [title] by [artist].`, not the literal word `FALLBACK`. This is a successful result.

For every actual AI comment, check that it:

- is 15-40 words;
- is concise and contains no generic radio cliche;
- contains no song lyric;
- makes no claim about lyrics, story, genre, release, album, awards, biography, geography, trivia, sound, vocals, instruments, rhythm, energy, or artist tendencies;
- does not explain that it is an AI.

Specific rejection checks:

- No `Finnish folk classic` or other origin/genre inference.
- No comparison with Johnny Cash's `usual tales` or other artist history.
- No claim that an artist name proves nationality, origin, ensemble type, or polka style.
- No `velvet-voiced`, `soulful`, `smooth`, or other description of unheard audio.
- No suit, dance-floor, energy, rhythm fusion, global flair, Texas porch, or similar remembered/cultural scene-setting.
- For `Amos Moses`, no alligators, Louisiana, plot, lyrics, genre, or gratuitous geography.

Facts are forbidden here even when they happen to be correct. If any appear, Milestone 3 grounding still fails.

## Required spoken regression

After the generate-only comments look safe, run one normal test:

```powershell
.\run-ai-comment-test.cmd
```

Confirm music keeps playing during generation. Inspect the final text before typing `y`. After confirmation, confirm Firefox pauses, the displayed final text is spoken, and Firefox resumes. If the model requested `FALLBACK`, confirm Windows speaks the fixed `That was...` sentence and never says the word `FALLBACK`.

## Previously passed mechanism checks

The checks below remain documented for regression use. They already passed manual review and need not all be repeated solely for this prompt correction unless something behaves differently.

### Safe refusal

1. Run `.\run-ai-comment-test.cmd` with music Playing.
2. At the confirmation prompt, press Enter.
3. Confirm generation may already have happened, but music never pauses and no speech occurs.
4. Confirm the log says `User confirmation: NO` and that no playback or speech commands were issued.

### Simulated AI failure and fallback

```powershell
.\run-ai-comment-test.cmd --simulate-ai-failure
```

1. Confirm the log says `SIMULATED AI FAILURE` and `FINAL TEXT SOURCE: FIXED FALLBACK`.
2. Confirm the displayed fallback is `That was [title] by [artist].`
3. Type `y` at the prompt.
4. Confirm the fallback is spoken and Firefox pauses/resumes normally.
5. Confirm `RESUME VERIFIED` and `AI COMMENT TEST RESULT: PASS` appear.

This mode intentionally makes no Ollama request, so it cannot accidentally load a model while testing fallback behavior.

### No-AI mode

```powershell
.\run-ai-comment-test.cmd --no-ai
```

1. Confirm the fixed fallback is displayed and `AI DISABLED: no Ollama request was made` appears.
2. Type `y` and confirm the proven pause/speech/resume sequence still works.
3. Confirm no model is loaded by this mode.

### Performance and unload

Run once:

```powershell
.\run-ai-comment-test.cmd --measure-ai
```

This mode never pauses or speaks. It generates once, temporarily keeps only `qwen3:4b-instruct` loaded, queries Ollama's loaded-model status, records available measured size/VRAM/context/timing/token values, then requests immediate unload and verifies the selected model disappeared. Values Ollama does not expose are labeled `UNAVAILABLE`; they are not guessed.

### Ctrl+C recovery

1. Run `.\run-ai-comment-test.cmd` with Firefox Playing.
2. After the final text appears, type `y`.
3. Once speech begins, press Ctrl+C once.
4. Confirm speech cancels and Firefox is not left Paused.
5. Confirm the log records `SPEECH CANCELLED`, `RESUME VERIFIED`, and `PLAYBACK RECOVERY: PASS`.

If the speech finishes too quickly, try a track with a longer title or artist and report the timing honestly.

## What to bring back to the Work chat after the correction retest

Bring these files from `logs`:

- the seven new generate-only `ai-comment-test-*.txt` logs, or the available-title substitutions;
- the new normal spoken-regression log;
- a short note saying which generated comments you accepted, which were fixed fallbacks, and which—if any—you rejected as unsupported.

If anything fails, also bring a screenshot of the full console and say:

- which test letter failed;
- the displayed title, artist, model, and final text source;
- whether music kept playing during generation;
- whether Firefox paused and whether it ended Playing or Paused;
- what speech you heard;
- any large warning or exact error shown.

If Firefox remains Paused, resume YouTube Music manually first. Do not share cookies, account details, browser URLs, passwords, or tokens.
