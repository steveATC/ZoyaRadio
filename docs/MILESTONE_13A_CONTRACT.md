# Milestone 13A — Music Knowledge and Identity Foundation

## Accepted records

On August 28, 2026 Steve accepted M13A-I1 as an isolated, fixture-only foundation for source-agnostic music knowledge and identity ownership. On September 1, 2026 he accepted M13A-R2-I1 as an additive, provider-neutral entity/claim foundation. These records document bounded accepted stages; they do not pass M13A as a whole, activate a provider, or authorize live enrichment.

## Stage R evidence and decision basis

Stage R established the following design/evidence policy:

- MusicBrainz is the initial primary identity/catalogue schema and evidence candidate.
- Linked Wikidata is supplemental evidence only after positive MusicBrainz identity resolution.
- Reviewed local facts remain the safe fallback when no positive source evidence is available.
- Discogs and Spotify were not chosen for the initial path.

This is a source-selection and evidence boundary, not live provider activation or an implementation authorization beyond I1.

This section remains the historical basis for the accepted original I1/I2.1 path. The September 1 R2-I1 decision below replaces MusicBrainz-first identity as the assumed universal gateway for future enrichment design without changing the accepted resolver or diagnostic evidence.

## I1 bounded implementation

I1 provides typed knowledge, provenance, and outcome models; fixture parsers; deterministic identity resolution and fact reconciliation; bounded fake cache/retry/report diagnostics; and parser totality. It is local, fake-driven, non-network, and unreachable from normal runtime. It adds no provider client, network access, persistence or disk cache, dependency, model, account, API key, scheduling, context, generation, speech, playback, UI, or runtime behavior.

Top-level/root artist collections exclusively own identity candidacy. Nested or direct artist credits are references and bind only to a declared root; credit-only identity acceptance is prohibited. Exact canonical object binding is required. MBID remains the canonical `EntityId`; a QID is provenance only. Malformed, schema-invalid, ambiguous, temporary, and negative factual states remain distinct. Parser operations are no-throw/total and never turn parser failure into negative factual evidence.

## Repair history

The accepted implementation includes the bounded repair history: alias policy; canonical identity handling on Wikidata outcomes; malformed/schema totality; nested-credit identity ownership; and the final 18-row data-driven matrix. Case 15's duplicate-property fixture was corrected and a regression guard was added.

## Verification and acceptance evidence

- Three Release builds completed cleanly with zero warnings/errors.
- The complete fake-driven suite passed **402/402**, with zero failures.
- `run-m13a-music-knowledge-evaluation.cmd` reported **402/402**.
- Final Warden read-only review reported no findings.
- Steve accepted M13A-I1 on August 28, 2026.

No desktop or subjective test was required or performed. These results are existing accepted evidence recorded here, not a verification run by Scribe.

## I2/I2.1 isolated provider-diagnostic evidence

Steve accepted I2/I2.1 only as isolated provider-diagnostic evidence with the limitations recorded below. This does not activate a provider, connect normal runtime, add persistence, or complete M13A. The original I2 contract/evidence did not itself authorize a commit; Steve separately authorized one local commit on August 28, 2026 for the accepted I2/I2.1 source/tests/launcher and acceptance documentation. The live artifact directories remain intentionally ignored and outside that commit. No push is authorized.

Accepted deterministic evidence, recorded historically and not rerun by Scribe: three serial Release builds passed with shared compilation disabled; the complete fake-driven suite passed **407/407**; the existing M13A launcher reported **407/407**; the fixture-only I2 launcher issued zero provider requests and wrote no artifacts; and final Warden review reported no findings.

The final owner-run live I2.1 diagnostic artifact `20260829T011223006Z` completed safely. Six identities were attempted across nine logical requests and fourteen attempts: six HTTP 200 responses and eight HTTP 503 responses. Outcomes were: Wake Up / Rage Against The Machine `AmbiguousIdentity` (2/3); Ouro De Tolo / Raul Seixas `AmbiguousIdentity` (2/3); T.N.T. / AC/DC `RateLimited` (1/2); Leraine / Secede & Kettel `Unknown` (1/1); Sweet Home Alabama / Lynyrd Skynyrd `RateLimited` (1/2); and Chop Suey! / System of a Down `RateLimited` (2/3). Two remaining fixtures were not attempted because the circuit breaker stopped after two consecutive final rate-limited identities. `incompleteRateLimited=yes`; no cancellation; zero canonical identities; zero Wikidata progression; and no schema failure in this final run. Contact data and raw response bodies were absent from the artifacts.

Relevant history is preserved: normal-sandbox run `20260829T002932605Z` was inconclusive (8 logical / 16 attempts, all status 0 `ProviderOutage` under restricted sandbox); escalated run `20260829T003339942Z` had 9 logical / 16 attempts, 3 HTTP 200 and 13 HTTP 503, with 1 `AmbiguousIdentity`, 1 `SchemaFailure`, 6 `ProviderOutage`, and zero canonical identities/Wikidata progression. That result motivated I2.1. I2.1 records MusicBrainz 503 -> `RateLimited`, 1.5-second pacing, `Retry-After` or at least five-second fallback cooldown carried forward, and stopping after two consecutive final rate-limited identities.

The 503 responses, conservative `AmbiguousIdentity`/`Unknown` outcomes, circuit breaking, and absent Wikidata progression are accepted limitations of this diagnostic evidence. Matching and identity policy must not be weakened merely to obtain positive resolutions. OAuth credentials or account registration were not used; no credential belongs in the repository. Public metadata OAuth remains unapproved and unnecessary for this accepted diagnostic.

The current conservative global resolver and its accepted I1/I2.1 evidence remain unchanged. The later accepted R2-I1 foundation below establishes an additive provider-neutral model for future work; it does not replace that resolver in normal runtime.

## R2-I1 provider-neutral entity/claim foundation

Steve accepted M13A-R2-I1 on September 1, 2026. It adds a fixture-only durable-knowledge skeleton that distinguishes provisional observed grouping, positively established durable subject identity, and external provider identity references. Exact observed labels can support local grouping and lookup reuse, but names alone cannot establish fact-bearing identity when same-name ambiguity is possible. Application-owned `KnowledgeEntityId` values identify durable Artist and Track entities; provider identifiers remain scoped references and may identify, narrow, or relate without redefining the local entity.

Durable knowledge is entity-centric. One Artist entity may be referenced by many Track entities, including Tracks with multiple explicit Artist credits. Artist claims are stored once on the Artist and reused through relationships rather than copied into Track records. Raw observed credit text is preserved and collaboration strings are not guessed or destructively split. Each claim owns its provenance, applicable scope, categorical evidence state, freshness, conflict, withdrawal, and replacement information. Conflicting and multi-valued genre/style claims may coexist without invalidating the entity.

R2-I1 models Artist and Track only. Recording- and Release-specific claim promotion is refused until those scopes are explicitly modeled. Its bounded lookup-cache and temporary context-projection seams are fixture-only: cache outcomes do not promote claims, and a projection may follow Track-to-Artist relationships without mutating or denormalizing durable state. Raw provider payloads cannot reach the projection.

Accepted verification evidence, recorded historically and not rerun by Scribe: Release source and test builds completed with zero warnings/errors; the complete suite passed **452/452**; existing M13A I1/I2.1 tests remained green; the fixture diagnostic issued zero provider requests and wrote zero artifacts; and Warden accepted the bounded implementation after repairs with no remaining findings. No desktop test applied because the new foundation is unreachable from production composition.

R2-I1 changes no existing MusicBrainz adapter, accepted resolver, cache/retry/throttle/circuit-breaker behavior, I1/I2.1 diagnostic route, M12 Creative Context, Qwen configuration, playback, scheduling, TTS, or normal runtime. It adds no provider activation, persistence, database or cache migration, live Creative Context integration, search, lyrics, M13B behavior, or external metadata transmission.

## Explicit non-goals and next boundary

This record does not authorize provider activation beyond the isolated I2/I2.1 diagnostic evidence, normal-runtime integration, persistence, cache-on-disk, runtime or scheduling/context/generation/speech/playback integration, weaker matching, more provider requests, new dependencies or models, M13B, search, or lyrics. Steve separately authorized a selective local acceptance commit for R2-I1 and its documentation; no push is authorized. M13A-R2-E1 is authorized for **design only** as the next bounded task. Its contract must return to Steve for approval before any live provider call or experiment execution.

## R2-E1 paused status

M13A-R2-E1 is authorized for design only. The current attempt is paused; its abandoned runner and incomplete replacement are unaccepted historical negative/process evidence and made zero provider calls in the current attempt. They must not resume automatically. Accepted R2-I1 remains valid, while M13A remains incomplete. Any future contract must return to Steve before live provider calls or experiment execution.

The attempt also informed the accepted workflow hardening: known unfinished work is `INCOMPLETE`, not review-ready and not a repair; structural overengineering returns for redesign/owner decision; and bounded experiments must remain proportional to the question.
