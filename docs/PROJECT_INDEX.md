# Zoya Radio documentation index

This is the front door to the canonical Zoya Radio documentation. The local Git repository is authoritative. If a copied or exported ChatGPT Project-source snapshot disagrees with the repository, use the repository version.

## Snapshot metadata

| Field | Value |
| --- | --- |
| Snapshot date | 2026-08-10 |
| Repository branch | `master` |
| Latest accepted commit | `63662342a374b323b639325f360ae7c17ace1432` |
| Current passed milestone | Milestone 7.5B |
| Latest automated result | 124 passed, 0 failed |
| Next gate | Milestone 8 is planned and unstarted |
| Snapshot ID | `zoya-radio-2026-08-10-m7.5b-63662342` |

Update this table whenever an accepted implementation commit becomes the new source snapshot.

## Start here

- [README.md](../README.md) - practical operator and developer commands.
- [PROJECT_STATE.md](PROJECT_STATE.md) - compact current operational status and immediate gates.
- [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md) - what the application actually implements now.
- [ROADMAP.md](ROADMAP.md) - completed milestones and approved near-term direction.
- [DECISIONS.md](DECISIONS.md) - durable accepted product and engineering decisions.
- [KNOWN_ISSUES.md](KNOWN_ISSUES.md) - unresolved defects, accepted limitations, and deferred improvements.
- [RESEARCH_INDEX.md](RESEARCH_INDEX.md) - research inventory and the boundary between evidence and recommendations.

## ChatGPT Project-source availability

**Shared as ChatGPT Project sources in the current synchronized package:**

- `PROJECT_INDEX.md`
- `PROJECT_STATE.md`
- `CURRENT_ARCHITECTURE.md`
- `ROADMAP.md`
- `DECISIONS.md`
- `KNOWN_ISSUES.md`
- `RESEARCH_INDEX.md`
- [K-Rose production analysis](Zoya_Radio_K-Rose_Production_Analysis.md)
- [K-DST production analysis](Zoya_Radio_K-DST_Production_Analysis.md)
- [V-Rock production analysis](Zoya_Radio_V-Rock_Production_Analysis.md)
- [RadioSanAndreas repository research](Zoya_Radio_RadioSanAndreas_Repository_Research.md)

**Canonical in Git but repository-only in the current shared snapshot:**

- [README.md](../README.md)
- Existing `MILESTONE_*_TEST.md`, voice-audition, and other milestone acceptance records

Repository-only does not mean non-authoritative. README and the milestone records remain canonical Git documentation. Project chats should use the seven synchronized canonical documents as their shared navigation/current-state layer and the synchronized research reports as mirrored evidence. The local Git repository remains authoritative whenever a Project-source copy differs from it. Do not upload or duplicate repository-only records merely to simplify this boundary.

## Authority map

| Subject | Governing document |
| --- | --- |
| Commands, launchers, logs, and local build instructions | [README.md](../README.md) |
| Current passed milestone, latest verification, and immediate blockers | [PROJECT_STATE.md](PROJECT_STATE.md) |
| Implemented components, runtime flow, and current safety behavior | [CURRENT_ARCHITECTURE.md](CURRENT_ARCHITECTURE.md) |
| Completed milestones and approved next work | [ROADMAP.md](ROADMAP.md) |
| Durable decisions and accepted rationale | [DECISIONS.md](DECISIONS.md) |
| Defects, limitations, unresolved observations, and deferred quality work | [KNOWN_ISSUES.md](KNOWN_ISSUES.md) |
| External research provenance and status | [RESEARCH_INDEX.md](RESEARCH_INDEX.md) |
| Milestone-specific manual evidence and regression procedures | Existing `MILESTONE_*_TEST.md` records |

## Information classes

- **Current implementation** means behavior present in the repository now. It belongs in `CURRENT_ARCHITECTURE.md` and must agree with source and tests.
- **Approved near-term roadmap** means work Steve has explicitly accepted as the next direction. It belongs in `ROADMAP.md` and is not implemented merely because it is listed.
- **Future product vision** is an explicitly labeled possibility beyond approved near-term work. It is not a requirement or architecture decision.
- **Research evidence** records what an external source or inspected artifact establishes. Recommendations derived from research remain recommendations until Steve approves them.
- **Brainstorming** is non-authoritative. Chat discussions, ideas, and hypothetical designs do not change implementation, decisions, or roadmap scope automatically.

## Snapshot and historical-record rules

- Synchronize only the files listed under **Shared as ChatGPT Project sources** as one dated snapshot.
- Keep the commit hash and snapshot date visible so stale exports are easy to identify.
- Never merge research recommendations or brainstorming into `CURRENT_ARCHITECTURE.md`.
- Milestone test documents are preserved historical acceptance and regression records. Some retain wording written before later manual acceptance; current milestone status is governed by `PROJECT_STATE.md` and `ROADMAP.md`.
- The M7.5B accepted evidence is preserved in [MILESTONE_7_5B_TEST.md](MILESTONE_7_5B_TEST.md).

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
