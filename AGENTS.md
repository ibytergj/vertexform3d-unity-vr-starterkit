# VertexForm3D GHA — Codex Instructions

## Migration work

Before continuing the QuantumVertex avatar migration, read
`GHA-SESSION-HANDOFF-2026-08-25.md` and `GHA-MIGRATION-RUNBOOK.md`. The runbook is the controlling
status, phase, ownership, verification, and handoff document. Do not infer migration completeness
from files already present in the target.

## Unity tooling and Play Mode

Use Unity's supported standalone CLI first for editor discovery, inspection, compilation, console
evidence, tests, builds, and validation whenever it exposes the required capability.

- CLI: `C:\Users\Blender\AppData\Local\Unity\bin\unity.exe` (verified `1.0.0-beta.6`).
- Always pass `--project-path "E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit-GHA"`.
- Pipeline is installed per project. Inspect `Packages/manifest.json`,
  `Packages/packages-lock.json`, and `unity --format json pipeline list` before proposing any
  install or upgrade. Do not force-install it without reviewing the exact package delta and
  preserving AnkleBreaker plus the embedded GHA/UMA package records.
- Never print `Library/Pipeline/.unity-pipeline-port`; it contains a bearer token. A sandboxed CLI
  may falsely report no editor when it cannot read that protected descriptor.
- Use AnkleBreaker only when the CLI lacks the operation or adequate inspection. Record why the
  fallback was needed, list instances, and select the exact GHA project rather than assuming a
  remembered port.
- Ask the project owner and wait for explicit approval before entering or exiting Play Mode.
- During synchronous builds, avoid main-thread-required inspection commands; poll only supported
  off-thread status/console commands. Verify compilation/build status and artifact properties,
  not merely command success.

## Public repository Git safety

This repository is public. Never stage files, create a commit, push a branch, or open/update a pull
request without the user's express approval for that specific Git action and its exact reviewed
scope. File edits and read-only Git inspection do not imply permission to publish or prepare
changes for publication. Before requesting approval, show or summarize the exact files involved and
explicitly exclude credentials, ignored vendor content, generated state, and unrelated user work.

## VertexForm3D developer documentation

Consult these pages before evaluating project layout, prefab integration, configuration, or
contribution work. Do not rely on browser tabs or conversation context retaining the URLs.

- Documentation hub:
  `https://vertexform-dev-docs.notion.site/Vertex-Form-Starter-Kit-Documentation-for-developers-3724bf8b713980ffb146fc98c21c5a76`
- Codebase organization, architecture, workflows, script reference, and dependencies:
  `https://vertexform-dev-docs.notion.site/DEVELOPER_DOCUMENTATION-3734bf8b713980c0a1a8d0e5791add43`
- Networked desktop/MR player prefab hierarchy and integration points:
  `https://vertexform-dev-docs.notion.site/NewGenericMRDesktopPrefab_DEVELOPER_DOCUMENTATION-3734bf8b7139801cb9bee584cff9a622`
- Main UI database, `UILayoutConfig`, prefab baking, runtime consumers, and verification:
  `https://vertexform-dev-docs.notion.site/UILayoutConfig-39e4bf8b713980029f6efe78c9304e81`
