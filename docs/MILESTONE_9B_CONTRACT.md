# Milestone 9B contract - grounded context integration

## Status and authority

Steve approved this bounded contract direction on August 15, 2026 after reviewing the
M9B proposal and approving its recommended decisions. Steve then explicitly authorized the bounded implementation on August 15, 2026. M9B is now an implemented candidate awaiting its staged Gate 2 owner evaluation; it is not passed or accepted runtime behavior.

M9B is not passed or accepted runtime behavior. Until automated verification, staged desktop testing, and Steve's final manual acceptance are complete,
the accepted Milestone 8 runtime at `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`
remains controlling.

## Goal

M9B may add an optional, source-backed commentary candidate to normal automatic radio.
It will prepare a compact grounded context card and a complete comment from local
`qwen3:14b-q4_K_M` while the current track continues playing. A prepared comment has no
right to play. Missing, ambiguous, late, stale, repetitive, invalid, or unsafe work is
discarded without delaying or controlling playback.

The desired improvement is natural, factually reliable Zoya commentary. Maximum
commentary frequency, a general web-aware DJ, and broader music discovery are not goals.

## Preserved accepted behavior

Unless this contract says otherwise, M9B must preserve the accepted M8 behavior:

- inclusive random scheduling every two to five genuine transitions;
- one-transition-ahead preparation while Firefox keeps playing;
- exact logical Firefox targeting and stale-work cancellation;
- no automatic pause, resume, toggle, or catch-up delivery;
- exactly one eligible Firefox Core Audio session, proportional ducking, and exact
  scalar/mute restoration through protected cleanup;
- Chrome, unrelated applications, endpoint volume, and master volume remain untouched;
- Silent Mode, Stop, window close, cancellation, wrapper churn, ambiguity, and unsafe
  state continue to invalidate or safely clean up work;
- Nicole remains the primary voice and Windows local TTS remains the accepted fallback;
- existing `--no-ai`, logging, GUI, packaging, and version behavior remain unchanged
  unless a later separately approved contract changes them.

## Runtime preparation flow

1. A genuine Track A transition creates the ordinary safe preparation opportunity.
2. The runtime captures only the exact observed Track A title and artist under the
   existing logical-session and stability guards.
3. A cancellable enrichment task starts alongside preparation. It never reserves a
   delivery, holds Firefox, or extends the later delivery window.
4. Exact identity and an eligible context card permit one bounded 14B whole-comment
   request. One bounded quality regeneration is allowed only for a duplicate or
   advisory-quality result and only while the same preparation opportunity is current.
5. Enrichment/model failure, timeout, cancellation, ambiguity, weak context, invalid
   output, or a missed opportunity discards the enriched candidate. There is no
   indefinite retry, catch-up queue, or late speech.
6. At Track B, the existing scheduler and delivery path revalidate the same target,
   current track/session, Silent Mode, cancellation, timing, and exactly one eligible
   Firefox audio session.
7. A ready and valid enriched candidate may enter the existing speech preparation and
   delivery mechanism. Otherwise the accepted M8 4B composition/fallback remains
   eligible if ready and safe; unsafe delivery remains silent.
8. Selected-model unloading follows protected cleanup. M9B never stops unrelated Ollama
   models or processes.

No live web request or model generation may occur after ducking begins.

## External source and privacy boundary

### Approved live requests

M9B may contact only:

- `https://musicbrainz.org/ws/2/` for exact artist and, where required, exact
  title-and-artist recording identity;
- `https://www.wikidata.org/wiki/Special:EntityData/` for an entity linked by the
  confirmed MusicBrainz identity; and
- the existing loopback-only Ollama API.

Only normalized title and artist leave the machine for MusicBrainz. Only confirmed
public Wikidata entity identifiers leave it for Wikidata. Steve's identity, browser
URLs/content, cookies, credentials, session identifiers, listening history, prior
comments, prompts, generated prose, and local paths must not leave the machine.

Requests use HTTPS, a product User-Agent, one request at a time, the MusicBrainz
one-request-per-second minimum interval, short per-request timeouts, cancellation, and
safe handling of rate limits or malformed responses. Redirects or configurable endpoints
must not escape the approved hosts. Raw page HTML or search-result text is never fetched,
stored, or sent to Ollama.

### Exact identity

- Artist identity requires exactly one normalized exact MusicBrainz match.
- Track-specific context requires exactly one recording bound to both the exact title
  and confirmed artist identity.
- Search rank, popularity, and model inference are never identity tie-breakers.
- Missing, duplicate, conflicting, malformed, or unsupported identity evidence means no
  enriched candidate.

### Reviewed local context catalog

M9B may include a small, versioned, reviewed local catalog to provide the richer facts
that made M9C.6 successful. It initially covers the familiar M9C.6 corpus and may grow
only through explicit reviewed source-data changes.

Catalog facts may cite reviewed direct pages such as Wikipedia articles, official artist
sites, Grammy editorial pages, or label pages, but M9B must not fetch those pages at
runtime. Each stored fact must be short, paraphrased, independently understandable,
bound to the correct artist or recording, and retain its source name and direct HTTPS
URL. Raw copied page passages are prohibited.

If a fact introduces a third party such as Belchior, Neil Young, or Chet Atkins, the
card must identify that person sufficiently for the resulting comment to stand alone.
Unexplained names, bare dates, generic nationality, or generic occupation alone do not
earn an enriched comment.

## Context-card contract

A typed immutable card contains:

- exact observed and confirmed title/artist identity;
- one to three useful facts, with an absolute maximum of five;
- fact applicability such as artist-wide, recording-specific, or release-specific;
- source name and direct HTTPS provenance for every fact;
- an editorial-usefulness decision;
- an expiry and request/opportunity fingerprint; and
- at most one explicitly approved deterministic persona-memory item.

Facts may include artist context, track-specific history, collaboration, a short
incident, or a useful curiosity. The model receives the compact card, not raw source
responses. Provenance appears only in local diagnostics and evaluation artifacts; Zoya
does not speak citations or URLs.

Persona memories are a small reviewed data set, not model improvisation. They must avoid
claims that Zoya was born, lived, attended, met someone, or witnessed an event unless
Steve explicitly approved that exact fictional memory. A safe approved observation such
as watching a historical event on television may be supplied as typed material; the
model may not extend it into a biography.

## Runtime cache

M9B may promote the diagnostic cache design into a bounded persistent cache under
`%LocalAppData%\ZoyaRadio\cache\music-context\`:

- at most 100 entries;
- SHA-256 keys derived from normalized title, artist, and identity requirement;
- confirmed entries expire after 24 hours;
- negative, ambiguous, and failed entries expire after two hours;
- entries contain requested metadata, match result, compact card/source URLs,
  timestamps, and safe reason codes only;
- no raw web content, prompt, generated prose, browser data, audio, or durable sequence
  of played tracks is stored.

Clear History must clear both the in-memory commentary/repetition state and this context
cache, with an understandable local log/activity result. Stop or shutdown clears
ephemeral recent-comment state but need not delete an otherwise valid context cache.

## Model and prompt contract

The selected model is local Ollama `qwen3:14b-q4_K_M`. The installed model reports
14.8B parameters, Q4_K_M quantization, a 40,960-token context limit, a 9.3 GB local
artifact, and Apache License 2.0. M9B uses a compact prompt and must not exploit the full
context window.

The model remains an optional owner-installed prerequisite. Zoya must not bundle,
silently download, pull, update, license-accept, or remove it. Readiness presentation
must disclose the approximately 9.3 GB download/storage cost observed on Steve's
machine, the materially higher RAM/VRAM and startup cost than the 2.5 GB 4B model, the
Apache-2.0 license, and the fact that actual RAM/VRAM residency and cold-load timing
must be measured on the target machine. Any future setup action requires separate
explicit consent and visible progress/cancellation under the existing prerequisite
rules.

If 14B is absent, disabled, late, or fails, normal radio preserves the accepted M8 4B
path. It must not silently label 4B output as enriched. Generation uses loopback only,
bounded cancellation/timeout, and selected-model unload through `keep_alive: 0` or its
verified equivalent. It never unloads another selected model or kills Ollama.

The prompt asks for one complete natural radio-host comment, normally one or two spoken
sentences. It may select, combine, paraphrase, and compress supplied facts. It must not
produce deterministic `[fact]. [reaction]`, merely recite a card, or mechanically repeat
both title and artist when natural context does not require them.

Training knowledge may help language and tone but may not introduce factual claims.
Externally verifiable claims must be supported by exact metadata, the context card, or
an explicitly supplied persona memory. Unsupported claims about lyrics, sound, genre,
emotion, motives, popularity, cultural meaning, geography, history, biography, other
works, or an artist's tendencies are prohibited. Exact `FALLBACK` is a valid response
and is never spoken literally.

## Validation and repetition

Validation separates hard safety/grounding failures from advisory quality signals.

Hard discard includes:

- empty, malformed, multi-answer, control/meta, prompt-leaking, unsafe-length, or
  literal-URL output;
- title/artist substitution, metadata contradiction, Unicode corruption, or a stale
  card/opportunity;
- a new unsupported named entity, number, date, relationship, event, or factual claim;
- unsupported lyric, sound, genre, emotional, cultural, geographic, historical, or
  biographical assertions;
- invented Zoya biography; and
- material semantic exaggeration or changed causality, such as turning “filming caused
  the exchange doors to close” into “the band shut down the stock exchange.”

Artist/title omission is not a rejection when the comment remains bound to the current
opportunity. Questions, mild verbosity, generic DJ language, “I'm picturing” phrasing,
fact recitation, and formulaic fact-then-reaction construction are advisory quality
signals unless they also create a grounding failure.

The runtime retains at most six accepted enriched-comment fingerprints and selected
fact identities in process memory. It detects exact repeats, high-confidence repeated
openings/templates, repeated questions/signature phrases, and reuse of the same fact for
the same identity. A duplicate or advisory result permits at most one regeneration;
another unsuitable result selects the accepted M8 fallback or silence. There is no
unbounded retry or persistent listening history.

Deterministic checks cannot prove every paraphrase true. M9B therefore combines strict
entity/number/date/card checks, conservative material-exaggeration rules, focused fake
tests, and Steve's staged qualitative acceptance. It must not claim a general semantic
truth verifier.

## Acceptance gates

M9B passes only after every applicable gate succeeds and Steve explicitly accepts the
audible runtime candidate.

### 1. Engineering and fake-driven verification

- All focused source, cache, prompt, fallback, timeout, rate-limit, cancellation,
  Unicode, repetition, model-unload, stale-work, and isolation tests pass.
- The complete accepted runtime regression suite passes.
- Release builds and formatting checks pass with zero warnings and zero errors.
- Tests prove no non-approved host, raw-page path, browser data, unrelated Ollama model,
  playback target, application volume, or master volume can be used.

### 2. Generate-only owner review

- Use at least the 16 familiar M9C.6 tracks, three independently generated candidates
  per track, and reviewed context cards.
- Do not spend Steve's qualitative review on corrupt metadata, injection fixtures, or
  deliberately absent context; those remain automated tests.
- Accepted displayed candidates contain zero unsupported factual claims or material
  exaggerations.
- Mean factual acceptability is at least 4.9/5; mean natural context integration and
  overall desire are each at least 4.25/5; mean variety is at least 4.0/5; and at least
  80% of scored displayed candidates receive overall desire of 4/5 or better.
- Report fallback, hard-discard, advisory, regeneration, and candidate-availability
  rates. At least 70% of opportunities with an eligible reviewed card must produce a
  displayable enriched candidate.
- Grounded warm-generation p95 should remain below five seconds on Steve's machine and
  no request may exceed the bounded generation timeout. The M9C.6 reference was roughly
  0.69-second median, 0.85-second p95, and one 3.55-second outlier; those warm benchmark
  measurements are evidence, not a runtime guarantee.

#### August 15, 2026 owner amendment

Steve accepted the recorded 48-request Gate 2 run with a narrow same-card repetition
exception. Thirty-two displayable candidates were scored; Raul Seixas candidate 3 was
intentionally unscored as an almost one-to-one copy of candidate 2. The run achieved
5.00 factual acceptability, 4.72 context integration, 4.44 personality, 4.38 spoken
naturalness, 3.72 variety, and 3.84 overall desire. Seventeen of 32 scored candidates
received overall 4 or 5. Mechanical availability was 68.75%.

The run therefore missed the original variety, overall-desire, 80%-at-four-or-better,
and 70%-availability thresholds. Steve accepted Gate 2 because 23 of 33 displayable
candidates were marked for repeated openings/templates under three generations from the
same song/card; the 10 scored candidates without that flag averaged 4.90 overall and
variety, with every one at 4 or better. All 15 hard discards were owner-acceptable, but
that judgment does not relax conservative runtime grounding or exaggeration refusals.

This dated exception applies only to the recorded Gate 2 artifact. It does not change
the general thresholds, repetition/regeneration bounds, M8 fallback, safe-silence rules,
Gate 3 requirements, Gate 4 authorization boundary, or final acceptance requirement. No
second generate-only run is required before Gate 3.

### 3. Silent desktop preparation trial

- Observe at least 12 genuine transitions with Firefox playing and all M9B preparation
  enabled but speech and ducking disabled.
- Record cold-load time, warm-generation latency, RAM/VRAM residency where observable,
  cache behavior, candidate readiness, cancellation, and unload behavior.
- At least 90% of eligible steady-state opportunities should be ready before the next
  genuine transition. Cold or unusually short-track misses are safe discards and must
  be reported separately.
- Confirm zero pause/resume/toggle commands, zero ducking, zero speech, zero catch-up,
  and correct discard on track/session changes, ambiguity, Silent Mode, Stop, and stale
  opportunities.

### 4. Bounded audible desktop trial

- Only after Gates 1-3 pass, deliver at least five scheduled enriched comments across
  multiple familiar tracks.
- Confirm the candidate is complete before ducking, only the validated Firefox session
  is ducked, captured scalar/mute state is restored exactly, Chrome/master volume remain
  untouched, normal playback is never paused, cancellation cleans up safely, and no
  stale or rejected comment is spoken.
- Steve judges factual reliability, naturalness, repetition, timing, fallback behavior,
  and whether each comment belonged on Zoya Radio.

### 5. Explicit acceptance

Automated success, generate-only scores, or a completed desktop run do not pass M9B by
themselves. Steve must explicitly accept the candidate. Packaging the 14B prerequisite,
broader distribution, additional live sources, or later commentary expansion requires
separate approval.

## Implementation surface and non-goals

An authorized implementation should remain concentrated in typed context/enrichment,
whole-comment composition, the existing preparation coordinator, bounded recent state,
local readiness/activity reporting, and focused tests. It must reuse the accepted
delivery and restoration path rather than create a parallel radio runtime.

M9B does not authorize arbitrary web search, live Wikipedia/page scraping, unrestricted
training-knowledge claims, automatic model installation, a general knowledge database,
durable listening history, a recommendations engine, broad persona biography, new
speech/ducking behavior, scheduling changes, package-version changes, telemetry, cloud
AI, or public distribution work.
