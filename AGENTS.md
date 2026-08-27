# Zoya Radio repository rules

## Repository and instruction scope

- Work locally and only inside this repository.
- Before working, read the applicable `AGENTS.md` instructions for the files in scope.
- Use `docs/PROJECT_INDEX.md` to locate governing project documents.
- For substantial AI-assisted work, follow the named-worker and explicit-handoff protocol in `docs/workflow/AI_WORKFLOW.md`. Declare exactly one primary worker role, stay within its contract, and do not silently perform the next worker's job.
- Treat the local Git repository as authoritative over copied or exported ChatGPT Project sources.
- Follow the explicitly assigned task and its approved scope. A chat message does not automatically change canonical decisions or milestone status.
- If task instructions materially conflict with canonical repository state, stop and report the conflict for resolution.

## Documentation authority

- `docs/PROJECT_STATE.md` governs the current milestone, verification, and immediate status.
- `docs/CURRENT_ARCHITECTURE.md` governs implemented behavior.
- `docs/ROADMAP.md` governs completed milestones and approved next direction.
- `docs/DECISIONS.md` governs accepted durable decisions.
- `docs/KNOWN_ISSUES.md` governs unresolved defects and limitations.
- Milestone contracts and acceptance records govern task-specific scope and evidence.
- `docs/RESEARCH_INDEX.md` and indexed research reports contain evidence and recommendations only. Research does not alter implementation, decisions, or roadmap scope automatically.
- Files under `docs/proposals/` are unapproved proposal history only. They cannot authorize Codex work.
- Examples and illustrative behavior are not requirements. A named milestone or roadmap entry does not by itself provide implementation scope.
- Milestone implementation requires an explicitly approved, bounded request or contract.

## Git and worktree protection

- Before editing, inspect the repository root, branch, HEAD, tracked changes, staged changes, and relevant untracked files.
- Preserve working features, all user changes, and unrelated work.
- Never discard, overwrite, revert, or absorb unrelated changes. Stop and report if unexpected changes overlap the assigned task.
- Limit edits to files required by the task and inspect the complete final diff before committing.
- Never amend, rebase, reset, force-push, or push unless explicitly authorized.
- Create a commit only when the task explicitly requests one, and include only the intended files.
- Keep credentials, tokens, and hard-coded API keys out of the project.

## Scope and dependency discipline

- Implement one bounded task or milestone at a time; never silently continue to another milestone.
- Avoid opportunistic refactors, cleanup, renaming, redesign, or architectural expansion outside the approved scope.
- Do not add packages, services, network dependencies, models, or tools unless the approved task explicitly allows them.
- Before requesting approval for a substantial dependency or AI model, explain its purpose, size, license, runtime cost, privacy implications, and relevant alternatives.
- Preserve local-first behavior unless the approved task explicitly changes it.
- Do not write implementation code from unapproved research or proposal material.

## Durable playback and privacy invariants

Consult `docs/CURRENT_ARCHITECTURE.md` and `docs/DECISIONS.md` for the exact current mechanisms. Unless an explicitly approved task changes an invariant and supplies corresponding verification:

- User playback intent wins over host content.
- Silence is the safe outcome when state or targeting is missing, stale, ambiguous, late, or unsafe.
- Generated or prepared content has no guaranteed right to play.
- Target identity must be positively validated; unrelated applications must never become fallback targets.
- Endpoint or master volume must never be changed.
- Any captured application-session volume and mute state must be restored through protected cleanup after success, cancellation, failure, or shutdown.
- Automatic host delivery must not pause, resume, or toggle playback.
- Silent Mode, cancellation, track or session changes, and stale opportunities must invalidate or discard unsafe pending work.
- Factual playback state and metadata must come from deterministic observation rather than AI inference.
- Credentials, listening history, metadata, prompts, and generated material must not be transmitted externally unless an explicitly approved feature requires it.

## Verification and acceptance

- Run the smallest relevant build and automated-test set for the assigned change. Run broader regression verification when shared runtime or safety behavior changes.
- Distinguish verification run for the current task from previously accepted results; never present an old test result as if it was rerun.
- Treat automated tests and Steve's manual desktop acceptance as separate evidence.
- Test before declaring work ready or successful, and never claim a milestone is passed or complete until all required automated and manual acceptance conditions are satisfied.
- Never fabricate desktop observations. Report skipped, blocked, unavailable, or failed verification honestly.
- Preserve unresolved issues unless new evidence actually resolves them.
- Documentation-only work normally does not require launching the radio or rerunning runtime tests unless the task or discovered changes justify it.

## Reporting and communication

- Final reports must identify the scope completed, files changed, and behavior changed or explicitly unchanged.
- Report verification actually performed and its results, manual checks still required, and known issues or risks discovered.
- Report final Git status, any authorized commit created, and confirmation that nothing was pushed unless pushing was explicitly requested.
- Keep logs, instructions, failures, and manual procedures understandable to Steve as a non-programmer without sacrificing technical accuracy.
- For substantial updates, follow the owner-facing summary and decision/handoff wording defined in `docs/workflow/AI_WORKFLOW.md`.

## Named-worker workflow

- `docs/workflow/AI_WORKFLOW.md` governs routing, role boundaries, owner observability, and substantial-task handoffs.
- Friendly worker names must always be paired with their formal role. Role contracts govern behavior; model personality or chat memory does not.
- A handoff is guidance, not authorization. Steve chooses whether to invoke the next worker and remains the approval authority for contracts, implementation, dependencies, product decisions, manual acceptance, and commits.
- Manual named-worker chats and paste-ready handoffs remain the active default and fallback. The optional Conductor-led orchestration policy in `docs/workflow/AI_WORKFLOW.md` is Stage 1 documentation only; no worker may activate or use subagent orchestration without separate Stage 2 or task-specific Steve authorization.
- If Steve later authorizes an orchestration envelope, it may route only explicitly permitted stages. All Steve gates, single-primary-role rules, mandatory handoffs, and manual fallback prompts remain in force.
- Keep automated verification and Steve's real-desktop/manual acceptance as separate evidence. Never infer or fabricate owner acceptance.
- Tiny edits and trivial questions do not require the full declaration and handoff ceremony.
