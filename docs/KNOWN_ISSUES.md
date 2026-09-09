# Known issues and limitations

This file separates unresolved defects from accepted platform limits and deferred quality improvements. An item here is not automatically approved milestone scope.

## Unresolved defects and observations

### Paused M13A-R2-E1 and workflow readiness evidence

- The current M13A-R2-E1 attempt is paused. Its abandoned runner and incomplete replacement are unaccepted historical negative/process evidence, made zero provider calls in the current attempt, and must not resume automatically.
- Accepted R2-I1 remains valid, but M13A remains incomplete. Any next experiment or live provider call requires a returned contract and Steve's approval.
- The workflow hardening records this as process evidence: known unfinished work is `INCOMPLETE`, not Warden-ready and not eligible for repair budget. Structural defects exposed by the attempt require redesign/owner decision rather than accumulated repairs.

### M13A-I2.1 provider diagnostic limitations

Steve accepted I2/I2.1 only as isolated diagnostic evidence. The final owner-run artifact `20260829T011223006Z` completed safely but attempted six identities and stopped before two remaining fixtures after two consecutive final rate-limited identities. It recorded nine logical requests and fourteen attempts, six HTTP 200 and eight HTTP 503, with conservative `AmbiguousIdentity`, `Unknown`, and `RateLimited` outcomes; zero canonical identities and zero Wikidata progression. `incompleteRateLimited=yes`; no cancellation and no final-run schema failure. Contact data and raw response bodies were absent.

These 503s, conservative outcomes, circuit breaker, and lack of Wikidata progression are accepted limitations of this evidence, not a reason to weaken matching or identity policy. I2/I2.1 is not provider activation, normal-runtime integration, persistence, or M13A completion. Its accepted source/tests/launcher and documentation are part of the repository snapshot containing this text; live artifact directories remain intentionally ignored. OAuth credentials and account registration were not used and remain unapproved/unnecessary for this accepted diagnostic.

The earlier scope-aware identity hypothesis is now addressed at the representation layer by accepted M13A-R2-I1: observed grouping is separate from established durable subject identity, claims remain usable only at supported scope, and narrower ambiguity need not erase independently established Artist knowledge. This does not repair the live provider limitations above or replace the current conservative global resolver. R2-E1 is authorized for design only; no live provider request is approved.

### Intermittent observer stall at 20:49

- A manual run stopped producing useful observer behavior at approximately 20:49.
- The later focused Firefox-plus-Chrome M7.5B acceptance run did not reproduce it.
- There is no evidence that the wrapper-continuity repair fixed this separate observation, so it remains unresolved and unreproduced.

### Excessive wrapper-refresh diagnostics

- Global GSMTC churn can produce many harmless `FIREFOX GSMTC WRAPPER REFRESHED` lines.
- The refresh handling is functionally accepted, but the repeated output is a non-blocking logging-quality defect.
- Logging reduction was deliberately deferred rather than mixed into the accepted safety repair.

### Firefox GSMTC source identity differs on an invited tester machine

- On Mortorro's external tester machine, package `0.8.0-alpha.1` acquired the Windows global media-session manager and enumerated exactly one Playing session while YouTube Music was playing in Firefox. The session exposed correct metadata for `Simple Man` by Lynyrd Skynyrd, but its raw `SourceAppUserModelId` was `308046B0AF4A39CB` rather than `firefox.exe`.
- Firefox's **Control media via keyboard, headset or virtual device** setting was enabled and `media.hardwaremediakeys.enabled` was `true`. Zoya therefore reached GSMTC successfully but safely reported `NO FIREFOX TARGET FOUND` because the accepted implementation matches only the literal `firefox.exe` source identifier case-insensitively.
- This proves a portability defect in the current Firefox GSMTC identity assumption. It does not establish whether `308046B0AF4A39CB` is universal, installation-specific, path-derived, registration-derived, or one of several possible Firefox identities. It also does not establish that a different tester's earlier zero-session result has the same cause.
- A future repair must discover a positive, bounded Firefox identity rule without accepting unrelated browsers or applications as fallback targets. No magic-string repair, implementation change, M8 reopening, or future milestone is authorized by this observation.

### `Borrowed Umbrella` grounding defect

- Manual Milestone 4 testing found `qwen3:4b-instruct` sometimes invented `Borrowed Umbrella` for unrelated Cyrillic metadata.
- This is a model/prompt factual-grounding defect, not Unicode corruption.
- Steve accepted the current persona and deferred further tuning rather than adding a brittle phrase blacklist.
- Mechanical validation still cannot prove factual grounding; generated prose requires human review when grounding quality is the acceptance target.

### M9A candidate remains unsuitable for runtime

- Steve's corrected blinded M9A review rejected the StructuredCandidate for runtime consideration. It was preferable to the accepted baseline in the fixed comparison, but still produced repetitive constructions, awkward output, unsupported listener/session claims, and did not reliably preserve the exact supplied Unicode metadata.
- In the Unicode scenario, the candidate translated/transliterated the supplied Cyrillic metadata while baseline samples substituted different Cyrillic metadata. Both violate the experiment's exact-metadata boundary; this is an output-grounding failure, not storage mojibake in the corrected fixture.
- This recorded result remains historical: M9B now follows its separately approved contract and explicit implementation authorization, but has not resolved the M9A candidate defect.

### M9B grounded-context candidate retained after M9 closure

- M9B passed its generate-only Gate 2 owner review on August 15, 2026 and its silent operational Gate 3 under Steve's revised acceptance policy. The historical Gate 4 attempt delivered three enriched M9B comments and three M8/fixed fallbacks, but did not meet the five-enriched-delivery requirement. A later resumed run confirmed two live enrichments and also recorded several MusicBrainz HTTP 503 responses. Steve concluded M9 as prototype evidence rather than continuing the trial; M9B is retained but is not accepted runtime behavior. See [MILESTONE_9_CLOSURE.md](MILESTONE_9_CLOSURE.md).
- Deterministic validation catches only defined high-confidence technical/safety failures. Familiar low-risk musical knowledge, natural host opinion, ordinary musical characterization, names/attributions, popularity/cultural observations, ordinary adjectives, modest exaggeration, and precise details outside a card may produce advisory diagnostics but are not automatic hard failures. The validator cannot prove arbitrary semantic truth, naturalness, or spoken quality; owner review remains required.
- Three independent generations from the same song/card frequently reused openings or templates. Runtime state bounds the last six delivered comments/fact identities and permits one regeneration; an advisory result that remains advisory after that bound is retained, while hard, stale, invalid, or unsafe work falls back or stays silent. Raw rejected output remains available for owner inspection.
- The optional 14B model has materially higher cold-load and RAM/VRAM cost than the accepted 4B route. Its absence, failed local enrichment, timing failure, or invalid candidate must preserve the M8 candidate/fallback or safe silence.
- The local reviewed catalog is intentionally small. Unknown tracks depend on strict MusicBrainz/Wikidata confirmation and may safely remain unenriched. Collaboration metadata represented by separate artist entities, including `Secede & Kettel` — `Leraine`, remains unsupported; no fuzzy combined-name workaround or special card is allowed.
- During the latest Gate 4 run, the accepted local 4B fallback route for `Mind` — System Of A Down received HTTP 500 because its underlying `llama-server` terminated with exit status `0xc0000409` and `GGML_ASSERT(check == -n_chars) failed`. Zoya used the deterministic title/artist fallback, attempted selected-model unload, restored Firefox exactly, and did not start unsafe partial text. This local Ollama crash is retained as a known runtime issue; no Ollama repair, update, downgrade, or model change is authorized here.

### Historical static-audit findings and evidence gaps

The following are source/document-review findings from a completed historical static audit, rechecked against the current source without a fresh runtime run. They are not reproduced desktop incidents, repair authorization, or evidence of owner acceptance.

- **Closed September 9, 2026 — prepared-liner ownership/disposal gap:** static review found that `RadioBehaviorLiner.cs` could retain a Ready `result.Speech` when cancellation prevented inventory acceptance after `ReusableLiner.cs` had transferred ownership away from pipeline cleanup. The original approved repair exhausted its 1/1 round after two Warden blocks; Advisor then consulted read-only as Steve directed. The revised candidate was initially **NOT REVIEW-READY — known contract work remains** because cancellation/cadence-history evidence was incomplete and its wrapper could attempt a second dispose after a throwing dispose. Steve authorized revised-candidate repair 1/1, which Imp completed. Fresh Imp Release builds passed with 0 warnings/errors and the full safe deterministic suite passed 481/481; Warden independently re-reviewed it **PASS** with no remaining findings in scope, independently observed the existing Release binary pass 481/481, and passed scoped-diff and strict UTF-8 checks. This closes only verified static cleanup correctness, not native-memory impact, unsafe speech, or a causal desktop incident. Manual desktop acceptance was not required; no audible, UI, or cadence-policy behavior or milestone change is established. Steve later authorized a selective commit; no push was authorized or performed.
### Retained native working set

- Repeated Nicole CPU preparation disposes the ONNX model each time, but the long-running .NET/native process retained roughly 333 MiB working set after three preparations.
- No GPU execution provider was loaded in that observation.
- This is a measured resource observation, not evidence that model disposal failed, and no memory-management redesign is approved yet.

### Resolved M12D D3.1 quick-reaction recent-comment topic carryover

- During accepted D3.1 listening, `Заберут меня в солдаты` by Otava Yo was correctly observed as Track A, but its newly generated quick reaction discussed the earlier accepted `Tommy the Cat` commentary instead.
- The behavior log confirms correct preparation binding to Otava Yo and correct later Track-A to Track-B delivery binding. This was not stale prepared audio, unsafe targeting, or a lifecycle/reservation failure.
- The pilot-only recent-comment anti-repetition context contaminated the new topic despite the prompt instruction not to quote, imitate, or continue prior lines. The same run otherwise demonstrated the intended reduction in repetitive Oh openings.
- Accepted D3.2 topic isolation resolves this historical quality incident by removing prior-line words and subject matter from the prompt. The incident was not stale prepared audio, unsafe targeting, or a lifecycle/reservation failure, and it is not an active repair item.

### M12D-VAR1 repeated format limitation

- Steve's August 27, 2026 generate-only simulated mixed-song evaluation was accepted with limitations. The local model often creates good individual comments, but title-first reactions and recurring `Oh ...` openings become recognizable and repetitive song after song.
- This is an expected quality/product limitation and temporary placeholder, not an established application/runtime defect. The underlying cause is unestablished; prompt strictness, personality, model choice/capacity, and context/enrichment are future research questions, not findings.
- Broader research and later formats, including possible commercials and listener calls, are deliberately deferred until broader evidence exists and require separate authorization. No model, dependency, enrichment, lyrics/facts, implementation, or milestone is authorized by this record.

## Accepted platform and product limitations

### Missing album metadata

- Windows/Firefox normally does not provide album data for the tested YouTube Music session.
- The application displays `<not provided>` and never guesses an album.

### Cyrillic display and pronunciation

- PowerShell may cosmetically display Cyrillic metadata as question marks even though the in-memory text and UTF-8 logs preserve the real characters.
- The fallback en-US Microsoft David voice may skip Cyrillic title/artist text.
- Kokoro's documented language list does not include Russian. Steve described the audition sample as natural-sounding but is not a Russian speaker, so pronunciation accuracy remains unverified.
- Poor pronunciation is an accepted voice/language limitation; actual Unicode replacement or corruption would be a defect.

### eSpeak-free private-alpha fallback for unusual names

- The M8B candidate deliberately excludes all eSpeak files.
- Offline package verification found that ordinary English, punctuation/numbers, and repeated Nicole preparation work without eSpeak, while one deliberately unusual/out-of-vocabulary name sample caused KokoroSharp to request the absent `espeak-ng` helper.
- The accepted pre-delivery provider boundary safely prepared the same text with local Windows TTS before any ducking, speech, or media control. This is expected safe fallback behavior, not a claim that Nicole can pronounce every input without eSpeak.
- Steve accepted the Nicole/eSpeak-free package for internal development. Clean-machine evidence remains deferred and is still required before claiming general compatibility or distribution validation.

### Track ending versus manual skip

- Current metadata events do not distinguish a natural track ending from a manual skip.
- The application calls both a track transition and does not claim that the previous song finished naturally.

### Firefox application-level ducking

- On Steve's PC, Windows aggregates multiple audible Firefox tabs into the same Firefox Core Audio application session.
- Those Firefox tabs duck together. This application-level isolation is accepted for Phase 1.
- Chrome, unrelated applications, and Windows master volume must remain untouched.

### Conservative delivery window

- Prepared speech must start within two seconds after Track B is observed.
- Slow or busy-machine opportunities may be abandoned. Silence is intentionally preferred to a late interruption or unsafe catch-up.

### Environment-specific GSMTC access

- A non-interactive Codex sandbox account was denied access to the Windows media-session manager during an automated `--once` run.
- Steve's normal signed-in Windows desktop account has repeatedly verified GSMTC operation. Automated tests therefore use deterministic fakes rather than desktop control.

### Zero or ambiguous sessions

- Windows may expose no compatible media session, multiple `firefox.exe` GSMTC sessions, no active Firefox Core Audio session, or multiple active Firefox audio sessions.
- The accepted response is a readable refusal and silence, not fallback targeting.

### Deferred private-alpha distribution validation

- Genuine clean Windows 10 22H2 x64 and clean Windows 11 x64 validation has not been performed.
- Invited private-alpha tester feedback has not been collected and has no predictable timetable.
- The package is unsigned; SmartScreen/antivirus behavior and reputation on other machines remain unknown.
- A clean machine may require the separately installed Microsoft Visual C++ x64 runtime.
- These items no longer block internal development, but the package must not be represented as clean-machine certified, broadly compatible, publicly released, or validated for third-party distribution. Later failures may require a bounded repair.

## Deferred quality improvements

- **M12D format-variety limitation:** Steve accepted VAR1's generate-only simulated mixed-song evaluation with limitations. Individual comments can be good, but repeated title-first reactions and recurring `Oh ...` openings become recognizable and repetitive song after song. This remains an expected quality/product limitation, not a runtime defect; broader research or format work requires separate authorization.
- Reduce harmless wrapper-refresh log repetition without weakening diagnostics.
- Improve multilingual speech or transliteration only under a separately approved milestone; do not alter Unicode metadata to accommodate an English voice.
- Consider semantic similarity for comment repetition only under an approved design. Current history avoids exact duplicate accepted comments but intentionally has no semantic regeneration loop.
- Genre/style-change detection would require a trusted factual source. It must not be inferred from title, artist name, model memory, or imagined audio qualities.
- Browser-tab-level Firefox ducking is not a current requirement and has no approved implementation plan.
- Persistent listening history is not implemented or approved; current history is intentionally process-local and bounded.

### Current commentary prompt is not a mature radio-format interface

- M11B accepted deterministic coordination of comments, a fixed reusable liner, and direct music continuation. It did not evaluate or accept the current generic commentary prompt as the future creative interface.
- During M11B listening, Steve observed that an enriched comment could sound like a random commentator receiving facts rather than a radio host performing a defined on-air job. Earlier M9 experiments separately showed repeated openings, formulaic fact-plus-reaction structures, unsupported inferences, and the need to make metadata mention optional.
- Future Creative Direction and Content Generation work must test bounded, format-specific producer assignments. Host reactions, session observations, commercials, listener calls/dialogue, imaging, fictional event promos, callbacks, and later chains/arcs should not inherit one universal prompt merely because they all use an LLM.
- Trusted facts should be optional, scope-labeled creative material rather than a checklist. Artist-level evidence must not silently become a track-level claim.
- These observations authorize documentation and later generate-only evaluation only. They do not authorize changing the production prompt, model, generation path, runtime behavior, or M11B acceptance.

### Deferred M9C.6 integration quality work

- Owner review selected grounded Qwen3 14B whole-comment synthesis as successful experimental evidence, not accepted runtime behavior.
- The 96-sample benchmark still showed repeated rhetorical openings and near-duplicate structures that exact-string counting did not fully detect.
- Its mechanical classifier wrongly hard-rejected ten acceptable comments merely because they did not repeat the artist or title. A future integration validator should treat metadata mention as optional when playback context is already clear.
- Some otherwise strong comments changed the strength or meaning of a supplied fact, or inferred sound, lyrics, emotional content, or genre without supporting context. A future contract must define proportionate semantic-grounding policy before speech is authorized.
- Training-augmented prompting is not selected because it did not materially improve owner preference and reduced factual acceptability.
- These are M9B candidate risks to be evaluated; they do not make M9B accepted runtime behavior.

## Recorded performance context

- Ollama performance run: 4.262 seconds wall-clock, 2.191 seconds API total duration, 1.999 seconds model load, 2.68 GiB loaded size/VRAM reported by Ollama, and context length 2048. Selected-model unload verification passed.
- These are measurements from Steve's run, not estimates or guaranteed future performance.
- The two-second speech-delivery window begins only after preparation is complete; AI generation and Nicole synthesis occur earlier while music plays.
