# Milestone 13A — Music Knowledge and Identity Foundation

## Accepted record

On August 28, 2026 Steve accepted M13A-I1 as an isolated, fixture-only foundation for source-agnostic music knowledge and identity ownership. This record documents the accepted bounded stage; it does not pass M13A as a whole, activate a provider, or authorize a later stage.

## Stage R evidence and decision basis

Stage R established the following design/evidence policy:

- MusicBrainz is the initial primary identity/catalogue schema and evidence candidate.
- Linked Wikidata is supplemental evidence only after positive MusicBrainz identity resolution.
- Reviewed local facts remain the safe fallback when no positive source evidence is available.
- Discogs and Spotify were not chosen for the initial path.

This is a source-selection and evidence boundary, not live provider activation or an implementation authorization beyond I1.

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

Scoped identity resolution remains an **OPEN DESIGN HYPOTHESIS / FOLLOW-UP QUESTION**, not an accepted architecture or implementation authorization: an artist might eventually be `Confirmed` while a recording is `Ambiguous`, or a recording `Confirmed` while release evidence is `Unknown`/`Ambiguous`; facts would be usable only at their established scope, narrower ambiguity would not erase securely established broader identity, and a canonical artist might remain reusable. The current conservative global resolver is unchanged. No schema or behavior change is authorized.

## Explicit non-goals and next boundary

This record does not authorize provider activation beyond the isolated I2/I2.1 diagnostic evidence, normal-runtime integration, persistence, cache-on-disk, runtime or scheduling/context/generation/speech/playback integration, schema changes, weaker matching, more provider requests, new dependencies or models, M13B, or any further M13A scope. The original record alone did not authorize a commit; Steve's separate August 28 authorization permits exactly the one local commit described above. No push, release, distribution, or provider/service/account/API-key action is authorized. Any next M13A stage requires a separately bounded authorization from Steve.
