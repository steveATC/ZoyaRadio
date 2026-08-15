# Zoya Radio documentation index

This is the front door to the canonical Zoya Radio documentation. The local Git repository is authoritative. If a copied or exported ChatGPT Project-source snapshot disagrees with the repository, use the repository version.

## Snapshot metadata

| Field | Value |
| --- | --- |
| Snapshot date | 2026-08-15 |
| Repository branch | `master` |
| Latest accepted implementation commit | `b56c03d1c913ba18b5d2dc1298964d33d5aeb9cf` |
| Documentation reorganization baseline | `d5594e7ee16be1f11afc732186fa16ae8c5cc1c9` |
| Current passed milestone | Milestone 8, closed for continued internal development |
| Latest accepted automated result | 178 passed, 0 failed at `b56c03d` |
| Accepted internal packaged baseline | `0.8.0-alpha.1`; owner packaged-desktop acceptance recorded August 11, 2026 |
| Deferred distribution evidence | Clean Windows 10/11 and invited private-alpha testing; not blockers to internal development |
| Next project step | M9C is closed as successful experimental evidence; grounded 14B whole-comment synthesis is selected for a future proposal, but no integration is authorized |
| Snapshot ID | `zoya-radio-2026-08-11-m8-b56c03d1` |

Update this table whenever an accepted implementation commit becomes the new source snapshot.

## Start here

- [README.md](../README.md) - practical operator and developer commands.
- [PROJECT_STATE.md](PROJECT_STATE.md) - compact current operational status and immediate gates.
- [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md) - what the application actually implements now.
- [ROADMAP.md](ROADMAP.md) - completed milestones and approved near-term direction.
- [DECISIONS.md](DECISIONS.md) - durable accepted product and engineering decisions.
- [KNOWN_ISSUES.md](KNOWN_ISSUES.md) - unresolved defects, accepted limitations, and deferred improvements.
- [MILESTONE_8_CONTRACT.md](MILESTONE_8_CONTRACT.md) - approved M8 scope and historical gates, plus Steve's dated sequencing amendment closing M8 for internal development while deferring distribution validation.
- [MILESTONE_8A_TEST.md](MILESTONE_8A_TEST.md) - accepted M8A desktop evidence and preserved regression procedure.
- [MILESTONE_8B_TEST.md](MILESTONE_8B_TEST.md) - accepted owner packaged-desktop evidence plus deferred clean-machine and invited-tester procedures.
- [MILESTONE_9A_CONTRACT.md](MILESTONE_9A_CONTRACT.md) - approved bounded structured-commentary experiment; normal runtime unchanged and M9B unauthorized.
- [MILESTONE_9A_TEST.md](MILESTONE_9A_TEST.md) - rejected candidate record.
- [MILESTONE_9C_CONTRACT.md](MILESTONE_9C_CONTRACT.md) - approved isolated source-backed enrichment experiment.
- [MILESTONE_9C_TEST.md](MILESTONE_9C_TEST.md) - completed experiment history, M9C.6 owner evidence, selected direction, and deferred integration issues.
- [RESEARCH_INDEX.md](RESEARCH_INDEX.md) - research inventory and the boundary between evidence and recommendations.

## ChatGPT Project-source availability

**Canonical current-state/navigation documents shared as ChatGPT Project sources:**

- `PROJECT_INDEX.md`
- `PROJECT_STATE.md`
- `CURRENT_ARCHITECTURE.md`
- `ROADMAP.md`
- `DECISIONS.md`
- `KNOWN_ISSUES.md`
- `RESEARCH_INDEX.md`

**Mirrored research evidence:**
- [K-Rose production analysis](Zoya_Radio_K-Rose_Production_Analysis.md)
- [K-DST production analysis](Zoya_Radio_K-DST_Production_Analysis.md)
- [V-Rock production analysis](Zoya_Radio_V-Rock_Production_Analysis.md)
- [RadioSanAndreas repository research](Zoya_Radio_RadioSanAndreas_Repository_Research.md)

**Supplemental unapproved proposal material:**

- [Post-M8 proposal intake](proposals/POST_M8_INTAKE.md)

The intake preserves what the Design / Brainstorming conversation proposed. It does not govern current implementation, change `DECISIONS.md` or `ROADMAP.md`, define Milestone 8, or authorize Codex work. Every proposal remains unapproved until Steve explicitly promotes it through the project's decision and roadmap process.

**Canonical in Git but repository-only in the current shared snapshot:**

- [README.md](../README.md)
- [Milestone 8 contract](MILESTONE_8_CONTRACT.md)
- Existing `MILESTONE_*_TEST.md`, voice-audition, and other milestone acceptance records

Repository-only does not mean non-authoritative. README and the milestone records remain canonical Git documentation. Project chats should use the seven synchronized canonical documents as their shared navigation/current-state layer, the synchronized research reports as mirrored evidence, and the intake only as supplemental unapproved proposal history. The local Git repository remains authoritative whenever a Project-source copy differs from it. Do not upload or duplicate repository-only records merely to simplify this boundary.

## Authority map

| Subject | Governing document |
| --- | --- |
| Commands, launchers, logs, and local build instructions | [README.md](../README.md) |
| Current passed milestone, latest verification, and immediate blockers | [PROJECT_STATE.md](PROJECT_STATE.md) |
| Implemented components, runtime flow, and current safety behavior | [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md) |
| Completed milestones and approved next work | [ROADMAP.md](ROADMAP.md) |
| Durable decisions and accepted rationale | [DECISIONS.md](DECISIONS.md) |
| Defects, limitations, unresolved observations, and deferred quality work | [KNOWN_ISSUES.md](KNOWN_ISSUES.md) |
| Approved Milestone 8 scope, gates, non-goals, and acceptance requirements | [MILESTONE_8_CONTRACT.md](MILESTONE_8_CONTRACT.md) |
| Approved M9A experimental scope and non-integration boundary | [MILESTONE_9A_CONTRACT.md](MILESTONE_9A_CONTRACT.md) |
| Approved M9C source-backed experimental scope and non-integration boundary | [MILESTONE_9C_CONTRACT.md](MILESTONE_9C_CONTRACT.md) |
| External research provenance and status | [RESEARCH_INDEX.md](RESEARCH_INDEX.md) |
| What the Design / Brainstorming conversation proposed | [POST_M8_INTAKE.md](proposals/POST_M8_INTAKE.md), as a non-authoritative historical intake only |
| Milestone-specific manual evidence and regression procedures | Existing `MILESTONE_*_TEST.md` records |

## Information classes

- **Current implementation** means behavior present in the repository now. It belongs in `CURRENT_ARCHITECTURE.md` and must agree with source and tests.
- **Approved near-term roadmap** means work Steve has explicitly accepted as the next direction. It belongs in `ROADMAP.md` and is not implemented merely because it is listed.
- **Future product vision** is an explicitly labeled possibility beyond approved near-term work. It is not a requirement or architecture decision.
- **Research evidence** records what an external source or inspected artifact establishes. Recommendations derived from research remain recommendations until Steve approves them.
- **Brainstorming** is non-authoritative. Chat discussions, ideas, and hypothetical designs do not change implementation, decisions, or roadmap scope automatically.

## Snapshot and historical-record rules

- Synchronize only the files classified above as canonical current-state/navigation documents, mirrored research evidence, or supplemental proposal material, according to their stated authority boundaries.
- Keep the commit hash and snapshot date visible so stale exports are easy to identify.
- Never merge research recommendations or brainstorming into `CURRENT_ARCHITECTURE.md`.
- Milestone test documents are preserved historical acceptance and regression records. Some retain wording written before later manual acceptance; current milestone status is governed by `PROJECT_STATE.md` and `ROADMAP.md`.
- The M7.5B accepted evidence is preserved in [MILESTONE_7_5B_TEST.md](MILESTONE_7_5B_TEST.md).
- The M8A accepted evidence is preserved in [MILESTONE_8A_TEST.md](MILESTONE_8A_TEST.md).
- The M8B internal acceptance and deferred distribution evidence are recorded in [MILESTONE_8B_TEST.md](MILESTONE_8B_TEST.md).

## Milestone records

- [Milestone 0](MILESTONE_0_TEST.md)
- [Milestone 1](MILESTONE_1_TEST.md)
- [Milestone 2](MILESTONE_2_TEST.md)
- [Milestone 3](MILESTONE_3_TEST.md)
- [Milestone 4](MILESTONE_4_TEST.md)
- [Milestone 5](MILESTONE_5_TEST.md)
- [Milestone 7 voice audition](MILESTONE_7_VOICE_AUDITION.md)
- [Milestone 7](MILESTONE_7_TEST.md)
- [Milestone 7.5A](MILESTONE_7_5A_TEST.md)
- [Milestone 7.5B](MILESTONE_7_5B_TEST.md)
- [Milestone 8 contract](MILESTONE_8_CONTRACT.md) - approved contract and dated sequencing amendment; M8 is closed for internal development while clean-machine and invited-tester validation remain deferred
- [Milestone 8A acceptance](MILESTONE_8A_TEST.md) - accepted desktop evidence plus preserved regression procedure
- [Milestone 8B acceptance](MILESTONE_8B_TEST.md) - accepted internal package and owner desktop evidence, with deferred distribution-validation procedures
- [Milestone 9A contract](MILESTONE_9A_CONTRACT.md) - approved experiment, candidate prompt boundary, benchmark, and non-goals
- [Milestone 9A evaluation](MILESTONE_9A_TEST.md) - blinded review procedure and the recorded rejected-candidate outcome
- [Milestone 9C contract](MILESTONE_9C_CONTRACT.md) - bounded source-backed experiment and non-integration boundary
- [Milestone 9C evaluation](MILESTONE_9C_TEST.md) - completed M9C.6 owner evidence and selected future candidate direction
