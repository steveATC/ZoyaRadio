# Milestone 8 contract - control application and private-alpha distribution

> Current checkpoint (August 11, 2026): M8A is manually accepted at `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc`, with its accepted 162-test result. M8B was separately authorized and now has a locally verified `0.8.0-alpha.1` implementation candidate with 178 tests passed and 0 failed, but clean-machine, Steve desktop, and invited-tester evidence remain pending. M8B and M8 as a whole are not passed. The approval-time status language below is retained as historical contract context.

## 1. Status and authority

Steve approved this bounded Milestone 8 contract on August 10, 2026. Milestone 8 is **approved/contracted but unstarted**. This document defines the work that may be authorized within M8; it does not authorize implementation by itself.

M8 is divided into two sequential gates:

- **M8A - Control application** requires its own explicit implementation request.
- **M8B - Private-alpha distribution** requires a later, separate implementation request and may not begin until M8A has been implemented, verified, manually accepted by Steve, and recorded in Git.

The local Git repository, applicable `AGENTS.md` instructions, existing safety and privacy invariants, and the current canonical documents remain controlling. [Research reports](RESEARCH_INDEX.md) are evidence and recommendations only. [POST_M8_INTAKE.md](proposals/POST_M8_INTAKE.md) is unapproved proposal history. Neither is implementation authority or a source of additional M8 scope.

Milestone 7.5B remains the latest accepted implementation. Its accepted evidence remains 124 automated tests passed and 0 failed; that historical suite is not treated as rerun by this documentation change. The intermittent 20:49 observer stall and excessive wrapper-refresh logging remain unresolved or deferred as recorded in [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## 2. Milestone goal

M8 turns the accepted M7.5B prototype into, in sequence:

1. a small, safe Windows control application; and
2. a manually shareable private-alpha Windows x64 ZIP for approximately 2-3 invited testers.

The accepted local-first radio runtime, playback safety, Firefox isolation, safe-silence rules, ephemeral state, and console diagnostics must be preserved. M8 is usability and private-alpha packaging work. It is not a new radio-behavior milestone.

## 3. Gate structure

Work must follow this order:

1. Implement and verify M8A under a separate explicit request.
2. Steve performs the required real-desktop M8A acceptance.
3. Record the accepted M8A checkpoint in Git.
4. Receive Steve's separate authorization for M8B.
5. Implement and verify M8B.
6. Perform clean-machine and invited-tester validation.
7. Steve manually accepts M8 as a whole.
8. Record the accepted M8 checkpoint in Git.
9. Stop implementation and return any future proposals to project-management review.

Automated success alone cannot pass M8A, M8B, or M8 as a whole. No post-M8 proposal is approved by this contract.

## 4. M8A - Control application scope

The approved M8A scope is:

- A small WinForms control application.
- Same-process hosting of the accepted radio runtime.
- One UI-independent runtime controller or facade.
- A single authoritative asynchronous lifecycle and cleanup path.
- Start.
- Safely awaited Stop.
- Silent Mode.
- Clear process-local recent history.
- Current title and artist.
- Concise runtime status.
- Firefox detection and targeting status.
- Open the logs folder.
- Safe window close that visibly waits for Stop and protected cleanup.
- Per-user log storage under an appropriate LocalAppData location.
- Optional persistence only for harmless window geometry, such as size and position.
- Preservation of existing console diagnostics and launchers as diagnostic frontends to the same accepted runtime.
- Automated controller and UI-state tests using fakes, without controlling real desktop media.

The architectural seam should support behavioral equivalents of:

- `StartAsync`
- `StopAsync`
- `SetSilentMode`
- `ClearRecentHistory`
- immutable status snapshots or events
- current title and artist
- Firefox target state
- log-folder location

These exact public names are not required if repository evidence supports clearer names. The behavioral boundary matters more than cosmetic naming.

The controller should receive media observation, ducking, speech, generation, logging, and timing dependencies through testable boundaries. The UI must present deterministic runtime observations; it must not become the authority for factual playback state.

## 5. M8A non-goals

M8A explicitly excludes:

- "Say something now."
- Five Queens content.
- New host scheduling or frequency controls.
- New commentary types, chains, calls, commercials, IDs, rants, or formats.
- New prompts or persona behavior.
- Web research or factual enrichment.
- Localization.
- Persistent listening history or durable memory.
- Cloud AI or cloud TTS.
- Tray or background behavior.
- Browser-page inspection.
- Installer work.
- Packaging or publishing.
- Changes to accepted transition timing or delivery rules.
- Fixing the intermittent 20:49 observer stall unless Steve separately authorizes a repair.
- Wrapper-log cleanup unless separately authorized.
- General refactoring unrelated to the minimum runtime/UI boundary.

## 6. M8A invariants

M8A must preserve all accepted playback and privacy invariants, including:

- User playback intent wins.
- Silence is the safe result when state is missing, stale, ambiguous, late, or unsafe.
- Prepared content has no right to play.
- Normal automatic delivery never pauses, resumes, or toggles playback.
- Exactly one eligible Firefox GSMTC target is required.
- Exactly one eligible Firefox Core Audio application session is required at delivery.
- Chrome and unrelated applications are never fallback targets.
- Endpoint or master volume is never changed.
- The exact captured Firefox application-session scalar and mute state are restored through protected cleanup.
- Silent Mode, cancellation, track or session change, stale opportunity, shutdown, and unsafe targeting invalidate pending work.
- There is no catch-up queue.
- Recent state remains process-local and bounded.
- Listening history, prompts, generated comments, and prepared audio do not become persistent.
- Local metadata and generated content remain local.
- Console diagnostics continue to use the same accepted runtime instead of diverging into a second implementation.

## 7. M8A verification and acceptance

Automated evidence must cover:

- Start and repeated or idempotent Start.
- Stop and repeated or idempotent Stop.
- Stop during generation.
- Stop during neural preparation.
- Stop during simulated target acquisition.
- Stop during simulated ducking.
- Stop during speech.
- Stop during restoration retry.
- Window close awaiting `StopAsync` and protected cleanup.
- Rapid or concurrent control actions.
- Silent Mode invalidating pending work while observation can continue.
- Clear affecting only bounded process-local history.
- Status and metadata updates.
- Firefox target-state presentation.
- Suppression of stale status after shutdown.
- Failure and cancellation paths.
- Log-folder resolution without opening Explorer during automated tests.
- Preservation of the existing regression suite.

Steve's real-desktop M8A acceptance must cover:

- Actual Firefox targeting.
- Start, Stop, Silent Mode, Clear, and window-close behavior.
- Closing during representative pipeline phases.
- Exact Firefox scalar and mute restoration.
- Chrome and Windows master volume remaining untouched.
- No pause, resume, or toggle during normal delivery.
- Continued operation of the existing console diagnostics.
- Readable and useful status wording.

## 8. M8B entry gate

M8B may start only after all of the following are true:

- M8A implementation is complete.
- Relevant automated checks pass.
- Steve manually accepts M8A.
- The accepted M8A result is recorded in Git.
- Steve explicitly authorizes M8B.

Approval of this contract alone does not authorize M8B implementation.

## 9. M8B - Private-alpha distribution scope

The approved primary route is:

- A versioned, unsigned, self-contained Windows x64 portable ZIP.
- Windows 10 22H2 or newer as the private-alpha support baseline.
- Extraction to a user-writable folder.
- The WinForms application and accepted shared runtime.
- The self-contained .NET 8 WindowsDesktop x64 runtime.
- Required Windows SDK and WinRT files.
- The required KokoroSharp managed closure.
- Only required Windows x64 ONNX native assets.
- The Kokoro FP32 model.
- The Nicole voice only.
- Windows local TTS fallback.
- Application-relative asset resolution.
- Logs under LocalAppData.
- Asset hashes and corruption checks.
- A package manifest.
- README and setup instructions.
- Private-alpha terms.
- Required licences, attributions, and third-party notices.
- A SHA-256 digest for the tester ZIP.
- PDB files retained privately but excluded from the tester ZIP.
- Manual side-by-side ZIP replacement and rollback after safe shutdown.
- Clean-machine testing.
- Invited testing by approximately 2-3 people.

Firefox and Ollama remain separate installations. Qwen is obtained separately through Ollama only after explicit disclosure and consent.

## 10. eSpeak distribution gate

The feasibility audit found:

- KokoroSharp currently copies eSpeak NG into build output.
- Engineering evidence indicates that the accepted Nicole/en-US path probably uses MisakiSharp and may not require eSpeak.
- That conclusion is not yet proven through a pruned runtime and clean-machine test.

M8B must resolve this in the following order:

1. Exclude all eSpeak files from the proposed neural package.
2. Verify Nicole/en-US operation with no eSpeak files present.
3. Test representative English, unusual names, out-of-vocabulary input, cancellation, fallback, and clean-machine startup.
4. Distribute the Nicole-enabled build only if that evidence passes.
5. If eSpeak proves necessary, do not casually bundle it or invent a compliance position.
6. Use the Windows-TTS-only fallback package or pause distribution until an explicitly approved compliance solution exists.

This contract does not claim final legal certainty. Licence review here is engineering compliance work, not legal advice.

## 11. External prerequisites and setup policy

The approved policy is:

- Firefox is installed separately.
- Ollama is installed separately from its official source.
- Zoya may open the official Ollama page.
- Zoya must not silently download or install Ollama.
- Zoya must not bundle the Ollama installer.
- Zoya may start an already installed Ollama application only after an explicit tester action.
- Zoya may invoke `ollama pull qwen3:4b-instruct` only after explaining the approximate download size, checking available space, obtaining explicit consent, showing progress, and permitting cancellation.
- Zoya must not automatically accept third-party terms.
- Zoya must not perform firewall, PATH, service, driver, or arbitrary installation repair.
- M8B must validate required Ollama API capabilities instead of treating Steve's observed Ollama 0.32.6 as a proven universal minimum.
- M8B should record model identity or digest where practical instead of relying only on a friendly tag.

The Visual C++ policy is:

- Initially detect the required x64 runtime.
- If it is absent, direct the tester to Microsoft's official redistributable.
- Do not bundle the Visual C++ redistributable until redistribution rights and clean-machine behavior are deliberately resolved.

## 12. Hardware and storage guidance

The following is provisional private-alpha guidance, not a set of false hard requirements:

- 16 GB of RAM is recommended.
- Do not automatically hard-block an 8 GB system solely because of the audit estimate.
- Approximately 10 GB of free disk space should be available before dependency and model setup.
- A supported GPU may improve Ollama latency but is not required.
- Kokoro remains CPU-only under the accepted dependency.
- Slow generation that misses the accepted timing window results in silence. M8 must not weaken timing or playback-safety rules merely to force delivery.

## 13. Windows-TTS fallback route

If compliant neural redistribution cannot be completed promptly, M8B may deliberately select an alternative package with:

- No bundled Kokoro model, Nicole voice, ONNX native closure, or eSpeak.
- Windows local TTS for speech.
- Ollama and Qwen still installed separately if AI generation is retained.
- Clear disclosure that voice quality differs from Nicole.
- No claim that Windows TTS is equivalent to the accepted Nicole experience.
- The same runtime-safety and clean-machine verification requirements.

## 14. M8B non-goals

M8B explicitly excludes:

- A traditional installer.
- A Store package.
- A bootstrapper.
- Fully bundled offline Ollama or Qwen distribution.
- Automatic third-party installation.
- Code signing.
- Public release.
- An automatic updater.
- Tray or background service behavior.
- Cloud services.
- New radio content or behavior.
- Localization.
- Persistent memory.
- Post-M8 proposal implementation.
- A general packaging framework designed for hypothetical future scale.

## 15. M8B clean-machine verification

Verification requires at least:

- A clean Windows 10 22H2 x64 environment.
- A clean Windows 11 x64 environment.
- No Visual Studio or .NET SDK.
- An initial state with no separately installed .NET runtime, Firefox media session, Ollama/Qwen, or extra Visual C++ runtime beyond the clean image.

Verify all of the following:

- ZIP extraction and SHA-256 verification.
- The unsigned SmartScreen and antivirus experience.
- First launch.
- Writable LocalAppData logs.
- Insufficient-disk warning.
- Missing or corrupt application assets.
- eSpeak absence in the neural candidate.
- Firefox absent, installed but idle, active, ambiguous, and stopped.
- Ollama absent, installed but stopped, running, unreachable, and incompatible.
- Qwen absent and present.
- Consent, progress, cancellation, and interrupted model-pull behavior.
- Nicole synthesis and Windows fallback.
- Start, Stop, Silent Mode, Clear, and window close.
- Shutdown during generation, synthesis, ducking, speech, and restoration.
- Exact Firefox scalar and mute restoration.
- Chrome and endpoint or master volume remaining unchanged.
- No automatic playback pause, resume, or toggle.
- No catch-up delivery.
- Offline operation after prerequisites are installed.
- Useful logs that testers can send to Steve without exposing credentials or unrelated private data.
- Side-by-side update and rollback.

## 16. Licence and notice requirements

The final M8B publish output, not estimates, must drive the notice inventory. At minimum, review and represent the applicable terms for:

- Zoya Radio and its private-alpha terms.
- The .NET runtime and WinForms.
- Microsoft Windows SDK and WinRT components.
- KokoroSharp.
- MisakiSharp.
- The Kokoro model and Nicole voice.
- ONNX Runtime and its third-party notices.
- The NAudio closure.
- NumSharp.
- OpenTK.
- Included Microsoft System or compatibility packages.
- Any native runtime requirements.

No package may be described as compliant merely because its top-level package metadata contains a licence identifier. Missing upstream licence or NOTICE material must be obtained and included where required.

## 17. M8 completion conditions

M8 may be marked passed only after:

- M8A is implemented and manually accepted.
- M8B is implemented and verified.
- The selected neural or Windows-TTS distribution route passes its required compliance gate.
- Clean-machine evidence exists.
- Required real-desktop safety evidence passes.
- Steve accepts the final private-alpha package.
- Canonical documentation records the accepted result.
- A known-good Git checkpoint is created.

Invited-tester feedback informs usability and compatibility, but it does not replace Steve's controlled playback-safety acceptance.

## 18. Risks and unresolved items

Contract approval does not resolve these risks:

- Safe extraction of the runtime from console control.
- UI shutdown deadlocks or competing lifecycle paths.
- Repository-relative paths.
- Publish closure and package size, which have not yet been measured.
- Visual C++ native-prerequisite behavior.
- Unsigned SmartScreen and antivirus friction.
- Proof that eSpeak can be excluded.
- Completeness of third-party notices.
- Slow-CPU latency.
- Firefox session ambiguity.
- Ollama and model setup friction.
- The unresolved 20:49 observer stall.
- Excessive wrapper-refresh logging.

No M8A or M8B implementation may silently absorb these items beyond the explicitly authorized gate scope.
