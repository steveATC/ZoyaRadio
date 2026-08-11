# Zoya Radio 0.8.0-alpha.1 clean-machine checklist

Record evidence separately for **Windows 10 22H2 x64 (build 19045 or newer)** and **Windows 11 x64**. Do not mark Milestone 8B passed from local developer-machine evidence.

Use a clean standard-user machine with no Visual Studio, .NET SDK, separately installed .NET runtime, Firefox media session, Ollama/Qwen, or added Visual C++ runtime beyond the clean image.

## Package and baseline

- [ ] Record exact Windows edition/version/build and x64 architecture.
- [ ] Record ZIP filename and SHA-256; verify it against the supplied digest.
- [ ] Extract to a new user-writable folder; do not run inside the ZIP.
- [ ] Confirm no administrator prompt is required.
- [ ] Run `ZoyaRadio.ControlApp.exe --package-self-test` from a different working directory and retain the report from `%LOCALAPPDATA%\Zoya Radio\logs`.
- [ ] Launch the GUI and run **Check Readiness** before installing prerequisites.
- [ ] Record package, disk, Firefox, Ollama, Qwen, Visual C++, and log-location states.

## Prerequisites and setup

- [ ] Confirm absent Firefox is explained without a crash.
- [ ] If Visual C++ x64 is missing, confirm Zoya does not install it; use only Microsoft's official x64 redistributable link from `TESTER-README.txt`, then recheck readiness.
- [ ] Install Firefox separately through its normal official process, then confirm installed-idle is not treated as permanent failure.
- [ ] Confirm the Ollama page opens only after clicking its labelled button.
- [ ] Install/start Ollama separately; record version and readiness state.
- [ ] With Qwen absent, confirm the app shows size/terms/disk disclosure and defaults to **No**.
- [ ] Choose No; confirm no pull begins.
- [ ] If fewer than 8 GiB are available, confirm the pull is refused without cleanup/deletion.
- [ ] With sufficient space, explicitly consent; record progress, cancellation, interrupted-pull recovery, and eventual qwen3:4b-instruct identity/digest.

## Neural and fallback route

- [ ] Run `ZoyaRadio.ControlApp.exe --neural-package-self-test`; confirm all package speech samples are prepared without audible speech or media control.
- [ ] Confirm ordinary English, punctuation/numbers, and repeated preparation use Nicole; unusual/OOV-looking names may use the documented Windows fallback before delivery.
- [ ] Confirm pre-cancellation and the separate deliberate Windows-TTS fallback preparation pass.
- [ ] Confirm the package and loaded-module audit report no eSpeak component.

## Real desktop acceptance

- [ ] Start YouTube Music playing in Firefox; confirm exactly one eligible Firefox target.
- [ ] Exercise Start, Stop, Silent Mode, Clear History, Open Logs Folder, and window close.
- [ ] Observe multiple transitions and at least two deliveries.
- [ ] Confirm preparation occurs while music plays; normal delivery never pauses playback.
- [ ] Confirm only Firefox is proportionally ducked and exact volume/mute state is restored.
- [ ] Add Chrome/unrelated media if convenient; confirm isolation and harmless Firefox-wrapper refresh continuity.
- [ ] Exercise cancellation during generation, Nicole preparation, ducking, and speech when practical.
- [ ] Confirm expected safe silence for missing, ambiguous, stale, late, or unsafe targets.
- [ ] Review logs for Unicode preservation and private-data boundaries.

## Update, rollback, removal

- [ ] Stop the old copy, extract a replacement side-by-side, and validate it before deleting the old folder.
- [ ] Roll back by launching the preserved old folder.
- [ ] Delete the extracted application folder; separately delete LocalAppData logs if desired.

Attach the completed evidence template, relevant reviewed logs, screenshots of readiness states, and exact observations. Do not claim unperformed checks.
