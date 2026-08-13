# Zoya Radio roadmap

This document records accepted milestone status and approved direction. It does not turn research recommendations, brainstorming, or a milestone number into implementation scope.

## Milestone status

| Milestone | Status | Accepted result |
| --- | --- | --- |
| 0 - Windows media observation | Passed | Firefox `firefox.exe` metadata, playback state, and deduplicated transitions verified. |
| 1 - Targeted playback control | Passed | Exact-session pause/resume, refusal, ambiguity, isolation, and recovery verified. |
| 2 - Basic spoken announcement | Passed | Local Windows speech and protected playback recovery verified. |
| 3 - Local AI comment generation | Passed | Loopback Ollama, grounding contract, validation/fallback, no-AI, timing, and unload verified after correction. |
| 4 - Host persona | Passed | Zoya/Steve persona accepted while preserving title/artist factual grounding. |
| 5 - Radio behavior and minimal listening state | Passed | Inclusive 2-5 transition scheduling, bounded in-memory context, Silent Mode, and stale-work safety verified. |
| 6 | Deferred | Deliberately deferred; no implementation or approved scope is recorded here. |
| 7 - Better local voice | Passed | Kokoro `af_nicole` accepted as primary CPU voice; `af_heart` runner-up; Windows speech retained as fallback. |
| 7.5A - Isolated Firefox ducking | Passed | Proportional application-session ducking and exact restoration verified without affecting Chrome or master volume. |
| 7.5B - Transition-aware delivery | Passed | One-transition-ahead preparation, ducked delivery over playing music, wrapper-churn continuity, Chrome isolation, and clean restoration accepted on August 10, 2026. |
| 8A - Control application | Passed | Shared controller/runtime, WinForms frontend, read-only activity status, LocalAppData logs, safe controls, and protected shutdown accepted on August 11, 2026. |
| 8B - Private-alpha distribution | Passed - internal-development gate | Version `0.8.0-alpha.1`, package integrity/neural self-tests, and Steve's packaged-desktop behavior accepted; clean-machine and invited-tester distribution validation deferred. |
| 9A - Structured host commentary experiment | Implemented candidate - awaiting qualitative evaluation | Typed deterministic context, separate candidate prompt, announcement candidates, and a blinded generate-only benchmark; normal runtime unchanged. |

Milestone 8 is closed for continued internal development after M8A and M8B owner acceptance. Clean Windows 10/11 and invited-tester evidence remain deferred compatibility/distribution gates and do not certify this package for broad or public distribution.

The latest accepted internal implementation snapshot is commit `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf`, with 178 automated tests passed and 0 failed. Later documentation-only commits do not replace that implementation snapshot or make the historical test result a new run.

## Milestone 7.5B repair provenance

- `e0bb294` introduced transition-aware ducked delivery.
- `cdca733` repaired Firefox GSMTC/Core Audio recovery, and `4870cd8` preserved logical work across harmless GSMTC wrapper churn.
- A power outage left only `tests/ZoyaRadio.MediaProbe.Tests/Program.cs` as an all-zero file. That file alone was restored exactly from `e0bb294`; six surviving repair files were preserved, focused tests were recreated, and repository integrity was rechecked.
- `63662342` recorded Steve's accepted Firefox-plus-Chrome evidence. The final automated suite passed 124/124.

## Milestone 8A provenance

- `f123ddd9de484de590ba596abf812314b265390c` implemented the original control-application candidate.
- `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc` added the deterministic Radio-activity presentation and is the accepted M8A implementation snapshot.
- Steve accepted M8A on August 11, 2026 after the 162-test suite, clean Release builds, and renewed real-desktop testing.

## Milestone 8B provenance

- `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf` implemented the self-contained `0.8.0-alpha.1` package, integrity/readiness/setup behavior, safe self-tests, distribution audit, and 178-test result.
- Steve accepted the extracted package on August 11, 2026 after both LocalAppData self-test reports passed and the requested packaged desktop behavior worked normally.
- Steve amended project sequencing so clean Windows 10/11 and invited-tester validation remain deferred distribution evidence rather than blockers to internal development.
- The package remains an unsigned private-alpha candidate, not a clean-machine-certified, broadly compatible, public, or third-party-distribution release.

## Completed capability arc

- **Observation:** read-only Windows media-session inventory, metadata, state changes, and deduplicated track transitions.
- **Explicit safety diagnostics:** exact Firefox selection, confirmation, targeted playback control, local speech, simulated failure, and cancellation recovery.
- **Local generation:** loopback-only Ollama comments constrained to trusted title/artist metadata with deterministic fallback and prompt-level grounding rules.
- **Host delivery:** accepted Zoya persona, Nicole CPU speech, and Windows speech fallback.
- **Automatic behavior:** bounded ephemeral history, inclusive 2-5 transition scheduling, Silent Mode, stale-work abandonment, and no catch-up queue.
- **Transition presentation:** one-transition-ahead generation/synthesis, strict delivery revalidation, Firefox-only proportional ducking, exact restoration, and safe continuity through harmless GSMTC wrapper churn.
- **Control application:** same-process WinForms frontend, UI-independent controller, one authoritative lifecycle, deterministic read-only activity/status, LocalAppData logs, and preserved console diagnostics.

## Accepted manual evidence summary

- Milestone 0 observed correct title/artist and Playing/Paused state across at least three transitions, treated album as optional, deduplicated repeated events, and remained read only.
- Milestone 1 passed at least five targeted Firefox pause/resume runs, refused default-NO and starting-Paused cases, recovered after Ctrl+C, and ignored a simultaneous Chrome media session.
- Milestone 2 passed three fully audible announcements plus simulated-speech-failure and Ctrl+C recovery; Unicode remained intact.
- Milestone 3 passed local generation, corrected literal-metadata grounding, timeout/fallback, no-AI, performance measurement, and selected-model unload.
- Milestone 4 accepted Zoya's warm, dry, conversational persona while retaining the title/artist-only factual boundary.
- Milestone 5 accepted automatic frequency, minimum-age/stale-track safety, Silent Mode, bounded in-memory state, and clean no-AI/shutdown behavior.
- Milestone 7 accepted Nicole as the primary local voice and Windows TTS as fallback; Cyrillic sounded natural to Steve, but Russian accuracy was not claimed.
- Milestone 7.5A verified 50.0% -> 12.5% -> 50.0% proportional restoration, normal/Ctrl+C/simulated-failure cleanup, a natural ramp, and Chrome/master-volume isolation.
- Milestone 7.5B verified two prepared deliveries through Chrome-driven GSMTC churn, exact 100% -> 25% -> 100% restoration, continued transition observation, Chrome isolation, and clean shutdown.
- Milestone 8A accepted the WinForms control application, readable Radio activity, safe Stop/window-close cleanup, Firefox-only duck/restoration, Chrome/master isolation, target refusal, LocalAppData logs, and preserved console diagnostics.

Milestone-specific evidence and preserved regression instructions remain in the existing `MILESTONE_*_TEST.md` documents.

## Approved near-term direction

Steve approved the bounded [Milestone 8 contract](MILESTONE_8_CONTRACT.md) on August 10, 2026. Its historical approval-time sequence used two authorization gates:

1. **M8A - Control application:** a small WinForms frontend, same-process hosting of the accepted runtime, a UI-independent controller, one authoritative asynchronous lifecycle, safe controls and status, LocalAppData logs, preserved console diagnostics, and fake-driven controller/UI-state tests.
2. **M8B - Private-alpha distribution:** a versioned unsigned self-contained Windows x64 portable ZIP for approximately 2-3 invited testers, subject to M8A acceptance, a separate authorization, eSpeak exclusion/compliance evidence, clean-machine testing, and final manual acceptance.

Steve accepted M8A, separately authorized M8B, and accepted the extracted M8B package on August 11, 2026. His dated contract amendment closes Milestone 8 for continued internal development while deferring clean-machine and invited-tester distribution validation.

- Steve's post-M8 review approved the bounded [Milestone 9A contract](MILESTONE_9A_CONTRACT.md) for a structured-commentary experiment only.
- M9A is implemented as an isolated candidate and awaits Steve's blinded qualitative evaluation; it is not passed and is not accepted radio behavior.
- M9B runtime integration is not authorized. It would require a separate bounded contract after M9A evidence is reviewed.
- Research remains evidence only and the post-M8 intake remains unapproved proposal history except for the exact scope Steve promoted into the M9A contract.
- Milestone 6 remains deferred unless Steve separately promotes it.

## Deferred and future boundaries

- Milestone 6 remains deliberately deferred without an approved implementation scope in the repository.
- Known limitations and possible quality improvements are tracked in [KNOWN_ISSUES.md](KNOWN_ISSUES.md), not silently promoted into milestone work.
- External production analyses and repository research are indexed in [RESEARCH_INDEX.md](RESEARCH_INDEX.md). Their recommendations require an explicit decision before entering this roadmap.
- Product-vision discussions outside Git remain brainstorming unless Steve promotes them into an approved milestone.

## Milestone gate policy

1. Work on one explicitly requested milestone or repair at a time.
2. Preserve all previously accepted behavior and regression coverage.
3. Complete safe automated verification before asking for desktop acceptance.
4. Mark a milestone passed only after Steve supplies its required real-desktop evidence.
5. Record the accepted result in Git before advancing to another milestone.
