# Durable decisions

These decisions summarize accepted project rules and rationale already evidenced by the repository and manual milestones. New ideas do not become decisions until Steve explicitly approves them.

## Authority and delivery discipline

- **The local Git repository is authoritative.** Exported ChatGPT Project-source copies are snapshots and may become stale.
- **One milestone at a time.** Passing automated checks makes work ready for manual acceptance; Steve's required real-desktop evidence is what passes a milestone.
- **Preserve proven paths.** Read-only observation, explicit control diagnostics, normal automatic behavior, and failure diagnostics remain separate so a safer mode cannot silently gain broader behavior.

## Local-first privacy and cost

- Song metadata, generated comments, speech preparation, state, and logs stay local.
- The application does not inspect browser pages, URLs, cookies, history, credentials, tokens, or account data and does not use unofficial YouTube APIs.
- AI uses Ollama's HTTP API only at loopback hosts (`localhost`, `127.0.0.1`, or `::1`); there is no API key or cloud-provider configuration.
- Normal model use requests immediate unload with `keep_alive: 0`. `--no-ai` is a first-class zero-model path.
- No large model or dependency is added without size, purpose, source, memory implications, alternatives, and Steve's approval.

## Media identity and metadata

- The intended Windows media session is the verified exact, case-insensitive `firefox.exe` GSMTC source. Zero or multiple candidates are unsafe; the application stays silent instead of guessing.
- Title and artist supplied by Windows are trusted metadata. Album is optional, is usually unavailable, and is never guessed.
- Duplicate media-property events with unchanged normalized identity do not become duplicate transitions.
- A fresh GSMTC wrapper is continuity only when exactly one eligible Playing Firefox candidate still has the same normalized title and artist. Unsafe or genuinely different targets require protected cancellation before reacquisition.

## Deterministic facts versus creative AI

- Exact title and artist, plus explicitly derived exact-string observations, are the only song/artist facts given authority in local generation.
- Pretrained knowledge, remembered trivia, lyrics, genre, sound, biography, release information, geography, and cultural associations are unavailable as factual evidence.
- Zoya may use literal wordplay, subjective reactions, and clearly imaginary or absurd framing without presenting them as facts.
- The deterministic validator catches mechanical problems; it does not claim to prove factual grounding or personality quality.
- Exact `FALLBACK`, invalid output, timeouts, AI failure, or disabled AI select the fixed title/artist announcement. Silence or fallback is preferable to unsupported prose.

## Scheduling and silence

- Automatic comments use an inclusive random interval of 2-5 genuine track transitions. The startup track does not count.
- Track A must be at least eight seconds old before preparation.
- For transition-aware delivery, generation and speech synthesis happen one transition ahead while music keeps playing. Prepared Track-A material may begin only inside the two-second Track-A-to-Track-B delivery window.
- There is only one pending/prepared opportunity and no catch-up queue. Late, unfinished, stale, rapidly skipped, silent, ambiguous, or unsafe opportunities are discarded.
- **Silence is valid behavior.** Safety and timing refusal are successful containment, not a reason to target another session or interrupt later.

## Voice choice

- Steve accepted Kokoro `af_nicole` as Zoya's primary local CPU voice and selected `af_heart` as runner-up.
- Windows local TTS remains the supported fallback and explicit troubleshooting path.
- Voice naturalness is separate from language accuracy. Steve found the Cyrillic sample natural-sounding but did not claim verified Russian pronunciation.

## Playback and volume safety

- The original media watcher remains completely read only.
- Earlier playback-control and one-announcement tests may use `TryPauseAsync()`/`TryPlayAsync()` only on the explicitly selected same Firefox GSMTC session with protected recovery.
- Normal automatic M7.5B delivery never pauses or resumes playback. It uses only the selected Firefox Core Audio application session.
- Ducking is proportional: 25% of the scalar captured immediately before delivery, not an absolute 25% volume. Protected cleanup restores the exact captured scalar and mute state.
- Chrome, unrelated applications, replacement sessions, global media keys, blind toggles, endpoint/master volume, and YouTube Music's in-page volume are never recovery fallbacks or control targets.
- Cancellation, speech failure, post-duck failure, stale state, and exceptions must all pass through restoration ownership before shutdown.

## Accepted isolation boundary

- Core Audio isolation is at the Firefox application-session level on Steve's PC. Multiple audible Firefox tabs may duck together.
- This application-level boundary is accepted for Phase 1. Chrome and Windows master volume remaining untouched are mandatory; browser-tab isolation is not a current requirement.
- GSMTC ownership is also Firefox application-level, not page-level. Ordinary YouTube may become the tracked Firefox media session when it owns Firefox media; Zoya does not inspect pages or distinguish it from YouTube Music.

## State and logging

- Recent listening context is deliberately ephemeral and bounded to 10 tracks and 6 accepted AI comments.
- Clearing state affects only in-memory context; timestamped UTF-8 diagnostics remain local until Steve deletes them. Shared-runtime logs use `%LOCALAPPDATA%\Zoya Radio\logs`; explicit diagnostic logs retain their repository-local location.
- Logs may contain media source, title, artist, optional album, state, timing, selected local model/voice, targeting, duck/restoration, and readable errors. They do not contain browser URLs, credentials, tokens, cookies, or unrelated process command lines.

## Accepted control application

- M8A is accepted at implementation commit `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc` after Steve's August 11, 2026 desktop test and the associated 162-test automated result.
- WinForms is a thin same-process frontend over the accepted radio runtime. It does not own factual playback state, scheduling, targeting, generation, speech, or restoration policy.
- `RadioRuntimeController` owns one authoritative asynchronous Start/Stop lifecycle and cleanup path. Stop and window close await protected cleanup; stale status after shutdown cannot revive the UI.
- Immutable status/activity snapshots or events are the presentation boundary. Radio activity is read only and reports deterministic runtime work; harmless wrapper refreshes remain detailed-log-only.
- Existing console launchers remain diagnostic frontends to the same runtime rather than a separate implementation.

## Tooling and dependencies

- The accepted application includes the WinForms control frontend, preserved console diagnostics, and the self-contained `0.8.0-alpha.1` Windows x64 package/readiness implementation at `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`.
- The Windows SDK APIs, loopback Ollama client, and KokoroSharp.CPU are the currently accepted implementation dependencies.
- Project-local SDK, package cache, models, generated audio, builds, and logs remain excluded from Git.

## Internal acceptance and deferred distribution validation

- Steve accepted M8B and closed Milestone 8 for continued internal development on August 11, 2026.
- Clean Windows 10 22H2, clean Windows 11, and invited private-alpha testing remain useful compatibility and distribution evidence but are deliberately deferred and do not block internal feature development.
- The accepted internal baseline must not be described as clean-machine certified, broadly compatible, publicly released, code-signed, SmartScreen-reputable, or validated for third-party distribution.
- A later tester failure may require a bounded compatibility or packaging repair; the absence of tester feedback is not evidence of compatibility.
- M9A was rejected for runtime consideration. M9C is closed as successful experimental evidence. M9B was implemented as a bounded candidate and supplied accepted Gate 2/3 evidence, but did not reach its final audible acceptance requirement.

## Milestone 9 closure

- Steve concluded Milestone 9 on August 15, 2026 as an exploration/prototype phase. This is a closure decision, not an acceptance of M9B as finished runtime behavior.
- No further M9 Gate 4 test, M9 repair, feature expansion, or runtime promotion is authorized. The implemented M9B candidate is retained unchanged for future product planning; the accepted M8 safety baseline remains controlling.
- M9 established source-backed-context, stronger-local-model, and safe-delivery evidence, while also establishing that remote metadata availability and current whole-card caching are future product-design concerns. Artist/track fact separation, context selection, source resilience, and a future product role for M9B remain deferred rather than implicitly approved.
- The detailed outcome is recorded in [MILESTONE_9_CLOSURE.md](MILESTONE_9_CLOSURE.md).

## Approved post-M9 capability direction

- Steve approved the capability-first roadmap in [POST_M9_CAPABILITY_ROADMAP.md](POST_M9_CAPABILITY_ROADMAP.md) on August 21, 2026. Roadmap approval establishes direction only; every implementation milestone still requires a separately approved bounded contract and explicit request.
- M10A pure deterministic opportunity kernel, M10B prepared-material lifecycle/binding, and M10C strict M8 runtime parity are the immediate foundation sequence. The first live migration reproduces accepted M8 behavior before adding formats.
- Formats mature as clients and vertical tests of reusable capabilities. A commercial, call, rant, liner, ID, callback, or chained bit is not considered mature merely because one fixture can play.
- Exact cadence, cooldown, retention, promotion, and decay values remain conservative milestone-local experimental defaults until real listening evidence supports durable policy.
- The deterministic engine and Editorial Arbiter own opportunity eligibility, preparation/airtime choice, music pressure, spacing, priority, treatment compatibility, reservation, final revalidation, and silence. Randomness is bounded to editorially equivalent alternatives and cannot itself create the reason to interrupt music.
- M11A establishes only the arbitration mechanism, typed reasons, and incremental relevance extension points. It evaluates relevance from state actually available at M11A and does not impose a speculative universal relevance framework.
- Creative Context reports compact established state. Creative Direction describes what a potential segment should accomplish. Content Generation writes exact words/renderable content. One early local-model call may perform direction and generation together, but these responsibilities remain conceptually separate.
- Creative AI never controls scheduling, priority, timing, cooldown, playback, safety, expiry, memory commits, or whether generated material earns airtime. Silence and direct music continuation remain valid winners.

## Approved format-aware creative-generation boundaries

- A generic `LLM + facts -> commentary` path is not the future universal creative interface. Every generation request must identify the speaker/role, on-air situation, specific format, editorial objective, intended duration/structure, trusted context and explicit unknowns, creative permissions, and output contract at the level required by that format. This is a responsibility boundary, not approval of one frozen schema or exact prompt.
- Stable Zoya personality is separate from the current producer assignment. Creative Direction states what a potential segment should accomplish; Content Generation writes the exact words or other renderable content. One local-model call may initially perform both responsibilities, but a premise may later be cached, rejected, rewritten, or routed to a format-specific writer without becoming airtime or canon.
- Trusted facts are optional creative material, not a recital checklist. A format may use none, one, or several supplied facts when editorially useful. Missing facts remain unknown, and supplied facts do not force speech, metadata repetition, or factual exposition.
- Factual scope remains explicit. Artist-level evidence may support a statement about the artist but does not establish track meaning, sound, lyrics, genre, mood, or history. Track-, recording-, release-, and session-level evidence likewise remain within their declared scope.
- Host reactions, session observations, commercials, listener calls/dialogue, station imaging, fictional event promos, callbacks, and later chains/arcs require format-specific direction, generation contracts, and owner evaluation. Success in one format does not validate another, and no giant universal prompt is approved.
- The producer-assignment style is an approved design principle to test: state the role, situation, job, constraints, permissions, and required deliverable clearly enough that the model is performing a radio function rather than producing generic commentary. Particular wording, field names, prompt layering, model choice, and whether direction and writing use one or two calls remain experimental.
- Cross-format continuity remains optional and editorially earned. Context may expose an approved open thread, but a keyword match does not script a callback, Creative Direction cannot make lore canonical, and the Arbiter may reject every continuation.
- Using a smaller model for Creative Direction and a larger model for exact writing is a Format Laboratory hypothesis only. It is not an architectural requirement or authorization to add or change models.

## Approved knowledge, memory, and continuity boundaries

- Music Knowledge, Listening Intelligence, and Station Lore are separate data domains. A bounded Creative Context Assembler may combine selected canonical facts/state, but must not expose raw provider payloads or raw long-term listening events to prompts.
- Music Knowledge is source-agnostic. Provider-specific Source Adapters parse source semantics; a conservative deterministic Entity/Identity Resolver establishes positive correspondence; fact-type-specific Evidence/Fact Reconciliation preserves provenance, disagreement, and categorical evidence state.
- Source outage, timeout/rate limit, parser/schema failure, ambiguous identity, unsupported representation, corrupt cache, stale evidence, confirmed no fact, and confirmed fact are distinct typed outcomes. Temporary or technical failure never becomes negative factual evidence.
- Identity requires strict positive resolution using stable IDs, source-declared relationships, structured credits, approved aliases/edition relationships, and safe normalization. Unrestricted fuzzy matching and generic display-suffix stripping are not approved.
- Genre/style may retain multiple source-specific tags plus only demonstrated deterministic parent mappings. Unknown remains unknown; an LLM does not decide or store factual genre classification.
- Session Listening Intelligence precedes durable affinity. It retains observed-track count, usable-classification count, category count, and both all-observed and classified-only proportions so sparse metadata cannot appear artificially dominant.
- Short-lived/session state may be detailed. Durable Listening Intelligence, if later accepted, remains compact derived state such as reinforced artist/track/tag relevance, last reinforcement, coarse strength, and trend; it is not an indefinite chronological listening diary. Reinforcement/decay equations remain experimental until evidence supports them.
- Raw Creative Director alternatives are ephemeral. A proposal reaches persistent pending Station Lore only for a deterministic reason such as Steve approval, selection into a still-valid prepared item, or an authorized complete-airtime effect. Rejected, expired, discarded, unaired, or partially heard material creates no canon.
- Open threads create creative possibility, never scheduling obligation. Threads may become dormant, expire, or retire; the Arbiter may reject every continuation indefinitely.
- Airtime effects begin with the smallest typed, idempotent effects demonstrated by real clients. No generic generated-lore transaction system is approved in advance.

## Approved voice, locale, delivery, and release sequencing

- Nicole remains the accepted default for the current Zoya build, but host identity, voice identity, renderer/backend, performance direction, and fallback are separate concepts. Host Voice/Renderer Foundation precedes broad cast work; cast/role capability is added when calls/commercials demonstrate need.
- Expressive-renderer capability evaluation is separate from later production integration. A bounded provider-neutral evaluation may compare normal-host quality, expressive controls, voice identities, multi-speaker potential, latency/resources, reliability/determinism, cancellation/fallback, EN/PT-BR support, licensing, model size, hardware, privacy, and local-first implications. Chatterbox is an example candidate only, not an approved dependency.
- A renderer selected for isolated speech must be evaluated again inside real prepared scenes for voice consistency, exact rendered-duration timing, performance-direction mapping, cancellation/disposal, processing tolerance, and produced-radio quality before it becomes a production capability.
- Language and locale are separate from host/voice identity. Full station configuration remains late, but English/Nicole assumptions must not become inseparable from the engine; bounded EN/PT-BR and pronunciation/cultural-fit experiments may occur earlier.
- Delivery treatment is independent from content format and duration. Content may declare allowed, preferred, or required treatments, and an opportunity must provide a compatible treatment. No rule equivalent to `duration > X => pause Firefox` is approved.
- The current automatic no-pause invariant remains controlling until an isolated exact-target hold diagnostic passes a major manual safety gate. An opt-in `HoldAndReplace` primitive requires a separate later decision and authorization; M15A does not authorize M15B.
- A bounded format experiment may use a second voice, one telephone/radio filter, a ring or hang-up effect, a simple bed/fade, gain adjustment, or normalization before the shared production phase. Such evidence does not establish a reusable production subsystem; proven needs must be consolidated rather than preserved as format-specific hacks.
- Prepared-scene representation/compilation, production voice-renderer integration, shared mixing/FX, and canonical sonic identity are distinct capability and acceptance boundaries. Voice identity, performance intent, scene timing, audio engineering, and station branding must remain independently replaceable and testable.
- Complex scenes should normally be completely prepared before their first audible beat. Real rendered durations, ownership, cancellation, disposal, treatment compatibility, final safety revalidation, and silence remain deterministic even when scene content or performance is AI-assisted.
- Distribution/Firefox compatibility work occurs when Steve decides the experience is worth distributing. Opportunistic Firefox evidence may be gathered earlier, but private alpha does not gate richer internal formats, production, or sonic identity.
- General configuration is extracted last from dimensions proven useful by the mature opinionated Zoya station; no speculative station builder is approved now.
- None of these roadmap decisions changes implemented behavior. The accepted M8 safety baseline remains controlling until a later bounded milestone is implemented, verified, and explicitly accepted.

## Accepted M13A-I1 identity and provenance boundary

- On August 28, 2026 Steve accepted I1 as a fixture-only, non-network foundation; no provider activation is approved.
- Root artist collections exclusively own identity candidacy. Nested/direct credits are references bound to a declared root, and credit-only identity acceptance is prohibited.
- Stable MusicBrainz MBID remains the canonical `EntityId`. A linked Wikidata QID is provenance only and may be considered only after positive MusicBrainz identity resolution.
- Malformed/schema failure, ambiguity, temporary outage, confirmed negative fact, and confirmed fact remain categorical and distinct. Parser failure is never negative factual evidence.

## Accepted M12D D3.2 topic-isolation boundary

- D3.2 is an accepted bounded repair to the default-off quick-reaction pilot, followed by an accepted exactly 32-request comparison on August 27, 2026. Three requests were mechanically rejected for being under 15 words; selected-model unload succeeded. This comparison does not prove universal topic isolation.
- The versioned `quick-reaction-v3-topic-isolation` prompt keeps current Track-A title/artist as the only subject matter. Up to three recent accepted lines are reduced to abstract form measurements: opening shape, length band, sentence count, ending shape, and dash/semicolon use. Their words, topics, facts, premises, entities, jokes, scenes, and continuations are absent.
- Existing spoken-text cleanup, current-track grounding, default-off/explicit-opt-in behavior, lifecycle/arbitration/binding/cancellation/targeting/ducking/restoration, and M11B pilot-off behavior remain controlling. The D3.1 Otava Yo/Tommy the Cat carryover was a quality defect, not a lifecycle or targeting defect, and is resolved by this boundary.

## Accepted M12D-VAR1 quality boundary

- On August 27, 2026, Steve accepted the generate-only simulated mixed-song format-variety evaluation with limitations.
- Good individual local-model comments do not establish sustained format variety: repeated title-first reactions and recurring `Oh ...` openings become recognizable and repetitive song after song.
- This is an expected quality/product limitation and temporary placeholder, not an established application/runtime defect. Its underlying cause remains unestablished.
- Future research into model choice, prompting, personality, and bounded context/enrichment is deferred until broader evidence from later formats, including possible commercials and listener calls. This decision authorizes neither that research nor any model, dependency, enrichment, lyrics/facts, format, implementation, or milestone.
- VAR1 artifacts remain generate-only; nothing became live, audible, or user-visible. Default-off/explicit-opt-in quick-reaction behavior and accepted runtime/safety behavior remain unchanged.

## Approved Milestone 9A experimental boundaries

- M9A is a generate-only comparison experiment, not an accepted change to ordinary radio behavior.
- The experimental brief may use exact title/artist, bounded exact-string history facts, an explicit typed reason for speaking, recent accepted comment text, and shallow deterministic style fingerprints. It may not add inferred music, artist, listener, or session facts.
- The accepted runtime prompt/context/fallback remains unchanged. The structured candidate prompt and deterministic announcement renderer cannot enter normal composition without a separately authorized M9B contract.
- Metadata that resembles instructions remains inert serialized data. Creative freedom is limited to subjective reaction, literal wordplay, playful exaggeration, clearly imaginary framing, and accepted-persona teasing without unsupported claims.
- The explicit benchmark uses fixed synthetic fixtures, two blinded arms, loopback-only `qwen3:4b-instruct`, bounded requests, ignored local artifacts, and selected-model unload. It cannot request GSMTC, use Core Audio, control playback/volume, produce speech, or open the GUI.
- Mechanical validation and latency do not determine creative quality. Steve's blinded grounding, relevance, personality, phrasing, variety, concision, and preference review is the M9A acceptance evidence.
- M9A does not claim the known `Borrowed Umbrella` grounding defect is fixed and does not authorize M9B.
- Steve's completed blinded review rejected the StructuredCandidate for runtime consideration. Although it drew fewer rejections than the accepted baseline, neither arm reliably met the required qualitative and exact-metadata grounding standard. The accepted runtime prompt, context, validator, and fallback remain controlling; a further experiment or any M9B work requires a new bounded contract.

## Approved Milestone 9C experimental boundaries

- M9C is a generate-only diagnostic. It may use exact title/artist only with approved MusicBrainz and linked Wikidata endpoints, plus loopback Ollama.
- Any ambiguous, unavailable, rate-limited, malformed, conflicting, unsupported, or failed source chain produces no enrichment and safe `FALLBACK`; no source fact can force speech.
- Every supplied external fact must be concise structured data with a direct approved HTTPS source URL. Raw page content, browser data, history, listener data, prompts, and generated prose never leave the machine.
- The bounded ignored cache is diagnostic-only, easy to clear, and is not listening history or runtime state.
- M9C diagnostic code cannot enter normal radio composition directly. Only the separately approved M9B contract may authorize a bounded reimplementation after Steve gives an explicit coding instruction.
- Steve's completed M9C.6 review selects local `qwen3:14b-q4_K_M` with reviewed multi-fact context packs and grounded whole-comment synthesis as the direction for any future integration proposal.
- The model must synthesize the entire comment; deterministic fact-plus-separate-reaction concatenation is rejected as unnatural and repetitive.
- Training-augmented factual freedom is not selected: it provided no material qualitative benefit and reduced factual acceptability. Externally verifiable claims in the selected candidate remain bounded to reviewed supplied facts.
- A future integration validator must not require every comment to repeat the current artist or title. It must guard against meaning-changing exaggeration and unsupported lyrics/meanings presented as fact, history, biography, credits, cultural influence, or other externally verifiable claims; ordinary subjective musical characterization is separately governed by the approved M9B policy below.
- M9C is accepted only as experimental design evidence. The accepted M8 runtime remains controlling until M9B is implemented, verified, and explicitly accepted.

## Approved Milestone 9B integration boundaries

- M9B is the approved name and bounded contract for the first grounded-context runtime integration. It is implemented as a candidate after Steve's explicit August 15 instruction, but remains unaccepted until its staged engineering and owner gates are complete.
- Live enrichment is restricted to exact MusicBrainz identity, official release/release-group fields needed for one bounded soundtrack fact, and linked structured Wikidata data. Exact song-level confirmation uses the nested canonical MusicBrainz artist-credit ID/name and accepts one or more exact recordings without claiming an edition. Arbitrary browsing, live Wikipedia/page retrieval, raw-page prompting, cloud AI, and unrestricted training-knowledge claims are prohibited.
- A small versioned local catalog of reviewed, paraphrased, source-attributed context facts is allowed. Its cited pages are provenance only and are not fetched at runtime.
- The runtime context cache may persist under bounded LocalAppData storage: at most 100 entries, 24-hour confirmed TTL, two-hour negative TTL, and Clear History deletion. The nested-credit repair invalidates pre-repair negative entries while preserving reusable confirmed entries. It is not durable listening history.
- `qwen3:14b-q4_K_M` is an optional owner-installed local prerequisite. It is not bundled or automatically downloaded. If absent, disabled, late, or failed, the accepted M8 4B behavior remains eligible and unsafe opportunities remain silent.
- The 14B model writes the whole comment with the reviewed card as its factual/editorial anchor. Steve accepts familiar low-risk music knowledge, natural host opinion, ordinary musical characterization, low-risk names/attributions, popularity/cultural observations, ordinary host adjectives, and modest exaggeration in this private program. Precise history, dates, quotations, credits, severe causal claims, and explicit context contradictions remain constrained.
- Omission of artist/title, questions, clichés, mild verbosity, imagined-scene phrasing, fact recitation, and formulaic construction are advisory quality signals unless they create another violation. They are not automatic hard rejections.
- Hard discard is reserved for high-confidence technical/safety failures: malformed or control output, Unicode/metadata corruption, explicit current-metadata or context contradiction, stale/mismatched opportunity, invented Zoya biography presented as real, or the precise severe exchange-door causal exaggeration. Low-risk music knowledge, names, numbers/dates, cultural/popularity observations, `iconic`, `legendary`, `shaped`, and modest exaggeration are not automatic hard discards; they can remain advisory diagnostics.
- Exact reviewed catalog cards and their individually reviewed exact artist-specific aliases take precedence over cached live-enrichment outcomes. This prevents a stale negative cache result from masking reviewed context; unknown tracks still use the bounded cache/live resolution path.
- M9B permits at most one bounded quality regeneration, six ephemeral enriched-comment fingerprints, and no catch-up or unbounded retry.
- M9B selected, prepared, delivered, and discarded text is recorded only in the existing private local runtime log. The lifecycle labels must not conflate a prepared comment with a delivered comment, and must not include prompts, raw enrichment responses, browser data, credentials, or unrelated listening history.
- Measured prepared speech strictly above the configured automatic maximum is discarded and disposed before any Core Audio enumeration, ducking, or speech. The exact limit is permitted. Existing delivery-time timeout and exact restoration remain defense in depth; no catch-up is created for an over-limit preparation.
- Enrichment failures retain a concise typed private reason identifying the bounded lookup stage, timeout versus HTTP status, safe `Retry-After` availability, and cached-negative status. Raw HTTP bodies, broad retries, and exception dumps remain prohibited.
- All accepted M8 targeting, scheduling, cancellation, Silent Mode, speech, Firefox-only ducking, exact restoration, shutdown, local-first, and safe-silence invariants remain controlling.
- Steve accepted the recorded M9B Gate 2 generate-only review on August 15, 2026 with a narrow same-card repetition exception. The exception recognizes that three outputs from each identical song/card amplified repeated templates; it does not relax grounding, hard-discard, repetition-state, fallback, silence, silent-desktop, audible-desktop, or final-acceptance requirements.
- M9B passed fake-driven engineering checks, a familiar-track generate-only review, and the revised silent operational Gate 3. The latest authorized Gate 4 run safely delivered three enriched comments and three fallback comments, while one 26.3-second enriched preparation failed safely after a delivery-time timeout and exact restoration. It did not meet the five-enriched-delivery requirement. Steve then concluded M9 without final M9B runtime acceptance; no intermediate gate or retained implementation changes that status.

## Approved Milestone 8 boundaries

- **The staged M8 authorizations were satisfied.** M8A and M8B were separately authorized and owner-accepted. The dated sequencing amendment closes M8 for internal development while preserving the original distribution-evidence requirements as deferred.
- **M8A uses WinForms and same-process hosting.** The accepted control application uses one UI-independent runtime controller or facade and one authoritative asynchronous lifecycle and cleanup path. Existing console diagnostics remain frontends to the same accepted runtime.
- **M8 does not redesign radio behavior.** Existing scheduling, transition timing, safe-silence decisions, Firefox-only targeting, exact restoration, local-first privacy, and bounded ephemeral state remain controlling.
- **M8B's implemented package is portable.** The internal baseline is a versioned, unsigned, self-contained Windows x64 ZIP with application-relative assets, LocalAppData logs, hashes, a manifest, notices, and manual side-by-side replacement/rollback. Clean-machine evidence remains deferred before broader distribution claims.
- **Firefox and Ollama remain separate.** Neither installer is bundled. Zoya may direct a tester to the official Ollama source, and may start an installed Ollama application only after explicit tester action.
- **Qwen setup requires informed consent.** Invoking `ollama pull qwen3:4b-instruct` requires size disclosure, a free-space check, explicit consent, visible progress, and cancellation. Third-party terms are never accepted automatically.
- **eSpeak exclusion remains a distribution gate.** The accepted internal Nicole/en-US package contains no eSpeak and passed representative owner-machine tests. Clean-machine validation remains deferred; if it reveals that eSpeak is necessary, broader distribution pauses or uses an explicitly approved fallback/compliance solution.
- **Windows TTS is a deliberate fallback route.** It omits the Kokoro model, Nicole voice, ONNX native closure, and eSpeak; it must be disclosed as a different voice experience and meet the same runtime-safety and clean-machine requirements.
- **M8 excludes broader distribution machinery.** No traditional installer, Store package, bootstrapper, automatic third-party installation, code signing, public release, automatic updater, tray/background service, or general future-scale packaging framework is approved.

The complete scope, non-goals, gates, evidence requirements, and unresolved risks are governed by [MILESTONE_8_CONTRACT.md](MILESTONE_8_CONTRACT.md).
