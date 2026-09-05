# UMA setup contract

The package setup workflow must:

1. Detect an existing compatible `Assets/UMA` installation.
2. Use the owner's clean UMA checkout on `master`, pinned to
   `0c69fee9b4871251d520ae2bfd84868b5a1646f3` (master plus the owner-approved
   selective shader backport). `Tools/Sync-Uma.ps1` never clones, fetches,
   pulls, or changes the source branch. The owner updates that checkout; a newer revision
   requires an explicit pin/layout review. No separate feature overlay is needed.
3. With the destination editor closed, copy `UMAProject/Assets/UMA` and
   `UMAProject/Assets/SourceShaders`, excluding disposable demos and archival documentation.
   Keep current Markdown documentation and the Photobooth authoring dependencies.
   The scene is now `Assets/UMA/SRP/Samples/Scenes/U3-Tools-Photobooth.unity`;
   its camera render textures still live under `UMA3/Scenes/Prefabs/Textures`.
4. Preserve vendor metadata and keep `Assets/UMA` outside the GHA/UMA adapter commit.
5. Reopen Unity and rebuild the UMA Global Library. The sync excludes the source
   `AssetIndexer.asset`; generated library data must come from the destination's assets.
6. Validate required races, wardrobe recipes, materials, and the GHA UMA catalog.
7. Never modify or vendor the source UMA repository.

The current `Tools/Sync-Uma.ps1` remains the migration reference until this workflow is implemented
as cross-platform Unity Editor tooling.

On this workstation the source is `E:/src/Unity/6000.3/UMA`. Preview with
`./Tools/Sync-Uma.ps1 -ReplaceExisting -WhatIf`, then run the same command without
`-WhatIf` after closing the destination editor. Replacement preserves the prior
UMA and SourceShaders trees beneath `Temp/UMA-Backup-*`; do not clean that directory
until the upgrade is accepted. Compilation, catalog validation, and runtime acceptance
are separate gates; successful copying alone does not establish compatibility.

Pin provenance: master `c9204fe4`, plus the exact 12 shader graphs from develop
`f4edf41ba` and `.gitattributes` from `800ee5e99`, committed locally as `0c69fee9b`.
No other develop changes are included. This local UMA commit has not been pushed;
other machines cannot fetch it from the official remote until it is published.

Current pin caveat (September 5): `SRP/ShaderGraphs/Materials/UMA_SG_Diffuse.shadergraph`
remains malformed on both upstream branches and is intentionally unchanged.
The upgrade is not yet an accepted rendering baseline. See the migration runbook.

## Project integration sequence

After installing and validating the compatible UMA checkout:

1. Run **Tools > GHA > Integration > Install GHA Host Layer**.
2. Wait for the define change and Unity domain reload to finish.
3. Run **Tools > GHA > Integration > Install UMA Provider Layer**.
4. Wait for compilation, then confirm the installer success log and zero compilation errors.

On the current Windows workstation, start `Tools/Keep-UnityCompileForeground.ps1` for the target
Unity process before each menu operation that changes a layer symbol. Stop that exact helper
process after AnkleBreaker reports `isCompiling: false`.

Remove the integration in reverse order: UMA provider first, then GHA host. The installers use
exact snapshots when safe and surgical removal when an installed target was changed later.

The provider installer uses `VERTEXFORM_GHA_UMA`; it must not add `UMA_INSTALLED`. The latter
currently enables an AnkleBreaker UMA 2 bridge that is incompatible with UMA 3.

## UMA-owned tags

UMA 3.0.2's `Assets/UMA/Core/Editor/Scripts/ImportProcessor.cs` unconditionally ensures the
`UMAIgnore` and `UMAKeepChain` tags. They are part of the separately installed UMA plugin state,
not the GHA UMA provider layer, and therefore remain after the provider layer is uninstalled.
