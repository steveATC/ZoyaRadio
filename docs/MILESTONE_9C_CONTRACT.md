# Milestone 9C contract - optional source-backed music-context enrichment

## Status

M9C is closed as successful experimental evidence. Steve selected the M9C.6 grounded whole-comment direction on August 15, 2026 for any future integration proposal. It remains diagnostic code only: it is not accepted automatic-radio behavior, does not change the accepted M8 runtime, and does not authorize M9B.

## Narrow network boundary

Only the explicit `run-source-backed-commentary-benchmark.cmd` path may contact:

- `https://musicbrainz.org/ws/2/` to resolve exact artist and, where required, exact title-and-artist recording identity;
- `https://www.wikidata.org/wiki/Special:EntityData/` only for a Wikidata identity linked by the confirmed MusicBrainz artist;
- the existing loopback-only Ollama API.

Only exact title and artist leave the machine for MusicBrainz. A confirmed public Wikidata ID alone leaves the machine for Wikidata. Steve's name, listening history, comments, prompts, generated prose, browser data, cookies, paths, and credentials do not leave the machine.

MusicBrainz requests use the documented one-request-per-second minimum interval and a product User-Agent. Wikidata requests use a product User-Agent, JSON only, short timeout, and safe failure on rate limiting. Wikidata facts are attributed in artifacts with direct URLs; no raw page content is retained or sent to Ollama.

## Confirmation and facts

- Artist identity is confirmed only when exactly one MusicBrainz candidate matches the supplied artist after case/whitespace normalization.
- A required recording identity is confirmed only when exactly one recording matches both exact normalized title and artist.
- Multiple exact candidates, no candidates, malformed data, unsupported fact shape, missing linked Wikidata ID, timeout, rate limit, or source conflict means no enrichment.
- A fact card contains only concise structured Wikidata description text, the confirmed identity, source name, and direct source URL. Every fact must have a direct approved HTTPS source URL.
- The M9C prompt may rely only on exact metadata and this fact card. It must return `FALLBACK` when no fact earns a useful line.

## Cache

The ignored diagnostic cache is `artifacts/m9c/cache/`. It is keyed by SHA-256 of normalized title, artist, and identity requirement. It stores only requested metadata, match outcome, small fact card/source URLs, timestamps, and a safe explanation.

- Confirmed entries expire after 24 hours.
- Negative/ambiguous/failed entries expire after 2 hours.
- The cache holds at most 100 entries.
- Clear it explicitly with `run-media-probe.cmd --clear-enrichment-cache`.

It is not normal runtime state, listening history, telemetry, or a new package feature.

## Benchmark and non-goals

The benchmark has 12 fixed scenarios, two blinded arms, and two samples each: 48 sample records. It writes ignored local artifacts under `artifacts/m9c/<timestamp>/`: a blinded evaluation sheet, anonymous JSON results, answer key, summary, and per-sample match/fact-card outcome.

It is generate-only. It starts before normal logging, GSMTC, Firefox, Core Audio, speech, ducking, GUI, and normal radio state. It does not alter scheduling, targeting, fallback, voice, package version, or accepted M8/M9A paths. M9B remains unauthorized.
## M9C.1 repair/retest rules

- Duplicate exact MusicBrainz artist names remain ambiguous unless exactly one candidate has a valid linked Wikidata identity, or an exact required recording identifies exactly one candidate artist ID. Search rank and popularity are never tie-breakers.
- The Massacration live case may confirm because only MusicBrainz artist `692915bd-bc68-4db7-af7a-4e2b091a264c` links to Wikidata `Q549504`; its fact card is `Wikidata describes Massacration as a Brazilian satirical heavy metal band.` with the direct source URL `https://www.wikidata.org/wiki/Q549504`.
- Fact cards contain 1-3 direct-source facts. Generic cards may be retained for diagnostics but never cause the candidate to call Ollama; they select safe `FALLBACK`.
- Source-backed output is one 5-28 word sentence or exact `FALLBACK`. It must make a natural link from a specific fact, not recite the fact card.
- M9C.1 adds curated fake fact-card cases to measure prompt quality separately from live resolver behavior. They are clearly labeled in evaluation artifacts and do not contact the web.
## M9C.2 fact-to-link rules

- A fact card is classified deterministically as `RichHook`, `GenericFact`, or `UnsafeOrUnavailable`. Only `RichHook` may call the isolated candidate; the other classes produce `FALLBACK`.
- RichHook output is one 5-26 word fact-to-link sentence: one grounded factual anchor followed by a dry, explicitly subjective reaction. It may not quote, recite, closely paraphrase, or sound like an encyclopedia entry.
- Mechanical rejection produces `FALLBACK`: fact recitation, banned imagined-scene starter, generic praise/cliche, near duplicate of a recent accepted candidate line, or invalid length/format.
- M9C.2 contains seven live resolver/safety cases, four curated RichHook cards (Massacration, Beatles, Kraftwerk, Devo), and two curated GenericFact controls (John Lennon, David Bowie). Curated cards are clearly identified in artifacts and retain their direct Wikidata URLs.
- This remains a 52-sample, two-arm, generate-only benchmark. It is not normal automatic-radio behavior and does not authorize M9B.

## M9C.3 owner-curated confirmed-context quality benchmark

- M9C.3 is a separate generate-only, blinded creative-quality comparison. It does not change accepted M8 radio behavior and does not authorize M9B.
- Its fixed corpus contains 16 owner-curated, confirmed scenarios. Each card is marked `curated-confirmed`, holds one to three concise source-backed facts, and displays direct approved Wikidata URLs in the evaluation sheet. The corpus is deliberately curated for stable qualitative review; the M9C.2 live resolver and its ambiguity/failure boundaries remain automated-only engineering checks.
- The benchmark compares the accepted local-only baseline and the source-backed candidate with the same selected local model/settings: three samples per arm per scenario (96 samples). It writes ignored timestamped artifacts, a blinded evaluation sheet, and a separate answer key. It requests selected-model unload after the run.
- Only the title, artist, structured fact text, source name, and direct source URL are provided to the local candidate. Raw web-page content is never retained or sent to Ollama.
- Candidate output is one or two natural spoken sentences totaling 6-32 words, or exact `FALLBACK`. It must use one supplied fact as an anchor, add at most one plainly subjective host reaction, avoid recitation, unsupported claims, imagined personal scenes, generic DJ hype, title-only wordplay, and repeated structures. `FALLBACK` is correct safe behavior when a card does not earn a good line.
- Human scoring is limited to factual grounding, natural fact-to-link, host voice/personality, natural spoken phrasing, variety, and whether Steve would want Zoya Radio to say it. The only human rejection flags are unsupported claim, fact-card recitation, repetitive structure, banned imagined scene, generic DJ cliche, unnecessary commentary, and Unicode corruption. Safety/refusal/caching/injection/unload behavior remains fake-driven automated coverage, not Steve's qualitative sheet.
- The command is `run-owner-curated-commentary-benchmark.cmd`. It starts before normal logs, GSMTC, Firefox, Core Audio, speech, ducking, GUI, and normal runtime state. It does not pause, resume, duck, speak, or otherwise control desktop media.
## M9C.4 deterministic-anchor benchmark

- M9C.3 was a useful direction-finding result: its confirmed-context arm produced strong source-backed fact-to-link comments, but manual review found unsupported cultural/geographic conclusions and incoherent invented Zoya biography. It is not accepted runtime behavior.
- M9C.4 is a separate explicit generate-only benchmark. It uses 20 fixed owner-curated, confirmed conditions with exactly three samples each (60 local generations). No live source retrieval occurs during this command.
- Each condition carries exact title/artist identity, one direct approved source URL, a source-backed hook, and a deterministic factual anchor rendered by application code. The model may generate only a 3-18 word subjective link or exact `FALLBACK`; final output is anchor plus a validated host reaction, or `FALLBACK`; useful cards never treat bare anchor-only trivia as a successful composition.
- The six enabled `FictionalPersonaFact` entries are fixed authored character facts. At most one selected memory reaches a prompt; no model-generated residence, job, trip, encounter, concert attendance, friendship, or other personal history is accepted.
- The deterministic validator uses typed card entities, bounded composition, and structural guards to reject anchor recitation, unapproved entities/numbers, cultural/geographic expansion, sound claims, imagined scenes, hype, persona invention, excessive length, and near duplicates. It cannot prove factual correctness; Steve's qualitative review remains required.
- The command is `run-deterministic-context-benchmark.cmd`. It starts before normal logs, GSMTC, Firefox, Core Audio, speech, ducking, GUI, enrichment network access, and normal runtime state. It requests selected-model unload after the run, never controls desktop media, does not change accepted M8 radio behavior, and does not authorize M9B.
## M9C.4 reaction-required revision

- Owner review of the first M9C.4 run found 49 anchor-only outputs, 8 anchor-plus-link outputs, and 3 deliberate fallbacks. The deterministic source-backed direction remains promising, but that candidate is not accepted.
- A useful curated hook now succeeds only as a deterministic anchor plus a separately validated host reaction. A missing, unsafe, weak, or uninteresting reaction produces `FALLBACK`; bare anchor-only trivia is deliberately suppressed.
- Card-level editorial value is fixed and curated as `useful` or `weak`. Weak cards may return `FALLBACK`; this is not a reason to weaken factual safety or force speech.
- The reaction validator permits bounded, clearly subjective, anchor-bound imagination, metaphor, or rhetorical framing. It rejects unlicensed first-person experience, collective artist participation, unsupported absolutes/universals, inferred motive/history claims, unsupported sound descriptions, new entities/numbers, factual recitation without a host reaction, generic radio cliches, and recent distinctive phrase reuse.
- A selected `FictionalPersonaFact` remains optional. It licenses only its exact supplied first-person statement; it never licenses an inferred memory, ownership, encounter, residence, or other personal biography.
- The revision remains isolated generate-only work. It does not change normal radio or authorize M9B. The fresh 60-sample rerun safely selected `FALLBACK` for all 60 samples, so it produced no candidate prose for owner-quality scoring; M9C.4 remains unaccepted.
## M9C.5 local-model comparison

- M9C.5 compared the installed `qwen3:4b-instruct` and explicitly approved Apache-2.0 `qwen3:14b-q4_K_M` models over fixed, byte-identical prompts: 20 conditions, three samples, and two blinded arms (120 local requests).
- The 14B model materially improved instruction following, accepted-output count, and output diversity, while remaining local. The benchmark did not change the normal-radio model or prompt.
- The deterministic-anchor-plus-reaction architecture still forced factual recitation and awkward joins. M9C.5 therefore selected 14B for the next isolated experiment but did not select that composition for runtime.

## M9C.6 grounded whole-comment synthesis

- M9C.6 used 16 owner-familiar artist/track scenarios, three samples, and two blinded modes (96 local requests) with `qwen3:14b-q4_K_M` only.
- Each scenario supplied a deterministic reviewed pack of three to five concise source-backed facts. Raw pages were never sent to Ollama, and no live web access occurred during the run.
- The model generated the complete spoken comment. The application did not prepend a deterministic anchor or append a separate reaction.
- The grounded arm limited externally verifiable claims to the supplied pack. The comparison arm permitted training-derived knowledge only to measure whether that freedom improved quality.
- Steve's completed review found both arms highly successful, with a combined mean overall desire of 4.86/5 over 83 scored clean candidates. All 10 mechanical hard rejects were also judged acceptable.
- The training-augmented arm did not materially improve quality and reduced factual acceptability. Grounded whole-comment synthesis with the 14B model is the selected future candidate direction.
- Repetition control, metadata-mention policy, semantic exaggeration, and unsupported sound/lyric interpretation remain future integration concerns. They do not authorize runtime changes.
- The experiment is closed. M9C code remains unreachable from normal radio, and any integration requires a separate explicitly approved bounded contract.
