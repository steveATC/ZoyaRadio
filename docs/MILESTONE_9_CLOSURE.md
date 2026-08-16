# Milestone 9 closure record

## Decision

On August 15, 2026, Steve concluded Milestone 9 as an exploration and prototype
phase. This closes M9 work and its remaining Gate 4 trial; it does **not** declare
M9B an accepted production/runtime feature.

The accepted Milestone 8 safety baseline remains the project's accepted runtime
baseline. The implemented M9B candidate is retained in the repository for future
product planning, but it has no further M9 acceptance claim and must not be
expanded, repaired, or promoted without a separately approved future milestone.

## Recorded results

- **M9A:** the structured-commentary candidate was rejected after blinded owner
  review. It improved on the baseline in aggregate but still produced repetition,
  awkward construction, unsupported listener/session claims, and exact-metadata
  failures. It supplied no runtime direction.
- **M9C:** the isolated source-backed experiments established that a local 14B
  model can produce natural, owner-preferred complete comments from a compact
  multi-fact brief. M9C.6 selected grounded whole-comment synthesis over
  fact-plus-reaction assembly and over training-augmented factual freedom. This
  is successful design evidence, not runtime acceptance.
- **M9B:** the bounded candidate integrated reviewed/local context, strict live
  MusicBrainz and linked Wikidata confirmation, a local 14B route, bounded
  caching, private lifecycle diagnostics, and the accepted M8 fallback/safety
  path. Gate 2 owner review and Gate 3 silent operational evidence were accepted.
  The final audible Gate 4 requirement was not met: the latest attempt delivered
  three enriched comments, three fallback comments, and one safely failed
  over-duration preparation. A later resumed run confirmed two additional live
  enrichments but encountered several MusicBrainz HTTP 503 responses. No
  subsequent M9 trial is required.

## What M9 proved

- Structured, source-backed context can materially improve Zoya's commentary.
- A stronger local model can synthesize a complete, natural comment rather than
  merely reciting a fact and appending a reaction.
- Enrichment, local generation, preparation, cancellation, fallback, Firefox-only
  ducking, exact restoration, and safe silence can coexist without compromising
  the accepted playback invariants.
- Remote metadata availability is a quality/reliability constraint, not a safe
  foundation for mandatory radio delivery.

## Deferred follow-on work

The following are deliberately deferred for future product planning; this record
does not approve them:

- artist- and track-level fact storage, expiry, and reuse;
- deterministic selection of artist-only, track-only, or combined commentary
  context, including fact-level repetition control;
- source resilience, respectful shared request pacing, retry/backoff policy, and
  broader metadata strategy;
- a future decision on the retained M9B candidate's product role and acceptance
  criteria.

## Next action

The next action is a project-planning discussion, using Steve's forthcoming
Brainstorm prompts to define product areas, their boundaries, a coherent roadmap,
and only then future milestones. No implementation is authorized by this closure.
