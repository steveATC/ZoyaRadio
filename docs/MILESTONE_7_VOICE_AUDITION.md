# Milestone 7 Stage B - voice audition

This stage compares local Kokoro voices before changing Zoya Radio. It has **not** replaced the working Windows voice and does not control Firefox or music.

## What to do now

1. Open this folder in File Explorer:

   `C:\Users\Steve\Documents\ZoyaRadio\artifacts\voice-audition\20260807-235846-809`

2. Double-click these files one at a time. They all use the same English sentence:

   - `01-af-heart.wav` - `af_heart`, American English
   - `02-af-bella.wav` - `af_bella`, American English
   - `03-af-nicole.wav` - `af_nicole`, American English
   - `04-bf-emma.wav` - `bf_emma`, British English
   - `05-af-sarah.wav` - `af_sarah`, American English

3. Compare naturalness, clarity, pacing, warmth, and whether the dry joke sounds conversational rather than like a navigation system having a difficult day.

4. Optionally play `06-unicode-af-heart.wav`. It contains exact Cyrillic text. Russian is not a documented Kokoro language, so poor pronunciation or skipped words are useful observations, not an automatic failure.

5. Tell the Work chat the exact voice name you prefer, for example: `I choose bf_emma.` Also mention any runner-up and anything that sounded wrong.

Do not judge from the file number alone; the next integration stage will use the exact voice identifier you choose.

## Measured audition results

| Voice | Synthesis time | Audio duration | Notes |
|---|---:|---:|---|
| `af_heart` | 3.475 s | 8.125 s | First cold synthesis; includes initial phonemizer/inference warm-up |
| `af_bella` | 0.968 s | 8.575 s | Warm |
| `af_nicole` | 1.404 s | 12.450 s | Warm; noticeably longer delivery |
| `bf_emma` | 1.079 s | 8.125 s | Warm; British English tokenization |
| `af_sarah` | 0.939 s | 8.500 s | Warm |
| `af_heart` Unicode check | 1.338 s | 11.400 s | Listen for omissions or poor Cyrillic pronunciation |

- Model load: 0.673 seconds.
- Working set: 24.5 MiB before model load; 486.2 MiB after load.
- Private memory after model load: 655.1 MiB.
- GPU memory: the audition process was not listed by `nvidia-smi`; CPU-only inference was explicitly used.
- Cleanup: `KokoroTTS.Dispose()` was called, then the audition process exited. The model was not deliberately left resident.
- Result: 6 samples completed, 0 failed.

The full UTF-8 measurement report is `voice-audition-report.txt` in the same folder.

## Create another set

From PowerShell in the ZoyaRadio folder, run:

```powershell
.\run-voice-audition.cmd
```

The command creates a new timestamped folder. It does not play audio automatically.

## Local installation and removal

- Engine: `KokoroSharp.CPU` 0.8.0 from NuGet, using Microsoft ONNX Runtime 1.22.0 on the CPU.
- Model: Kokoro 82M v1.0 full-precision ONNX, stored at `.tools\kokoro\kokoro.onnx` (310.5 MiB).
- Package cache: `.packages\` (the audition dependency closure is about 559.69 MiB extracted).
- Generated samples: `artifacts\voice-audition\`.
- Approximate current local footprint including duplicated build output: 1.20 GiB.
- Administrator privileges: not required.
- Global PATH or system configuration changes: none.

The audio and model are reversible local files. `artifacts\voice-audition\` may be deleted to remove samples. `.tools\kokoro\` may be deleted to remove the model. Deleting `.packages\` clears the repository's entire local NuGet cache, so do that only if you intentionally want the normal project packages restored again on a future build.

Licenses: KokoroSharp is MIT; the Kokoro model and voices are Apache-2.0; the bundled eSpeak NG phonemizer is GPLv3. This stage is a private local audition, not a distribution decision.

## Current gate

Stop here until Steve chooses a voice. Stage C integration, fallback behavior, real Firefox pause/speak/resume testing, and any later milestone have not begun.
