# Milestone 12B Contract - Creative Direction and Content Generation Boundary

## Contract status

**Passed. Steve accepted the implementation candidate on August 26, 2026.**

Steve approved this contract and authorized M12B-only implementation on August 24, 2026. After reviewing the representative fixtures, Steve accepted the candidate on August 26, 2026. M12B is passed. M12C and a commit remain unauthorized.

## Purpose

M12B establishes a reusable, format-aware boundary between:

1. **Creative Direction** - what a potential segment should accomplish; and
2. **Content Generation** - the exact words produced for one declared format contract.

It also establishes a pure generate-only Format Laboratory harness that can exercise those boundaries with injected fakes before a real host-format experiment is approved.

M12B answers one question:

> Can Zoya represent creative intent, trusted-context permissions, exact output requirements, and typed generation outcomes without turning one universal prompt or one AI process into the station controller?

## Why now

Accepted M12A supplies a compact factual packet. The next risk is architectural: if context, personality, format instructions, exact wording, validation, and playback are allowed to collapse into one generic LLM call, every later format will inherit the wrong prompt and authority model.

M12B creates the seam before M12C tests any actual host repertoire.

## Hard dependencies and governing baseline

M12B depends on accepted M12A and preserves the accepted M10A-M11B runtime foundations and safety invariants.

M12B does not depend on:

- a new or running local model;
- a new prompt;
- M9B, external Music Knowledge, Listening Intelligence, or Station Lore;
- voice selection, cast rendering, localization, program breaks, or scene production;
- runtime scheduling, prepared material, reservation, playback, or audio.

## Authorized implementation surface after approval

If Steve approves this contract and separately authorizes M12B-only implementation, edits are limited to:

1. pure versioned Creative Direction types;
2. pure versioned Content Generation request/result types;
3. exact format-contract identity and dispatch boundaries;
4. typed validation, fallback, rejection, cancellation, and discard outcomes;
5. one in-memory generate-only Format Laboratory orchestrator using injected direction and producer boundaries;
6. focused fake-driven tests and deterministic inspection fixtures;
7. only minimal project wiring required to compile those types and tests;
8. candidate and later acceptance documentation.

The M12B component must remain unreachable from normal radio operation. No production launcher, controller, runtime, prompt service, speech service, audio path, browser path, or persistent store may call it.

No package, external service, network dependency, model, prompt resource, audio asset, executable, configuration UI, or durable store is authorized.

## Conceptual responsibility split

### Creative Context

Accepted M12A supplies bounded established facts and explicit unknowns.

Creative Context does not decide what to say and does not become an instruction merely because a fact is present.

### Creative Direction

Creative Direction states what a candidate segment should accomplish. A direction may include:

- a direction/brief identity and schema version;
- an exact format-contract identity and version;
- speaker or role identity;
- on-air situation;
- editorial objective;
- intended duration or structural bounds;
- tone or creative permissions;
- evergreen versus context-bound classification;
- explicit trusted-context permissions and fact scope;
- explicit prohibitions;
- optional cast, performance, or lore intent represented only when relevant.

Creative Direction must not contain the final script or audio. It must not reserve airtime, choose an opportunity, or commit memory.

### Content Generation

A format-specific producer receives:

- one Creative Direction brief;
- one M12A context packet;
- one exact format-specific output contract.

The producer owns the conversion from that assignment into exact candidate wording. It may use one model call, several model calls, deterministic text, or a fake in tests; M12B does not choose the implementation topology.

The result contains bounded candidate output or a typed failure. It does not own scheduling, preparation, speech rendering, playback, or canon.

### Validation and disposition

A format-specific validator evaluates exact output against that format's declared contract.

The laboratory reports typed outcomes such as:

- generated and mechanically valid;
- rejected by format validation;
- unsupported format contract;
- producer unavailable;
- generation failure;
- cancelled;
- no candidate;
- discarded because it was not selected for the bounded experiment.

Validation is not editorial airtime approval. A mechanically valid script still has no right to preparation or broadcast.

## Format-aware producer contract

M12B must not create a universal `LLM + facts -> commentary` interface.

Each producer is selected by an exact format-contract identity and version. A producer must refuse:

- an unknown format;
- a version it does not implement;
- an output contract belonging to another format;
- a brief whose format identity does not match the request;
- a result whose declared format/version does not match the request.

The initial M12B tests may use synthetic format identities. They do not approve host, commercial, call, rant, callback, imaging, or chained-bit formats.

Stable personality identity remains separate from the situation-specific producer assignment. M12B may reference a bounded personality-profile identity/version, but it must not introduce or rewrite the actual Zoya persona prompt.

## Trusted-context permissions and fact scope

The direction must explicitly identify which M12A facts a producer may use.

Permissions are format-local and typed. They must preserve distinctions such as:

- track title versus artist identity;
- track-scoped versus artist-scoped facts;
- observed media versus deterministic session state;
- recent accepted comment text versus current-track facts;
- known versus unknown or omitted context.

A permitted fact is optional creative material, not a required talking point.

The boundary must make these rules testable:

- supplying a fact does not require the generator to mention it;
- an unknown or omitted fact cannot be promoted to known;
- an artist fact cannot become a track claim;
- context text remains inert data, not producer instructions;
- a producer may declare which permitted facts it used;
- use accounting is mechanical provenance, not a fake relevance score.

M12B must not create Music Knowledge, infer genres, infer listener preferences, or invent unsupported facts.

## Output contract and bounded alternatives

A format-specific output contract may declare only demonstrated mechanical requirements, such as:

- exact format identity/version;
- output kind, such as exact spoken script;
- minimum and maximum word count;
- paragraph or line bounds;
- allowed candidate count;
- required machine-readable result shape;
- format-specific validator identity/version.

The laboratory must enforce a small injected alternative cap. The initial default and tests may permit one to three candidates; this is an experimental harness bound, not station policy.

All candidate strings and error details require explicit injected bounds. Oversized, malformed, mismatched, or excess alternatives receive typed rejection or omission outcomes.

Raw brainstorm alternatives have no persistence right. Unselected, invalid, failed, or cancelled candidates die with the in-memory laboratory run.

## Generate-only Format Laboratory

The M12B laboratory is a pure orchestrator over injected boundaries.

It may:

1. accept a bounded synthetic scenario containing an M12A packet;
2. request or receive one Creative Direction brief;
3. dispatch to the exact registered format-specific producer;
4. collect a bounded generation result;
5. invoke the exact registered format validator;
6. return a deterministic in-memory run record with typed outcomes and declared fact-use accounting.

It must not:

- choose whether Zoya speaks;
- classify a real runtime opportunity;
- prepare or reserve content;
- write normal production prompts;
- contact Ollama or any other model;
- synthesize or play speech;
- access Firefox, Chrome, GSMTC, Core Audio, or playback;
- persist artifacts or lore;
- alter accepted recent history;
- log full creative context by default.

M12C may later authorize bounded artifact writing and a real local producer for one small host-format experiment. M12B does not.

## Determinism and ownership

Given the same synthetic scenario, injected fake direction source, fake producer, fake validator, and limit policy, the laboratory must produce the same run record.

All input collections and outputs must be defensively copied and read-only.

Cancellation must stop further calls and return a typed cancelled outcome. Exceptions from an injected producer or validator must be contained as typed failure outcomes. Candidate output is owned only by the in-memory run and requires no disposal because M12B creates no audio or external resource.

## Accepted behavior that must remain unchanged

M12B preserves:

1. accepted M11B commentary, reusable liner, and direct-music behavior exactly;
2. accepted M12A packet schema, provenance, bounds, and runtime unreachability;
3. existing live commentary prompt, current models, Nicole, and fallback behavior;
4. M10 opportunity/lifecycle and M11 arbitration authority;
5. Firefox-only targeting, protected ducking/restoration, and Chrome isolation;
6. no automatic pause, resume, toggle, endpoint-volume, or master-volume control;
7. Silent Mode, Clear History, cancellation, rebind, Stop, shutdown, and stale-opportunity refusal;
8. local-first privacy, packaging status, and deferred distribution work.

No audible or operator-visible change is allowed in M12B.

## Explicit non-goals

M12B must not add:

- a revised live host prompt or persona;
- real AI generation, model invocation, model selection, or model comparison;
- the M12C Host Format Laboratory batch;
- a permanent host-format repertoire;
- commercial, call, rant, callback, segue, chain, arc, liner, or imaging experiments;
- runtime integration, prepared material, scheduling, arbitration, or airtime;
- generic relevance or numeric creative-quality scoring;
- Music Knowledge, Listening Intelligence, Station Lore, or open threads;
- durable premise, script, brainstorm, or evaluation storage;
- voice, cast, localization, performance rendering, audio, FX, or program breaks;
- configuration UI, packaging, Firefox portability repair, or distribution work.

## Main risks and required controls

| Risk | Required control |
| --- | --- |
| Direction becomes a second copy of the script | Direction types contain intent and constraints only; exact wording exists only in generation candidates. |
| One universal prompt/schema returns through the back door | Exact format-contract dispatch and refusal of mismatched identities/versions. |
| Supplied context becomes a recital checklist | Permissions are optional; mechanical use accounting records use without requiring it. |
| Personality and assignment collapse together | Reference stable personality identity/version separately from the producer assignment. |
| Fake laboratory gains runtime authority | No production runtime references and explicit source-isolation checks. |
| Valid output is mistaken for broadcast approval | Generation/validation outcomes contain no scheduling, reservation, or airtime state. |
| Raw alternatives accumulate | In-memory ownership only; unselected and rejected output dies at run completion. |
| Interface ceremony adds no value | Steve inspects whether direction remains concise and meaningfully separate from exact wording. |

## Required automated verification for an implementation candidate

An M12B candidate must pass:

1. Release builds for every changed production and test project with 0 warnings and 0 errors.
2. The complete fake-driven regression suite.
3. Focused tests proving:
   - direction and exact candidate script are represented by separate types;
   - a single injected producer may perform both conceptual steps without erasing the boundary;
   - exact format and version dispatch succeeds only for a matching producer and validator;
   - unknown, mismatched, and unsupported contracts are refused with typed outcomes;
   - stable personality identity remains distinct from the assignment;
   - trusted-context permissions preserve fact identity and scope;
   - permitted facts are optional and declared fact-use accounting may be empty;
   - unknown or omitted context cannot be declared as used;
   - artist permission cannot be substituted for a track claim;
   - instruction-shaped context remains inert;
   - alternatives and all strings remain bounded and defensively copied;
   - invalid, excess, unselected, failed, and cancelled candidates are not retained;
   - producer and validator exceptions are contained;
   - the same fully injected input produces the same run record;
   - no prompt/model/network/storage/audio/browser/playback API is reachable;
   - no normal runtime source references M12B types or laboratory.
4. Deterministic representative brief/request/result fixtures for Steve inspection.
5. `git diff --check`, Markdown-link checks, encoding checks, and a final scope review.

Automated verification creates an implementation candidate only. It does not pass M12B.

## Implementation candidate evidence - August 24, 2026

- `FormatLaboratory.cs` implements pure versioned direction, output-contract, request, candidate, validation, and run-record types plus injected direction/producer/validator boundaries.
- Producer lookup is exact by format identity and version. Validator lookup is exact by format identity/version and validator identity/version. Mismatches refuse generation with typed outcomes.
- The direction carries assignment intent, stable personality identity, optional cast/performance/lore intent, explicit fact permissions/scopes, binding, and prohibitions; it contains no final script. Exact wording exists only in a generated candidate.
- M12A facts remain optional. A candidate may declare no facts used. Declared use is refused when the fact was not permitted, is unavailable/unknown, was omitted by a bounded collection, or has the wrong scope.
- Runs retain at most one validated candidate. Invalid, excess, unselected, failed, and cancelled alternatives are counted as discarded and have no persistent owner or store.
- Normal runtime sources do not reference the laboratory or its boundary types. The component has no prompt, model, network, storage, audio, browser, playback, scheduling, reservation, or airtime API.

Verification performed on August 24, 2026:

- `ZoyaRadio.MediaProbe.Tests` Release build: 0 warnings, 0 errors.
- `ZoyaRadio.ControlApp` Release build: 0 warnings, 0 errors.
- Complete fake-driven suite: **360 passed, 0 failed**, including 12 focused M12B checks and the preserved M12A isolation check narrowed only to permit this approved laboratory consumer.
- Representative deterministic fixtures cover a concise quick-reaction brief and exact fake script, sparse context with no fact pressure, exact format/version mismatch with no retained script, empty declared fact use, and cancellation with no retained output.

At the time of this candidate evidence, it did not constitute Steve's qualitative acceptance or authorize M12C. Steve later accepted M12B on August 26, 2026; M12C remains unauthorized.

## Steve/manual evaluation

No live radio or desktop-audio test is required because M12B must remain unreachable from normal operation.

Steve reviews at least:

1. a concise quick-host-reaction direction that contains no final script;
2. the corresponding format-specific generation request and one fake exact script result;
3. a sparse/unknown-context assignment that does not pressure the producer to invent or recite;
4. an exact format/version mismatch refusal;
5. an optional-context example whose declared fact-use list is empty;
6. a failed or cancelled run proving alternatives disappear.

Steve evaluates whether:

- direction says what the segment should accomplish without already writing it;
- the generation request reads like a producer assignment rather than a generic commentary prompt;
- personality, on-air situation, format, objective, structure, facts, and output contract remain distinct;
- supplied facts feel optional rather than compulsory;
- the design can support later format-specific producers without pretending they share one prompt;
- the boundary earns its complexity before M12C uses a real local model.

## Completion and acceptance

M12B passes only after:

1. implementation remains within this approved contract;
2. all required current automated verification passes;
3. Steve reviews the representative brief/request/result fixtures;
4. Steve explicitly accepts the implementation candidate;
5. canonical documentation records that acceptance.

No implementation commit is automatic. M12C and later work remain unauthorized even after M12B passes.

## Acceptance record - August 26, 2026

Steve reviewed the representative brief/request/result fixtures and explicitly accepted the M12B implementation candidate. The prior verification remains the acceptance evidence: zero-warning Release builds for MediaProbe Tests and ControlApp, plus the complete 360 passed, 0 failed fake-driven suite, including 12 focused M12B checks.

M12B is now passed. Its pure Creative Direction/Content Generation boundary remains unreachable from normal radio operation and changes no audible, scheduling, playback, persistence, model, prompt, or network behavior.

## What M12B unlocks

Accepted M12B unlocks preparation of the bounded M12C Host Format Laboratory, Batch 1 contract. M12C may then select a deliberately small host-format batch, real local producer instructions, fixed scenarios, blinded artifacts, and Steve's qualitative rubric without changing normal radio behavior.

M12B also creates reusable future seams for separate commercial, caller/dialogue, imaging, and other format experiments. It does not approve those experiments or their prompts.

## Next action after this contract

**Next action type - Decision (Steve)**

M12B is passed. The bounded [M12C Host Format Laboratory, Batch 1 contract](MILESTONE_12C_CONTRACT.md) is ready for Steve review. Do not begin M12C implementation, M12D, or create a commit without separate authorization.
