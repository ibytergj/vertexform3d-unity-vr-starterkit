# UMA performance checkpoint — September 4, 2026 EDT

## Status

Pre-optimization baseline, not a completed migration or release qualification.
The current Avatar Studio UI has been accepted for continued work. The first two
performance steps have been audited but are not implemented or activated.

## Measured normal Login → Home load

One approved Editor capture ran September 5 at 01:35:44 UTC (September 4 EDT),
without deep profiling. Play Mode was stopped afterward. No crash occurred in
that test; this does not establish that every crash scenario is resolved.

| Measurement | Result |
|---|---:|
| Home build request to ready | 21,457 ms |
| DCA.Start | 18,615.630 ms |
| Loading.ReadObject under DCA.Start | 18,459.805 ms |
| AssetIndexer object's ReadObjectThreaded | 6,711.641 ms |
| Remaining 991 ReadObjectThreaded calls | 10,968.740 ms |
| CharacterBegun to CharacterCreated, across 32 frame intervals | 2,542 ms |

These nested measurements must not be added together. The main blocking interval
is synchronous loading of the index and its referenced assets, before generation.
The complete stalled profiler frame was 25,288.690 ms and included a separate
5,079.828 ms Editor GUI asset-lock wait. Do not attribute that separate wait to
UMA generation. Storage/security causes of file-opening latency remain unknown.
Editor results do not establish Windows-player, Web, or headset frame times.

## Content audit and next two steps

The installed index has 493 records, 488 direct references, and no addressable
records. The catalog-scoped candidate has 76 records: 20 startup records (one
race, one base recipe, 17 wardrobe recipes, one mesh-hide asset) and 56 proposed
streamed records (25 slots, 24 overlays, seven UMA materials). Its dependency
closure contains 427 paths. No explicit recipe-name dependencies were missing;
runtime string-based dependencies still require validation.

1. Implement a project-owned runtime index retaining all catalog choices and
   stable IDs. Keep the full authoring index recoverable and editor-only; do not
   leave a second full Resources index in the player build.
2. Configure GHA-owned local Addressables bundles and UMA's existing asynchronous
   recipe loading. Validate bundle layout, shared dependencies, error handling,
   cancellation, and all choices before enabling UMA_ADDRESSABLES. No synchronous
   WaitForCompletion or replacement avatar on failed load is proposed.

No runtime index, Addressables groups, or streaming symbols were changed during
the audit. Repeat the same cold-load capture after implementation, then validate
real player/headset maximum frame time as well as total loading time. Stock-only,
multiplayer, and target-platform migration gates remain outstanding.

## Reproduction and local evidence

- Tools/ProfileUmaLogin.cs: one-shot approved normal-login capture.
- Tools/InspectUmaLoadSamples.cs: inspect the full capture, including the stall.
- Tools/AuditUmaLoadIndex.cs: read-only installed-index audit.
- Tools/PlanUmaStreaming.cs: read-only catalog/dependency feasibility report.
- UMA source pin: 722b308aebfe5dee7d0048e24c1c464c00b5fc06.
- Raw local capture: Library/GHAProfiles/20260905-013544-027-normal-login-20260905-013544.raw.
- Detailed local findings: Logs/MetaOpenXR-Fix-2026-09-04/UMA-PROFILE-FINDINGS.md.
- Local content plan: Logs/UMA-Streaming/content-plan-20260905-033738.json.
- Local implementation plan: Logs/UMA-Streaming/IMPLEMENTATION-PLAN.md.

Library and Logs artifacts are intentionally excluded from Git. The earlier local
UMA-PERFORMANCE-BASELINE.md predates this capture; the measurements above supersede
its preliminary narrowing. Diagnostic scripts require deliberate invocation;
running them is not authorization to enter Play Mode.
