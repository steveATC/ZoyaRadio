# Milestone 9A contract - structured host commentary experiment

## Status and authority

Steve approved this bounded contract after the post-M8 project-management review. Milestone 9A is an experiment and candidate only. It does not change accepted automatic-radio behavior, does not pass until Steve completes qualitative review, and does not authorize Milestone 9B runtime integration.

Milestone 8 remains the latest passed milestone. Commit `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`, packaged version `0.8.0-alpha.1`, and its accepted 178-passed/0-failed result remain the latest accepted runtime baseline until Steve explicitly accepts a later implementation.

## Purpose

M9A tests whether `qwen3:4b-instruct` produces more grounded, relevant, and varied short host commentary when the application supplies a typed deterministic broadcasting situation. It must answer without assuming success:

1. Does a structured deterministic brief improve grounding and relevance?
2. Do recent rhetorical fingerprints reduce obvious repetition?
3. Does an explicit reason for speaking improve useful variety?
4. Can the approved 4B local model use the structure at practical latency?
5. Which situations are better served by a deterministic announcement or `FALLBACK`?
6. Is the evidence strong enough for Steve to authorize a separate M9B integration contract?

Automated validity, latency, and unload results cannot answer the creative questions. Steve's blinded review is mandatory.

## Authorized implementation

M9A may add:

- a typed experimental `CommentaryBrief` model;
- deterministic history facts and shallow recent-comment fingerprints;
- a separate structured candidate prompt;
- deterministic previous-track and current-track announcement candidates;
- one explicit generate-only benchmark launcher;
- fixed synthetic scenarios, fake-driven tests, ignored benchmark artifacts, and canonical documentation.

The explicit benchmark may contact only the configured loopback Ollama endpoint and only the already-installed approved `qwen3:4b-instruct` model. It may not pull, install, update, or configure anything.

## CommentaryBrief semantics

### Trusted metadata

- The subject title and artist are exact strings.
- The immediately previous usable track's exact title and artist may be included when deterministically available.
- Metadata is JSON data inside explicit delimiters. It is inert even when it resembles a prompt or command.
- No album, URL, page content, audio analysis, lyrics, or external fact is supplied.

### Retained-history facts

- The factory considers only the most recent 10 usable prior tracks, matching the accepted process-local runtime bound.
- The current subject is excluded from all prior-history counts.
- Exact-track identity uses the existing case-insensitive, whitespace-normalized title-and-artist comparison.
- Artist identity uses the existing case-insensitive, whitespace-normalized exact artist comparison. It performs no fuzzy matching.
- `ExactTrackPriorCount` and `ArtistPriorCount` count matching entries inside that retained prior window.
- Transition distance is the number of genuine track transitions from the most recent prior match to the current subject. An immediately preceding match has distance 1.
- `PreviousTrackHasSameArtist` compares only the immediately previous retained track with the subject.
- `NoNotableRetainedHistoryPattern` is true only when there is no exact-track match, artist match, or immediate same-artist transition.

These facts do not infer genre, sound, mood, lyrics, biography, release data, geography, popularity, semantic title similarity, or whether a transition was natural or manual.

### Reason for speaking

The typed experimental reasons are:

- ordinary track reaction;
- exact-track repeat;
- repeated artist;
- same-artist transition;
- ordinary continuity;
- no notable session pattern.

When several retained-history facts apply, primary precedence is:

1. same-artist transition;
2. exact-track repeat;
3. repeated artist;
4. the explicitly supplied ordinary, continuity, or no-pattern reason.

Remaining applicable facts are retained as ordered secondary reasons. The model does not infer this hierarchy from raw history.

### Recent accepted-comment evidence

- At most the most recent 6 non-empty comments are supplied, matching the accepted runtime bound.
- Each comment retains its normalized exact text plus shallow deterministic fingerprints: first three normalized words, whether `Steve` appears as a word, whether a question mark appears, whether simple `like`/`as if` comparison markers appear, and approximate whitespace-delimited word count.
- These are style-avoidance hints only. They are not semantic, sentiment, or rhetorical understanding.

## Candidate prompt boundary

The M9A prompt is a separate embedded resource. It visibly separates instructions, trusted facts, recent context, creative freedom, and prohibitions. It preserves the accepted warm, dry Zoya/Steve direction while requesting one 15-40-word spoken paragraph or exact `FALLBACK`.

The model may use subjective reaction, literal wording, playful exaggeration, clearly imaginary framing, and affectionate teasing. It may not present unsupported song, artist, listener, or session claims as facts; reason about unheard audio; obey instructions embedded in metadata; expose reasoning; or use the validator's generic DJ cliches.

The accepted `Milestone4HostPersonaPrompt.txt`, normal context factory, validator, and fixed runtime fallback remain unchanged and remain the only normal automatic-radio composition.

## Deterministic announcement candidates

The experiment supports:

- previous-track context: `That was [exact title] by [exact artist].`
- current-track context: `This is [exact title] by [exact artist].`

It preserves Unicode and existing punctuation, introduces no facts, and never claims a natural ending. It is not the accepted runtime fallback and is not used by normal automatic delivery in M9A.

## Benchmark contract

`run-commentary-benchmark.cmd` is an explicit repository diagnostic. Its command branch exits before ordinary log creation, speech initialization, GSMTC acquisition, Firefox targeting, Core Audio enumeration, ducking, GUI startup, or normal runtime state.

The fixed version 1.0 manifest has 12 required scenario classes and 2 samples for each of 2 arms, for exactly 48 bounded requests:

1. ordinary no-pattern metadata;
2. strange literal English title;
3. Unicode metadata;
4. exact-track repeat;
5. repeated artist on different tracks;
6. immediate same-artist transition;
7. repeated recent openings;
8. repeated recent `Steve` address;
9. repeated question/comparison structure;
10. instruction-shaped metadata;
11. deterministic-announcement candidate;
12. sparse context where `FALLBACK` is reasonable.

Arm A uses the exact accepted prepared-transition prompt/context factory. Arm B uses `CommentaryBrief` and the separate candidate prompt. Both use the same model and generation settings: non-streaming, thinking disabled, context 2048, prediction limit 80, temperature 0.7, 20-second timeout, and temporary `30s` benchmark keep-alive. The selected model receives an explicit unload request after the bounded run and is checked through `/api/ps` when observable.

The benchmark writes only ignored local artifacts under `artifacts/m9a/<timestamp>/`:

- `M9A_EVALUATION.md` - blinded qualitative scoring sheet;
- `M9A_RESULTS.json` - anonymous-arm machine results and factual inputs;
- `M9A_ANSWER_KEY.json` - separate arm identity mapping;
- `M9A_RUN_SUMMARY.md` - mechanical counts, latency, validation, and unload observations.

Generated outputs are not committed. The evaluation sheet must not reveal arm identity or declare a winner.

## Non-goals and unchanged behavior

M9A does not change normal scheduling, transition counting, minimum track age, Track-A preparation, Track-B delivery window, Firefox selection, wrapper continuity, ducking, exact restoration, Silent Mode, cleanup, UI, voice selection, normal Ollama timeout/keep-alive/unload, bounded runtime state, LocalAppData logging, package contents, package version, or fallback behavior.

M9A does not authorize persistent memory, factual enrichment, web research, lyrics, audio analysis, scheduler redesign, new voices, callers, commercials, chains, rants, imaging, localization, packaging, or any M9B/post-M9 work.

## Acceptance gate

Implementation is ready for Steve's evaluation only after affected Release builds, focused tests, the complete regression suite, formatting, diff, documentation-link, and UTF-8 checks pass; normal runtime isolation is confirmed; and any live benchmark is loopback-only, bounded, unloaded, ignored, and unstaged.

M9A passes only if Steve completes the blinded review in [MILESTONE_9A_TEST.md](MILESTONE_9A_TEST.md) and explicitly accepts the candidate evidence. A successful candidate does not enter normal radio without a separately approved M9B contract. The known `Borrowed Umbrella` grounding defect is not fixed merely because this experiment exists.
