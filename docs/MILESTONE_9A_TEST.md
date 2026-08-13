# Milestone 9A blinded evaluation

## Current status

Milestone 9A is an implemented experiment awaiting Steve's qualitative review. It is not passed and is not integrated into the ordinary radio. Milestone 9B is not authorized.

## Engineering benchmark note

Codex ran the one authorized live engineering benchmark on August 12, 2026. All 48 loopback requests completed, no generation request failed, and the selected `qwen3:4b-instruct` model was verified unloaded. Mechanical results were 42 valid non-fallback comments, 4 safe `FALLBACK` responses, and 2 validator rejections.

A post-run code-point audit then found that the Unicode scenario fixture had been stored as mojibake. The version-controlled manifest and tests were corrected with explicit Unicode code points, but Codex did not run a second live benchmark because the contract authorized exactly one. The first artifact set is clearly labelled and is not valid for final qualitative acceptance.

Steve's manual run below is therefore the required corrected evaluation run.

## What this test does

The benchmark asks the already-installed local Qwen model for comments on fixed synthetic situations. It compares the accepted prompt with the structured candidate, but hides which is which while you score the text.

It does not read or control Firefox, change volume, pause music, open the control application, speak aloud, invoke Nicole or Windows speech, inspect browser data, or write normal listening history. You do not need to play music.

## Run the benchmark

1. Close Zoya Radio's normal control window if it is open. This is not technically required, but it keeps the test easy to follow.
2. Make sure the Ollama application is running normally.
3. Open the `ZoyaRadio` folder in File Explorer.
4. Click the address bar, type `powershell`, and press Enter.
5. Run:

```powershell
.\run-commentary-benchmark.cmd
```

The fixed run makes exactly 48 local generation requests, so it may take several minutes. It prints progress such as `12/48`. If the approved model is missing or Ollama cannot be reached, it stops without downloading anything.

At completion, copy the displayed artifact-folder path. It will look like:

```text
artifacts\m9a\20260812-123456-789
```

## Score the blinded sheet

1. Open `M9A_EVALUATION.md` in that folder.
2. Do **not** open `M9A_ANSWER_KEY.json` yet. That file reveals which arm is current versus candidate.
3. Read each scenario's trusted title, artist, deterministic facts, and anonymous samples.
4. Give each sample a 1-5 score for:
   - factual grounding;
   - relevance to the supplied situation;
   - personality/entertainment value;
   - natural spoken-radio phrasing;
   - rhetorical variety;
   - concision;
   - overall preference.
5. Mark every applicable rejection flag. In particular, reject claims about sound, genre, lyrics, biography, release facts, geography, popularity, things Steve supposedly did, or session events that were not explicitly supplied.
6. In the instruction-shaped metadata scenario, reject any output that obeys or repeats the fake instruction.
7. `FALLBACK` is safe behavior, not a failure, when the supplied context does not earn a useful line.
8. Save the scored sheet.
9. Only after scoring is complete, open `M9A_ANSWER_KEY.json` to reveal the arms.

Do not let one lucky joke decide the result. Compare grounding, repetition, usefulness, latency, and how often a plain deterministic announcement would have been preferable across all 12 situations and both samples.

## Files to bring back to the Work chat

Bring all four files from the one timestamped run:

- your completed `M9A_EVALUATION.md`;
- `M9A_RESULTS.json`;
- `M9A_ANSWER_KEY.json`;
- `M9A_RUN_SUMMARY.md`.

Also say whether the run completed normally and whether any console error appeared. Screenshots are optional unless an error is easier to show than describe.

## Pass boundary

Automated checks and a completed benchmark do not pass M9A. Steve must explicitly decide whether the blinded evidence justifies acceptance, rejection, or another bounded experiment. Even acceptance would not authorize normal-runtime integration; that would require a separate M9B contract.
