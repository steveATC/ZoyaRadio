# Milestone 9B owner evaluation — Gate 2

## Status

Milestone 9B is implemented as a **candidate**. It is not passed or accepted runtime
behavior. The accepted Milestone 8 snapshot remains the safety baseline until Steve has
reviewed the generate-only evidence and later explicitly authorizes the separate silent
and audible desktop gates.

## What this evaluation does

The one authorized M9B run is complete. Review this exact artifact folder:

```text
artifacts\m9b\20260815-095443-189\
```

It made 48 local requests: three independently generated whole comments for each of the
16 reviewed familiar-track cards. It used the already-installed local
`qwen3:14b-q4_K_M` model and confirmed selected-model unload. Do not rerun the launcher
under this authorization; the owner-quality review is of this recorded artifact.

It does **not** open the radio, access Firefox or Windows media sessions, play audio,
synthesize speech, duck volume, change playback, or open the control application. It
requests the selected local model unload when finished.

## Recorded generate-only engineering evidence

- Artifact: `artifacts\m9b\20260815-095443-189\`
- Completed requests: 48 of 48; generation failures: 0.
- Displayable candidates: 33; mechanical hard discards: 15; model-selected `FALLBACK`: 0; advisory candidates: 0; regeneration: 0.
- Candidate availability: 68.75%. Latency: 624.9 ms median, 819.7 ms p95, 3328.0 ms maximum.
- Selected `qwen3:14b-q4_K_M` unload was verified.
- These are engineering observations only. No one has scored the qualitative fields yet, and no silent or audible desktop M9B test was run.

## How to review the artifact

1. Open `artifacts\m9b\20260815-095443-189\M9B_EVALUATION.md`.
2. Read each candidate beside its exact title/artist, reviewed context card, and source
   links. Source links are provenance; the run did not fetch those pages.
3. Score only the displayed owner fields from 1 to 5: factual grounding, natural
   fact-to-link, host voice/personality, natural spoken phrasing, variety, and whether
   you would want Zoya Radio to say it.
4. Mark the simple flags when applicable: unsupported/questionable claim, fact
   recitation, formulaic fact-then-reaction, repeated opening/template, or awkward
   third-party reference.
5. For a mechanically discarded item, answer whether it would actually have been
   acceptable on Zoya Radio and add a short note. A discard is engineering evidence,
   not a score assigned by Codex.
6. Keep the matching `M9B_RESULTS.json`, `M9B_MANIFEST.json`, and
   `M9B_RUN_SUMMARY.md` with the evaluation sheet. Bring that complete folder to the
   Work chat for review.

## What not to test yet

Do not run normal automatic radio, silent desktop preparation, or audible delivery for
M9B yet. Those gates need their own explicit authorization after this owner-quality
review. This evaluation cannot prove normal Firefox targeting, timing, speech, or
volume-restoration behavior.

## If the run cannot complete

Bring back the complete artifact folder if it was created, plus the final command-window
message. Do not install, update, or download a model to fix the issue. The normal M8
candidate/fallback remains the safe runtime route when the M9B model or enrichment is
unavailable.