# Historical proposal — M9B grounded context integration

> **Status: Historical proposal promoted on August 15, 2026.** Steve approved the
> resulting bounded decisions in [the canonical M9B contract](../MILESTONE_9B_CONTRACT.md).
> This file remains proposal history and does not govern implementation. The accepted
> M8 runtime remains authoritative until M9B is implemented, verified, and accepted.

## Why this draft exists

M9C.6 was an isolated, generate-only comparison. It supplied reviewed factual context
to local `qwen3:14b-q4_K_M` and compared a grounded whole-comment arm with a
training-augmented arm. Steve's review of 83 displayed outputs produced a combined mean
overall desire of 4.86/5. The arms were effectively tied for quality (4.85 grounded,
4.86 training-augmented), while grounded factual acceptability was higher (5.00 versus
4.84). The selected future direction is therefore **grounded 14B whole-comment
synthesis**, not training-augmented commentary.

That evidence is encouraging, not an authorization. M9C's diagnostics are unreachable
from the normal radio: they do not observe Firefox, prepare an automatic delivery,
speak, duck audio, write normal radio state, or alter the accepted 4B local-only M8
path. The remaining integration risks include repetitive rhetorical openings,
near-duplicates that exact-string checks miss, semantic exaggeration of a supplied
fact, and unsupported sound, lyric, emotional, or genre interpretation.

This proposal calls the possible next bounded integration milestone **M9B** because
that is the existing reserved label in the canonical records. This is a naming
recommendation only. It does not make M9B approved, active, implemented, or passed.

## Proposed goal and non-goal boundary

### Candidate goal

If Steve approves a future M9B contract, it could add an optional, bounded,
source-backed commentary candidate to automatic radio. The candidate would use one
confirmed, concise context card and the selected local 14B model to prepare a complete
spoken comment before the existing delivery opportunity. If the context, candidate,
timing, model readiness, or safety state is unsuitable, the radio would keep the
accepted non-enriched M8 behavior or remain silent where delivery is unsafe.

The candidate goal is *better factual reliability and natural fact-to-link commentary*,
not maximum commentary frequency. A prepared comment never has a right to play.

### Explicitly out of scope for a first M9B contract

- Arbitrary web browsing, search-result scraping, raw-page retrieval, or raw-page text
  sent to Ollama.
- Claims from an LLM's training knowledge that are not in a supplied context card.
- Automatic model installation, model download, or use of a cloud/provider API.
- A broad artist biography, general music knowledge database, listening-history
  database, or recommendations engine.
- New persona backstory, unrestricted fictional memories, or automatic audience/session
  claims.
- Changes to scheduling frequency, Firefox selection, Core Audio targeting, ducking,
  restoration, Silent Mode, voice selection, package version, GUI, or telemetry.

## Proposed runtime flow

The following is a design candidate, not a specification for source changes.

1. A genuine Firefox Track A transition produces the same safe preparation opportunity
   used by the accepted M8 scheduler. Firefox continues playing.
2. The existing logical-session and track-stability guards capture only Track A's exact
   title and artist. No browser content, URL, cookie, credential, or listening history
   is read.
3. In parallel with existing preparation work, a cancellable enrichment task attempts
   exact identity confirmation and a small context card. It is bounded by the current
   opportunity and cancellation token; it must never delay or reserve a future delivery.
4. If identity and context are confirmed, the local 14B model creates and validates one
   whole-comment candidate while Firefox continues playing. The generated material is
   held only as preparation for the current opportunity.
5. If enrichment is unavailable, ambiguous, late, cancelled, stale, invalid, or the
   model is unavailable, the prepared enriched candidate is discarded. The system does
   not retry indefinitely and does not queue a catch-up announcement.
6. At the ordinary Track B delivery decision, the runtime revalidates the same logical
   Firefox target, track/session freshness, Silent Mode, schedule state, and exactly one
   eligible Firefox Core Audio session within the existing bounded delivery window.
7. Only a candidate that is ready and still belongs to that safe opportunity may use the
   existing M8 delivery mechanism. Otherwise the system uses the already accepted
   non-enriched M8 composition/fallback behavior if that behavior is ready and safe, or
   it remains silent. It never pauses Firefox to wait for enrichment or generation.
8. After a candidate has been accepted or discarded, generation resources follow the
   selected-model unload policy. No result is retained as a durable listening history.

The important ordering remains:

```text
Track A -> prepare context and candidate while music plays -> validate -> Track B decision
       -> optional existing duck/speak/restore delivery
```

It must never become `Track A -> duck/pause -> wait for web/model -> speak`.

## Background enrichment and identity policy

### Proposed minimum source route

Retain the M9C proven narrow route initially:

1. Send only exact normalized title and artist to `https://musicbrainz.org/ws/2/`.
2. Require exactly one confirmed MusicBrainz artist; require exactly one exact
   title-and-artist recording when the chosen context needs recording identity.
3. Follow only a linked public Wikidata identifier to
   `https://www.wikidata.org/wiki/Special:EntityData/`.
4. Accept only compact structured facts with a direct approved HTTPS source URL.

Only title and artist leave the machine for MusicBrainz. Only a confirmed public
Wikidata ID leaves it for Wikidata. Steve's identity, browser data, session data,
listening history, prepared/generated text, prompts, local paths, and credentials do
not leave the machine. Ollama remains loopback-only.

The M9C resolver is sufficient for a conservative identity gate and simple structured
facts; it is **not yet sufficient by itself** to guarantee three-to-five interesting
facts for the quality demonstrated by M9C.6. A first integration must decide whether to
use only sparse live MusicBrainz/Wikidata cards, a separately reviewed small curated
card catalog, or an explicitly approved additional structured source adapter. It must
not silently expand into arbitrary Wikipedia/page retrieval. Until that decision is
approved, an insufficient card simply earns no enriched candidate.

### Proposed bounds

These are proposed starting limits to approve or revise in a future contract:

| Concern | Proposed boundary |
| --- | --- |
| MusicBrainz rate | Product User-Agent and at least one request per second, as in M9C. |
| Wikidata access | JSON only, product User-Agent, one request at a time; respect failures/rate limits. |
| Network timeout | Short per-request timeout (M9C used a bounded timeout); overall preparation is cancelled when its opportunity becomes stale. |
| Cache | At most 100 local entries, SHA-256 key of normalized title/artist/identity requirement, 24-hour confirmed TTL and two-hour negative/ambiguous/failed TTL. |
| Stored cache contents | Requested metadata, match outcome, small fact card, direct source URLs, timestamps, and a safe explanation only. No raw page content, prompts, generated prose, or browser data. |
| Failure policy | No enrichment, no retry loop, no alternate target, and no catch-up delivery. |

The M9C cache was explicitly diagnostic-only. A future contract must decide whether a
runtime cache remains process-local, has the same ignored local-artifact form, or is an
approved bounded LocalAppData cache. That is a privacy/product decision, not a detail to
bury in implementation.

## Context-card contract

A candidate context card should be typed and self-contained rather than free-form web
text. Proposed fields are:

| Field | Purpose |
| --- | --- |
| Exact title and artist | Binds the card to the observed media metadata. |
| Confirmed MusicBrainz/Wikidata identity | Prevents title/artist guesswork. |
| One to three, at most five concise facts | Gives the model useful but bounded material. |
| Fact type and applicability | Distinguishes artist-wide from recording-specific claims. |
| Source name and direct HTTPS URL per fact | Preserves provenance for logs and review. |
| Editorial usefulness | Allows a weak/generic card to select `FALLBACK` before model work. |
| Card lifetime and request fingerprint | Supports cancellation and cache expiry without becoming listening history. |

Facts must be direct, source-backed, and proportionate. A generic nationality, a bare
birth/death year, or “is a musician” is normally not enough to earn speech. Context card
provenance belongs in local diagnostics and any manual-review artifact; URLs are not
spoken unless a later approved product decision says otherwise. Raw page content never
enters the card or the model prompt.

## Whole-comment generation and factual boundary

### Proposed composition contract

The selected candidate is a complete comment from `qwen3:14b-q4_K_M`, normally one or
two natural spoken sentences. It should use one supplied source-backed fact as the
anchor, optionally mention the exact title or artist when natural, and add at most one
clearly subjective Zoya reaction. Metadata mention is optional when the surrounding
radio context makes it clear; it must not be mechanically forced.

The proposed generation prompt would say, in substance:

- Treat only the exact metadata, selected context card, and explicitly supplied approved
  persona-memory item (if any) as factual material.
- Do not use, infer, or embellish from model training knowledge.
- Do not assert lyrics, sound, genre, emotion, motives, popularity, cultural meaning,
  biography, geography, chronology, other works, or an artist's usual tendencies unless
  the card explicitly supports the exact claim.
- Make a concise natural link, not an encyclopedia recitation; do not use generic DJ
  hype, imagined scenes, unsupported listener/session claims, or invented personal
  history.
- Return exact `FALLBACK` when the fact does not earn a good line.

`FALLBACK` is a valid safe answer, not an error, and must route to the accepted
non-enriched behavior or silence according to the normal delivery state. It must never
be spoken literally.

### Proposed output and validation layers

The current M9C evidence shows that a deterministic validator cannot prove that prose is
factually true. The design should use layered checks:

| Layer | Proposed hard rejection / discard conditions | Proposed advisory or human-review signals |
| --- | --- | --- |
| Mechanical format | Empty/malformed output, multi-answer output, control text, literal `FALLBACK` routing, unsafe length, prompt/meta language. | Slightly awkward phrasing or minor verbosity. |
| Metadata/card binding | Contradiction of exact metadata or card, new unsupported named entity/number/date, raw URL/source disclosure, title/artist substitution or Unicode corruption. | Natural omission of metadata when context is already clear. |
| Grounding policy | Asserted facts not supported by the card; unsupported lyric, sound, genre, emotional, cultural, geographic, historical, or biographical claims; invented persona history. | Clearly marked subjective reaction or harmless metaphor that does not assert a real-world fact. |
| Repetition/quality | Exact or high-confidence near duplicate, generic DJ cliché, fact-card recitation, banned imagined-scene construction. | Distinctive phrasing that needs owner review rather than automatic censorship. |

The most difficult category is semantic exaggeration: for example, turning “the exchange
temporarily closed” into “the song shut down the exchange.” A future contract must choose
whether the semantic guard treats this as a hard factual transformation, a conservative
discard, or a human-review-only flag. This draft recommends conservative discard whenever
the changed intensity or causal meaning is material, because speech cannot be recalled.

## Proposed repetition and recent-state policy

M8 already keeps bounded process-local recent state: 10 tracks and six accepted AI
comments. A future M9B design should extend that existing bounded state rather than add
a listening-history database:

- Retain six accepted enriched-comment fingerprints and their selected fact identity in
  memory only for the running process.
- Reject exact repeats and high-confidence structural repeats before audio preparation.
- Avoid reusing the same context-card fact for the same identity within the short memory
  window when another approved fact is available.
- Clear the state through the existing Clear History semantics and on application stop;
  do not write it as durable listening history.

The exact fingerprint/semantic-similarity rule needs fake-driven tests and owner review;
the 96 M9C.6 samples proved that exact-string checks alone are too weak.

## Model readiness, cost, and packaging

The candidate model is `qwen3:14b-q4_K_M`, selected from the M9C.5/M9C.6 experiment. It
is a local Ollama model, not a cloud service. M9C evidence measured quality, not a
product-ready desktop latency or memory budget. A future contract must record this
machine's actual first-load time, generation latency, RAM/VRAM residency, and unload
behavior before promising a radio experience.

The known trade-off is straightforward: 14B quality was materially better than the
accepted 4B experiment candidate, but it has a much higher disk and memory/VRAM cost.
It must be presented as an optional owner-installed local prerequisite, never silently
downloaded or bundled into the current package. The future contract must state the
official model size, quantization, license, expected resources, setup/removal steps, and
the exact fallback if it is absent.

Proposed readiness behavior:

- Do not hold Firefox delivery while a first model load occurs.
- Generate only during preparation; if readiness or generation misses the opportunity,
  discard that enriched candidate.
- Use loopback Ollama with a bounded request timeout and explicit selected-model unload
  (`keep_alive: 0` or the documented equivalent) after work.
- Verify unload only for the selected model and never terminate unrelated Ollama models
  or processes.
- When the 14B model is absent or disabled, retain the accepted M8 4B/local-only path;
  do not substitute a model silently and do not claim enriched behavior occurred.

Packaging must keep the existing M8B package contract intact. A future decision must
choose whether the optional model is documented as a separate user prerequisite or
whether a separately authorized later package workflow handles it. This proposal does
not choose either path.

## Playback, cancellation, and privacy invariants

No enriched candidate may weaken an accepted radio safety rule:

- Normal automatic delivery still never calls GSMTC pause, play, resume, or toggle.
- Only exactly one eligible logical `firefox.exe` media session and exactly one active
  Firefox Core Audio session may be used. Chrome and all unrelated applications remain
  excluded.
- Firefox application-session volume/mute is captured immediately before delivery,
  ducked proportionally, and restored exactly in protected cleanup after success,
  cancellation, failure, wrapper churn, or shutdown. Endpoint and master volume remain
  untouched.
- Track/session changes, Silent Mode, cancellation, target ambiguity, stale material,
  missed deadlines, network/model failure, invalid text, and Core Audio ambiguity all
  produce discard/silence, never retargeting or catch-up.
- Enrichment and generation are cancellable before delivery. A cancellation after ducking
  uses the existing protected speech/duck restoration path; it does not invent a second
  recovery mechanism.
- Enrichment diagnostics must remain local. Network-visible data stays limited to the
  stated MusicBrainz/Wikidata route; model-visible data stays loopback-only and limited
  to exact metadata plus the structured approved card.

## Likely implementation surface — for future review only

If Steve approves a contract, the smallest likely change set would be concentrated in
the existing radio composition/preparation boundary, not a new framework:

| Area | Likely responsibility |
| --- | --- |
| `MusicContextEnrichment` and typed card records | Promote/reuse only the safe resolver/card parts after runtime-cache and source decisions. |
| New enriched-comment composition service | Build a bounded prompt, route `FALLBACK`, request unload, and expose timing/reason codes. |
| Existing scheduler/preparation coordinator | Start cancellable enrichment/model work at Track A and discard stale work. |
| Existing validator/recent-comment state | Add card-binding, semantic/repetition guards, and bounded fingerprints. |
| Existing logs/activity UI | Present local, understandable reason codes and source provenance without raw web content. |
| Fake-driven tests and manual procedures | Prove source, cancellation, playback, and quality gates separately. |

Likely files can only be confirmed from the approved contract and a fresh code review.
This list is deliberately an impact map, not a permission to modify them now.

## Proposed acceptance stages

The following staged evidence should be part of a future contract, with exact thresholds
approved before implementation rather than retrofitted after it works.

1. **Contract and prerequisite review.** Steve approves the source set, data boundary,
   cache location/retention, model/size/license/resource disclosure, fallback policy,
   semantic-grounding policy, and provisional quality/latency thresholds. No model is
   installed automatically.
2. **Fake-driven engineering coverage.** Verify exact/ambiguous/no identity refusal,
   allowed-host enforcement, rate-limit/timeout/failure fallback, cache bounds,
   cancellation, no raw-page handling, source URL provenance, `FALLBACK` routing,
   Unicode preservation, repetition discard, local model unload, and complete M8
   playback/ducking regressions. These tests do not use real desktop media or speech.
3. **Generate-only factual/quality review.** Use a reviewed representative corpus and
   blinded owner scoring. Score factual grounding, natural context integration, host
   voice, spoken naturalness, variety, and “Would I want Zoya Radio to say this?” Keep
   hard safety cases in automated tests rather than spending Steve's qualitative review
   on them. The contract should specify a zero tolerance for ungrounded factual claims
   in accepted samples and a pre-agreed quality threshold.
4. **Silent desktop preparation trial.** With Firefox playing, observe many transitions
   while source/model preparation occurs. Confirm no GSMTC control, no ducking, no speech,
   no catch-up, and correct discard on track/session/Silent Mode changes.
5. **Bounded audible delivery trial.** Only after the preceding gates pass, exercise
   normal schedule delivery across multiple tracks. Confirm generation completes before
   ducking, only Firefox is ducked/restored exactly, Chrome stays untouched, cancellation
   restores audio, fallback/silence is safe, and no stale comment is spoken.
6. **Owner decision.** Steve reviews artifacts, logs, resource measurements, and manual
   results. Passing an experiment does not auto-authorize packaging or any later
   milestone; acceptance is explicit.

## Decisions Steve must make before any M9B implementation

1. Approve or reject M9B as the next bounded milestone and its exact name.
2. Approve the allowed live sources: MusicBrainz/Wikidata only, and whether any curated
   card catalog or additional structured adapter is allowed.
3. Approve the runtime cache location, retention, clearing behavior, and whether it is
   process-only or a bounded local persistent cache.
4. Approve the 14B model as an optional local prerequisite after reviewing its official
   size, license, disk/RAM/VRAM estimates, and actual machine measurements.
5. Approve absence/disabled-model behavior: continue accepted M8 local-only composition
   versus remain silent for that opportunity; this draft recommends preserving M8.
6. Approve the semantic-grounding rule for material exaggeration and the exact hard versus
   advisory validation split.
7. Approve measurable quality, fallback-rate, latency, and manual acceptance thresholds.
8. Approve whether source URLs/provenance appear only in local logs/artifacts or have any
   future UI representation. This draft recommends local diagnostics only.

## Recommendation

Use this proposal as the starting point for a bounded M9B contract, not as the contract
itself. The strongest evidence supports local, grounded 14B whole-comment synthesis,
prepared before delivery and allowed to fail silently. The first contract should resist
the temptation to make it a general web-aware DJ. Keep sources narrow, cards structured,
fallback honest, and the existing M8 playback safety rules completely boring—which is
exactly what we want when someone else's music is playing.
