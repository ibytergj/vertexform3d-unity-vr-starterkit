# VertexForm3D GHA — Agent Instructions

Read these files before doing any migration or Unity work:

1. `AGENTS.md`
2. `GHA-SESSION-HANDOFF-2026-08-25.md`
3. `GHA-MIGRATION-RUNBOOK.md`
4. The package-boundary documentation listed by the runbook

The owner's current tooling policy is Unity standalone CLI first, with an explicit GHA
`--project-path`. AnkleBreaker is an approved fallback only when the CLI cannot perform or
sufficiently validate the required operation; record the reason. Never expose Pipeline descriptor
tokens. Ask for explicit approval before entering or exiting Play Mode.

This is a public repository. Never stage, commit, push, or open/update a pull request without
explicit approval for that exact Git action and reviewed file scope.
