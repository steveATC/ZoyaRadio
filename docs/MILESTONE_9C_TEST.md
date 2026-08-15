# Milestone 9C source-backed commentary experiment record

## Outcome

Steve accepted M9C.6 on August 15, 2026 as successful experimental evidence and the selected direction for a future runtime candidate. This does not make M9C normal automatic-radio behavior and does not authorize M9B or any other integration work.

The selected direction is:

- local `qwen3:14b-q4_K_M`;
- deterministic, reviewed three-to-five-fact context packs with direct source URLs;
- grounded whole-comment synthesis, where the model writes one coherent spoken comment rather than an application-owned fact followed by a separately generated reaction;
- no permission to add externally verifiable claims from model training in the selected grounded mode.

Normal radio remains on the accepted M8 prompt, model, validator, composition, scheduling, speech, ducking, GUI, packaging, and version.

## Evaluation history

M9C progressed through deliberately isolated generate-only experiments:

- M9C through M9C.2 tested strict MusicBrainz identity confirmation, linked Wikidata facts, safe failure, bounded diagnostic caching, and increasingly useful fact cards.
- M9C.3 showed that curated source-backed context could produce excellent commentary, but it also exposed unsupported cultural/geographic expansion and invented Zoya biography.
- M9C.4 tested deterministic factual anchors plus separately generated reactions. The design remained awkward and frequently collapsed to `FALLBACK`; it was rejected as a commentary composition strategy.
- M9C.5 compared `qwen3:4b-instruct` with `qwen3:14b-q4_K_M` over identical prompts. The 14B model produced 16 accepted candidates versus 2 for 4B, with 59 distinct raw outputs versus 47. Model capacity helped, but anchor-plus-reaction composition still recited facts and sounded formulaic.
- M9C.6 removed deterministic anchor concatenation. The 14B model received reviewed multi-fact context packs and generated the complete comment in two blinded modes: grounded and training-augmented.

## M9C.6 owner evidence

The completed artifacts are under ignored local directory:

`artifacts/m9c/20260815-063705-452/`

The run produced 96 of 96 local responses, verified selected-model unload, and did not access Firefox, Core Audio, speech, ducking, the GUI, or normal radio state.

Mechanical classification reported 43 clean and 5 hard-rejected candidates in each arm. Steve scored 83 clean candidates:

| Measure | Grounded | Training-augmented | Combined |
| --- | ---: | ---: | ---: |
| Scored candidates | 40 | 43 | 83 |
| Mean factual acceptability | 5.00 | 4.84 | 4.92 |
| Mean context integration | 4.97 | 4.91 | 4.94 |
| Mean Zoya personality | 4.90 | 4.88 | 4.89 |
| Mean spoken naturalness | 4.82 | 4.88 | 4.86 |
| Mean variety | 4.50 | 4.44 | 4.47 |
| Mean overall desire | 4.85 | 4.86 | 4.86 |

Of the 83 scored comments, 73 received overall 5, 8 received overall 4, and 2 received overall 3 or lower. Steve marked all 10 mechanically hard-rejected comments acceptable; several were especially strong. The hard rejection merely required the comment to repeat the supplied artist or title, which owner review showed is not necessary when playback context already identifies the track.

The training-augmented arm offered no material qualitative advantage and had lower factual acceptability. Grounded whole-comment synthesis is therefore selected for any later integration proposal.

## Deferred integration issues

These findings do not block closing the experiment, but a future runtime contract must address them proportionately:

- detect near-duplicate structures, not only exact duplicate strings;
- reduce repeated openings and rhetorical forms such as “Can you imagine,” “Who knew,” and “wasn't just”;
- do not require every comment to repeat the current artist or title;
- distinguish harmless opinion and figurative language from factual claims;
- detect meaning-changing exaggeration, such as changing “caused the exchange doors to close” into “shut down the exchange”;
- avoid lyric, sound, or emotional interpretations unless the reviewed context supports them;
- preserve silence and fallback whenever context, timing, identity, or playback state is unsafe.

No additional model, prompt, runtime, network, playback, voice, or packaging work is authorized by this record.

## Current automated verification

Verification rerun August 15, 2026 after registering focused M9C.6 coverage:

- MediaProbe, ControlApp, VoiceAudition, and test-project formatting checks passed.
- All four Release builds passed with 0 warnings and 0 errors.
- Complete automated suite: 236 passed, 0 failed.
- The suite includes M9C.6 context-pack, prompt-mode, classification/raw-output, blinded-artifact, and normal-runtime-isolation coverage.
- No audible speech, playback control, Firefox control, volume change, GUI launch, installation, download, or live web benchmark occurred during this verification.

## Preserved commands

The experimental launchers remain available for reproducibility:

```powershell
.\run-source-backed-commentary-benchmark.cmd
.\run-owner-curated-commentary-benchmark.cmd
.\run-deterministic-context-benchmark.cmd
.\run-m9c5-model-comparison.cmd
.\run-m9c6-whole-comment-benchmark.cmd
```

These commands are diagnostic-only. Their generated artifacts remain ignored and local.
