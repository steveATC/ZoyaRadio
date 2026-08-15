# Milestone 9B owner evaluation — Gate 2

## Status

Steve accepted M9B Gate 2 on August 15, 2026 with the narrow same-card repetition
exception recorded below. M9B remains an implemented candidate, not passed or accepted
runtime behavior. The accepted Milestone 8 snapshot remains the safety baseline.

The next possible stage is Gate 3, the silent desktop preparation trial. It has not run
and requires a separate explicit authorization. Audible delivery remains prohibited
until Gate 3 passes and Steve separately authorizes Gate 4.

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

No prompt change, validator relaxation, code repair, or second generate-only run is
required before Gate 3.

## Gate 3 remains unrun

A later separately authorized silent desktop preparation trial must observe at least 12
genuine transitions with M9B preparation enabled but speech and ducking disabled. It
must record cold-load and warm latency, resource residency where observable, cache and
candidate readiness, cancellation, and unload behavior. It must confirm zero playback
control, ducking, speech, catch-up, or stale delivery.

Gate 3 evidence is separate from this generate-only acceptance. Do not start normal
radio, Firefox/media testing, or audible delivery under this documentation authorization.
