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

## Recorded performance context

- Ollama performance run: 4.262 seconds wall-clock, 2.191 seconds API total duration, 1.999 seconds model load, 2.68 GiB loaded size/VRAM reported by Ollama, and context length 2048. Selected-model unload verification passed.
- These are measurements from Steve's run, not estimates or guaranteed future performance.
- The two-second speech-delivery window begins only after preparation is complete; AI generation and Nicole synthesis occur earlier while music plays.
