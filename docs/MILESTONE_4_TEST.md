# Milestone 4 host-persona manual test

Milestone 4 tests Zoya's written personality. It does not change the Windows voice, add radio scheduling, or add song research. Ollama still receives only the title and artist as music facts.

Every run writes a Unicode log under `logs` named `ai-comment-test-YYYYMMDD-HHMMSS-fff.txt`.

## Generate-only personality review

1. Open YouTube Music in Firefox and play a song.
2. Open the `ZoyaRadio` folder in File Explorer.
3. Click the address bar, type `powershell`, and press Enter.
4. For each sample, run:

   ```powershell
   .\run-ai-comment-test.cmd --generate-only
   ```

Collect at least ten samples across varied title wording. If convenient, include `Stayin Alive`, `Rasputin`, `You're Beautiful`, `Cochise`, `Cocaine Blues`, a title likely to tempt outside trivia, and a Unicode title.

For each actual AI comment, check that it:

- is 15-40 words;
- sounds conversational, warm, dry, clever, or playfully deadpan rather than clinical;
- sometimes produces a genuinely amusing line;
- varies its construction rather than repeating one template;
- uses `Steve` naturally and only occasionally, not in most comments;
- uses sarcasm or teasing without becoming hostile or obnoxious;
- does not sound like a commercial DJ, assistant, critic, or corporate message;
- contains no radio cliché or empty unsupported praise;
- makes no factual claim about sound, genre, lyrics, history, biography, geography, culture, popularity, or other unsupplied information.

Clearly imaginary wording is allowed. For example, `I'm picturing...` is safe when it is obviously Zoya inventing a scene from the title. Presenting that imagined scene as something the real song contains is not safe.

An exact model `FALLBACK` is also safe. The log should show `AI SAFE FALLBACK REQUESTED`, followed by the fixed `That was [title] by [artist].` text. The literal word `FALLBACK` must never become the final spoken text.

Milestone 4 fails manual review if the samples remain mostly sterile title analysis, lack a recognizable personality, repeat the same structure, use Steve's name constantly, become obnoxious, fall into radio clichés, or invent song/artist facts.

## Optional spoken checks

After reviewing the generated text, perform one or two spoken tests:

```powershell
.\run-ai-comment-test.cmd
```

1. Confirm music continues while Zoya generates the comment.
2. Read the final text before answering.
3. Type `y` only when the comment is safe.
4. Confirm Firefox pauses only after confirmation, the displayed text is spoken, and the same Firefox session resumes.
5. Confirm the log contains `PAUSE VERIFIED`, `SPEECH COMPLETED`, `RESUME VERIFIED`, and `AI COMMENT TEST RESULT: PASS`.

Voice femininity or naturalness is not part of this milestone. Microsoft David remains the existing test voice.

## What to bring back

Bring these items to the Work chat:

- all ten or more new generate-only `ai-comment-test-*.txt` logs;
- the one or two optional spoken logs, if performed;
- your three favorite comments;
- any comments that felt sterile, repetitive, obnoxious, overly familiar, or factually unsupported;
- a note on whether use of `Steve` and mild swearing felt occasional and natural.

If playback or speech behaves differently from Milestone 3, also include a full-console screenshot and say whether Firefox ended Playing or Paused. Do not share cookies, browser URLs, account details, passwords, or tokens.
