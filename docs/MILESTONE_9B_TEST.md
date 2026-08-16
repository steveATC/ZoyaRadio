# Milestone 9B owner evaluation — Gate 2

## Status

Steve accepted M9B Gate 2 on August 15, 2026 with the narrow same-card repetition
exception recorded below. Steve also accepted the cumulative Gate 3 evidence under the
later operational policy recorded in the M9B contract: the silent gate now requires safe
transition observation, at least one ready candidate, at least one safely suppressed
prepared delivery, no forbidden desktop-media activity, and selected-model unload.

M9B remains an implemented candidate, not passed or accepted runtime behavior. The
accepted Milestone 8 snapshot remains the safety baseline. Gate 4 is historically recorded as paused pending the bounded live-enrichment repair and Steve's resumed desktop test. Steve subsequently concluded M9 without final M9B acceptance; no further M9 Gate 4 test is authorized. See [MILESTONE_9_CLOSURE.md](MILESTONE_9_CLOSURE.md).
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

## Gate 3 history, repair, and accepted operational evidence

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

The fixed local reviewed-card smoke at `artifacts\m9b\gate3-smoke\20260815-124500-256\`
resolved all four exact aliases from `ReviewedCatalog`, verified selected-model unload, and made no
Firefox, GSMTC, audio, speech, ducking, playback, volume, GUI, cache, or live-enrichment call. Its
former mechanical classifier marked 7/12 samples validator-ready (58%); the earlier fixed-plan smoke
at `artifacts\m9b\gate3-smoke\20260815-124201-165\` marked 10/12. Those are retained diagnostic
measurements, not a Gate 3 acceptance threshold.

Steve reviewed the twelve final smoke outputs and accepted all 12 as suitable for Zoya to speak.
The revised policy allows familiar low-risk music knowledge, natural host opinion, ordinary musical
characterization, low-risk names/attributions, popularity/cultural observations, ordinary host
adjectives, and modest exaggeration. The validator now hard-discards only high-confidence technical
or safety failures; advisory results get at most one regeneration and remain deliverable if still
advisory after that bound. Raw rejected outputs remain available for owner inspection.

Under that explicit operational policy, Gate 3 is accepted from the cumulative evidence: 48 genuine
silent transitions, zero forbidden playback/audio/volume activity, selected-model unload, observed
cancellation and stale-work disposal, one ready candidate before a transition, and a resulting
prepared delivery safely suppressed. Final policy verification passed all four Release builds with 0 warnings and 0 errors, all four formatting checks, `git diff --check`, and 259 fake-driven tests with 0 failures. This acceptance does not authorize or start audible Gate 4.
The next step must be a separately authorized bounded audible Gate 4 trial, followed by Steve's final
acceptance decision.
## Gate 4 first audible attempt: safe but incomplete

Steve's first authorized Gate 4 run is recorded in
`%LOCALAPPDATA%\Zoya Radio\logs\radio-behavior-20260815-140809-914.txt`.

- Five prepared comments were delivered safely, all using the accepted M8 fallback route.
- Firefox alone was ducked and restored exactly for each delivery; normal playback remained
  unpaused, and the radio stopped cleanly.
- Zero M9B enriched comments were delivered because all five live-context attempts became
  unavailable before the local 14B generation path.
- The `Wake Up` — Rage Against The Machine attempt exposed the cause: the resolver read a
  nonexistent top-level MusicBrainz artist-credit ID and also rejected multiple exact recordings
  even though song-level artist/title evidence was sufficient.

The bounded repair now reads the nested canonical artist object, accepts one or more exact
song-level recordings for the confirmed artist, emits a soundtrack fact only for one distinct
official soundtrack release group, and ignores pre-repair negative cache entries. The direct
read-only MusicBrainz lookup found 35 exact `Wake Up` recordings with nested credits and one
distinct official soundtrack release group, `The Matrix: Music From the Motion Picture`.
`Secede & Kettel` remains a deferred collaboration-resolution limitation; no combined-name
workaround or special card was added.

Repair verification passed all four Release builds with 0 warnings and 0 errors, all four
formatting checks, `git diff --check`, and 266 fake-driven tests with 0 failures. The focused
coverage includes nested artist-credit parsing, multiple exact recordings, official soundtrack
group deduplication, legacy-negative cache invalidation, unsupported collaboration metadata,
and the absence of desktop-media APIs from the enrichment path.

This safe first attempt does not satisfy Gate 4's requirement for five enriched deliveries.
At that point, Gate 4 remained authorized but paused until Steve resumed the same bounded desktop test after
this repair. M9B remains unpassed.

## Gate 4 latest audible run: evidence and paused repair

Steve's latest authorized Gate 4 run is recorded in
`%LOCALAPPDATA%\Zoya Radio\logs\radio-behavior-20260815-164408-236.txt`. It recorded seven
scheduled preparation opportunities:

- Raul Seixas — `Ouro De Tolo`: exact live MusicBrainz identity/context; one M9B comment
  prepared and delivered.
- AC/DC — `T.N.T.`: exact live MusicBrainz context; one M9B comment prepared and delivered.
- Pantera — `Cemetery Gates`: approved context unavailable; M8 fallback delivered.
- System Of A Down — `Mind`: approved context unavailable; the 4B local fallback request
  received HTTP 500 after `llama-server` exited with `0xc0000409` and
  `GGML_ASSERT(check == -n_chars) failed`; the deterministic `That was Mind by System Of A Down.`
  fallback was prepared/delivered, selected-model unload was attempted, and Firefox restored.
- Black Label Society — `We Live No More`: approved context unavailable; M8 fallback delivered.
- Black Sabbath — `Paranoid (2009 Remaster)`: reviewed local card `M9C6-02`; one advisory
  regeneration retained a second 14B result, but Kokoro prepared 26.3 seconds against the
  25-second automatic maximum. The old path ducked, timed out, and restored Firefox exactly.
- Lynyrd Skynyrd — `Sweet Home Alabama`: reviewed local card `M9C6-01`; one M9B comment
  prepared and delivered.

The aggregate was four M9B preparations, three successful enriched M9B deliveries, three
successful M8/fixed-fallback deliveries, and one safe over-duration M9B delivery failure. Every
ducked attempt restored the exact captured Firefox volume/mute state; playback was never paused;
and the application stopped cleanly. The live `Ouro De Tolo` and `T.N.T.` results confirm the
MusicBrainz credit repair works beyond the earlier `Wake Up` example.

This run exposed three bounded diagnostic/safety issues: M9B final text was absent from normal
runtime logs, approved-source absence lacked a typed lookup reason, and known over-duration audio
was allowed into the ducking path. The repair records private selected/prepared/delivered/discarded
M9B text, retains typed bounded MusicBrainz/Wikidata failure diagnostics, and rejects/disposes
prepared audio strictly above the automatic maximum before Core Audio enumeration, ducking, or
speech. It preserves the delivery-time timeout/restoration path as defense in depth.

The repair's fake-driven verification passed all four Release builds with 0 warnings and 0 errors,
all four formatting checks, `git diff --check`, and 278 tests with 0 failures. It did not run the
radio, contact live enrichment/Ollama, produce speech, or alter desktop media. At that point, Gate 4 remained
incomplete because five successful enriched deliveries have not yet been observed. Steve has paused
further testing; the next action is Steve's decision whether and when to resume the same bounded
