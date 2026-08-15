# Milestone 9B owner evaluation — Gate 2

## Status

Steve accepted M9B Gate 2 on August 15, 2026 with the narrow same-card repetition
exception recorded below. M9B remains an implemented candidate, not passed or accepted
runtime behavior. The accepted Milestone 8 snapshot remains the safety baseline.

Gate 3 has now had two safe silent desktop attempts, both of which failed candidate
readiness and therefore did not pass. The repaired candidate requires one fresh, separately
authorized Gate 3 trial. Audible Gate 4 remains prohibited unless that fresh Gate 3 trial
passes and Steve separately authorizes Gate 4.

## Evaluated artifact

Steve reviewed this exact ignored local artifact set:

```text
artifacts\m9b\20260815-095443-189\
```

The run generated three independent comments for each of the 16 familiar reviewed
context cards using the already-installed local `qwen3:14b-q4_K_M` model.

- Requests: 48 of 48; generation failures: 0.
- Displayable candidates: 33.
- Mechanical hard discards: 15.
- Model-selected `FALLBACK`: 0; advisory candidates: 0; regeneration: 0.
- Mechanical candidate availability: 68.75%.
- Latency: 624.9 ms median, 819.7 ms p95, 3328.0 ms maximum.
- Selected-model unload: verified.
- Firefox, media sessions, playback, speech, Core Audio, ducking, GUI, and normal radio
  were not used.

## Owner scores

Steve numerically scored 32 displayable candidates. Raul Seixas candidate 3 was
intentionally left unscored because it was almost a one-to-one copy of candidate 2;
that omission is part of the qualitative evidence rather than an incomplete review.

| Measure | Result |
| --- | ---: |
| Mean factual acceptability | 5.00/5 |
| Mean natural context integration | 4.72/5 |
| Mean Zoya personality | 4.44/5 |
| Mean spoken naturalness | 4.38/5 |
| Mean variety | 3.72/5 |
| Mean overall desire | 3.84/5 |
| Overall desire 4 or 5 | 17 of 32 |
| Overall desire 5 | 11 of 32 |

No displayable candidate was owner-flagged for an unsupported/questionable claim, fact
recitation, formulaic fact-then-reaction construction, or an awkward third-party
reference. Five were explicitly marked as comments Steve wanted Zoya to say.

Steve marked 23 of the 33 displayable candidates for repeated opening/template; 22 of
those were numerically scored. Their mean overall desire was 3.36 and mean variety was
3.18. The 10 scored candidates without that flag averaged 4.90 for both overall desire
and variety, and every one received overall desire of at least 4.

All 15 mechanically hard-discarded comments were judged acceptable by Steve, including
Shakira candidate 1, which Steve confirmed in the Work chat on August 15. This owner
judgment does not automatically relax runtime hard-discard rules: conservative grounding
and material-exaggeration refusals remain in force unless separately amended.

## Gate 2 owner exception and acceptance

The recorded run met the factual, natural-integration, latency, request-completion, and
model-unload expectations. It missed the pre-agreed mean variety, mean overall desire,
80%-at-four-or-better, and 70% mechanical-availability thresholds.

Steve determined that the qualitative misses were driven by the benchmark asking for
three independent outputs from the same song and same fact card. That construction
amplified repetition beyond normal varied-track listening. The implemented runtime
retains six recent comment/fact fingerprints, treats recent same-fact or repeated-opening
output as advisory, permits at most one regeneration, and otherwise preserves the M8
fallback or silence. Same-song repetition therefore remains bounded and test-covered.

Steve explicitly accepted Gate 2 with this narrow repetition exception on August 15,
2026. This is a one-run owner acceptance based on the recorded evidence. It does not:

- lower the grounding requirement;
- authorize unsupported factual claims or material exaggeration;
- remove the repetition state, regeneration bound, M8 fallback, or safe-silence behavior;
- make the M9B candidate accepted runtime behavior;
- waive Gate 3 timing/cancellation/playback-isolation evidence; or
- authorize audible Gate 4 testing.

## Gate 3 attempts: safe but failed

Two separately authorized silent desktop trials completed 12 genuine Firefox transitions
without speech, ducking, playback control, volume change, catch-up, or stale delivery:

- `artifacts\m9b\gate3\20260815-112516-439\`: four preparation opportunities, zero
  ready candidates, three cancelled preparations, two stale disposals, and selected-model
  unload verified.
- `artifacts\m9b\gate3\20260815-114211-775\`: five preparation opportunities, zero
  ready candidates, no cancellations or stale disposals, and selected-model unload verified.
  The two observed Firefox title variants did not match exact catalog cards, while the
  remaining reviewed-card candidates were conservatively hard-discarded for unsupported
  sound/genre/vocal claims.

Both attempts were safe failures, not Gate 3 acceptance. The diagnostic also incorrectly
returned success after reaching 12 transitions even when readiness was zero. The repair adds
reviewed typed exact artist-specific aliases for the two observed titles, strengthens the
whole-comment prompt without weakening the validator, and makes the Gate 3 process verdict
fail unless all required acceptance conditions are met. The evidence now distinguishes typed
eligible opportunities, hard discards, enrichment-unavailable results, cancellation, stale
work, model unload, and forbidden desktop-media activity.

A fresh separately authorized Gate 3 trial must still observe at least 12 genuine transitions
with M9B preparation enabled but speech and ducking disabled. It must meet at least 90% ready
candidates across eligible steady-state opportunities, verify selected-model unload, and record
zero playback control, ducking, speech, volume change, catch-up, or stale delivery. Gate 3
evidence remains separate from Gate 2 owner acceptance. Do not start audible Gate 4 unless
this fresh Gate 3 trial passes and Steve separately authorizes it.
## Fresh Gate 3 trial — authorization required

Do not run this command until Steve explicitly authorizes a fresh Gate 3 trial. From the
repository folder, run:

```text
.\run-m9b-gate3-silent.cmd
```

Keep exactly one eligible Firefox/YouTube Music session playing. The trial is bounded: it
ends after 12 genuine Firefox transitions, writes local evidence below
`artifacts\m9b\gate3\`, and returns exit code `0` only when its typed Gate 3 verdict is
`PASS`. It never starts speech, ducks audio, changes any volume, or controls playback.
A `FAIL` verdict is safe evidence, not a reason to begin audible Gate 4.
