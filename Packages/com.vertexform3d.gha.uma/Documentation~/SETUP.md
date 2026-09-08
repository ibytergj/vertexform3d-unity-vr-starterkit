# UMA setup contract

**Reviewers: start with the [getting-started and testing guide](../../../GHA-GETTING-STARTED.md).**
This document describes the maintainer contract and body-type authoring rules.

Fresh-install status (September 7): the provider installer now automatically creates and
populates a missing index from the default UMA content. The source-generated index stays
excluded. The complete workflow still needs a fresh-checkout acceptance test.

The package setup workflow must:

1. Detect an existing compatible `Assets/UMA` installation.
2. Use the owner's clean UMA checkout on `master`, pinned to
   `c9204fe475b334162617da5ca923afcc6050e01e` (verbatim upstream master).
   `Tools/Sync-Uma.ps1` never clones, fetches,
   pulls, or changes the source branch. The owner updates that checkout; a newer revision
   requires an explicit pin/layout review. Never commit GHA-specific changes to this source checkout.
3. With the destination editor closed, copy `UMAProject/Assets/UMA` and
   `UMAProject/Assets/SourceShaders`, excluding disposable demos and archival documentation.
   Keep current Markdown documentation and the Photobooth authoring dependencies.
   The scene is now `Assets/UMA/SRP/Samples/Scenes/U3-Tools-Photobooth.unity`;
   its camera render textures still live under `UMA3/Scenes/Prefabs/Textures`.
   For this exact baseline, the script exports the 12 reviewed shader graphs from upstream
   develop commit `f4edf41ba1017b4a745fd1ba7286824332652b7d` into destination staging only.
   It verifies exact Git blob contents, preserving LF without altering the source `.gitattributes`.
4. Preserve vendor metadata and keep `Assets/UMA` outside the GHA/UMA adapter commit.
5. Reopen Unity and run the host and UMA provider installers in the sequence below. The UMA
   installer creates and populates a missing Global Library from the standard `Assets/UMA`
   content. No custom assets or manual library rebuild are required for first-time setup.
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

Pin provenance: official master `c9204fe4`; destination-only shader files from official develop
`f4edf41ba`. The earlier agent-created source commit `0c69fee9b` was removed from local master
on September 7 at the owner's direction. It was never pushed and is not an installation dependency.
Source master must remain an unmodified upstream checkout. The script only reads existing local
Git objects; if the required upstream fix is unavailable, it stops rather than fetching or altering
the source. No other develop changes are installed. When reviewing a newer master pin, check
whether it already includes the repairs; the old shader overlay is scoped only to `c9204fe4`.

Current pin caveat (September 5): `SRP/ShaderGraphs/Materials/UMA_SG_Diffuse.shadergraph`
remains malformed on both upstream branches and is intentionally unchanged.
The upgrade is not yet an accepted rendering baseline. See the migration runbook.

## Project integration sequence

After installing and validating the compatible UMA checkout:

**Before running the integration tools, rebuild an existing UMA Global Library.** Close
**UMA > Global Library**, open **UMA > Global Library Maintenance**, and, if the Asset Index
field contains an index, run **Rebuild Library From Project** and wait for completion.
This refreshes the index after the default UMA content has been installed or replaced.
For a genuinely fresh installation with no index, proceed with the installers below; the UMA
provider creates and populates it automatically. An existing index that fails to load is an
error to investigate, not permission to replace it.

1. Run **Tools > GHA > Integration > Install GHA Host Layer**.
2. Wait for the define change and Unity domain reload to finish.
3. Run **Tools > GHA > Integration > Install UMA Provider Layer**.
4. Wait for compilation, then confirm the installer success log and zero compilation errors.
5. Open **UMA > Global Library** and verify both default human races and their wardrobe content.

Keep the Global Library window closed during installation. The provider installer preserves
existing indexes using UMA's own path precedence: project-owned index first, then the
installation index. Only when neither exists does it create
`Assets/UMAProjectData/Resources/AssetIndexerProject.asset` through Unity's AssetDatabase.
Before saving that new index, it uses UMA's rebuild API scoped to `Assets/UMA/`, verifies
both human races plus slot/overlay/wardrobe entries, and removes the temporary scan filters.
It does not copy the source index, initialize the UMA singleton, or rebuild an existing library.
A wrong-type/unloadable asset occupying the selected index path stops installation without
overwriting it. The generated index remains after provider uninstall because it belongs to
the destination UMA installation, not GHA's provider wiring. Updating an existing content
installation remains a separate maintenance/validation operation.

Keep the Editor in the foreground during symbol changes and wait for compilation and the
installer's success log. Historical agent-specific foreground helpers and AnkleBreaker are not
prerequisites for manual setup. Stop and inspect compile errors if a layer does not finish.

Remove the integration in reverse order: UMA provider first, then GHA host. The installers use
exact snapshots when safe and surgical removal when an installed target was changed later.

The provider installer uses `VERTEXFORM_GHA_UMA`; it must not add `UMA_INSTALLED`. The latter
currently enables an AnkleBreaker UMA 2 bridge that is incompatible with UMA 3.

## UMA-owned tags

UMA 3.0.2's `Assets/UMA/Core/Editor/Scripts/ImportProcessor.cs` unconditionally ensures the
`UMAIgnore` and `UMAKeepChain` tags. They are part of the separately installed UMA plugin state,
not the GHA UMA provider layer, and therefore remain after the provider layer is uninstalled.

## Body types and wardrobe compatibility

UMA calls a character family a `RaceData`; it is not limited to species. Avatar Studio displays
the catalog's body-type label, keeping the actual UMA race name unchanged for lookups and
race-specific thumbnails. The initial choices are Human Male and Human Female.

To introduce another type:

1. Verify Humanoid animation mapping, T-pose and base recipe. The runtime definition gate
   checks those configured assets; validate the generated Animator and bones separately.
2. Append the RaceData to `races`. Never reorder or remove entries: the index is the saved/network
   race ID. Ship the identical catalog on every client.
3. Add a `bodyTypes` entry with its label and explicit starting outfit. Append new clothing to
   `wardrobeRecipes`; never change existing wardrobe IDs.
4. Author explicit compatible races or cross-compatibility and wardrobe-slot mappings. Empty
   compatibility lists are not treated as universal support. Humanoid animation compatibility
   alone does not establish that a garment fits or that its overlays map correctly.
5. Verify exposed DNA names, shared Skin/Hair/Eyes channels, URP materials, thumbnails, clipping,
   and generation cost. Expose only controls supported by the curated content.
6. Test preview, Save Avatar/reopen, Home/networked reconstruction, desktop idle, VR hands,
   head hiding, seating and device performance before accepting a new type.

The Body selector remembers outfits per type for the current customizer session; Save Avatar
persists only the currently selected type and its active recipe. Compatible choices, including
shared hairstyles, are kept on a first switch. Incompatible items are replaced by the explicitly
configured starter outfit, not an arbitrary garment. Explicit None is preserved.
`defaultWardrobe` remains the initial outfit for `defaultRaceId`; keep those existing fields
consistent when changing the deployment's initial body type.

Official UMA references: [Creating a New Race](https://github.com/umasteeringgroup/UMA/blob/master/UMAProject/Assets/UMA/Docs/CreatingANewRace.md)
and [Wardrobe Recipe Editor](https://github.com/umasteeringgroup/UMA/blob/master/UMAProject/Assets/UMA/Docs/WardrobeRecipeEditor.md).
