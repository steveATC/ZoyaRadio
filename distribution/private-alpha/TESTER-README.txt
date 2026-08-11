ZOYA RADIO 0.8.0-alpha.1 - PRIVATE ALPHA

This is invitation-only, unfinished Windows test software. It is unsigned.
Windows SmartScreen may warn you because the executable is not code-signed.

Supported system
- Windows 10 22H2 x64 (build 19045) or newer, including Windows 11 x64.
- Extract the ZIP to a normal folder you can write to. Do not run it inside the ZIP.
- A separately installed Firefox is required for YouTube Music media observation.
- Ollama and qwen3:4b-instruct are separate, optional prerequisites for AI comments.
  Without them, the accepted fixed local fallback remains available.

Integrity
1. Keep the supplied ZIP and .sha256.txt file together.
2. In PowerShell, run: Get-FileHash .\ZoyaRadio-0.8.0-alpha.1-win-x64.zip -Algorithm SHA256
3. Confirm the displayed value exactly matches the value in the .sha256.txt file.
4. After extraction, use Check Readiness before Start. Required packaged files are
   also checked against package-manifest.json.

First launch
1. Double-click ZoyaRadio.ControlApp.exe.
2. Choose Check Readiness and read every line.
3. If Firefox is installed but idle, that is not a broken installation. Start
   YouTube Music playing in Firefox before pressing Start.
4. If the Visual C++ x64 runtime is missing, use only Microsoft's official link:
   https://aka.ms/vs/17/release/vc_redist.x64.exe
   Zoya Radio never downloads or installs it.
5. If Ollama is absent, use the clearly labelled official-download-page button
   only if you want local AI comments. Zoya Radio never installs or starts it.
6. If Ollama is running but Qwen is missing, Get Qwen through Ollama explains the
   approximate 2.7 GiB download, checks for at least 8 GiB free, asks Yes/No with
   No as the default, reports progress, and permits cancellation.

Normal controls
- Start: begins the accepted radio observer/runtime after readiness checks.
- Stop: cancels pending work and restores protected Firefox audio state.
- Silent Mode: observes without announcements.
- Clear History: clears only small in-memory context; it does not delete files.
- Open Logs Folder: opens %LOCALAPPDATA%\Zoya Radio\logs.

Expected safe silence
Zoya Radio remains silent when Firefox media/audio targeting is missing,
ambiguous, stale, late, or otherwise unsafe. Ordinary YouTube playback in Firefox
can become Firefox's tracked Windows media session; check the displayed title and
artist. Normal automatic delivery never pauses Firefox. It proportionally lowers
only the positively validated Firefox application audio session, then restores
the exact captured volume and mute state.

Voice route
The primary route is local Kokoro FP32 with the Nicole (af_nicole) voice and CPU
ONNX Runtime. The candidate contains Nicole only and no eSpeak files. A local
Windows voice is the deliberate fallback if neural preparation fails safely.
Some unusual or out-of-vocabulary names may use that fallback because eSpeak is
deliberately not included. The fallback is prepared before Firefox is ducked.

Logs and privacy
Logs remain under %LOCALAPPDATA%\Zoya Radio\logs and may contain timestamps,
Firefox media metadata, generated comments, targeting details, and readable
errors. Review logs before sharing them. The package contains no credentials,
cookies, listening-history database, prepared audio, or telemetry.

Update, rollback, and removal
1. Stop Zoya Radio completely.
2. Extract a newer version into a separate folder; never overwrite a running copy.
3. Keep the previous folder until the new copy has been tested.
4. Roll back by closing the new copy and launching the old folder's executable.
5. Remove a version by closing it and deleting its extracted folder.
6. Logs are separate; delete %LOCALAPPDATA%\Zoya Radio\logs manually if desired.

If something fails
Stop Zoya Radio, verify Firefox volume, then preserve the relevant local log.
Report the Windows version, the exact Zoya version, what you clicked, what you
expected, what happened, and the reviewed log. Never share credentials or cookies.

Read PRIVATE-ALPHA-TERMS.txt and THIRD-PARTY-NOTICES.txt before testing.
