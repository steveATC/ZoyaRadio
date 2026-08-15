# Milestone 9B owner evaluation — Gate 2

## Status

Steve accepted M9B Gate 2 on August 15, 2026 with the narrow same-card repetition
exception recorded below. M9B remains an implemented candidate, not passed or accepted
runtime behavior. The accepted Milestone 8 snapshot remains the safety baseline.

Gate 3 has now had four safe silent desktop attempts, all of which failed candidate readiness
and therefore did not pass. The bounded repair's final reviewed-context, loopback-only smoke
also reached only 7/12 validator-ready samples. The one permitted prompt-only revision has been
used. Further Gate 3 desktop trials and audible Gate 4 are prohibited pending a separately
approved future direction.

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

## Gate 3 attempts and bounded repair: safe but failed

Four separately authorized silent desktop trials completed 12 genuine Firefox transitions
without speech, ducking, playback control, volume change, catch-up, or stale delivery:

- `artifacts\m9b\gate3\20260815-112516-439\`: four preparation opportunities, zero
  ready candidates, three cancelled preparations, two stale disposals, and selected-model
  unload verified.
- `artifacts\m9b\gate3\20260815-114211-775\`: five preparation opportunities, zero
  ready candidates, no cancellations or stale disposals, and selected-model unload verified.
- `artifacts\m9b\gate3\20260815-121706-020\`: two eligible opportunities, one ready
  candidate (50%); the selected model unloaded and no forbidden desktop-media action occurred.
- `artifacts\m9b\gate3\20260815-121902-189\`: three eligible opportunities, zero ready
  candidates (0%); the selected model unloaded and no forbidden desktop-media action occurred.

The first two attempts exposed two safe repair targets: exact observed Firefox title variants
were masked by stale negative cache outcomes, and rejected prose retained only a typed reason,
not owner-inspectable local raw evidence. The repair makes an exact reviewed card or individually
reviewed exact artist-specific alias win before cache lookup, preserves raw/normalized rejected
candidate diagnostics locally with typed classification/reason and timing, and keeps unknown tracks
on the bounded cache/live-resolution path. It does not make a catalog match fuzzy or contact live
enrichment for a reviewed card.

Steve separately clarified the qualitative policy: ordinary subjective musical characterization
is permitted. Terms such as riffs, vocals, instrumentation, production, sound, style, genre, and
“heavy metal” are not automatic hard-discard conditions. Specific historical incidents, dates,
people, credits, quotations, motives, lyrics/meanings presented as fact, biography, influence, and
strong-superlative claims still require supplied context and remain rejectable when unsupported.
The final prompt/validator change applies that policy directly; it is not merely prompt avoidance.

The required one bounded prompt-only revision was then measured with the fixed local reviewed-card
smoke plan at `artifacts\m9b\gate3-smoke\20260815-124500-256\`. All four exact aliases resolved
from `ReviewedCatalog`, selected-model unload verified, and the smoke made no Firefox, GSMTC,
audio, speech, ducking, playback, volume, GUI, cache, or live-enrichment call. It produced 7/12
validator-ready samples (58%): Sweet Home Alabama 1/3, Paranoid (2009 Remaster) 1/3, Master of
Puppets 2/3, and the exact Johnny Cash Folsom title 3/3. This is below the 90% Gate 3 readiness
threshold. The preceding fixed-plan smoke at `artifacts\m9b\gate3-smoke\20260815-124201-165\`
produced 10/12 before the owner-approved validator policy correction.

These are safe failures, not Gate 3 acceptance. The final smoke artifact retains the full raw local
output and typed result for every sample; it demonstrates that deterministic checks still cannot
prove all semantic grounding. The one permitted prompt-only revision has been consumed. Do not run
another desktop Gate 3 trial, do not tune the prompt/validator again under this repair, and do not
start audible Gate 4.
