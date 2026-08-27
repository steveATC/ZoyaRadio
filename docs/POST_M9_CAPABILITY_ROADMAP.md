# Post-M9 capability roadmap

## Status and authority

Steve approved this capability roadmap as the project's post-M9 direction on
August 21, 2026. It defines sequencing and architectural boundaries. It does not
make any capability implemented, change the accepted Milestone 8 runtime, pass a
milestone, or authorize work beyond a separately approved bounded milestone
contract.

Steve approved the post-M11 format-aware generation and staged voice/production
refinements on August 24, 2026. That approval preserves the sequence below and its
capability boundaries; it does not authorize implementation, dependencies, or a
commit for any future milestone.

Milestone 9 remains closed as prototype/design evidence. M10A, M10B, M10C, M11A,
and M11B have since passed under their bounded contracts and acceptance records.
M11B proved commentary, one fixed reusable liner, and direct music continuation as
deterministically arbitrated clients; it did not accept the current commentary
prompt as a mature radio-host format. Steve accepted the pure runtime-unreachable
[M12A context assembler](MILESTONE_12A_CONTRACT.md) on August 24, 2026. The bounded
[M12B contract](MILESTONE_12B_CONTRACT.md) was subsequently approved and implemented
as an isolated 360/360 candidate. Steve reviewed its fixtures and accepted M12B on August 26, 2026. Steve accepted [M12C](MILESTONE_12C_CONTRACT.md) after its 366/366 automated result and blinded qualitative review. It selected quick reaction only for later bounded work, retained deterministic handoff as the no-model reference, and deferred session-aware observation for revision/retest. M12C remains local-only and unreachable from normal runtime. Steve subsequently authorized and accepted M12D D3.2 and VAR1 with limitations; the default-off pilot remains bounded and no further milestone or commit is authorized.

## Product strategy

Build one opinionated, music-first Zoya Radio before extracting a general station
builder. Formats are clients and vertical tests of shared capabilities, not
isolated feature silos. A commercial, call, rant, liner, ID, callback, or chained
bit may begin as a fixture, mature through bounded experiments and prepared
runtime integration, then gain continuity, multiple voices, production treatment,
and sonic-identity integration only when shared capabilities justify those steps.

The deterministic system remains the broadcast controller. Creative AI may
propose premises and words, but it never controls whether the station speaks,
which target is used, priority, timing, cooldown, delivery, playback, safety,
expiry, memory commits, or whether generated material earns airtime. Silence and
direct music continuation remain fully valid editorial outcomes.

Exact cadence, cooldown, retention, promotion, and decay values are conservative
milestone-local experimental defaults. They become durable policy only after real
listening evidence and a separate decision.

## Architectural responsibility boundaries

| Responsibility | Owns | Must not own |
| --- | --- | --- |
| Deterministic opportunity kernel | Pure transition/session state and opportunity decisions | Generation, audio, network access, or playback |
| Prepared material lifecycle | Binding, state, reservation, expiry, release, and disposal | Editorial invention or a right to airtime |
| Editorial Arbiter | Preparation and airtime choice, music pressure, spacing, priority, treatment compatibility, typed reasons, and silence | Creative writing or playback control |
| Creative Context Assembler | A compact packet of established deterministic facts and approved state | Raw provider payloads, invented facts, or scheduling |
| Creative Direction | What a potential segment should accomplish: premise, intent, format, cast/performance/lore intent | Exact scheduling, memory commits, or playback |
| Content Generator | Exact script or renderable content for an approved creative brief | Inventing broadcast authority or bypassing factual boundaries |
| Music Knowledge | Source-backed canonical artist/track/fact evidence with provenance and typed lookup outcomes | Scheduling or inferred listener preference |
| Listening Intelligence | Deterministic aggregates derived from actual listening with explicit evidence coverage | Permanent raw listening diary or unsupported classification |
| Station Lore | Approved station-world entities, bounded open threads, and completed-airtime effects | Raw brainstorm output or narrative obligation |
| Voice capability | Host/voice identity separation, renderer adapters/capabilities, semantic performance intent, cast roles, language/locale, and fallback | Scene timing, audio mixing, station branding, or treating one backend as permanent |
| Prepared Scene Compiler | Deterministic component ordering/overlap, real-duration timeline resolution, ownership, cancellation, disposal, and compile validation | Renderer selection, DSP/mixing policy, scheduling, or sonic-brand decisions |
| Audio Production / Mixing | Gain staging, normalization, filters, beds, fades, reusable FX, final scene rendering, and production-resource cleanup | Editorial selection, voice identity, or station-lore authority |
| Sonic Identity / Imaging | Canonical sound assets, motifs, IDs, stings, bumpers, announcer treatment, imaging grammar, and provenance | Safe scheduling, scene-engine mechanics, or general station configuration |
| Safe playback layer | Positively validated target operations and protected restoration | Editorial decisions, guessing targets, or blind toggles |

Creative Direction and exact Content Generation are conceptually separate even
when an early experiment performs both in one local model invocation. This keeps
premises independently cacheable and allows later format-specific writers,
renderers, cast direction, or models without prematurely requiring two calls.

Creative requests are format-aware producer assignments rather than one universal
`LLM + facts -> commentary` interface. At the level needed by the client, a request
identifies the speaker/role, on-air situation, specific format, editorial objective,
duration/structure, trusted context and unknowns, creative permissions, and output
contract. Stable personality remains separate from the assignment. This principle
does not freeze field names, prompt wording, prompt count, or model topology.

Trusted context supplies permissions, not a checklist. A generator may use none,
one, or several relevant facts; it must not mention facts merely because they were
provided. Fact scope remains explicit: artist-level evidence does not become a
claim about a track's lyrics, sound, mood, genre, or meaning. Host reactions,
session observations, commercials, calls/dialogue, imaging, fictional event promos,
callbacks, and later chains/arcs use independent direction, generation, validation,
and evaluation contracts. A successful prompt for one format proves nothing about
the others.

## Capability phases and milestones

### Phase A - Deterministic broadcast foundation

#### M10A - Deterministic Opportunity Kernel

- **Purpose:** reproduce accepted M8 opportunity decisions in pure, deterministic,
  directly testable code.
- **Why now:** every later lifecycle, arbitration, memory, and format capability
  needs a stable decision foundation; this can be proved without risking live
  playback.
- **Hard dependencies:** accepted M8 behavior and characterization traces.
- **Responsibilities:** pure internal inputs, state, decisions, typed reasons,
  injected transition targets, and golden/characterization parity tests.
- **Explicit non-goals:** runtime connection, new format, persistence, AI,
  knowledge, voice work, audio, or changed cadence.
- **Accepted behavior preserved:** all M8 scheduling, duplicate/unusable handling,
  Silent Mode, cancellation, rebind, clear-history, one-ahead preparation, rapid
  transition refusal, and safe-silence semantics.
- **Unlocks:** lifecycle extraction without mixing it with runtime migration.
- **Main risks:** copying only the happy path, accidentally changing schedule
  semantics, or making the candidate reachable from live entry points.
- **Automated verification:** Release build, complete fake-driven suite, focused
  golden traces, and proof that normal launch paths do not reference the kernel.
- **Steve/manual evaluation:** none; this milestone is intentionally inaudible.
- **Next action type:** Implementation, but only after separate contract approval.

#### M10B - Prepared Material Lifecycle and Binding

- **Purpose:** define the deterministic lifecycle of potential prepared content.
- **Why now:** arbitration and multiple formats need shared rules for material
  that may be prepared but has no right to airtime.
- **Hard dependencies:** M10A accepted.
- **Responsibilities:** typed material identity, opportunity/track/session binding,
  prepared state, reservation, release, expiry, completion, and disposal;
  distinguish reusable inventory from single-opportunity material.
- **Explicit non-goals:** choosing a format, creative generation, persistent lore,
  live audio, or new cadence policy.
- **Accepted behavior preserved:** one M8 comment opportunity, stale-work discard,
  no catch-up queue, cancellation, and silence.
- **Unlocks:** a safe ownership model for liners, comments, commercials, calls,
  and future prepared scenes.
- **Main risks:** leaking resources, replaying bound content in the wrong context,
  or confusing generated, prepared, selected, aired, and canonical state.
- **Automated verification:** lifecycle transition tables, invalid-transition
  refusal, idempotent disposal, reservation conflict, expiry, release, and binding
  tests.
- **Steve/manual evaluation:** none unless a debug trace needs readability review.
- **Next action type:** Implementation contract after M10A acceptance.

#### M10C - Accepted M8 Runtime Migration

- **Purpose:** move accepted M8 runtime behavior onto M10A/M10B with strict parity.
- **Why now:** the foundation has value only when the live path can use it without
  changing the station experience.
- **Hard dependencies:** M10A and M10B accepted.
- **Responsibilities:** adapt current observation, preparation, delivery, Silent
  Mode, rebind, history, and shutdown paths to the new foundation; retain legacy
  behavior until parity is demonstrated.
- **Explicit non-goals:** Editorial Arbiter, new format, new voice, knowledge,
  persistence, program breaks, or cadence tuning.
- **Accepted behavior preserved:** the full accepted M8 safety and delivery
  baseline, including no automatic pause/resume and exact Firefox restoration.
- **Unlocks:** safe removal of duplicated legacy scheduling state and a stable live
  base for editorial arbitration.
- **Main risks:** race/cancellation drift, activity-status regressions, or hidden
  differences between test and desktop timing.
- **Automated verification:** full regression suite, parity traces, Release builds,
  formatting, and protected cleanup/failure tests.
- **Steve/manual evaluation:** focused real-desktop parity: ordinary delivery,
  Silent Mode, Clear History, Stop/window close, Firefox-only ducking/restoration,
  Chrome isolation, and safe refusal.
- **Next action type:** Manual Test, then Decision on acceptance.

### Phase B - Editorial arbitration

#### M11A - Pure Editorial Arbiter

- **Purpose:** choose deterministically among eligible content or silence.
- **Why now:** multiple formats must not be added before the station has a coherent,
  explainable editorial clock.
- **Hard dependencies:** M10C accepted.
- **Responsibilities:** separate preparation arbitration from airtime arbitration;
  classify opportunities; enforce hard eligibility, binding, duration, freshness,
  cooldown, capability and delivery-treatment compatibility; account for currently
  available music-first pressure, speech density, spacing, policy priority, and
  bounded randomness among editorially equivalent choices; reserve one result;
  emit typed decision reasons; preserve silence as a candidate.
- **Explicit non-goals:** Music Knowledge, Listening Intelligence, Station Lore,
  Creative Direction, speculative universal relevance scoring, or live new-format
  integration.
- **Accepted behavior preserved:** M8 commentary is the only substantive format;
  existing cadence and safety remain unchanged unless the contract defines an
  isolated experimental default.
- **Unlocks:** incremental deterministic relevance contributors and a second-format
  proof without `random.choice(eligible_content)`.
- **Main risks:** overbuilding relevance before evidence exists, hiding policy in
  numeric scores, or allowing randomness to create the reason to interrupt music.
- **Automated verification:** table-driven conflict scenarios, explicit silence
  wins, stable reason categories, deterministic seeded ties, treatment mismatch,
  final revalidation, and reservation tests.
- **Steve/manual evaluation:** reason-trace readability only; no quality claim yet.
- **Next action type:** Implementation contract after M10C acceptance.

Future context domains may add small deterministic relevance contributions through
typed extension points. M11A evaluates only facts genuinely available at M11A and
does not force future systems into a speculative universal relevance model.

#### M11B - Commentary and Reusable Liner Proof

- **Purpose:** prove that one arbiter can handle M8 commentary plus a tiny reusable
  liner while often choosing music.
- **Why now:** a micro-liner is the smallest useful second-format vertical test.
- **Hard dependencies:** M11A accepted.
- **Responsibilities:** fixed reusable liner fixture, independent eligibility and
  cooldown, reusable release after missed opportunities, compatible overlay
  treatment, reservation, final safety revalidation, and explainable selection.
- **Explicit non-goals:** AI liners, station imaging system, lore, commercials,
  calls, or permanent cadence values.
- **Accepted behavior preserved:** commentary behavior and safe silence; liner
  eligibility cannot force speech or displace safety.
- **Unlocks:** confidence that shared arbitration works before richer content.
- **Main risks:** liner annoyance, accidental priority inflation, or stale
  reservation.
- **Automated verification:** two-format conflict matrices, spacing, reuse/release,
  silence, and runtime regression tests.
- **Steve/manual evaluation:** conservative listening trial focused on music flow,
  annoyance, and decision-log plausibility.
- **Next action type:** Evaluation, then policy adjustment or acceptance.

### Phase C - Creative content foundation

#### M12A - Creative Context Assembler v1

- **Purpose:** create a compact, deterministic, trusted brief from state already
  available at this point.
- **Why now:** creative systems need a factual boundary before more prompts or
  formats are tested.
- **Hard dependencies:** M11A; M11B is a useful prerequisite, not a hard one.
- **Responsibilities:** versioned packet schema; current track/session facts,
  recent accepted comments, opportunity reason, recent-use/cooldown facts, and
  explicit unknowns; bounded size and provenance labels.
- **Explicit non-goals:** provider JSON, Music Knowledge, durable profiling, lore,
  invented facts, scheduling, or playback.
- **Accepted behavior preserved:** missing context remains unknown and cannot make
  a safe opportunity speak.
- **Unlocks:** reusable generation experiments and later incremental context
  contributors.
- **Main risks:** prompt-shaped schemas or leaking internal/provider structures.
- **Automated verification:** deterministic packet snapshots, size bounds,
  omission/unknown handling, and inert hostile metadata fixtures.
- **Steve/manual evaluation:** inspect representative packets for usefulness and
  factual restraint.
- **Next action type:** Documentation/Implementation contract.

#### M12B - Creative Direction and Content Generation Boundary

- **Purpose:** establish reusable interfaces between creative intent and exact
  script generation.
- **Why now:** formats should share an experiment harness without requiring one
  giant benchmark or one giant AI controller.
- **Hard dependencies:** M12A accepted.
- **Responsibilities:** typed creative brief/premise, format goal, constraints,
  speaker/role, on-air situation, editorial objective, duration/structure,
  evergreen/context-bound flag, optional cast/performance/lore intent, trusted
  context permissions and fact scope, and a format-specific output contract;
  generator request/result; validation/fallback/discard outcomes; a reusable
  generate-only Format Laboratory harness. One invocation may initially fill both
  roles, while preserving direction and exact wording as distinct responsibilities.
- **Explicit non-goals:** testing every future format, runtime scheduling, durable
  premise storage, one universal prompt/schema, required multi-model routing,
  audio, or lore commits.
- **Accepted behavior preserved:** AI output has no airtime right and unsupported
  facts remain prohibited. Supplied facts are optional creative material rather
  than required talking points, and missing context remains unknown.
- **Unlocks:** small evidence-driven benchmark batches for host, commercial, and
  dialogue concepts.
- **Main risks:** a supposedly conceptual boundary becoming unnecessary ceremony,
  stable personality and situation-specific instructions collapsing into one giant
  prompt, facts becoming a recital checklist, or exact prose leaking back into
  deterministic direction state.
- **Automated verification:** schema/version tests, failure/fallback containment,
  bounded alternatives, factual-scope fixtures, optional-fact-use accounting, and
  proof the lab cannot access playback/audio.
- **Steve/manual evaluation:** inspect whether briefs express useful intent without
  prematurely writing or scheduling the segment.
- **Next action type:** Evaluation framework.

#### M12C - Host Format Laboratory, Batch 1

- **Purpose:** evaluate a deliberately small set of host formats before choosing
  runtime clients.
- **Why now:** evidence should select formats rather than milestone labels assuming
  every idea deserves implementation.
- **Hard dependencies:** M12B accepted.
- **Responsibilities:** one bounded batch such as straight announcement, quick
  reaction, and session-aware observation; a small comparison of format-specific
  producer-assignment variants; fixed scenarios, blinded artifacts, qualitative
  rubric, mechanical outcomes, and explicit selection/rejection.
- **Explicit non-goals:** long rants, callbacks, chains, arcs, commercials, calls,
  runtime integration, or a permanent repertoire.
- **Accepted behavior preserved:** normal radio remains untouched.
- **Unlocks:** a defensible initial host-format set; later batches reuse the lab
  only when earlier evidence justifies them.
- **Main risks:** too many dimensions in one batch or benchmark artifacts that do
  not resemble real listening.
- **Automated verification:** complete bounded run, arm blinding, exact fixture
  accounting, local model unload, and no runtime/audio access.
- **Steve/manual evaluation:** score whether it sounds like radio, format
  fulfillment, naturalness, personality, grounding, useful rather than compulsory
  context use, fact-recital avoidance, generic-prose avoidance, structural variety,
  format-appropriate concision, and desire to hear it.
- **Next action type:** Evaluation by Steve.

#### M12D - Selected Contextual Host Integration

- **Status:** Accepted bounded D3.2 topic-isolation pilot and VAR1 format-variety evaluation with limitations. Further work requires a separate decision.

- **Purpose:** add only the host formats selected by M12C to prepared runtime
  arbitration.
- **Why now:** quality evidence and deterministic controls both exist.
- **Hard dependencies:** M11B, M12A, M12B, and an accepted M12C selection.
- **Responsibilities:** prepare selected format candidates, type their binding and
  delivery-treatment compatibility, arbitrate against commentary/liner/music,
  discard unselected output, and log explainable lifecycle decisions.
- **Explicit non-goals:** formats rejected by the laboratory, durable lore,
  provider facts, program breaks, or fixed final cadence.
- **Accepted behavior preserved:** M8 safety, music-first silence, bounded work,
  no catch-up, and no automatic pause/resume.
- **Unlocks:** session-conditioned host variety and real evidence for later context
  and memory work.
- **Main risks:** quality regressions outside benchmark fixtures and excessive
  speech density.
- **Automated verification:** arbiter/lifecycle/runtime integration, stale and late
  cancellation, fallback, and complete regression tests.
- **Steve/manual evaluation:** extended listening for usefulness, pacing, variety,
  and annoyance using deliberately conservative experimental clocks.
- **Next action type:** Evaluation, then Decision.

### Phase D - Knowledge, listening intelligence, memory, and continuity

#### M13A - Source-Agnostic Music Knowledge Resolution

- **Purpose:** turn independently approved provider evidence into canonical,
  source-backed Music Knowledge without making it a scheduling dependency.
- **Why now:** trusted genre/style and identity evidence can improve context, but
  M9 proved provider availability and schema quirks cannot be treated as truth or
  as a foundation for safe broadcast behavior.
- **Hard dependencies:** none from core scheduling; M12A is the useful consumer
  boundary. Existing M9 evidence informs design but is not accepted runtime.
- **Responsibilities:** provider-specific Source Adapters; conservative,
  deterministic Entity/Identity Resolution; fact-type-specific Evidence/Fact
  Reconciliation; categorical evidence states; typed lookup outcomes; bounded
  cache/retry policy; canonical artist/recording/release/credit/alias facts; small
  evidence-driven genre/style normalization with provenance and multiple tags.
- **Explicit non-goals:** five-provider integration, unrestricted fuzzy matching,
  raw JSON in prompts, LLM fact adjudication, fake numeric confidence, mandatory
  enrichment, or a universal genre ontology.
- **Accepted behavior preserved:** exact observed title/artist remain factual
  playback input; knowledge failure yields unavailable context, never unsafe
  scheduling or forced speech.
- **Unlocks:** reusable trusted evidence for Listening Intelligence and Creative
  Context, provider resilience, and future corroboration without subsystem rewrite.
- **Main risks:** false entity matches, source formatting leaking into canonical
  identity, stale negative cache, or over-normalized genre claims.
- **Automated verification:** recorded schema fixtures; nested/collaboration credit,
  alias, edition/remaster/live, punctuation/case, mismatch, outage, timeout, parser,
  ambiguity, cache, provenance, conflicting-fact, and taxonomy tests.
- **Steve/manual evaluation:** inspect a bounded familiar-track evidence report;
  no runtime listening gate is required for the initial service.
- **Next action type:** Research/Implementation contract when its consumers need it.

#### M13B - Session and Recent-Session Listening Intelligence

- **Purpose:** derive bounded evidence about actual listening without retaining a
  permanent chronological diary.
- **Why now:** session-conditioned creativity needs transparent counts before any
  durable affinity experiment.
- **Hard dependencies:** M13A for trusted genre/style aggregation; artist/track
  counts can be prototyped without it.
- **Responsibilities:** session-local counts, recency, repetition, artist/track
  frequency, recent-session compressed summaries, trusted tag/category counts,
  metadata coverage, and both denominators: all observed tracks and tracks with
  usable classification.
- **Explicit non-goals:** permanent event log, unsupported genre inference, durable
  preference declarations, fixed decay equations, or scheduling authority.
- **Accepted behavior preserved:** current bounded ephemeral history and Clear
  History semantics until a separate persistence policy is approved.
- **Unlocks:** evidence such as `17/35 observed` and `17/26 classified`, with the
  Creative Context Assembler free to present a simpler but coverage-aware summary.
- **Main risks:** sparse metadata appearing dominant or detailed events leaking
  into durable storage.
- **Automated verification:** denominator, unknown-data, multi-tag, duplicate,
  session-boundary, bounded-retention, clear/reset, and deterministic summary tests.
- **Steve/manual evaluation:** compare summaries with recognizable listening
  sessions and judge whether they are useful rather than creepy or misleading.
- **Next action type:** Evaluation.

#### M13C - Durable Compact Affinity Experiment

- **Purpose:** test whether reinforced/decaying compact tendencies improve Zoya
  across sessions.
- **Why now:** only M13B evidence can show which aggregates deserve durability.
- **Hard dependencies:** accepted M13B evaluation.
- **Responsibilities:** experimental compact artist/track/tag relevance,
  reinforcement count, last reinforcement, coarse strength/state, trend direction,
  and bounded recent-session support; explicit promotion/decay policy subject to
  evaluation.
- **Explicit non-goals:** indefinite listening timeline, immutable preference
  labels, permanent equations selected by architecture alone, or raw event replay.
- **Accepted behavior preserved:** unknown remains unknown; failure or empty state
  does not affect safe scheduling.
- **Unlocks:** optional medium/long-term context grounded in repeated behavior.
- **Main risks:** overfitting, stale affinity, surveillance-like retention, or
  tuning equations without enough listening evidence.
- **Automated verification:** bounded-store, reinforcement, provisional decay,
  expiry, reset, migration, corruption, and no-event-log tests.
- **Steve/manual evaluation:** longitudinal usefulness and comfort review before
  enabling durable use in creative prompts.
- **Next action type:** Experiment/Evaluation; runtime integration is a later
  decision, not automatic.

#### M13D - Station Lore and Open Threads

- **Purpose:** preserve only useful station-world continuity and allow bounded,
  optional follow-ups.
- **Why now:** cross-format callbacks need structured state, but raw generation
  must die cheaply.
- **Hard dependencies:** M12B; actual clients and M13E are useful prerequisites.
- **Responsibilities:** approved entities; active, dormant, expired, and retired
  threads; last-related airtime, completed/unused beats, cooldown-relevant state;
  minimal inspect/reset/debug; deterministic promotion boundary.
- **Explicit non-goals:** polished lore UI, automatic serialized story arcs,
  permanent raw alternatives, or a generic database of every generated joke.
- **Accepted behavior preserved:** generated or unaired content is not canon;
  thread existence creates possibility, never scheduling obligation.
- **Unlocks:** a commercial may later support a host callback or caller response
  without becoming narrative debt.
- **Main risks:** AI landfill, endless sitcom garbage, accidental canon from
  rejected/partial content, or unbounded growth.
- **Automated verification:** lifecycle, expiry/retirement, boundedness, reset,
  promotion, non-promotion of raw alternatives, and corruption handling.
- **Steve/manual evaluation:** inspect/debug state for clarity and approve the first
  promotion policies; no polished interface is required.
- **Next action type:** Implementation contract when a real continuity client
  exists.

Lore promotion is explicit:

1. Raw Creative Director alternatives are ephemeral and disappear when not
   selected.
2. A selected/prepared proposal may remain attached to that prepared item only
   while its deterministic lifecycle remains valid.
3. Steve may explicitly approve an entity or proposal.
4. Complete airtime may create one bounded pending proposal or predefined effect
   when the format contract authorizes it.
5. Rejected, expired, discarded, or partially heard material creates no canon.

#### M13E - Minimal Airtime Effects and Context Assembly v2

- **Purpose:** commit the smallest demonstrated continuity effects only after
  complete successful airtime and expose approved lore/intelligence/knowledge to
  creative context.
- **Why now:** real formats need transactional continuity, but a giant generic lore
  transaction system would be premature.
- **Hard dependencies:** M12A, M13D, and at least one real client effect.
- **Responsibilities:** idempotent typed effects such as record complete airtime,
  update last use, touch an existing thread, or create one bounded pending review
  proposal; assemble canonical Music Knowledge, coverage-aware Listening
  Intelligence, approved lore, open threads, recent segments, and explicit
  unknowns into a compact packet.
- **Explicit non-goals:** arbitrary generated writes, commit on partial/unheard
  endings, raw provider payloads, raw listening events, scheduling, or a universal
  transaction language.
- **Accepted behavior preserved:** unaired/rejected/partial content has no canonical
  effect; complete commits are idempotent and predefined.
- **Unlocks:** optional, editorially earned cross-format continuity.
- **Main risks:** duplicate commits after retries or excessive context size.
- **Automated verification:** complete/partial/cancel/retry matrices, idempotency,
  allowed-effect whitelist, compact packet bounds, provenance, and unknown handling.
- **Steve/manual evaluation:** inspect one or two end-to-end thread histories and
  context packets before broader use.
- **Next action type:** Evaluation, then incremental extension by demonstrated
  client need.

### Phase E - Voice and localization foundation

#### M14A - Host Voice and Renderer Abstraction Foundation

- **Purpose:** make Nicole the accepted default voice for the current Zoya build,
  not the conceptual definition of Zoya.
- **Why now:** future testers may prefer another voice, and this separation should
  exist before serious cast work or distribution.
- **Hard dependencies:** accepted current speech preparation; independent auditions
  may happen earlier.
- **Responsibilities:** host identity separate from voice identity and renderer;
  replaceable renderer/backend adapter boundary; small approved alternative host
  voice; explicit capability descriptions for voice/language/performance/resource
  needs; deterministic selection and fallback; unavailable-voice containment;
  cancellation/disposal ownership; licensing/provenance record.
- **Explicit non-goals:** large configuration UI, arbitrary downloaded voices,
  multi-character casting, choosing an expressive backend without evaluation,
  production-scene integration, audio mixing/FX, full performance direction, or
  localization.
- **Accepted behavior preserved:** Nicole default, local CPU preparation, Windows
  local TTS fallback, pre-delivery failure containment, and no bundled unapproved
  dependency.
- **Unlocks:** tester-selectable host voice plus clean seams for renderer evaluation, cast roles, localization, and later production integration.
- **Main risks:** voice availability/licensing or fallback changing perceived host
  identity.
- **Automated verification:** selection, capability, unavailable voice, fallback,
  cancellation, disposal, and package-boundary tests.
- **Steve/manual evaluation:** audition and approve the small alternative set.
- **Next action type:** Research/Evaluation may precede implementation.

#### M14B - Expressive Renderer Capability Evaluation

- **Purpose:** evaluate provider-neutral local voice backends for normal Zoya host
  delivery before any backend is trusted inside produced scenes.
- **Why now:** host suitability, expressive control, language support, resource cost,
  and fallback are voice/backend questions; solving them once does not prove later
  scene-production quality.
- **Hard dependencies:** accepted current speech-preparation safety. M14A is a useful
  integration boundary, but an isolated render-only audition may occur earlier.
- **Responsibilities:** bounded disclosed candidate set; normal-host voice quality;
  voice identity and multi-speaker potential; actually supported performance
  controls; latency and CPU/GPU/RAM/VRAM measurements; reliability, repeatability,
  cancellation, unload/disposal, and fallback feasibility; EN and PT-BR capability
  where claimed; license, model-size, hardware, privacy, and local-first review.
- **Explicit non-goals:** approving Chatterbox or any named backend by roadmap text,
  automatic dependency installation, runtime selection, scene assembly, mixing/FX,
  voice cloning, or claiming isolated speech proves produced-radio quality.
- **Accepted behavior preserved:** Nicole/Kokoro and Windows fallback remain the
  current accepted route unless a later bounded contract integrates an approved
  result; the radio and desktop media remain untouched during isolated evaluation.
- **Unlocks:** evidence for a separately authorized renderer integration and clearer
  capability requirements for cast, localization, and production scenes.
- **Main risks:** choosing on one attractive sample, hidden hardware cost, unstable
  voice identity, weak PT-BR, unsupported control claims, or licensing surprises.
- **Automated verification:** bounded artifact/accounting harness, truthful capability
  reporting, cancellation/disposal, resource measurements, fallback isolation, and
  proof that the evaluation cannot access playback or normal runtime.
- **Steve/manual evaluation:** host fit, expressiveness without gimmickry, clarity,
  fatigue, identity consistency, EN/PT-BR quality where applicable, and value versus
  resource cost.
- **Next action type:** Research/Evaluation contract; no candidate is approved merely
  because it appears in this roadmap.

Chatterbox is one example candidate for a future bounded comparison. The eventual
contract must review the then-current candidate versions and alternatives rather
than treating this roadmap mention as a dependency choice.

#### M14C - Cast and Role Capability

- **Purpose:** support announcer, caller, spokesperson, guest, and other roles
  without assuming every role requires a distinct voice.
- **Why now:** commercials and calls need reusable casting rather than format-local
  voice switches.
- **Hard dependencies:** M14A; accepted M14B renderer evidence and a multi-voice
  format experiment are useful prerequisites.
- **Responsibilities:** role/cast registry, compatible voice/capability requirements,
  deterministic assignment/fallback, same-voice allowance, and explicit refusal
  when no approved renderer can satisfy a role.
- **Explicit non-goals:** unlimited characters, recurring-character lore by default,
  audio rendering, multi-voice resource ownership, full scene production, or
  voice-cloning.
- **Accepted behavior preserved:** host fallback and preparation safety.
- **Unlocks:** reusable cast plans consumed by later commercial/call/dialogue renderer and scene-production capabilities.
- **Main risks:** indistinct speakers, poor local second voice, role proliferation,
  or assignment beyond an approved renderer's capabilities.
- **Automated verification:** role resolution, same-voice allowance, fallback,
  unavailable cast member, capability mismatch/refusal, and deterministic assignment.
- **Steve/manual evaluation:** dialogue intelligibility and believability audition.
- **Next action type:** Experiment/Evaluation before runtime integration.

#### M14D - Language and Locale Foundation

- **Purpose:** keep language, locale, voice, and host identity separable.
- **Why now:** English/Nicole assumptions must not become inseparable from the
  engine before future private-alpha work.
- **Hard dependencies:** M12A and M14A are useful prerequisites; M14B may supply
  renderer evidence, while PT-BR feasibility research may happen earlier.
- **Responsibilities:** explicit language and locale values; localized deterministic
  templates; locale-aware creative constraints; renderer pronunciation/capability
  checks; fallback/refusal; EN and PT-BR bounded experiment.
- **Explicit non-goals:** full translation system, every locale, automatic cultural
  adaptation, stereotypes, or station-builder UI.
- **Accepted behavior preserved:** current English behavior remains default and
  unavailable locale/voice combinations fail safely.
- **Unlocks:** evidence-based localization and later proven configuration.
- **Main risks:** superficially translated but culturally poor output or unsupported
  pronunciation.
- **Automated verification:** language/locale separation, template selection,
  unsupported combinations, fallback, Unicode, and prompt packet tests.
- **Steve/manual evaluation:** bilingual EN/PT-BR naturalness, pronunciation, and
  cultural-fit review.
- **Next action type:** Research/Evaluation, then bounded implementation.

Performance direction begins as small renderer-neutral semantic intent in
creative/voice requests. M14B discovers which controls actually work; later M17B
maps only supported intent into a production renderer. Expressiveness is neither
bundled into M14A nor assumed from a provider's advertised feature list.

### Phase F - Delivery grammar and program-space safety

#### M15A - Delivery Treatment Model and Exact-Target Hold Diagnostic

- **Purpose:** model delivery treatment independently from content format and test,
  in isolation, whether an exact validated transition target can be safely held.
- **Why now:** produced breaks require a deliberate safety gate; content duration
  must never become implicit permission to pause Firefox.
- **Hard dependencies:** M10C; treatment compatibility should be represented by
  M11A before runtime formats depend on it.
- **Responsibilities:** content declares allowed, preferred, and required treatments;
  opportunities expose available treatments; arbiter considers compatibility;
  isolated explicit diagnostic validates exact-target hold, user-intent override,
  replaced/ambiguous refusal, no blind toggles, ownership, and protected cleanup.
- **Explicit non-goals:** enabling automatic program breaks, classifying all long
  content as held, changing ordinary commentary, or authorizing M15B.
- **Accepted behavior preserved:** automatic runtime still never pauses/resumes;
  user Play/Pause/track-change intent wins; unsafe state yields silence.
- **Unlocks:** an evidence-based decision on optional `HoldAndReplace`.
- **Main risks:** overriding user intent, resuming a replaced target, or conflating
  format/duration with delivery treatment.
- **Automated verification:** treatment matrices plus exact-target, user override,
  zero/multiple/replaced target, cancellation, exception, and no-blind-toggle tests.
- **Steve/manual evaluation:** major explicit desktop safety gate; success is
  diagnostic evidence only.
- **Next action type:** Manual Test, then separate Decision.

Examples of deliberate grammar, not fixed policy: a short comment or rant may use
a ducked overlay; a caller may allow overlay or program break; a commercial or
produced scene may require a program break; a liner/ID may require an intro/outro
overlay. No rule equivalent to `duration > X => pause Firefox` is permitted.

#### M15B - Opt-In HoldAndReplace Program-Break Primitive

- **Purpose:** provide an optional safe delivery capability for genuine station
  breaks.
- **Why now:** only an accepted M15A diagnostic can justify revisiting the current
  no-pause invariant.
- **Hard dependencies:** M15A manual acceptance and a separate explicit Steve
  authorization.
- **Responsibilities:** opt-in policy, exact validated target hold/resume, explicit
  ownership, user-intent override, final revalidation, safe non-resume, protected
  cleanup, and compatible-treatment arbitration.
- **Explicit non-goals:** default enablement, blind media toggles, duration-based
  activation, or forcing long content to use it.
- **Accepted behavior preserved:** ordinary host overlay may remain unchanged;
  missing/ambiguous/replaced targets and ownership uncertainty produce silence and
  no automatic resume.
- **Unlocks:** prepared commercials, scenes, and calls that genuinely require
  program space.
- **Main risks:** playback ownership mistakes with high user impact.
- **Automated verification:** full safety matrix and runtime regressions.
- **Steve/manual evaluation:** repeated real-desktop hold/resume, user intervention,
  track change, Stop, shutdown, and failure acceptance.
- **Next action type:** separate Implementation authorization; never automatic
  after M15A.

### Phase G - Procedural format expansion

#### M16A - Fixed Long-Form Plumbing Fixture

- **Purpose:** prove scheduling, lifecycle, delivery-treatment, and runtime plumbing
  with one prebuilt commercial.
- **Why now:** a known fixture isolates plumbing before procedural quality.
- **Hard dependencies:** M11B, M10B, and the required accepted treatment capability;
  if the fixture requires a held break, M15B is hard.
- **Responsibilities:** one fixed fixture, prepared lifecycle, spacing, reservation,
  final safety, complete-airtime record, release/discard, and music-first silence.
- **Explicit non-goals:** advertiser pack as product model, AI commercial writing,
  lore campaign, multiple voices, or production FX.
- **Accepted behavior preserved:** no generated material has airtime rights and
  cadence values remain provisional.
- **Unlocks:** a clean runtime path for procedural prepared formats.
- **Main risks:** accidentally treating the fixture as the mature commercial
  design or over-speaking.
- **Automated verification:** lifecycle/arbiter/treatment integration, missed
  opportunities, expiry, safe refusal, and regression tests.
- **Steve/manual evaluation:** flow and annoyance, not creative quality.
- **Next action type:** Manual Test/Evaluation.

#### M16B - Procedural Commercial Laboratory Batch

- **Purpose:** determine whether context-conditioned fictional commercial premises
  and scripts are worth runtime integration.
- **Why now:** fixed plumbing exists and the reusable lab can test creativity
  without risking airtime.
- **Hard dependencies:** M12B; M13B is a useful prerequisite for trusted session
  context but not required for an initial fixed-context batch.
- **Responsibilities:** bounded context packets, Creative Direction brief, exact
  format-specific commercial generation contract, exact commercial generation,
  evergreen versus opportunity-bound classification, alternatives that die if
  unselected, mechanical review, and owner rubric. Commercial instructions are not
  inherited from the host-comment prompt.
- **Explicit non-goals:** static advertiser pack as mature model, runtime airtime,
  automatic lore, multi-voice requirement, or FX system.
- **Accepted behavior preserved:** trusted evidence only; unsupported genre guessing
  is unavailable, not filled by model memory.
- **Unlocks:** evidence for procedural premise/script quality and reuse policy.
- **Main risks:** arbitrary stereotype, repetitive joke category, fake factual
  claims, or retaining brainstorm garbage.
- **Automated verification:** bounded run/accounting, context provenance, no runtime
  access, alternative disposal, validation/fallback, and model unload.
- **Steve/manual evaluation:** commercial-format fulfillment, originality, fit to
  trusted context without fact recital, replay value, Zoya-station tone, structural
  variety, and desire to hear it.
- **Next action type:** Evaluation.

#### M16C - Shared Procedural Preparation Pipeline

- **Purpose:** integrate an accepted procedural format through direction,
  generation, rendering, lifecycle, and arbitration; commercial is the first
  required client, not the owner of the capability.
- **Why now:** quality and plumbing evidence both exist.
- **Hard dependencies:** accepted M16A/M16B evidence, M12D foundations, and required
  delivery capability. M17 is hard only when the selected client requires a
  composite or mixed produced scene.
- **Responsibilities:** bounded ahead-of-time requests, candidate selection,
  material classification, single-component rendering/preparation where sufficient,
  reuse/release, expiry/discard, typed delivery treatment, and no-airtime-right
  arbitration.
- **Explicit non-goals:** every procedural format, durable advertiser canon by
  default, automatic cross-format continuation, composite-scene compilation, shared
  mixing/FX, or a full production system.
- **Accepted behavior preserved:** scheduling, safety, and final revalidation remain
  deterministic and independent of generation success.
- **Unlocks:** reuse by calls, liners, host bits, and later scenes.
- **Main risks:** uncontrolled work queues, stale contextual material, or generator
  latency driving editorial policy.
- **Automated verification:** end-to-end fake generation/render/lifecycle/arbiter
  matrices, cancellation, expiry, reuse, disposal, and complete regressions.
- **Steve/manual evaluation:** live quality, cadence, repetition, and music flow.
- **Next action type:** Evaluation/Decision.

#### M16D - Caller and Dialogue Laboratory

- **Purpose:** test believable caller/dialogue writing and local production before
  runtime calls.
- **Why now:** a poor second voice or dialogue style is a major dependency worth
  discovering early; this experiment may be scheduled before M16A-C.
- **Hard dependencies:** M12B. M14B renderer evidence and M14C cast structure are
  useful, but an isolated one-shot voice/production experiment may precede them.
- **Responsibilities:** bounded caller premise/script batch, second-voice audition,
  format-specific dialogue contract, turn-taking, dialogue duration, and an
  explicitly disposable production proof that may include one simple telephone-band
  filter, ring/hang-up effect, basic bed/fade, and gain/normalization pass.
- **Explicit non-goals:** runtime calls, recurring caller canon, reusable scene or
  mixer architecture, broad FX library, production dependency selection, or
  obligation to implement calls after the experiment. Caller instructions are not
  inherited from host-comment or commercial prompts.
- **Accepted behavior preserved:** generate/render-only isolation from Firefox and
  normal radio.
- **Unlocks:** decision on local voice viability and caller-format value.
- **Main risks:** uncanny dialogue, weak speaker distinction, or mistaking a phone
  filter for character quality.
- **Automated verification:** bounded artifacts, cleanup, voice fallback, no
  playback access, and repeatable render settings.
- **Steve/manual evaluation:** believability, clarity, timing, character restraint,
  dialogue-format fulfillment, non-monologue structure, and desire to hear it.
- **Next action type:** Experiment/Evaluation; independently schedulable.
Narrow caller or commercial production experiments remain isolated artifacts with
bounded tools and disclosed settings. They may prove that a second voice, filter,
effect, bed, fade, or normalization step helps a format, but they do not establish
shared APIs or production dependencies. M17 must consolidate accepted needs behind
reusable ownership, renderer, scene, and mixing boundaries rather than copy the
experimental path into runtime.


#### M16E - Optional Cross-Format Continuity

- **Purpose:** allow an aired segment to create an optional thread that may inspire
  later host, commercial, or caller proposals.
- **Why now:** real procedural clients, minimal lore, and airtime commits exist.
- **Hard dependencies:** M13D/M13E and at least two accepted client formats.
- **Responsibilities:** context exposes active/dormant threads; Creative Direction
  may propose a continuation; arbiter evaluates it as an ordinary candidate;
  cooldown, expiry, retirement, and non-selection remain normal.
- **Explicit non-goals:** serialized plot obligations, automatic callbacks, or
  keeping every aired joke alive.
- **Accepted behavior preserved:** music-first silence; every continuation may be
  rejected indefinitely.
- **Unlocks:** earned station-world continuity such as commercial, host reaction,
  and later caller complaint.
- **Main risks:** narrative debt, repetition, and station identity collapsing into
  sitcom continuity.
- **Automated verification:** optional proposal, rejection, dormancy, expiry,
  retirement, spacing, and complete-airtime commit tests.
- **Steve/manual evaluation:** whether continuity feels surprising and alive rather
  than compulsory or annoying.
- **Next action type:** Evaluation.

### Phase H - Prepared production and sonic identity

Narrow production tricks may validate a format earlier; this phase turns only
proven needs into coherent reusable capabilities. Experimental caller/commercial
artifacts are evidence, not production APIs, and may be discarded or reimplemented.

#### M17A - Deterministic Prepared Scene Model and Compiler Foundation

- **Purpose:** represent, validate, own, and compile a multi-component prepared
  scene without also choosing voices, inventing DSP policy, or designing branding.
- **Why now:** calls, commercials, imaging, and later chained bits need one atomic
  prepared unit whose timing and resources are known before its first audible beat.
- **Hard dependencies:** M10B, the delivery-treatment model, and one accepted
  composite-scene client or fixture that demonstrates the minimum grammar.
- **Responsibilities:** versioned scene/timeline representation; ordered and
  overlapped components; explicit pauses and turn boundaries; real rendered-duration
  inputs; deterministic timeline resolution and total-duration calculation; opaque
  prepared-component handles; atomic scene ownership, cancellation, disposal,
  invalidation, and compile diagnostics.
- **Explicit non-goals:** expressive-renderer selection, voice synthesis, gain/EQ,
  beds, filters, fades, FX library, final audio mix, sonic identity, scheduling,
  playback expansion, or universal scene language.
- **Accepted behavior preserved:** scene preparation creates no airtime right;
  incompatible, incomplete, stale, cancelled, or failed scenes remain silent and
  dispose safely before playback mutation.
- **Unlocks:** one deterministic assembly target for renderer and mixing capabilities
  used by several formats.
- **Main risks:** an over-general scene schema, timing guessed before rendering,
  leaking component resources, or confusing compilation with editorial scheduling.
- **Automated verification:** timeline/order/overlap fixtures, real-duration
  substitution, total-duration bounds, missing-component refusal, atomic
  cancellation/disposal, compile determinism, and no playback/audio-device access.
- **Steve/manual evaluation:** inspect readable scene plans and one dry sequential
  fixture; no polished-radio acceptance is expected yet.
- **Next action type:** Implementation contract after a real client proves the
  minimum scene grammar.

#### M17B - Production Voice Renderer Integration

- **Purpose:** integrate an accepted voice backend through M14A boundaries so
  prepared scenes can contain reliable, directed, duration-known voice components.
- **Why now:** M14B can establish isolated backend potential, but only scene use can
  prove consistent voices, real timing, cancellation, and useful expressiveness.
- **Hard dependencies:** M14A, accepted M14B candidate evidence, and M17A. M14C is
  required when the accepted client uses multiple cast roles.
- **Responsibilities:** provider-neutral production adapter; semantic Voice
  Direction mapping only to demonstrated controls; single- and multi-voice
  rendering; voice-identity consistency; exact rendered-duration return; bounded
  preparation; capability negotiation and refusal; fallback; CPU/GPU/RAM/VRAM
  ownership; cancellation, unload, disposal, and scene-component handoff.
- **Explicit non-goals:** approving Chatterbox by name, audio mixing/FX, station
  imaging assets, arbitrary voice cloning, scheduler authority, or assuming an
  isolated audition automatically survives production.
- **Accepted behavior preserved:** current Nicole/Windows behavior remains available
  until separately changed; renderer failure occurs before audible scene start and
  cannot create airtime debt or unsafe fallback.
- **Unlocks:** reliable voice components for calls, commercials, announcers, guests,
  and produced host scenes.
- **Main risks:** expressive controls that are unstable or provider-specific, voice
  drift across lines, resource spikes, long preparation, weak fallback, or license
  and packaging constraints.
- **Automated verification:** capability matrices, supported/unsupported direction,
  exact duration, multi-line identity consistency indicators, bounded resources,
  cancellation/unload/disposal, fallback, and no desktop-media access.
- **Steve/manual evaluation:** compare isolated audition evidence with dry assembled
  scenes for host fit, cast distinction, expressiveness, pacing, consistency, and
  fatigue.
- **Next action type:** Evaluation, then a separate integration decision.

#### M17C - Shared Mixing and FX Toolkit

- **Purpose:** turn compiled prepared components into one controlled final scene
  render using shared, deterministic audio-production tools.
- **Why now:** proven formats need acoustic treatment, but telephone filters, beds,
  fades, and normalization should not remain copied experimental tricks.
- **Hard dependencies:** M17A and an accepted production need. M17B is hard for a
  voice-bearing client but not for a fixed-audio-only mixer fixture.
- **Responsibilities:** explicit sample-format conversion; gain staging; level and
  peak bounds; normalization policy; deterministic mixing; EQ/filter primitives;
  telephone/radio treatment; beds; fades; reusable effects; component alignment;
  final duration/format validation; cancellable offline render; asset provenance;
  final-scene ownership and cleanup.
- **Explicit non-goals:** Creative Direction, cast selection, backend approval,
  station motif/sound-pack authorship, scheduler policy, live improvisational DSP,
  or a giant workstation-style effects system.
- **Accepted behavior preserved:** complex output is fully prepared before its first
  audible beat; production failure, clipping risk, missing assets, or incompatible
  treatment yields refusal/silence before playback mutation.
- **Unlocks:** reusable produced calls, commercials, IDs, stings, and other scenes
  without format-specific mixers.
- **Main risks:** clipping, pumping, damaged voice quality, inconsistent loudness,
  excessive latency, nondeterministic native tools, asset licensing, or cleanup
  leaks.
- **Automated verification:** offline golden/metric fixtures, sample-format and
  duration checks, peak/loudness bounds, filter/fade alignment, missing-asset
  refusal, cancellation/disposal, provenance, repeatability, and no audio-device or
  desktop-playback access.
- **Steve/manual evaluation:** headphone and speaker review for intelligibility,
  balance, processing restraint, voice damage, pacing, and produced-radio quality.
- **Next action type:** Iterative Evaluation with separately approved primitives.

#### M17D - Canonical Zoya Sonic Identity

- **Purpose:** create the coherent, recognizable Zoya sound that uses the proven
  scene, renderer, and mixing capabilities.
- **Why now:** branding should refine formats already shown to entertain and should
  not hide weak editorial content or determine lower-level production architecture.
- **Hard dependencies:** M17A and M17C, plus accepted format and voice evidence.
  M17B is required for any identity element using expressive or cast voice.
- **Responsibilities:** curated canonical sound pack; motif; station IDs, stings,
  bumpers, beds, announcer treatment, and imaging variants; imaging grammar and
  production conventions; asset integrity/licensing/provenance; restrained
  progressive migration of proven formats.
- **Explicit non-goals:** distribution gate, speculative asset explosion, general
  station builder, new scheduler authority, or forcing identical treatment on
  every format.
- **Accepted behavior preserved:** content/treatment compatibility, music-first
  arbitration, prepared-before-air scenes, safe playback, fallback, and silence.
- **Unlocks:** an experience close to the intended full Zoya Radio identity.
- **Main risks:** overproduction, inconsistent motif, fatigue, asset bloat, or
  branding that overwhelms music and host content.
- **Automated verification:** asset integrity/provenance, scene compilation and
  rendering, duration/level bounds, missing-capability fallback, and package audit.
- **Steve/manual evaluation:** broad listening acceptance of identity, restraint,
  consistency, recognizability, and progressive format upgrades.
- **Next action type:** Evaluation/Decision.

### Phase I - Experience-ready private alpha

#### M18A - Distribution Decision and Compatibility Repair

- **Purpose:** decide that the station experience is worth distributing, then
  address concrete portability and packaging risks.
- **Why now:** compatibility work should serve a valuable experience rather than
  block internal radio design.
- **Hard dependencies:** Steve's product decision; no architectural dependency on
  M17D, though richer station maturity is the sequencing preference.
- **Responsibilities:** revisit known Firefox identity defect, use opportunistically
  gathered diagnostics, compatibility matrix, package/runtime prerequisites,
  licensing, clean-machine procedures, and bounded repairs.
- **Explicit non-goals:** public release, broad installer/update system, or treating
  lack of tester evidence as compatibility.
- **Accepted behavior preserved:** M8 safety invariants and accepted internal
  baseline claims remain accurately scoped.
- **Unlocks:** credible near-full private-alpha candidate.
- **Main risks:** large effort sink, machine-specific Firefox behavior, signing and
  reputation friction, or dependency packaging.
- **Automated verification:** package self-tests, clean builds, integrity/license
  audit, compatibility fakes, and targeted repair regressions.
- **Steve/manual evaluation:** owner package acceptance and selected machine tests.
- **Next action type:** Decision, then bounded Compatibility/Implementation work.

Firefox identity evidence may be collected opportunistically at any earlier phase.
That does not authorize a repair or make distribution a prerequisite for creative,
production, or sonic-identity work.

#### M18B - Near-Full Private Alpha

- **Purpose:** evaluate a station much closer to the intended experience with a
  small invited group.
- **Why now:** the product and compatibility decision has been made.
- **Hard dependencies:** M18A acceptance and Steve's distribution authorization.
- **Responsibilities:** versioned package, clean-machine evidence, tester guide,
  privacy/disclosure, rollback, bounded feedback rubric, and issue triage.
- **Explicit non-goals:** public launch, automatic updates, broad support promise,
  or using tester feedback to silently change canonical policy.
- **Accepted behavior preserved:** local-first operation and precise distribution
  claims.
- **Unlocks:** external experience evidence for polish and later configuration.
- **Main risks:** support burden and feedback confounded by machine compatibility.
- **Automated verification:** final package audit and complete relevant suite.
- **Steve/manual evaluation:** clean Windows 10/11 and invited-tester evidence.
- **Next action type:** Evaluation.

### Phase J - Proven configuration extraction

#### M19 - Zoya Configuration Extraction

- **Purpose:** expose only dimensions proven useful by the mature opinionated Zoya
  station.
- **Why now:** real formats, voices, locales, policies, and treatments now reveal
  what is genuinely configurable.
- **Hard dependencies:** mature internal station and Steve's explicit product
  decision.
- **Responsibilities:** extract proven host voice, locale, station policy, format,
  cadence, delivery, and asset dimensions behind stable internal boundaries;
  preserve safe defaults and migration.
- **Explicit non-goals:** speculative universal station builder, arbitrary plugin
  system, or exposing internal implementation knobs as product settings.
- **Accepted behavior preserved:** the canonical Zoya profile reproduces the
  accepted station and safety defaults.
- **Unlocks:** later alternate stations/configurations based on demonstrated need.
- **Main risks:** configuration surface explosion or weakening safe invariants.
- **Automated verification:** default-profile parity, schema/version migration,
  invalid-combination refusal, and full regressions.
- **Steve/manual evaluation:** verify the default remains Zoya and each exposed
  dimension has a real use case.
- **Next action type:** Product Decision/Implementation contract.

## Revised dependency map

Legend: `=>` hard dependency; `~>` useful prerequisite; `..>` independently
schedulable experiment or evidence; `+>` shared capability consumed by several
clients.

```text
M8 accepted runtime
  => M10A pure kernel
  => M10B lifecycle/binding
  => M10C parity migration
  => M11A Editorial Arbiter
  => M11B commentary + liner proof

M11A => M12A Context Assembler v1 => M12B Direction/Generator/Lab boundary
M12B => M12C host batch 1 ~> M12D selected host integration

M9 evidence ..> M13A source-agnostic Music Knowledge
M13A +> M13B session/recent-session Listening Intelligence
M13B => M13C durable-affinity experiment
M12B ~> M13D Station Lore/open threads
M13D +> M13E minimal airtime effects/context v2
M13A +> M13E canonical factual context
M13B/M13C +> M13E coverage-aware listening context

Voice auditions ..> M14A Host Voice/Renderer Abstraction
Expressive renderer auditions ..> M14B provider-neutral capability evaluation
M14A ~> M14B integration boundary
M14A + accepted M14B evidence ~> M14C Cast/Role
PT-BR/local TTS experiments ..> M14D Language/Locale
M14A/M14B ~> M14D

M10C + M11A => M15A treatment model/hold diagnostic
M15A manual acceptance + separate decision => M15B optional HoldAndReplace

M11B + required treatment => M16A fixed plumbing fixture
M12B ..> M16B procedural commercial lab
M16A + accepted M16B ~> M16C procedural preparation pipeline
M12B ..> M16D caller/dialogue lab
M14B ~> M16D isolated renderer evidence
M14C +> M16C/M16D multi-role clients
M13D/M13E + at least two clients => M16E optional cross-format continuity

M10B + treatment model + accepted composite client => M17A scene compiler
M14A + accepted M14B evidence + M17A => M17B production renderer
M14C +> M17B multi-role rendering
M17A + accepted production need => M17C shared mixing/FX
M17B +> M17C voice-bearing scene input
M17A/M17C + accepted station formats/voice => M17D sonic identity

Firefox diagnostics ..> M18A later compatibility work
Steve distribution decision => M18A => M18B private alpha
Mature Zoya + Steve product decision => M19 configuration extraction
```

Sequencing preferences are not false dependencies: M13A can advance alongside
creative work; call/voice/telephone and PT-BR experiments can happen earlier;
Firefox evidence can be collected whenever a tester exposes it; narrow production
experiments can validate formats before M17 and must be consolidated there if
accepted; and distribution deliberately does not gate M17D sonic identity.

## Shared-capability map

| Shared capability | Host comment/rant | Commercial | Listener call | Liner/ID | Future chained bits |
| --- | --- | --- | --- | --- | --- |
| M10 lifecycle/binding | Bound comment and prepared rant | Reusable or context-bound spot | Prepared dialogue | Reusable inventory | Sequence ownership |
| M11 Editorial Arbiter | Speech pressure and format spacing | Break spacing and priority | Long-format spacing | Reuse/cooldown | Sequence preconditions |
| M12 context/direction/generation | Format-specific host assignment and exact wording | Commercial premise then format-specific script | Caller premise then dialogue contract | Imaging/promo assignment and copy variants | Beat/arc intent only after new lab evidence |
| M13 Music Knowledge | Source-backed factual relevance | Trusted contextual inspiration | Factual caller premise constraints | Optional factual ID | Grounded callback facts |
| M13 Listening Intelligence | Session observation/rant | Procedural product inspiration | Listener reaction premise | Contextual liner | Relevance over time |
| M13 Lore/open threads | Callback or reaction | Advertiser/campaign state | Caller relationship/complaint | Station-world naming | Completed/open beats |
| M14 voice/renderer/cast/locale | Host/renderer selection, performance capability, and language | Announcer/spokesperson capability | Host/caller roles and backend evidence | Imaging voice | Multiple characters |
| M15 delivery treatments | Overlay or deliberate break | Usually held break | Overlay or held break | Intro/outro overlay | Scene-specific requirement |
| M17A scene model/compiler | Ordered prepared components | Atomic commercial scene | Turn/pause timeline | ID/sting component plan | Sequence/timeline ownership |
| M17B production renderer | Directed host/guest voice | Consistent announcer/cast clips | Duration-known host/caller voices | Imaging/announcer voice | Directed multi-character clips |
| M17C mixing/FX | Optional shared bed/fade | Shared mix, FX, bed, normalization | Telephone/dialogue mix | Final sting/ID render | Reusable production toolkit |
| M17D sonic identity | Zoya performance/bed grammar | Canonical commercial production fit | Canonical call sound | IDs, stings, bumpers, motif | Station-wide imaging grammar |

## Format-maturity map

Maturity steps describe evidence and capability use; they are not automatically
separate milestones.

| Maturity step | Host comment/rant | Commercial | Listener call | Liner/ID |
| --- | --- | --- | --- | --- |
| Fixed fixture | Accepted M8 comment; later fixed rant fixture only if needed | One prebuilt plumbing spot | Scripted one-shot production sample | Tiny reusable liner is first second-format proof |
| Generate-only experiment | M12C bounded host batches | M16B procedural commercial batch | M16D dialogue batch | Optional copy/imaging batch after fixed proof |
| Contextual generation | M12D session-conditioned selected formats | Trusted session/listening brief inspires fictional product | Context/lore supports caller premise | Optional context-aware copy |
| Prepared runtime integration | M12D through shared lifecycle/arbiter | M16C first procedural client; M17 when composite/produced | Later client after lab/voice evidence; M17 when composite/produced | M11B reusable inventory |
| Persistence/continuity | Optional callback through M13D/E | Aired advertiser/thread only; unaired alternatives vanish | Recurring caller only after earned airtime/policy | Usually reusable state, not story canon |
| Multiple voices | Usually unnecessary; guest later optional | M14C cast plus M17B renderer when accepted | M14C strongly useful; M17B for production | Announcer voice optional through M17B |
| Narrow production | Early isolated bed/fade only when useful | Early simple proof; M17C consolidates accepted needs | M16D disposable proof; M17C consolidates accepted needs | Early stinger proof; M17C consolidates accepted needs |
| Mature sonic identity | M17D Zoya performance/bed grammar | M17D coherent branded parody scene | M17D canonical call sound | M17D IDs, stings, bumpers, motif |
| Optional later enhancement | Long rant, chain, arc only after new lab evidence | Campaign development and callbacks | Recurring relationships/cross-format response | Localized or procedural imaging |

## Music Knowledge data flow

```text
Observed deterministic media identity
  -> bounded lookup planner
  -> independently approved Source Adapters
       - each adapter understands provider schema and semantics
       - recording title vs release title
       - canonical artist entity vs display/release/collaboration credit
       - aliases, editions, remasters, live versions, and stable source IDs
  -> typed provider records plus typed lookup outcome
  -> conservative Entity / Identity Resolver
       - stable IDs and source-declared relationships first
       - structured credits and explicit aliases/edition relations
       - safe punctuation/case normalization
       - no unrestricted fuzzy or generic suffix stripping
  -> fact-type-specific Evidence / Fact Reconciler
       - strict positive identity
       - multi-valued genre/style
       - appropriate date/credit/alias policies
       - provenance and disagreement retained
  -> small deterministic genre/style normalization
       - source-specific tags retained
       - demonstrated mappings such as Nü Metal -> Metal
       - multiple tags allowed; unknown remains unknown
  -> canonical Music Knowledge records and bounded cache
  -> Listening Intelligence aggregates trusted evidence with coverage
  -> Creative Context Assembler selects compact canonical facts/state
  -> Creative Direction / Content Generator receive no raw provider payloads
```

Initial evidence states are categorical and explainable, such as direct confirmed
fact, corroborated fact, single-source support, conflicting evidence, ambiguous
identity, unavailable, and stale/expired. They are not fake numeric truth scores.

Lookup outcomes must remain distinct:

| Outcome class | Meaning | Cache/retry consequence |
| --- | --- | --- |
| Confirmed fact | Positively resolved entity supplied the fact | Cache with provenance and fact-appropriate expiry |
| Confirmed no fact | Successful, positively resolved lookup established no relevant fact | Conservative negative cache may be allowed |
| Temporary source failure | HTTP 503, timeout, rate limit, or temporary unavailability | Never negative factual evidence; short retry/backoff state only |
| Parser/schema failure | Response could not be safely interpreted | Engineering/provider-adapter failure; never `no fact` |
| Ambiguous identity | More than one plausible entity or no strict positive match | No fact promoted; may await better evidence |
| Unsupported representation | Source expresses a case the adapter/resolver does not safely handle | No fact promoted; explicit diagnostic, not negative evidence |
| Corrupt cache | Stored record fails integrity/schema checks | Quarantine/invalidate; do not turn into a negative fact |
| Stale/expired | Previous evidence exceeded its policy | Retain provenance if useful but do not present as current without refresh policy |

Source outage can reduce creative context quality but cannot suppress the broadcast
engine, change hard eligibility, or imply that an artist has no genre metadata.

## Deliberately deferred work

- No generalized station builder before M19.
- No permanent cadence clock from planning guesses.
- No universal commentary prompt that silently becomes the interface for host
  formats, commercials, calls, imaging, promos, or chained bits.
- No compulsory use of every trusted fact supplied to a creative request.
- No fixed small-model-direction/large-model-writing split without bounded lab
  evidence and a separately approved dependency/model decision.
- No durable raw listening diary.
- No raw Creative Director alternative landfill.
- No raw provider payloads in prompts or canonical identity.
- No unrestricted fuzzy entity matching or LLM genre adjudication.
- No automatic program break without M15A acceptance and separate M15B approval.
- No distribution gate before richer internal creative/production work.
- No full sonic-identity system merely to add one narrow phone filter, bed, fade,
  effect, or multi-voice render.
- No expressive TTS/backend, including Chatterbox, is approved by roadmap mention,
  isolated audition, advertised capability, or an early format experiment.
- No caller/commercial experiment may silently become the shared scene, renderer,
  or mixing implementation; accepted needs return through bounded M17 contracts.
- No assumption that every benchmark concept deserves runtime implementation.

## Recommended immediate handoff

M10A through M12D are passed under their bounded contracts and acceptance records.
The recommended next step type is **Decision - Steve**: select the next separately
bounded roadmap task. VAR1's repeated-form limitation remains active; broader
research, later formats, implementation, and a commit remain unauthorized.
