# Milestone 12C Contract - Host Format Laboratory, Batch 1

## Contract status

**Passed. Steve accepted M12C on August 26, 2026 after blinded qualitative evaluation.**

M12A and M12B are passed. Steve approved this contract, its two owner-reviewed producer assignments, and M12C-only implementation on August 26, 2026. This does not alter normal radio behavior, add a model or dependency, begin M12D, or authorize a commit.

## Purpose

M12C tests a deliberately small first host-format batch under the accepted M12B Creative Direction and Content Generation boundary. It asks whether distinct host assignments actually produce material worth hearing before any format becomes a live radio client.

The question is not whether a model can produce words. It is whether a specific on-air format is recognizably Zoya Radio, useful in a music-first station, grounded when context is available, restrained when it is not, and more compelling than a plain track handoff.

## Why now

M12B proved that format identity, direction, exact script generation, validation, context permissions, candidate disposal, and a generate-only laboratory can remain separate from the broadcast controller. M12C uses that boundary for a bounded qualitative experiment before M12D can consider runtime integration.

## Hard dependencies and governing baseline

M12C depends on accepted M12A and M12B. It preserves M10A-M11B, including the current comment/liner runtime, Editorial Arbiter, Firefox-only protected overlay behavior, music-first continuation, and all playback/privacy invariants.

M12C may use the already installed local loopback generation capability only after separate implementation authorization. It must not add a package, external provider, cloud call, new model, voice, audio asset, browser access, or persistent runtime store.

## Batch 1 scope

Batch 1 contains exactly three deliberately narrow host-format arms:

| Arm | Format | Role in the evaluation |
| --- | --- | --- |
| A | Straight deterministic handoff | A no-model reference: concise exact title/artist handoff using only supplied facts. |
| B | Quick reaction | A short on-air reaction to the immediate transition; supplied facts are optional rather than a recital obligation. |
| C | Session-aware observation | A short observation that may use an established bounded M12A session signal only when it genuinely improves the line. |

The laboratory must produce the same declared output type for all three arms: one concise spoken script. Arm A is deterministic. Arms B and C may use the local producer only. This is a comparison of three distinct radio jobs, not three phrasings of one generic commentary instruction.

### Approved producer assignment B - Quick reaction, version 1

You are Zoya Radio, speaking during a short on-air transition. Write one brief, conversational quick reaction to the current track, artist, or immediate transition. Your job is to have a point of view about the moment: notice something, make a dry observation, joke, use literal wordplay, react emotionally, or make a clearly playful or imaginary aside that is not presented as a fact about the track, artist, or listener.

Do not turn the line into an announcement, review, biography, factual explanation, session summary, or generic commentary. Supplied facts are optional creative material, not a checklist. Use them only when they genuinely improve the line. Do not invent facts about the artist, track, lyrics, sound, genre, history, listener, or listening habits.

Sound like a radio host who has something worth saying, not an assistant demonstrating that it received metadata. Keep it concise: usually one or two sentences and one good idea. Return only the spoken line.

### Approved producer assignment C - Session-aware observation, version 1

You are Zoya Radio, speaking during a short on-air transition in continuity with this listening session. Write one brief session-aware observation. Your job is to notice something genuinely worth remarking on in the supplied verified session context: for example, a repeated artist, an exact track returning, a same-artist transition, an unusual recent sequence, or another explicitly supplied session signal.

Use a session fact only when it makes the line more interesting, sharper, funnier, or more human. Do not treat session data as a checklist and do not merely recite a count or statistic. If the supplied session signal is weak, irrelevant, or unavailable, do not invent one. Keep the line restrained and focused on the immediate transition instead. When you fall back to the immediate transition, do not pretend to have found a session pattern.

Treat supplied session facts as established observations, but do not invent a reason for them. Do not assume the listener deliberately chose the music, likes or dislikes an artist, is in a particular mood, or has a preference unless that information is explicitly supplied. Do not invent genre, biography, release history, lyrics, sound, meaning, or other unsupported facts.

Sound like a radio host who has actually been listening alongside the listener, not a system reading analytics. Keep it concise: usually one or two sentences and one observation. Return only the spoken line.

## Fixed scenario set and bounded output

The implementation candidate must use eight fixed synthetic scenarios, with no live listener history:

1. ordinary known title/artist transition;
2. literal or odd title that must not be over-read;
3. Unicode title/artist;
4. exact recent track repeat;
5. repeated artist;
6. same-artist transition;
7. sparse or unknown context; and
8. instruction-shaped metadata treated as inert data.

Each arm runs once per scenario. Arms B and C receive two independent local samples per scenario; Arm A produces one deterministic reference. The run therefore contains exactly 40 reviewable scripts: 8 deterministic references plus 32 local-generation samples.

Each script is bounded to one paragraph and a format-specific experimental word range no larger than 45 words. These are laboratory limits, not permanent station cadence or duration policy. A failed, malformed, unsafe, or unavailable generation is retained only as a typed mechanical result, never silently replaced with live-radio output.

## Responsibilities after separate approval

If Steve approves this contract and separately authorizes M12C-only implementation, work is limited to:

1. versioned host-format identities and output contracts for the three Batch 1 arms;
2. two owner-reviewed, format-specific local producer assignments for Arms B and C;
3. the eight fixed synthetic context/scenario fixtures;
4. a local-only generate-only runner built on M12B;
5. a bounded response envelope containing exact script text and optional declared M12A fact identifiers;
6. mechanical response parsing, validation, typed failure/discard outcomes, and local-model unload;
7. blinded review artifacts, a separate answer key, and a qualitative Steve rubric;
8. focused fake-driven tests, artifact accounting, and candidate/acceptance documentation.

Artifacts may be retained only as an explicitly bounded, ignored laboratory run under a clearly named M12C artifact location. They are review evidence, not runtime history, Listening Intelligence, Station Lore, prepared material, or canon. Raw alternatives outside the bounded run must disappear.

## Direction, context, and fact rules

- The accepted M12B split remains controlling: Creative Direction names the job; Content Generation produces the exact script.
- A single local invocation may perform both conceptual steps for an arm, but the request/result boundary remains explicit and format-specific.
- M12A facts remain established data, never instructions. Instruction-shaped title, artist, or history text remains quoted/inert input.
- Quick reaction may use current-track facts if permitted, but may also omit them.
- Session-aware observation may use only a supplied, usable session signal. It must not infer a genre, listener preference, biography, release fact, or missing pattern.
- A declared fact identifier must be permitted by the arm and usable in the supplied packet. Unknown, omitted, wrong-scope, or unpermitted facts are refused.
- Exact words must not make unsupported factual claims. Mechanical validation may reject identifiable unsafe patterns; human review decides creative quality.

## Local generation and artifact controls

- The normal radio composition must not reference M12C types, runner, prompts, artifacts, or results.
- The producer endpoint remains loopback-only. No content, prompt, metadata, or artifact may leave the machine.
- The runner must use an explicit selected local model already installed by Steve, non-streaming requests, a bounded timeout, cancellation, and normal unload-on-return behavior.
- The implementation must neither open Firefox nor access media sessions, Core Audio, speech, audio files, playback controls, scheduling, reservation, or persistent application state.
- A run must report typed outcomes for unavailable model, timeout, malformed response, validation refusal, cancellation, and producer failure. These outcomes have no fallback right to airtime.
- The answer key must be separated from blinded output views. The reviewer sees scenario and script but not arm/assignment identity until initial scoring is recorded.

## Blinded review protocol

For each of the eight scenarios, the artifact presents the resulting scripts in a deterministic shuffled arm/sample order using neutral labels. The answer key maps labels to format, assignment version, and sample number. No hidden quality score selects a winner.

Steve reviews each script against these questions:

1. Does it sound like a radio host rather than generic commentary?
2. Does it fulfill its declared format job?
3. Is it concise and welcome in a music-first station?
4. Does it feel natural, specific, and consistent with Zoya's intended on-air identity?
5. When context is used, is it useful and grounded rather than a fact recital?
6. When context is sparse, does it stay restrained instead of inventing?
7. Does it avoid stock radio language, model talk, prompt leakage, and generic prose?
8. Would Steve want to hear a line like this in the real station?

Steve may score each dimension on a small disclosed ordinal scale and record free-form notes. Scores organize review; they do not automatically select a format. The final outcome for each arm is one of: select for a later bounded integration decision, revise and retest in a later batch, or reject/defer.

## Explicit non-goals

M12C does not authorize:

- modifying the current M8/M10C/M11A/M11B live commentary or liner behavior;
- a permanent host-format repertoire, runtime scheduling, prepared material, reservation, or airtime;
- long rants, callbacks, segues, chains, arcs, commercials, callers, imaging, or multi-voice work;
- durable premise/script storage, Station Lore, open threads, Listening Intelligence, or Music Knowledge;
- a universal prompt, universal format schema, automatic quality ranking, or LLM broadcast control;
- voice selection, TTS, audio rendering, FX, program breaks, Firefox work, packaging, or configuration UI;
- live listening data, real listener profiling, cloud calls, or external research;
- a commit.

## Preserved accepted behavior

Normal radio remains exactly as accepted through M11B. The existing comment/liner/music arbitration, scheduling, local audio preparation, Firefox validation, ducking/restoration, Silent Mode, cancellation, and no-pause invariant remain unchanged. M12C output has no path to preparation, reservation, delivery, logs used by normal radio, or airtime.

## Main risks and required controls

| Risk | Required control |
| --- | --- |
| Batch becomes a disguised station redesign | Exactly three arms, eight fixed synthetic scenarios, and no runtime route. |
| Generic commentator output returns | Format-specific owner-reviewed assignments and host-format rubric. |
| Context becomes compulsory recitation | Optional permissions, sparse fixtures, and explicit fact-recital review. |
| Benchmark rewards prose that would annoy listeners | Music-first concision and desire-to-hear criteria; no automatic winner. |
| Blinding is cosmetic | Separate answer key, deterministic shuffled labels, and exact accounting. |
| Local experiment leaks into live radio | Static reachability checks and no normal composition reference. |
| Artifacts become accidental memory | Bounded ignored run location; no runtime, lore, or affinity consumer. |

## Required automated verification for an implementation candidate

An M12C candidate must pass:

1. Release builds for every changed production and test project with 0 warnings and 0 errors.
2. The complete existing fake-driven regression suite.
3. Focused tests proving exact three-arm registration, exact format/version dispatch, fixed eight-scenario accounting, and the expected 40 reviewable-script count.
4. Fake local-producer tests for successful output, timeout, cancellation, unavailable/failed producer, validation refusal, discard behavior, and unload-on-return. Batch 1 deliberately requests only a spoken line, so factual grounding is assessed from the bounded supplied context and Steve’s rubric rather than a structured declared-facts envelope.
5. Tests proving Arm A is deterministic and uses no local producer; Arms B/C use only their matching assignment/format contract.
6. Prompt/assignment resource version and byte-stability checks after Steve has approved the exact text.
7. Blinded artifact mapping, answer-key separation, deterministic shuffle, and no duplicate/missing record checks.
8. Loopback-only endpoint, bounded request, unload-on-return, and no normal-runtime/audio/browser/playback/storage reachability checks.
9. UTF-8, Markdown-link, ignored-artifact, `git diff --check`, and final scope checks.

## Steve/manual evaluation

No desktop-radio listening test is required because M12C remains isolated from normal radio. Steve instead reviews the blinded artifacts, records the rubric outcome, then inspects the answer key.

Steve should explicitly decide whether each arm is selected, revised/retested, or rejected/deferred. A promising arm does not gain runtime access until a separate M12D contract is prepared and authorized.

## Completion and acceptance

M12C passes only after:

1. implementation remains within this approved contract;
2. all required automated verification passes;
3. Steve reviews the blinded artifacts and answer key;
4. Steve records an explicit outcome for all three arms;
5. Steve explicitly accepts the laboratory candidate; and
6. canonical documentation records the result and selected/rejected formats.

No implementation commit is automatic. M12D, other format experiments, and all runtime integration remain unauthorized even after M12C passes.

## Acceptance record

Steve completed the blinded evaluation on August 26, 2026 and accepted M12C. The deterministic straight handoff remains a fixed no-model reference, not an LLM client. Quick reaction is selected as a promising future candidate, subject to a later bounded real-track/real-session batch before any runtime integration. Session-aware observation is deferred for revision/retest: it should require a meaningful verified session signal and otherwise decline rather than invent silence/static or unsupported music description. The adversarial M12C-08 fixture is not needed for future private-alpha format laboratories; ordinary metadata must still remain data rather than instructions. M12C remains unreachable from normal runtime. M12D and a commit are not authorized by this acceptance.

## What M12C unlocks

Accepted M12C may unlock preparation of a bounded M12D contract only for formats Steve explicitly selects. A revised or rejected arm remains outside runtime. Later host, commercial, dialogue, and imaging batches may reuse the laboratory only under their own bounded contracts.

## Next action after this contract

**Next action type - Decision (Steve)**

M12C is passed. If Steve wants quick reaction to progress, he may separately authorize preparation of a bounded M12D contract; that contract must preserve the selected-format limits and must not treat session-aware observation as selected. M12D implementation and a commit remain unauthorized.
