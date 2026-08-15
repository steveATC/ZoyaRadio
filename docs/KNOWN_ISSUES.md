# Known issues and limitations

This file separates unresolved defects from accepted platform limits and deferred quality improvements. An item here is not automatically approved milestone scope.

## Unresolved defects and observations

### Intermittent observer stall at 20:49

- A manual run stopped producing useful observer behavior at approximately 20:49.
- The later focused Firefox-plus-Chrome M7.5B acceptance run did not reproduce it.
- There is no evidence that the wrapper-continuity repair fixed this separate observation, so it remains unresolved and unreproduced.

### Excessive wrapper-refresh diagnostics

- Global GSMTC churn can produce many harmless `FIREFOX GSMTC WRAPPER REFRESHED` lines.
- The refresh handling is functionally accepted, but the repeated output is a non-blocking logging-quality defect.
- Logging reduction was deliberately deferred rather than mixed into the accepted safety repair.

### `Borrowed Umbrella` grounding defect

- Manual Milestone 4 testing found `qwen3:4b-instruct` sometimes invented `Borrowed Umbrella` for unrelated Cyrillic metadata.
- This is a model/prompt factual-grounding defect, not Unicode corruption.
- Steve accepted the current persona and deferred further tuning rather than adding a brittle phrase blacklist.
- Mechanical validation still cannot prove factual grounding; generated prose requires human review when grounding quality is the acceptance target.

### M9A candidate remains unsuitable for runtime

- Steve's corrected blinded M9A review rejected the StructuredCandidate for runtime consideration. It was preferable to the accepted baseline in the fixed comparison, but still produced repetitive constructions, awkward output, unsupported listener/session claims, and did not reliably preserve the exact supplied Unicode metadata.
- In the Unicode scenario, the candidate translated/transliterated the supplied Cyrillic metadata while baseline samples substituted different Cyrillic metadata. Both violate the experiment's exact-metadata boundary; this is an output-grounding failure, not storage mojibake in the corrected fixture.
- This recorded result remains historical: M9B now follows its separately approved contract and explicit implementation authorization, but has not resolved the M9A candidate defect.

### M9B grounded-context candidate remains under staged evaluation

- M9B passed its generate-only Gate 2 owner review on August 15, 2026 and its silent operational Gate 3 under Steve's revised acceptance policy. The four historical desktop trials recorded 48 genuine transitions, zero forbidden desktop-media activity, cancellation/stale-work disposal, selected-model unload, one ready candidate, and safely suppressed prepared delivery. The final reviewed-context smoke's former 7/12 validator-ready measurement remains diagnostic only; Steve accepted all twelve final smoke outputs for Zoya to speak. The latest authorized Gate 4 run prepared four enriched comments, delivered three enriched M9B comments and three M8/fixed fallbacks, and safely timed out one known 26.3-second M9B preparation with exact Firefox restoration, no playback pause, and clean shutdown. It did not meet the five-enriched-delivery requirement. The over-duration preflight, M9B text-lifecycle logging, and typed enrichment diagnostics are now verified; Gate 4 is paused while Steve decides whether and when to resume it, and final M9B acceptance remains outstanding.
- Deterministic validation catches only defined high-confidence technical/safety failures. Familiar low-risk musical knowledge, natural host opinion, ordinary musical characterization, names/attributions, popularity/cultural observations, ordinary adjectives, modest exaggeration, and precise details outside a card may produce advisory diagnostics but are not automatic hard failures. The validator cannot prove arbitrary semantic truth, naturalness, or spoken quality; owner review remains required.
- Three independent generations from the same song/card frequently reused openings or templates. Runtime state bounds the last six delivered comments/fact identities and permits one regeneration; an advisory result that remains advisory after that bound is retained, while hard, stale, invalid, or unsafe work falls back or stays silent. Raw rejected output remains available for owner inspection.
- The optional 14B model has materially higher cold-load and RAM/VRAM cost than the accepted 4B route. Its absence, failed local enrichment, timing failure, or invalid candidate must preserve the M8 candidate/fallback or safe silence.
- The local reviewed catalog is intentionally small. Unknown tracks depend on strict MusicBrainz/Wikidata confirmation and may safely remain unenriched. Collaboration metadata represented by separate artist entities, including `Secede & Kettel` — `Leraine`, remains unsupported; no fuzzy combined-name workaround or special card is allowed.
- During the latest Gate 4 run, the accepted local 4B fallback route for `Mind` — System Of A Down received HTTP 500 because its underlying `llama-server` terminated with exit status `0xc0000409` and `GGML_ASSERT(check == -n_chars) failed`. Zoya used the deterministic title/artist fallback, attempted selected-model unload, restored Firefox exactly, and did not start unsafe partial text. This local Ollama crash is retained as a known runtime issue; no Ollama repair, update, downgrade, or model change is authorized here.

### Retained native working set

- Repeated Nicole CPU preparation disposes the ONNX model each time, but the long-running .NET/native process retained roughly 333 MiB working set after three preparations.
- No GPU execution provider was loaded in that observation.
- This is a measured resource observation, not evidence that model disposal failed, and no memory-management redesign is approved yet.

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

- Reduce harmless wrapper-refresh log repetition without weakening diagnostics.
- Improve multilingual speech or transliteration only under a separately approved milestone; do not alter Unicode metadata to accommodate an English voice.
- Consider semantic similarity for comment repetition only under an approved design. Current history avoids exact duplicate accepted comments but intentionally has no semantic regeneration loop.
- Genre/style-change detection would require a trusted factual source. It must not be inferred from title, artist name, model memory, or imagined audio qualities.
- Browser-tab-level Firefox ducking is not a current requirement and has no approved implementation plan.
- Persistent listening history is not implemented or approved; current history is intentionally process-local and bounded.

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
