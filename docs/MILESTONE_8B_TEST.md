# Milestone 8B private-alpha candidate test

## Status

Milestone 8B has an implementation candidate, version `0.8.0-alpha.1`. It is **not passed or accepted**. Milestone 8 as a whole is also not passed.

Local build, fake-driven automation, package audit, safe extracted-package checks, and offline Nicole preparation are engineering evidence. They do not replace the required clean Windows 10, clean Windows 11, Steve desktop, and invited-tester evidence.

The latest accepted implementation remains M8A commit `41b7824201c5c163a0fe0752bd4b3e70c68b9bdc`. The M8A acceptance-documentation checkpoint remains `dfe32e56455876804a87ee9fcc85b01776863581`.

## Candidate files

After running the repository packaging script, use:

- ZIP: `artifacts\private-alpha\0.8.0-alpha.1\ZoyaRadio-0.8.0-alpha.1-win-x64.zip`
- SHA-256: `artifacts\private-alpha\0.8.0-alpha.1\ZoyaRadio-0.8.0-alpha.1-win-x64.zip.sha256.txt`
- private symbols: `artifacts\private-alpha\0.8.0-alpha.1\private-symbols\`
- build/audit report: `artifacts\private-alpha\0.8.0-alpha.1\package-build-report.json`
- clean-machine checklist: `distribution\private-alpha\CLEAN-MACHINE-CHECKLIST.md`
- evidence form: `distribution\private-alpha\EVIDENCE-TEMPLATE.md`

The ZIP is unsigned, portable, Windows x64 only, and intended for invitation-only testing. It contains the self-contained .NET 8 desktop runtime, the application, the Kokoro FP32 model, Nicole only, Windows local TTS fallback, and required notices. It must not contain eSpeak, Firefox, Ollama, Qwen, PDBs, logs, or user data.

## Rebuild the local candidate

From PowerShell in the repository:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\New-PrivateAlphaCandidate.ps1
```

The process-only execution-policy option does not change Windows policy. The script publishes, stages, audits, creates the manifest, retains PDBs privately, creates and verifies the ZIP/hash, extracts a fresh copy, runs the safe package self-test from another working directory, and performs silent offline Nicole preparation. It does not control media, produce audible speech, contact Ollama, or pull a model.

## Before transferring the ZIP

1. Make sure Zoya Radio is stopped.
2. Keep the ZIP and its `.sha256` file together.
3. Do not send `private-symbols`, repository files, logs, models outside the ZIP, or other artifacts.
4. Do not overwrite an existing extracted copy. Every version gets a separate folder.
5. Give the tester `TESTER-README.txt` and `PRIVATE-ALPHA-TERMS.txt` from inside the ZIP.

## Clean Windows 10 22H2 test

Use a genuine Windows 10 22H2 x64 machine or clean VM with no Visual Studio, .NET SDK, separate .NET runtime, Firefox media session, Ollama/Qwen, or extra Visual C++ runtime beyond the clean image.

1. Copy the ZIP and `.sha256` file to the machine.
2. In PowerShell, run `Get-FileHash .\ZoyaRadio-0.8.0-alpha.1-win-x64.zip -Algorithm SHA256` and compare the result with the digest file.
3. Extract into a new ordinary user-writable folder such as Documents. Do not run from inside the ZIP.
4. Record Windows Security or SmartScreen behavior. The executable is unsigned; use **More info > Run anyway** only if the filename and hash match the supplied candidate.
5. Start `ZoyaRadio.ControlApp.exe` without administrator privileges.
6. Record the readiness wording for valid package assets, disk space, Firefox, Visual C++, Ollama, and Qwen.
7. Verify the application does not permanently fail merely because Firefox is absent or idle.
8. If Visual C++ is reported missing, stop and use only Microsoft's official x64 redistributable. Record the before/after result; Zoya must not install it itself.
9. Install Firefox and Ollama separately only for the later prerequisite tests. Zoya must never silently install either.
10. With Ollama absent, verify the application offers only an explicit official-download-page action and does not open it automatically.
11. With Ollama installed but stopped, confirm the status is understandable and no silent start occurs.
12. Start Ollama explicitly, then verify compatible-running and missing-Qwen states.
13. Choose **Get Qwen through Ollama**. Verify the app discloses the approximate 2.7 GB model download, checks free space, defaults to No, requires affirmative consent, shows progress, and permits cancellation.
14. Cancel one pull, then retry or interrupt one safely. Confirm no crash and honest status.
15. Complete the Qwen setup and confirm the app records the exact `qwen3:4b-instruct` identity and digest when Ollama supplies it.
16. Complete the desktop checks below.
17. Stop and close Zoya, verify the log under `%LOCALAPPDATA%\Zoya Radio\logs`, and record removal/rollback behavior.
18. Fill in the evidence template. Do not mark an unchecked item passed.

## Clean Windows 11 test

Repeat every Windows 10 step on a genuine clean Windows 11 x64 machine. Record the exact Windows build, hardware, free space, SmartScreen/antivirus behavior, Visual C++ result, first-launch result, setup states, package hash, and runtime evidence separately. Windows 10 evidence cannot stand in for Windows 11 evidence or vice versa.

## Steve's packaged desktop acceptance

Use the extracted candidate, not a development launcher.

1. Play YouTube Music in Firefox and ensure no second eligible Firefox media session exists.
2. Start `ZoyaRadio.ControlApp.exe` and inspect readiness. Confirm the displayed Firefox title/artist and targeting state are correct.
3. Test **Start**, repeated Start, **Stop**, repeated Stop, **Silent Mode**, **Clear History**, **Open Logs Folder**, and window close.
4. Skip enough tracks to see the 2-5-transition countdown, preparation, prepared waiting, and a delivery.
5. Confirm music continues while AI and Nicole prepare.
6. Confirm normal delivery never pauses Firefox. It must duck only Firefox proportionally to 25% of the volume captured at delivery time and restore the exact scalar and mute state.
7. Change Firefox volume to a non-100% value and repeat. Confirm proportional ducking and exact restoration.
8. Press Stop and close the window during generation, Nicole preparation, ducking, speech, and restoration in separate runs. Confirm protected cleanup completes before the app stops or closes.
9. Enable Silent Mode during pending work. Confirm stale material is discarded and no catch-up announcement occurs.
10. With Chrome or another application producing audio, confirm only Firefox is targeted; Chrome and Windows master volume remain unchanged.
11. Exercise absent, idle, stopped, and ambiguous Firefox states where practical. Unsafe targeting must produce readable safe silence.
12. Verify Nicole is used normally and Windows local TTS remains the safe fallback if neural preparation fails.
13. Review the LocalAppData log for useful diagnostics and private information before sharing it.
14. Preserve the prior extracted folder, test the new version side by side after safe shutdown, then verify rollback by reopening the prior version.

## Invited-tester procedure

Each invited tester should use the tester README, complete the appropriate clean-machine checklist, and return:

- Windows edition/build and x64 confirmation;
- CPU, RAM, GPU if any, and initial/remaining free disk space;
- ZIP SHA-256 result;
- SmartScreen/antivirus observations;
- Visual C++ readiness result;
- Firefox/Ollama/Qwen readiness and setup results;
- Start/Stop/Silent/Clear/window-close results;
- Nicole and Windows-fallback observations;
- Firefox-only duck/restoration and unrelated-application isolation evidence;
- update, rollback, and removal observations;
- exact steps, expected result, actual result, screenshot if helpful, and only the relevant reviewed log excerpt for any defect.

Never send passwords, browser cookies, account tokens, unrelated personal data, or an unreviewed complete log.

## Acceptance boundary

Do not mark M8B or Milestone 8 passed until:

- the final candidate has green local engineering verification;
- genuine clean Windows 10 22H2 and Windows 11 evidence is complete;
- the Nicole/eSpeak-free route works on those machines;
- Steve's packaged real-desktop safety acceptance passes;
- invited private-alpha testing is complete;
- Steve explicitly accepts the result and it is recorded in Git.

The intermittent 20:49 observer stall remains unresolved. Excessive harmless wrapper-refresh logging remains a deferred quality issue. Neither is claimed fixed by packaging.
