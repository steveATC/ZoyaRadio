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

## Explicit non-goals and next boundary

This record does not authorize live/provider adapter activation, network access, persistence, cache-on-disk, runtime or scheduling/context/generation/speech/playback integration, new dependencies or models, M13A-I2, M13B, or any documentation authorization beyond this record. It authorizes no commit, push, release, distribution, or provider/service/account/API-key action. Any next M13A stage requires a separately bounded authorization from Steve.
