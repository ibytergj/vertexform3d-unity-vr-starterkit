# QuantumVertex to Generic Humanoid Avatars Migration Runbook

Status date: 2026-09-07
Migration source: `E:\src\Unity\6000.3\QuantumVertex`  
Migration target: `E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit-GHA`

## Purpose

This is the controlling implementation and handoff document for migrating the avatar work from
QuantumVertex into the public VertexForm3D `Generic-Humanoid-Avatars` branch.

It reconciles the original QuantumVertex plan with the partial migration already present in the
target repository. A new agent must use this document to determine:

- what is authoritative;
- what has merely been copied;
- what remains unfinished from the original plan;
- which package or repository owns each change;
- which verification gate must pass before moving to the next phase.

Do not interpret the presence of a file in the target as proof that the behavior was migrated
correctly. The current target is a migration workbench, not an accepted baseline.

## Status vocabulary

| Status | Meaning |
|---|---|
| `DONE` | Implemented and verified for the stated scope. |
| `PROVISIONAL` | Present in the target, but parity or runtime behavior has not been accepted. |
| `BLOCKED` | Work cannot be validated until the stated blocker is resolved. |
| `PENDING` | Required work that belongs in the active migration. |
| `DEFERRED` | Intentionally retained in the roadmap but not required for the next migration gate. |
| `NOT STARTED` | Required original-plan work with no accepted implementation. |
| `SUPERSEDED` | Replaced by a documented design or implementation decision. |

## Read first

Target repository:

1. `AGENTS.md`
2. `GHA-SESSION-HANDOFF-2026-08-25.md`
3. This runbook
4. `Packages/com.vertexform3d.gha/Documentation~/PACKAGE-BOUNDARIES.md`
5. `Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md`
6. `Packages/com.vertexform3d.gha/Documentation~/REFERENCE-REVIEW.md`
7. `Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md`
8. `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/README.md`

QuantumVertex source references:

1. `V2-PLAN.md`
2. `AVATAR-FRAMEWORK-HANDOFF-2026-07-12.md`
3. `Assets/QVAddons/AvatarSuite/ARCHITECTURE.md`
4. `Assets/QVAddons/AvatarSuite/REFERENCE-REVIEW.md`
5. `UMA-UPGRADE-DELTA-ANALYSIS-2026-07-24.md`

When documentation and live source disagree, do not silently choose one. Record the discrepancy in
the migration ledger and establish the frozen QuantumVertex commit before porting the affected
slice.

## Non-negotiable rules

1. QuantumVertex becomes read-only after its current functional avatar state is checkpointed and
   committed. Avatar timing analysis and performance optimization are explicitly deferred until
   the full functional migration is complete. Until the checkpoint commit exists, the target
   cannot claim source parity.
2. Migrate by responsibility and verification slice, not by bulk folder replacement.
3. Keep these change sets independently reviewable:
   - minimal provider-neutral VertexForm3D core seams;
   - `com.vertexform3d.gha`;
   - `com.vertexform3d.gha.uma`;
   - host-coupled staging adapters;
   - scene, prefab, Animator, and project configuration;
   - diagnostics and tests.
4. `Assets/UMA` and `Assets/SourceShaders` are pristine, pinned third-party UMA vendor content
   plus documented pruning/compatibility corrections. They are ignored and never committed as
   part of GHA.
5. Do not copy or merge UMA's generated `AssetIndexer.asset`; rebuild the Global Library.
6. Do not commit Photon credentials or other machine/deployment credentials.
7. Preserve Unity `.meta` files when moving existing Unity assets. Perform scene, prefab,
   ScriptableObject, and Animator work through the Unity editor tooling.
8. Always enter Play Mode from `LoginScene`.
9. Do not submit an upstream pull request until the migration, validation matrix, and commit
   separation are complete. The current branch is not PR-ready.
10. This is a public repository. Never stage, commit, push, or open/update a pull request without
    the user's express approval for that specific Git action and its exact reviewed file scope.
    Editing files or receiving general permission to continue the migration is not Git approval.

## Current repository truth

### September 7 UMA installer catalog-reference recovery

The owner completed the test provider install at 18:51 EDT: 488 default index entries, successful
compilation and no new errors. Subsequent Home runtime testing nevertheless failed the generic
Humanoid guard. Inspection confirmed both human race definitions were valid, but the installed
Home/player/panel catalog fields and panel slider had been saved as null. The earlier log/hash
check established that files were saved, not that their required references were correct.

UMA's `RebuildLibrary` starts asynchronous `Resources.UnloadUnusedAssets`; Unity does not retain
assets referenced only from stack variables. This is consistent with the first-install reference
loss. The provider installer now loads and statically roots its catalog/slider/controllers before
index creation, releases those temporary roots in finally, validates default/all catalog Humanoid
definitions, checks prefab-save results and verifies saved catalog/controller/slider references
before marking the provider installed. Failed attempts clear installed status without replacing
original snapshots. Runtime errors now distinguish an unassigned catalog from an invalid race.
No runtime substitution, saved-choice reset, vendor edits or catalog ID/default changes were made.

`Tools/ValidateUmaInstallerReferences.cs` passed seven Edit Mode checks in `E:/Test/GHA-Review`:
retention through actual unused-asset cleanup; male default plus two complete human definitions;
invalid default rejection; successful reference repair; byte-identical repeated install;
post-install cleanup retention; and unchanged catalog, existing index and saved recipe. The
corrected provider installer has already been rerun in the review project. Runtime retest is
pending; Play Mode was not entered for this repair. This is not a new full fresh-clone acceptance.

### September 7 clean-test host installer recovery

Owner authorized removing the confirmed obsolete Cesium components and correcting the installer.
Upstream commit `033e4cc4` intentionally removed Cesium but left CesiumGlobeAnchor script GUID
`74f14e1eb550b9a4fb6c0a2f0456845b` on both player roots. Both development and `E:/Test/GHA-Review`
player prefabs were repaired through Unity: exactly 39 lines removed per prefab, consisting only
of that component record and its added-component attachment. Zero missing scripts remain; other
component records and prefab metadata are preserved. No Cesium package was reinstalled.

`GhaVertexFormAssetInstaller` now preflights its target prefabs and Home station before asset
edits, checks every prefab-save result, skips unchanged player component saves, clears the
installed flag on failure, and preserves the original recovery snapshots across retries.
`GhaIntegrationBootstrap` clears failed pending operations and tells the owner to resolve the
error and explicitly rerun the same menu, rather than retrying unexpectedly on domain reload.

Unity 6000.3.11f1 compilation passed in both editors. `Tools/ValidateGhaHostInstaller.cs` passed
eight checks in the test project: valid targets, missing-path rejection, full-install missing-script
preflight rejection without prefab mutation, failed status/original snapshot preservation, failed
Unity save propagation, successful repaired install, byte-identical second install, and one sync
plus one UMA bridge per player with zero missing scripts. The deliberate save-failure case logs
an expected error for a disposable `GhaHostInstallerValidation-*` prefab; temporary assets were
removed. Play Mode was not entered. Host installation is complete in the current test copy;
the owner's next menu is Install UMA Provider Layer, not another host install or uninstall.

Historical uninstall snapshots in both local projects also contained the obsolete component.
Preserved their originals under `Logs/ReviewTransfer/OriginalInstallBackups-20260907` in development,
then removed only those same component/attachment records from the dormant `.backup` files.
Unity reserialization of temporary copies added newer schema fields, so those results were rejected;
the dormant backups were instead patched explicitly and compared to the exact expected deletion.
Active prefab edits were all performed through Unity. Original active prefabs are retained under
each project's `Logs/ReviewTransfer/CesiumRepair-*`. No source VertexForm/UMA repo changes,
staging, commits or publication. Separate malformed UMA graph, inherited environment keyboard
references and sample-model import assertions remain unresolved; full clean-install acceptance
still requires a later fresh-clone test after the remaining issues are addressed.

### September 7 external-review setup guide draft

`GHA-GETTING-STARTED.md` is the reviewer entry point, linked from the root README and UMA
package documentation. It covers public source revisions, Windows sync, integration menus,
local configuration, Desktop/PC Link/two-client checks and known limitations. It is a draft,
not a verified clean-machine install or approval to publish the current dirty worktree.

The initially identified fresh-index gap is now implemented in `GhaUmaAssetInstaller`:
when neither destination index exists, create and populate a project-owned index from the
standard installed `Assets/UMA/` content using UMA's rebuild API. Validate both human races
and slot/overlay/wardrobe entries before persisting it. Existing indexes are preserved;
wrong-type/unloadable occupied paths stop installation without replacement. Do not copy
the source-generated index. The new generated index and its metadata are ignored by Git.

Owner clarified that first-time reviewers should use only the default UMA assets and should
not have to manually rebuild a library. The guide now uses the automatic installer path,
contains no Ready Player Me reference, and identifies Robocopy as included with Windows.

Validation: Unity 6000.3.11f1 compilation passed. Isolated Edit Mode checks in
`Tools/ValidateUmaIndexInitialization.cs` created 488 default-UMA-only entries including both
human races, retained after asset unload/reload; preserved project/installation indexes and project-first precedence; rejected
wrong-type occupied paths; and preserved the development project's actual index. Temporary
test assets were removed. The initial CLI request exceeded Pipeline's 5-second response limit;
the queued test subsequently completed and its SessionState report was retrieved successfully.
No Play Mode test, complete installer rerun, clean-checkout acceptance, staging, commit or
publication was performed. The owner will run the fresh-repository test after including the
reviewed changes; cloning the currently published branch alone does not include this fix.
Pre-publication testing does not require a local commit. At the owner's request, supply the
intended uncommitted setup/UI files directly to the clean test clone, with a SHA-256 manifest
outside that repository; exclude credentials, installed vendor content, generated caches and
unrelated scene/project-setting edits. The base commit plus that manifest identifies the test
snapshot. Keep subsequent test fixes reviewable before committing or publishing.

The test-clone installer dry run exposed an additional first-install preflight defect:
`git check-ignore` did not match directory-only ignore rules when the vendor folders did not
exist yet. `Sync-Uma.ps1` now checks `Assets/UMA/` and `Assets/SourceShaders/` explicitly as
directories, retaining the separate metadata checks and the requirement that vendor content
be ignored. No placeholder vendor folders or bypasses are needed.

### September 7 correction: keep the source UMA checkout verbatim

Owner clarified that approval to use upstream develop shader files was not authorization to
commit a backport on the source UMA master. The earlier approval characterization below was
incorrect. The intended change was to GHA's installed UMA only.

Removed agent-created commit `0c69fee9b` from the source master using `git reset --keep`
after verifying exact HEAD, master branch, upstream parent and a clean working tree.
`E:/src/Unity/6000.3/UMA` now matches official upstream commit `c9204fe4`, with clean
index/working tree and 0 ahead / 0 behind its recorded origin/master. No revert commit, new
branch, push, fetch, ignored-file cleanup, or source configuration change was made. Git's normal
reflog still permits recovery; it does not add a commit to master.

GHA's existing 12 repaired shader files are retained unchanged. `Tools/Sync-Uma.ps1` again pins
official master `c9204fe4`, and for that baseline only exports exact shader blobs from official
develop `f4edf41ba` into destination staging. It never changes source files/index/branch or
requires `0c69fee9b`; no source `.gitattributes` alteration is needed. Missing upstream blobs
stop the import. Review/remove this baseline-specific overlay when advancing the master pin.
No full UMA sync or Unity Play Mode was run for this correction. GHA changes remain unstaged.
Verification: PowerShell parsing and `Sync-Uma.ps1 -ReplaceExisting -WhatIf` passed. Executed
the script's actual shader-export block against isolated staging under
`Logs/UpstreamReview/ShaderSyncValidation-35946e6d9ee54f6e9bdb5357393544b9`: all 12 exports
matched upstream Git blobs and GHA's installed SHA-256 values. Before/after hashes confirm all
12 GHA shaders were unchanged by restoring the source. Source master remained clean and 0/0
against recorded origin/master after validation. No new commits or pushes were made.

### September 7 human body-type selector (runtime acceptance pending)

`PROVISIONAL`: the Body tab now exposes Human Male / Human Female through the existing
selector. `UmaAvatarCatalog.asset` retains male race ID 0 and wardrobe IDs 0-16 unchanged;
female is appended at race ID 1, with seven female wardrobe entries at IDs 17-23. No vendor
content was modified. All clients must ship the same expanded catalog.

The catalog now owns explicit body-type labels and starter outfits. Outfit choices are filtered
by authored UMA race/cross-compatibility and wardrobe slots; missing compatibility metadata is
not treated as permission to equip. Shared hairstyles remain available to both types. Switching
retains compatible choices and restores each type's selections (including None) within the open
customizer session. DNA/color selections remain unchanged; Save Avatar persists the active
type through the existing wire recipe. New types must provide Humanoid RaceData, T-pose and
base recipe; that definition gate is not a substitute for generated-rig/VR acceptance.

Preview and puppet race changes stage wardrobe/DNA/colors while UMA building is disabled,
then enable one build. UMA's internal race-state cache is disabled on these GHA-owned avatars
because the GHA recipe owns their state. Desktop/VR arm policy is otherwise unchanged.

Verification: compilation completed with zero errors. `Tools/ValidateUmaHumanBodyTypes.cs`
passed isolated Edit Mode checks for stable IDs, compatibility rejection, body-type clicks and
wraparound, remembered outfits/None, shape/colors, wire encode/decode, all 17 exposed DNA names
on both races, and selector dimensions. The actual Body controls were rendered and inspected at
`Logs/AvatarStudio-Header-2026-09-07/body-type-verified.png`. A screenshot-cleanup error in the
first capture was corrected; the validation was rerun. `Tools/PlanUmaStreaming.cs` found zero
missing named dependencies, report `Logs/UMA-Streaming/content-plan-20260907-162859.json`.

Play Mode was not entered for this change; permission has been requested. Outstanding: actual
male/female build and back-switch, saving/reopening, Home/player reconstruction, animation,
head hiding and arm IK in VR, remote reconstruction, and device performance. The earlier compact
header and outfit-subsection highlight changes remain uncommitted alongside this work. Nothing
has been staged, committed or pushed for these September 7 UI changes.

### September 5 checkpoint and desktop-arm acceptance

The owner approved completing the staged VertexForm 1.1.9 merge and checkpointing
the six reviewed UMA follow-up files. Merge commit `be13ec6e` records the exact
13-file merge scope, with parents `18bdff38` and `58bf4e77`.

The UMA follow-up includes the reviewed sync pin and setup documentation, UMAKeepChain
and UMAIgnore tags, removal of delayed embedded-preview repositioning, and VR-only
arm IK creation/update. Leaving VR now releases existing arm constraints to the
Animator. Unity compilation completed without errors, and the owner explicitly
confirmed that desktop arm animations are fixed. The owner will recheck VR after
this checkpoint; VR regression acceptance remains PENDING.

Jacket thumbnail/material differences and lighting adjustments are explicitly
DEFERRED at the owner's direction. No material or lighting changes accompany the
arm fix. Sensitive Photon configuration, unrelated local scene/settings changes,
untracked authoring tools and generated files remain excluded and preserved.
No push is included in this checkpoint approval.

### September 5 UMA shader backport (source commit undone September 7)

Historical record, superseded by the September 7 correction above: the agent incorrectly
interpreted permission to use the shader fixes as permission to commit in the source repository.
Local master commit `0c69fee9b4871251d520ae2bfd84868b5a1646f3` contains the exact
12 shader graphs from `f4edf41ba1017b4a745fd1ba7286824332652b7d` plus
`UMAProject/.gitattributes` from `800ee5e99da1d7486bd7c7c777b9579f6221a3d5`.
Unrelated develop code, generated data, user settings and layouts were excluded.
At that time UMA was clean on master, one commit ahead of origin/master; no push was performed.
The local commit must not be described as available from the official remote.

`Tools/Sync-Uma.ps1` then pinned the local commit. The owner requested applying
the approved fixes immediately after reporting pink rendering. The exact 12 shader
files were copied from the owner's clean UMA checkout through the stopped GHA editor
in one AssetDatabase editing batch. No other vendor files or metadata were replaced.
All 12 installed graphs match source SHA-256, load as supported shaders, and report
zero ShaderUtil errors/messages. The skin material resolves to UMA3_SkinShader_URP;
the active pipeline is UniversalRenderPipelineAsset (UniversalRP-WebGL.asset).
No new console errors after cursor 153 (352 after import). No Play Mode visual test.
Backup retained outside Unity's temporary directory at
`Logs/UpstreamReview/ShaderBackup-20260905-151858`. The remaining
`SRP/ShaderGraphs/Materials/UMA_SG_Diffuse.shadergraph` is malformed on develop too
and is explicitly excluded from this backport. Detailed scope:
`Logs/UpstreamReview/UMA-SHADER-BACKPORT-REVIEW.md`.

### September 5 UMA master upgrade

Owner direction: use the existing `E:/src/Unity/6000.3/UMA` checkout, master only;
the owner pulled master. Source HEAD and origin/master are both
`c9204fe475b334162617da5ca923afcc6050e01e`, with a clean source working tree.
No source clone, fetch, pull, checkout, or source modification was performed by this upgrade.

`Tools/Sync-Uma.ps1` now pins that revision, requires the master branch, validates
the authoring allowlist during WhatIf, checks for an open destination editor, and
rechecks source cleanliness/revision before replacement. The Photobooth moved to
`SRP/Samples/Scenes`; its seven camera render textures remain under
`UMA3/Scenes/Prefabs/Textures`. Disposable sample scenes in both locations remain
excluded. The source-generated `AssetIndexer.asset` is explicitly excluded.

After the owner closed GHA, replacement completed: 5,511 UMA files and 83 source
shader files. All 5,594 copied files matched the owner's source checkout by SHA-256
before Unity import. Prior installation retained at
`Temp/UMA-Backup-20260905-100838`. C# compilation passed. Destination Global Library
rebuilt to 508 records. Catalog references (one race, 17 wardrobe entries, four
defaults and three palettes) resolve; the existing read-only recipe dependency
audit reports zero missing explicit slot/overlay dependencies.

Upgrade acceptance is blocked by 13 upstream shader graphs failing import with
JSON parse errors. Their source formatting contains blank lines inside JSON objects,
which Unity's MultiJson parser treats as object separators. No vendor repair or
older-file substitution was applied. Owner approval is required before repairing
the source UMA graphs. Evidence and file list:
`Logs/UpstreamReview/UMA-MASTER-IMPORT-2026-09-05.md`. GHA is open, Play Mode stopped;
runtime/platform acceptance remains pending.
The 13 staged VertexForm merge paths are unchanged; this upgrade is unstaged.

### September 5 checkpoint and VertexForm3D 1.1.9 merge review

The five owner-reviewed checkpoint commits are now local: `4f6e3173`, `72c1c3a8`,
`dd53e48f`, `13968802`, and `18bdff38`. The historical uncommitted-state descriptions
below refer to earlier work, not these saved checkpoints. Nothing has been pushed.

Official Master `58bf4e775653c8befbc6d36584742360dbc7033a` (1.1.9) has been fetched
and merged with `--no-commit`. All conflicts are resolved; 13 files are staged for
owner review before the merge commit. The UI database was preserved and saved
through Unity; all 14 world entries retain their Web-support values. A
`FormerlySerializedAs("WebGPU")` attribute accompanies the upstream `Web` rename.
Unity compilation passed; 10 stock avatars, both Studio providers, one embedded
panel, and preview position/scale `(0.9, -0.5, 0)` / `0.4` were verified. No Play
Mode acceptance test was run. UMA remains at its existing pin. Detailed evidence,
diagnostic limitations and exclusions: `Logs/UpstreamReview/VERTEX-1.1.9-VALIDATION.md`.
This status-note edit is not part of the currently staged 13-file merge scope.

### QuantumVertex source

| Item | Current state |
|---|---|
| Branch | `spike/uma-avatars` |
| Last committed HEAD | `994d9aa4cca49b98e333d19aaededd51fdcbb6b4` |
| HEAD description | `checkpoint: freeze functional avatar framework source` |
| Working tree | Avatar checkpoint is committed; unrelated local/project changes remain uncommitted and excluded |
| Frozen migration commit | `994d9aa4cca49b98e333d19aaededd51fdcbb6b4` |
| Immediate task | Treat this commit as read-only and compare target migration slices against it |

The July 12 handoff records `2e5aa974` as an accepted local-VR baseline. That is historical
evidence, not the final migration source. The later committed HEAD and the current uncommitted
working tree must be reconciled and checkpointed.

### GHA target

| Item | Current state |
|---|---|
| Branch | `Generic-Humanoid-Avatars` |
| Upstream-derived HEAD | `5257c76b7ebce05b43b8d43f4186ce7498a50bea` (official PR #55) |
| Working tree | Dirty; contains the partial GHA migration and unrelated local/project changes |
| GHA package | `Packages/com.vertexform3d.gha` |
| UMA adapter package | `Packages/com.vertexform3d.gha.uma` |
| Host-coupled staging | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration` |
| UMA vendor installation | `Assets/UMA` and `Assets/SourceShaders`, ignored |
| UMA sync script | `Tools/Sync-Uma.ps1` |

All 26 QuantumVertex AvatarSuite C# filenames have a counterpart in the target packages or staging
area. A normalized content audit still reports all 26 as different. Some differences are expected
package/assembly/accessibility changes, but parity has not been established. Treat every migrated
class as `PROVISIONAL` until compared against the frozen source commit.

### UMA baseline

| Item | Value |
|---|---|
| Supported release | UMA NextGen 3.0f4 (`v3.04`) |
| Pinned source commit | `722b308aebfe5dee7d0048e24c1c464c00b5fc06` |
| Upstream feature overlay | None; Icon Creator/Sprite Atlas PR #564 is included upstream in the pinned release |
| Source layout | `UMAProject/Assets/UMA` plus `UMAProject/Assets/SourceShaders` |
| Destination | `Assets/UMA` plus `Assets/SourceShaders` |
| Source-code patch delta from old QuantumVertex UMA | None to reapply |
| Local delta | Purpose-based pruning policy, retained current Markdown docs and selected UMA authoring tools, preserved top-level shader sources, and regenerated Global Library |

`Tools/Sync-Uma.ps1` verifies the commit, stages the copy, applies the pruning policy, and preserves
a recoverable backup during replacement. Its retained `UMA_INSTALLED` call is temporarily commented
out because AnkleBreaker's optional UMA bridge targets UMA 2 and does not compile against UMA 3.

### Upstream Icon Creator and Sprite Atlas overlay

Status: `REMOVED -- INCLUDED UPSTREAM`

UMA PR #564 was merged into `develop` as
`0440f163c8c08253da279fa4f3872fd92a4f2e87`. It makes thumbnail output deterministic, makes Icon
Dimensions functional, adds capture supersampling, and adds the optional race-and-region Sprite
Atlas V2 workflow plus tests and documentation.

UMA v3.04 contains PR #564 and its later fixes, tests, and documentation. On 2026-08-18 the sync
pin was advanced to release commit `722b308aebfe5dee7d0048e24c1c464c00b5fc06`; the feature archive
overlay, documentation-index injection, and feature-commit parameter were removed. The installed
Icon Creator files now come directly from the pinned release.

The ignored GHA UMA installation was rebuilt from a clean detached v3.04 worktree at
`E:/src/Unity/6000.3/UMA-v3.04-sync`. The previous installation remains recoverable at
`Temp/UMA-Backup-20260818-095256`. On 2026-08-25 Unity `6000.3.11f1` completed package resolution
and compilation, the Global Library was rebuilt in place, and the GHA UMA catalog validator passed.
Runtime validation remains pending.

The UMA Core suite was requested in EditMode and completed with 216 tests: 212 passed, three failed,
and one skipped. During its post-test assembly refresh, Unity nevertheless logged an internal
test-runner transition (`Entering playmode with assembly reload locked`); no application runtime
gate was performed. The skipped test covers the intentionally absent optional UMA2 package. Two
failures expect sample content deliberately removed by the sync pruning policy (`UMA3/Scenes` and
`UMA3/RandomCharacters`). The remaining release validator failure reports 11 unresolved optional
HDRP/ShaderGraph script GUIDs in a URP target that does not install those packages. These retained
baseline/pruning-policy exceptions did not produce compiler errors and are separate from the GHA
catalog validation. After the owner reopened the project on 2026-08-26, the exact editor was ready,
Play Mode was stopped, and recompile status completed with no errors.

### UMA documentation and shader-source completeness

Status: `ACTIVE`

The initial pruning policy excluded `Assets/UMA/Docs`, but UMA's Documentation Browser searches
that exact directory. The sync now retains the current Markdown documentation and validates
`Docs/!README.md` plus `Docs/UMAMaterial.md`. The archival `UMA3/Documentation` PDF tree remains
excluded.

UMA 3 also keeps shader authoring sources in the sibling `UMAProject/Assets/SourceShaders` tree,
outside `UMAProject/Assets/UMA`. The shipped `.umaShaderPack` assets contain direct GUID references
to those sources, including diffuse-alpha hair shaders. The sync therefore copies
`Assets/SourceShaders` with its original metadata, validates representative diffuse-alpha hair and
opaque surface-shader sources, and requires both the directory and its `.meta` file to remain
ignored.

UMA v3.04 fixed the former shader-folder issue upstream. `UMAPathUtility.ShaderPackagesRelativePath`
now resolves `SRP/ShaderPackages`, `UMASettings` migrates the legacy value, and the Welcome window
uses that shared path logic. The pinned `WelcomeToUMA.cs` text correction was removed from the sync
on 2026-08-18.

### UMA authoring-tool completeness

Status: `ACTIVE`

UMA content must not be classified as disposable solely because it is stored beneath a demo or
sample directory. Apply these categories before pruning a pinned UMA tree:

- runtime-required content;
- authoring, repair, and diagnostic tooling;
- current documentation and shader/source material;
- disposable demonstrations and obsolete examples.

The original sync excluded the complete `UMA3/Scenes` tree. That removed
`U3-Tools-Photobooth.unity` even though the retained `IconCreator` component depends on that scene
for regenerating incorrect wardrobe thumbnails. The Photobooth also depends on its complete lighting
subfolder and the body, chest, face, feet, hands, head, and legs camera render textures beneath
`UMA3/Scenes/Prefabs/Textures`.

`Tools/Sync-Uma.ps1` continues to omit the approximately 196 MB UMA3 sample-scene tree, but restores
the pinned Photobooth dependency set from an explicit allowlist and fails if any required path is
missing. The retained set is approximately 0.6 MB. Whenever the UMA pin changes, re-audit that
allowlist against the Photobooth scene before accepting the new commit.

Icon generation remains an intentional authoring operation rather than an automatic sync step:

1. Open `Assets/UMA/UMA3/Scenes/U3-Tools-Photobooth.unity`.
2. Set the Photobooth avatar to the race being repaired.
3. Enter Play Mode and select **Generate All Icons**.
4. Review the generated PNGs and the modified `UMAWardrobeRecipe.wardrobeRecipeThumbs` entries.

This isolated UMA authoring workflow is the sole exception to the normal requirement to enter
VertexForm3D Play Mode from `LoginScene`. Stop Play Mode and reopen `LoginScene` before any
VertexForm3D runtime validation.

`IconCreator` saves PNGs beneath `Assets/<rootFolder>/<region>/<race>` and updates the matching
race-specific thumbnail entry on each wardrobe recipe. Because `Assets/UMA` is ignored vendor
content, locally regenerated recipe references are not a distributable GHA fix by themselves.
Before publication, either upstream corrected thumbnails to UMA, pin a source commit containing
them, or provide an explicitly reviewed GHA-owned regeneration/override workflow.

### Temporary UMA 3.0.2 player-build compatibility guard

Status: `REMOVED -- FIXED UPSTREAM`

UMA 3.0.2 commit `dbf76d8e815e60accbf3613c9232190b93b65e31` places these editor-only
custom inspectors beneath the all-platform `UMA_Core.asmdef` without `UNITY_EDITOR` guards:

- `Assets/UMA/Core/Physics/Editor/Jiggle/JiggleBreastEditor.cs`;
- `Assets/UMA/Core/Physics/Editor/Jiggle/JiggleButtEditor.cs`.

Unity therefore includes them in the `UMA_Core` player assembly and fails Windows
Addressables/player compilation because `UnityEditor.Editor` and `CustomEditor` are unavailable.
The clean pinned UMA checkout and the installed files were byte-identical. The official UMA
`develop` head `7449f76dd662cb7ebe0e5d68960e15a43f2241b2` was also inspected on 2026-07-27:
it remains affected, has no editor-only assembly definition/reference at this path, and is
divergent from `master` (four commits ahead and three behind).

User-approved resolution on 2026-07-27: wrap both installed files in `#if UNITY_EDITOR` and have
`Tools/Sync-Uma.ps1` reproduce the guards after copying the pristine source. The sync script ties
the workaround to the affected commit and deliberately stops if the UMA pin changes, forcing this
section and the workaround to be reviewed instead of carrying the delta forward silently.

UMA v3.04 wraps both custom inspectors in `#if UNITY_EDITOR`. On 2026-08-18 the sync's
`Add-UnityEditorCompatibilityGuard`, affected-commit pin, guarded-path list, and related staging and
reporting logic were removed. Windows Addressables/player validation remains required after import,
but the installed files now come directly from upstream without this patch.

### Resolved compile blocker

Status: `RESOLVED`

Enabling `UMA_INSTALLED` causes the installed AnkleBreaker package
`com.anklebreaker.unity-mcp@4c03a9601a8c` to compile its UMA bridge. Against UMA 3.0.2, that bridge
currently produces 30 compiler errors in `MCPUMACommands.cs`, including:

- `UMASlotProcessingUtil.SlotBuildResult` versus `SlotDataAsset`;
- removed or changed `SlotDataAsset.material` and `materialName` APIs;
- read-only `slotName`, `overlayName`, and `raceName` properties;
- changed `UMAAssetIndexer.GetAllAssets` overloads.

This was an AnkleBreaker UMA-version compatibility blocker, not evidence that the GHA runtime
adapter had failed. Inspection confirmed that UMA does not consume `UMA_INSTALLED`; UMA creates it
only as a third-party presence indicator. AnkleBreaker consumes it to compile its UMA 2 command
bridge, route registration, and self-test.

User-approved resolution on 2026-07-27: leave `UMA_INSTALLED` undefined, retain the disabled
auto-add code with an explanatory note, and continue using the rest of AnkleBreaker. Do not patch
`Library/PackageCache`. Revisit the define after AnkleBreaker supports UMA 3 or offers a dedicated
UMA integration opt-out. Unity then reported zero compilation errors through AnkleBreaker.

The AnkleBreaker advanced-tool catalog continues to advertise UMA commands while their guarded
routes are absent; invoking `unity_uma_list_global_library` returned
`Unknown API endpoint: uma/list-global-library`. The user approved treating AnkleBreaker's UMA
category as unavailable for the current migration. Continue using AnkleBreaker for non-UMA Unity
work, and do not interpret advertised UMA tool metadata as a callable UMA 3 integration.

### Target dependency record

Recorded 2026-07-27; VertexForm3D baseline advanced 2026-08-25:

| Dependency | Exact target version |
|---|---|
| Unity | `6000.3.11f1 (3000ef702840)` |
| VertexForm3D target | `5257c76b7ebce05b43b8d43f4186ce7498a50bea` (Starter Kit `1.1.7`) |
| UMA | NextGen `3.0f4` (`v3.04`) at `722b308aebfe5dee7d0048e24c1c464c00b5fc06`; Icon Creator PR #564 included upstream |
| AnkleBreaker | package `2.39.4` at `4c03a9601a8c5133bfca1b580b23866a96b0b603` |
| Unity Pipeline | package `0.5.0-exp.1` |
| Photon Fusion | `2.0.9` |
| GHA | embedded `com.vertexform3d.gha` `0.1.0` |
| GHA UMA adapter | embedded `com.vertexform3d.gha.uma` `0.1.0` |
| Animation Rigging | `1.4.1` |
| XR Hands | `1.7.3` |
| XR Interaction Toolkit | `3.3.1` |
| XR Management | `4.5.4` |
| Meta OpenXR | `2.4.0` |
| OpenXR | `1.16.1` |

The local target branch was fast-forwarded from `346d84c3c47dcd0e0a1800ede649d05028ac7228`
to official Starter Kit PR #55 at `5257c76b7ebce05b43b8d43f4186ce7498a50bea` on 2026-08-25.
`origin/Generic-Humanoid-Avatars` remains at the older baseline, so the local branch is four commits
ahead solely because of the fetched upstream history. Migration work remains uncommitted. Nothing
was staged, committed, stashed, or pushed during the integration.

### Upstream Starter Kit PR #55 integration

Status: `DONE`

The clean reference checkout at `E:/src/Unity/6000.3/vertexform3d-unity-vr-starterkit` was on
`Master` at `5257c76b7ebce05b43b8d43f4186ce7498a50bea`. Relative to the former GHA baseline,
the official update changed six files:

- removed the unused `com.cesium.unity` dependency and its lock entry;
- removed Moon Terrain from the local Addressables scene group;
- advanced `Project Data SO.asset` from package version `1.1.6` to `1.1.7`;
- added the Tutorials link to `VertexForm3DHelp`;
- corrected `VertexFormSDKMenu` to search for `SettingsUISO` and reuse the asset at the expected
  path before trying to create it.

Only `Packages/manifest.json` and `Packages/packages-lock.json` overlapped the dirty GHA migration.
The reconciliation accepts the upstream Cesium removal while retaining the GHA-owned AnkleBreaker
dependency and the embedded `com.vertexform3d.gha` and `com.vertexform3d.gha.uma` lock records.
The four non-package files match the clean reference checkout after line-ending normalization.
Unity `6000.3.11f1` subsequently resolved 80 packages, completed compilation, and reached an idle
editor state with no relevant compiler errors. Pipeline `0.5.0-exp.1` was added to the resolved
lock; Cesium remained absent and the retained AnkleBreaker/GHA records remained intact.

### Target dirty-file ownership record

Recorded 2026-07-27:

| Classification | Files or paths | Handling |
|---|---|---|
| GHA migration | `Packages/com.vertexform3d.gha/**`, `Packages/com.vertexform3d.gha.uma/**`, `Assets/VertexForm3D/3rdPartyAssets/GHA/**` | Keep migration-scoped and review by package boundary. |
| Minimal host seams | `AvatarInputConverter.cs`, `AvatarSelectionManager.cs`, `PlayerNetworkSetup.cs`, `SitSpot.cs`, `SceneLoader.cs` | Reconcile against the frozen QuantumVertex commit; keep independently reviewable. |
| Required package/setup wiring | `.gitignore`, `Packages/manifest.json`, `Packages/packages-lock.json`, `Tools/**` | Keep; excludes UMA vendor content, pins tooling, registers embedded packages, and supplies the reproducible UMA/install workflow. |
| UMA-owned project delta | `ProjectSettings/TagManager.asset` | UMA 3.0.2's `Assets/UMA/Core/Editor/Scripts/ImportProcessor.cs` unconditionally ensures `UMAIgnore` and `UMAKeepChain` during asset post-processing. The tags remain while UMA is installed and are not owned or removed by either GHA integration layer. Exclude this file from a GHA-only change set unless the separately reviewed UMA installation policy explicitly includes it. |
| Migration documentation | `AGENTS.md`, `GHA-MIGRATION-RUNBOOK.md`, package documentation, staging `README.md` | Keep with the migration. |
| Credential-bearing local configuration | `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` | Never stage or commit. |
| Generated/editor state | `Assets/XR/Settings/OpenXR Package Settings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, `ProjectSettings/URPProjectSettings.asset`, `vertexform3d-unity-vr-starterkit-GHA.slnx` | Exclude unless a later gate proves a specific functional setting change is required. Current tracked settings changes are line-ending-only after removing `UMA_INSTALLED`. |
| Unrelated local content | Ocean Villa `diet-healthy-...tint0xF4B81CFF.jpg` and `.meta` | Preserve locally; exclude from migration. |
| Unrelated editor cleanup | deleted `VertexForm3DPackageExporter.cs.meta` | Exclude pending separate cleanup; it is not part of GHA migration behavior. |
| Ignored vendor state | `Assets/UMA/**`, `Assets/UMA.meta` | Never commit; reproduce from the pinned checkout. The local `ImportProcessor.cs` opt-out is a machine-local safeguard. |

## Ownership boundaries

### VertexForm3D core

Core may contain only minimal, inert, provider-neutral seams. Candidate migrated changes include:

- `PlayerNetworkSetup.AvatarConstructionOverride`;
- provider-neutral avatar/posture state access and seating events;
- `AvatarSelectionManager.SuppressLegacyAvatars`;
- generic seated correction support in `AvatarInputConverter`;
- `SitSpot` occupancy and `SeatedPelvisTarget`;
- the generic `SceneLoader` EventSystem sweep/activation retry;
- provider-neutral core seams only; Fusion-specific `AvatarExtensionSync` remains in the
  VertexForm host-integration layer.

Core must not reference UMA, GHA provider implementations, recipes, wardrobe, DNA, or UMA catalogs.

### `com.vertexform3d.gha`

Owns provider-neutral Unity Humanoid behavior:

- contracts and capability surfaces;
- calibration and physical-fit policy;
- rig input abstraction;
- arm IK and future generic leg/finger/face contracts;
- locomotion and posture;
- tracking lifecycle and XR startup stabilization;
- presentation and camera visibility;
- provider-neutral network payload/posture structures;
- generic diagnostics;
- eventually the provider-neutral avatar configuration panel shell.

No file in this package may import UMA.

### `com.vertexform3d.gha.uma`

Owns UMA-specific behavior:

- DCA construction and rebuild lifecycle;
- catalog, recipes, DNA, wardrobe, shared colors, and persistence;
- renderer discovery and head separation;
- UMA customizer controls;
- UMA validation/import setup tooling.

It depends on GHA and a separately installed UMA checkout. It never vendors UMA source.

### Host-coupled integration staging

`Assets/VertexForm3D/3rdPartyAssets/GHA/Integration` temporarily owns classes that still directly
depend on current VertexForm3D implementation types such as `PlayerNetworkSetup`,
`AvatarInputConverter`, `ProjectManager`, `RoomManager`, `SitSpot`, and the legacy Home station.

Files leave staging only after stable host contracts replace those direct dependencies.

### Project wiring

Scenes, player-prefab component wiring, UI layout assets, catalog assets, Animator assets, build
profiles, Photon versioning, and platform settings are project integration. They are neither
generic framework source nor UMA vendor source.

## Ordered migration phases

Do not skip a gate because a later phase appears to compile.

### Phase 0 -- Freeze QuantumVertex

Status: `DONE`

Actions:

1. Stop new timing analysis and performance optimization in QuantumVertex.
2. Preserve the existing timing diagnostics and known delay observations without attempting to
   resolve them.
3. Confirm the current functional source compiles and run only the minimum smoke test needed to
   ensure the checkpoint is coherent.
4. Record known functional limitations separately from deferred performance concerns.
5. Separate unrelated user changes from the avatar checkpoint without deleting or reverting them.
6. Commit the current avatar source, required host hooks, assets, prefabs, and scenes.
7. Record the final commit below and make QuantumVertex read-only for migration.

Required record:

```text
Frozen QuantumVertex commit: 994d9aa4cca49b98e333d19aaededd51fdcbb6b4
Checkpoint date: 2026-07-27
Minimum functional smoke test: Zero-error Unity compile plus project-owner acceptance from the
last known-good functional VR test; a current headset was unavailable and no new performance claim
was made.
Known functional limitations: Recorded in AVATAR-FRAMEWORK-CHECKPOINT-2026-07-27.md at the frozen
commit and carried into the original-plan reconciliation ledger below.
Performance work: Deferred until post-migration Phase 8
```

Gate 0:

- a named, immutable QuantumVertex commit exists;
- the avatar-relevant working tree at that commit is understood;
- known functional limitations and the performance deferral are written down;
- no target parity claim uses an uncommitted source snapshot.

### Phase 1 -- Restore a trustworthy GHA baseline

Status: `DONE`

Actions:

1. Resolve the AnkleBreaker/UMA 3.0.2 compatibility failure.
2. Confirm `UMA_INSTALLED` remains intentionally undefined until AnkleBreaker supports UMA 3 or
   provides a dedicated integration opt-out.
3. Obtain zero GHA/UMA compile errors.
4. Inventory and classify every dirty target file as migration, required local setup, generated
   state, credential-bearing configuration, or unrelated user work.
5. Record exact versions for Unity, VertexForm3D target commit, UMA, AnkleBreaker, Fusion, XR
   packages, and GHA packages.
6. Confirm the target branch still starts from the intended upstream
   `Generic-Humanoid-Avatars` history before creating migration commits.

Gate 1:

- clean compilation;
- no unknown dirty-file ownership;
- credentials and ignored vendor content are excluded;
- dependency versions are recorded.

### Phase 2 -- Reproduce the external UMA installation

Status: `DONE`

Actions:

1. Run `Tools/Sync-Uma.ps1 -WhatIf`.
2. Run the pinned UMA v3.04 installation/replacement only after reviewing the plan.
3. Confirm `Assets/UMA` and `Assets/UMA.meta` remain ignored.
4. Rebuild the UMA Global Library through compatible editor tooling.
5. Verify all races, wardrobe recipes, materials, and palettes referenced by
   `UmaAvatarCatalog.asset`.
6. Record the installed commit and pruning result.

Completion evidence recorded 2026-08-25:

- installed source commit `722b308aebfe5dee7d0048e24c1c464c00b5fc06` with the documented
  purpose-based pruning policy;
- Unity `6000.3.11f1` completed package resolution and compilation;
- the ignored Global Library index was rebuilt rather than copied, yielding 146 slots, 114
  overlays, 11 races, 10 text recipes, 162 wardrobe recipes, 34 UMA materials, and one mesh-hide
  asset;
- the GHA UMA catalog validator passed for one race, 17 wardrobe entries, four default wardrobe
  entries, three color channels, 26 slot references, 28 overlay references, and seven unique
  materials;
- `Assets/UMA` and the rebuilt index remain ignored; no UMA vendor files were staged.

Gate 2:

- reproducible UMA installation from the pinned checkout;
- Global Library rebuilt rather than copied;
- catalog references resolve;
- no UMA vendor files are staged.

### Phase 3 -- Establish file-by-file parity and package placement

Status: `DONE`

For each source file at the frozen QuantumVertex commit:

1. Classify it as core, generic GHA, UMA provider, host staging, project wiring, diagnostic, test,
   or obsolete.
2. Compare source behavior with the target counterpart.
3. Reapply missing behavior intentionally; do not overwrite target package adaptations blindly.
4. Rename remaining QuantumVertex/QV identifiers to GHA where they describe this product.
5. Preserve provider names such as `Uma*` when the class is genuinely UMA-specific.
6. Preserve Unity GUIDs for moved assets where references depend on them.
7. Record the result in the migration ledger.

Special checks:

- generic GHA contains no UMA imports;
- provider-neutral contracts do not depend on the UMA package;
- host adapters do not leak into generic runtime assemblies;
- asmdef references express the intended dependency direction;
- animation controller parameter/state names remain synchronized with runtime code;
- catalog wire IDs remain append-only.

Gate 3:

- every QuantumVertex AvatarSuite source and asset is accounted for;
- no item is omitted without a documented `SUPERSEDED` or `DEFERRED` decision;
- package boundaries compile independently as designed.

### Phase 4 -- Reconcile minimal VertexForm3D core changes

Status: `PROVISIONAL`

Actions:

1. Diff each target core file against the current upstream branch, not against old QuantumVertex
   wholesale.
2. Port only the smallest provider-neutral behavior required by GHA.
3. Keep each core seam inert when GHA and UMA are absent.
4. Add null/bounds guards needed to keep stock avatars functional.
5. Keep `AvatarExtensionSync` in the stable VertexForm host-integration layer; do not add Fusion
   to the provider-neutral GHA runtime assembly.
6. Verify stock-only behavior before enabling an external provider.

Implementation reconciliation completed on 2026-07-27:

- `PlayerNetworkSetup` exposes an optional avatar-construction delegate, seated-state event/current
  seat access, and XR-safe posture height handling. All paths retain stock behavior when no
  integration subscribes.
- `AvatarSelectionManager.SuppressLegacyAvatars` defaults to `false`; the stock preview path is
  unchanged unless an external integration explicitly assumes ownership.
- `AvatarInputConverter` preserves/restores the complete camera offset across seated correction,
  keeps the seated avatar facing the seat, and exposes a horizontal-only view-correction seam.
- `SitSpot.SeatedPelvisTarget` is optional and includes an editor gizmo; `SitPoint` remains the
  player/root anchor.
- `SceneLoader` raises a provider-neutral transition event, waits up to ten seconds for the
  additive world scene to resolve, and disables world-scene `EventSystem` objects after activation
  so the persistent host scene remains the UI-input owner.
- `AvatarExtensionSync` was moved to
  `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/` with GUID
  `989f755490ee91249b0d5fa7b310dd94` preserved. The generic GHA runtime asmdef no longer references
  `Fusion.Unity`.
- The QuantumVertex `RoomManager` scene-timing call was deliberately excluded because it belongs to
  deferred performance diagnostics and depends on timing APIs not carried into the target.

The five modified core scripts contain no UMA or GHA provider types, `git diff --check` reports no
content errors, and Unity 6000.3 compiles with zero errors or warnings. Phase 4 remains
`PROVISIONAL` only until the stock-only runtime gate is exercised in the target project.

Gate 4:

- core diff is small, generic, and independently reviewable;
- stock avatars work with GHA absent or disabled;
- no UMA type appears in core.

### Phase 5 -- Migrate project wiring and UI

Status: `IN PROGRESS`

Do not copy entire QuantumVertex scenes or prefabs over the newer VertexForm3D versions.
Reapply the required components and references through the Unity editor.

Actions:

1. Wire both player prefabs with the same provider-neutral network and host contracts.
2. Reconcile Home avatar host/customizer wiring.
3. Reconcile seat occupancy, `SitPoint`, and optional `SeatedPelvisTarget` authoring.
4. Assign and verify the GHA locomotion/posture Animator controller.
5. Keep the existing Change Avatar button as the sole avatar-configuration entry point. Do not
   register Avatar as an enabled `UILayoutConfig` Custom entry, because the documented baker turns
   every enabled entry into a bottom-navigation tab.
6. Replace scene-specific UMA UI assumptions with a provider-neutral GHA avatar panel shell.
7. Preserve optional UMA controls as provider-contributed content.
8. Set a distinct Fusion AppVersion before any external add-on deployment.
9. Keep Photon App IDs and credentials local and unstaged.

Player-prefab wiring completed through AnkleBreaker on 2026-07-27:

- `NewGenericMRVRPrefab.prefab` and `NewGenericMRDesktopPrefab.prefab` each carry
  `VertexFormCore.AvatarExtensionSync` and `GHA.AvatarSuite.UmaAvatarBridge` on the root beside the
  existing `PlayerNetworkSetup` and `NetworkObject`.
- Each bridge resolves
  `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset` and
  `Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion_v2.controller`.
- Bridge defaults match on both prefabs: local VR cull layer `7`, moving turn speed `720`, idle
  turn speed `200`, and movement-facing threshold `0.6`.
- Unity compiled the result with zero errors or warnings.

Fusion bake verification completed on 2026-08-26 through Unity's supported Pipeline CLI. Each
player prefab was loaded into an isolated prefab-editing scene, processed by Fusion's official
`NetworkObjectBakerEditTime`, inspected, and unloaded without saving. Both
`NewGenericMRDesktopPrefab.prefab` and `NewGenericMRVRPrefab.prefab` returned `hadChanges=false`, 12
network objects, 20 network behaviours, and zero null baked entries. `AvatarExtensionSync` was
present in each baked behaviour table; `UmaAvatarBridge` correctly remained outside the table
because it is not a `NetworkBehaviour`. This supersedes the earlier AnkleBreaker inspection anomaly
(`Index was outside the bounds of the array`, ticket ID `8`) without routing to another project or
modifying either prefab.

The previously blank Photon Fusion AppVersion was set locally to `gha-migration-20260826` on
2026-08-26 so migration test sessions do not share the default matchmaking partition. This changed
only the non-secret AppVersion field through Unity serialization. The credential-bearing
`PhotonAppSettings.asset` remains unstaged and must remain outside any publishable change set.

Current avatar-management UI:

- `UmaAvatarCatalog.asset` has provider-level `Enable Stock Avatars`,
  `Enable UMA Avatars`, and `Default System` fields.
- `Vertex Form > Main UI Database > Right Section > Avatar Datas` manages the stock avatar list,
  but has no per-avatar enabled flag.
- the runtime customizer shows UMA/Classic selection when both providers are enabled.
- `Vertex Form > Creator Toolkit > Avatars` only places the 3D selection rig.

Change Avatar integration corrected through the idempotent GHA installer on 2026-07-28:

- The provider-neutral `GHAAvatarPanel.prefab` is embedded beneath the existing
  `AvatarSelectionManager.customAvatarSelectionUI` station in `HomeSceneComponent.prefab`.
- The existing Change Avatar and close flow is retained. The obsolete station Previous/Next
  controls are hidden because the Stock provider now owns those controls.
- The earlier `UILayoutConfig` Custom Avatar entry was removed and both menu-shell prefabs were
  rebaked. Neither `MainMap.prefab` nor `MainMap Desktop.prefab` contains an Avatar screen or
  bottom-navigation Avatar button.
- A second installer run was a no-op: panel/config/bake/Home-prefab change flags were all false.
- Unity compiled with zero errors.

Reversible integration lifecycle verified on 2026-07-28:

- `Tools > GHA > Integration > Install GHA Host Layer` applies the provider-neutral patch to the
  five VertexForm seam scripts, enables `VERTEXFORM_GHA_HOST`, creates the panel shell, embeds it
  beneath the existing Change Avatar station, and adds `AvatarExtensionSync` to both player
  prefabs.
- `Tools > GHA > Integration > Install UMA Provider Layer` requires the host layer, enables
  `VERTEXFORM_GHA_UMA`, contributes the UMA panel provider and Home preview/host, and adds
  `UmaAvatarBridge` to both player prefabs. It never defines or consumes `UMA_INSTALLED`.
- Uninstall in reverse order. Each layer records exact pre-install snapshots beneath
  `UserSettings`, outside the repository. An unchanged installed asset is restored byte-for-byte;
  if a user changed it after installation, the uninstaller performs a surgical removal of only
  the layer-owned contributions.
- A complete host-plus-UMA install/uninstall audit restored the original Home and player prefabs
  and all five patched host scripts. The only original tracked deltas left after the audit were
  the independent `AddressablesDownloader` fix, independent `SceneLoader` world/EventSystem work,
  and the two tags continuously ensured by UMA's own import processor.
- The final reinstall completed with zero compilation errors. AnkleBreaker serialization checks
  confirmed `AvatarExtensionSync` and `UmaAvatarBridge` on both player roots, all three providers
  on `GHAAvatarPanel.prefab`, `UmaHomeAvatar` on the Home XR rig, and `GHA UMA Preview` at local
  position `(0.9, -0.50, 0)` and local scale `(0.40, 0.40, 0.40)`.

Unity domain reloads triggered by the layer symbols must be run with the editor in the real Windows
foreground on this workstation. Start `Tools/Keep-UnityCompileForeground.ps1` for the target Unity
process before selecting an install/uninstall menu item, wait until AnkleBreaker reports
`isCompiling: false`, then stop that exact helper process. With the helper active before the symbol
change, the final UMA-provider compilation completed in under one minute.

Desktop smoke correction and verification on 2026-07-28:

- The first Login smoke exposed a null in `LoginManager.ConnectToServer`: the live unpacked
  `LoginScene` copy of `ProjectManager.settingsUI` was null even though
  `LoginSceneComponent.prefab` still referenced `SettingsUI.asset`.
- The scene object was explicitly reassigned to
  `Assets/VertexForm3D/ScriptableObjects/SettingsUI.asset` through Unity and the scene was saved.
  Reopening `LoginScene` from disk confirmed that the reference resolves.
- Connect Anonymously then loaded `HomeScene`; Change Avatar opened the provider-neutral Stock/UMA
  panel; Close Avatar returned to Home; the bottom-nav Avatar entry was absent; and the Console
  remained at zero errors for the full path.
- Flat Web/Desktop UMA grounding now accounts for the host `CharacterController.skinWidth`.
  AnkleBreaker measured the pre-fix UMA root/capsule bottom at `Y=-0.102495` and the live floor
  collider at `Y=-0.182495`, an exact `0.08 m` gap matching the Home rig's skin width. The
  first non-XR correction anchored to `capsule bottom - skinWidth`, but visual inspection showed
  the shoe sole slightly penetrating the floor. The current formula adds a serialized
  `flatAvatarGroundClearance` of `0.02 m`, yielding
  `capsule bottom - skinWidth + max(0, clearance)`. It compiles with zero errors; the revised shoe
  placement still requires a visual Desktop/Web retest. WebXR remains on `HumanoidVrAlignment`
  and does not execute this flat-screen correction.

Remaining Phase 5 follow-up:

- visually confirm the revised `0.02 m` flat-platform footwear clearance;
- run the stock-only install/runtime gate after uninstalling the optional UMA provider layer;
- retain provider registration and provider-specific configuration as separate concerns when
  adding future providers.

Gate 5:

- scene/prefab wiring is recreated on the current upstream assets;
- the Home configuration flow works without a scene-specific hard dependency on UMA;
- stock and UMA provider policy can be authored without editing code;
- no credentials are staged.

### Phase 6 -- Runtime validation

Status: `PENDING`

Partial Desktop evidence accepted on 2026-08-26: `LoginScene` anonymous login reached `HomeScene`;
Change Avatar opened the refreshed Avatar Studio; the Classic and Custom providers were both
present; switching Custom -> Classic -> Custom completed; Classic recovered three active preview
renderers; and Custom retained one active preview renderer. Final close-up and player-view captures
are under `Assets/Screenshots/GHA_Avatar_Studio_*_Final.png`. Compilation completed with no errors
and Play Mode was stopped afterward. This does not satisfy stock-only, UMA-only, save/persistence,
spawn/remote, two-client, scene-transition, seating, platform, or device cases. Repeated pre-existing
XR Affordance receiver and `AvatarInputConverter.Update` null-reference errors also remain outside
this UI slice.

Classic control follow-up passed on 2026-08-28 through the same real login/player flow. Next changed
the saved stock index 4 -> 5 and rebuilt the preview as `Head6(Clone)`/`Body6(Clone)`; Previous
returned 5 -> 4 and rebuilt it as `Head5(Clone)`/`Body5(Clone)`. Classic and Custom now both use the
label `SAVE AVATAR` and the same live RectTransform values (176 x 56, bottom-right anchor,
anchored position (-12, 12)). Compilation completed without errors, the final Classic capture was
refreshed, and Play Mode was stopped. Save/persistence beyond this in-session control check remains
part of the pending matrix.

Preview composition follow-up passed on 2026-09-01. Classic uses the provider-neutral
renderer-bounds fit after tab selection and every Previous/Next rebuild, centering each stock model
and uniformly scaling it to 94% of the usable backdrop. UMA uses a provider-specific framing
policy: preserve the authored preview scale (`GHA UMA Preview` at 0.40 and DCA child scale 1.0),
center rendered bounds horizontally, and align the rendered feet to the preview bottom after every
character update. This makes the height slider grow the body upward from planted feet and permits
top overflow. The final card layout moves the provider caption into the top header, extends the
usable preview floor to 3.5% above the actual card edge, and leaves about 8.6 px of visible shoe
clearance at both center and maximum height. Center/max-height horizontal error was 0.081/0.029 px,
and projected height grew from about 247 px to 389 px. The shared Classic/UMA preview-card
backdrop is opaque black. Retained evidence is under
`Assets/Screenshots/GHA_Avatar_Studio_UMA_Position_Check.png`,
`GHA_Avatar_Studio_UMA_Final.png`, and `GHA_Avatar_Studio_UMA_Max_Height_Check.png`;
`GHA_Avatar_Studio_Classic_Final.png` was refreshed. Compilation passed and Play Mode was stopped.

Reference-layout follow-up passed on 2026-09-03. The lower Avatar Studio now has a provider-owned
category rail on the left, opaque-black live preview in the centre, and active controls on the
right. Provider and card backgrounds use generated nine-sliced rounded rectangles; the Classic
Avatar tile and Custom Body/Face/Colors/Outfits tiles have visible borders and selected states.
Provider tabs remain at the top and both providers retain the matching bottom-right `SAVE AVATAR`
action. The live `LoginScene` -> anonymous `HomeScene` pass confirmed preview RGBA `(0, 0, 0, 1)`,
Custom category switching, and Classic Next 4 -> 5 / Previous 5 -> 4. The Console returned zero
errors. Evidence is `Assets/Screenshots/GHA-avatar-studio-reference-layout.png` and
`Assets/Screenshots/GHA-avatar-studio-classic-three-column.png`; compilation passed and Play Mode
was stopped.

Run from `LoginScene` and retain evidence for each case:

| Case | Required result |
|---|---|
| Stock-only policy | Stock selection, spawn, remote rendering, and persistence work without UMA |
| UMA-only policy | UMA selection, customization, save, spawn, and remote reconstruction work |
| Mixed policy | Player can choose UMA or Classic; both render correctly in the same room |
| Home -> Ocean -> Home -> Ocean | Avatar size is invariant; only grounding/IK is re-established |
| Standing start | Stable calibration and eye/sole alignment |
| Seated start | Authored/locked scale is preserved until valid standing calibration |
| Sit/stand | Lower-body posture, pelvis target, camera correction, and visibility transition work |
| Two clients | Recipe, posture, seat, visibility, and authority agree |
| Late join standing/seated | Avatar and posture reconstruct deterministically |
| Disconnect while seated | Stale seat claim recovers |
| Controller/hand switch | Correct hand targets and wrist profile without scale changes |
| HMD sleep/wake | Tracking and constraints recover without moving the world or recalibrating size |
| Local HMD view | Head hidden; body and hands visible |
| Mirror/remote cameras | Full avatar remains visible |
| UMA rebuild | Animator, IK, visibility, diagnostics, and provider bindings reacquire |
| WebGL/WebXR | Provider works and XR loader is enabled where required |
| WebGPU flat | Materials render; no synchronous readback failure |
| Quest standalone | Project builds and the migrated avatar path functions on device; performance tuning is deferred |

Gate 6:

- all required cases pass or have user-approved, documented deferrals;
- no source-only behavior is lost.

### Phase 7 -- Commit and handoff

Status: `PENDING`

Recommended commit sequence:

1. documentation and package scaffolding;
2. minimal VertexForm3D core seams;
3. provider-neutral GHA runtime and assets;
4. UMA provider runtime/editor code;
5. VertexForm host adapters;
6. scene, prefab, Animator, catalog, and UI wiring;
7. diagnostics, validation artifacts, and cleanup.

Before each commit:

- obtain the user's express approval for the specific staging and commit action;
- inspect the exact diff;
- exclude `Assets/UMA`;
- exclude Photon credentials and unrelated project settings;
- ensure renamed/moved Unity assets retain required `.meta` files;
- state which migration ledger rows the commit closes.

The target becomes ready for an upstream PR only when every required ledger row is `DONE` or has an
explicit user-approved deferral.

### Phase 8 -- Post-migration timing and performance

Status: `DEFERRED`

Owner priority override, 2026-09-04: the owner requested resuming investigation of UMA load
blocking after accepting the current UI. Timing investigation is now active under that explicit
request; this does not close the outstanding functional gates or authorize vendor-source changes.
Initial evidence and the required next capture are recorded in
`Logs/MetaOpenXR-Fix-2026-09-04/UMA-PERFORMANCE-BASELINE.md`. No runtime optimization has yet been
applied. The original post-migration policy below remains the default for broader architecture
changes and the remaining platform performance pass.

Approved profiling follow-up, September 4 EDT: normal LoginScene → HomeScene captured a
18.46-second synchronous AssetIndexer/reference-graph load inside DynamicCharacterAvatar.Start.
The index audit found 488 hard references and no addressable entries. Detailed evidence,
limitations, and the proposed catalog-scoped/asynchronous loading direction are in
`Logs/MetaOpenXR-Fix-2026-09-04/UMA-PROFILE-FINDINGS.md`. Play Mode and profiling are stopped.
No runtime optimization or content/build configuration change has been applied yet.

First-two-step investigation, September 4 EDT: `Tools/PlanUmaStreaming.cs` produced a read-only
catalog dependency plan with 76 candidate index records (20 startup, 56 streamable), preserving
one race and all 17 wardrobe entries, with zero missing explicit slot/overlay dependencies.
`Logs/UMA-Streaming/IMPLEMENTATION-PLAN.md` records the existing Resources-index build-inclusion
constraint, scoped local Addressables setup, stock generator side effects, and verification gates.
No streaming configuration or runtime change is active yet.

This phase begins only after the functional migration is complete. Do not change avatar
construction strategy, UMA content policy, or GHA architecture in pursuit of performance while
source parity and runtime behavior are still being established.

Actions:

1. Review the UMA developer's recommendations and record each recommendation with its expected
   benefit, risk, and applicable platform.
2. Establish a repeatable migrated baseline for Home construction, customizer preview,
   save/refresh, player spawn, remote reconstruction, scene transitions, memory, and frame time.
3. Separate UMA generation time, content/import cost, network delay, scene/UI delay, and GHA
   embodiment work before selecting optimizations.
4. Apply one optimization at a time in the owning layer.
5. Re-run the functional validation matrix after each change.
6. Run the dedicated Quest and multi-avatar performance pass.

Performance work is not part of the functional migration definition of done and must not delay
the migration checkpoint.

## Original-plan reconciliation ledger

Update this table as work proceeds. Never delete a row merely because its priority changes.

| Original plan item | Current target status | Required next action |
|---|---|---|
| Stock/UMA interop and author policy | `PROVISIONAL` | Compare against frozen source; validate stock-only, UMA-only, mixed, switching, persistence, and two-client behavior. |
| First-person UMA head renderer split | `PROVISIONAL` | Validate Quest HMD, mirror, remote camera, rebuild, hair/eyes/mouth routing, and two-renderer cost. |
| Remote-avatar proximity hide | `PROVISIONAL` | Validate in VR and with both stock/UMA renderers; fade remains optional. |
| Personal-space collider | `DEFERRED` | Retain as a complementary comfort feature; design must account for room-scale HMD movement. |
| Local VR embodiment | `PROVISIONAL` | Reconcile the latest QuantumVertex calibration behavior and rerun Home/world functional acceptance. |
| Scene-invariant physical fit | `PENDING` | Finish tracking-space calibration validation; persist/replicate locked fit if required. |
| Real walk animation | `PROVISIONAL` | Source was accepted; verify clips, controller state, speed normalization, and packaging in target. |
| Seated posture/network occupancy | `PROVISIONAL` | Run local, proxy, late-join, double-booking, stale-claim, pelvis-target, and camera-correction tests. |
| Customizer V2 DNA/colors/wardrobe | `PROVISIONAL` | Validate catalog contents, slider prefab, preview, save/refresh, provider switching, and two-client persistence. |
| Avatar loading timing investigation | `DEFERRED` | Preserve existing diagnostics, but begin analysis and optimization only after full functional migration. |
| Generic framework extraction | `PROVISIONAL` | Establish all 26-file parity; eliminate accidental provider/host dependencies. |
| Local Humanoid prefab/FBX proof provider | `NOT STARTED` | Implement after generic package parity to prove GHA does not depend on UMA. |
| Provider registry/capabilities | `PENDING` | Define stable provider discovery, readiness, visibility, bounds, rebuild, fit, and configuration contracts. |
| Provider-neutral avatar configuration panel | `PROVISIONAL` | Refreshed shell plus Stock/UMA contributions are installed behind Change Avatar with no main-nav tab. Desktop click, provider switching, and preview construction passed on 2026-08-26; Classic Previous/Next preview rebuilding and exact Classic/Custom save-action parity passed on 2026-08-28; Classic bounds-fit plus authored-scale, bottom-pinned UMA framing and height-slider behavior passed on 2026-09-01. Validate close, save/refresh, persistence, stock-only, UMA-only, spawn/remote, and two-client behavior. |
| Individual avatar enable/disable editor UI | `NOT STARTED` | Add stable IDs and availability flags without list removal/reordering. |
| Locked scale synchronization | `PENDING` | Verify deterministic remote/late-join reconstruction; add explicit sync only if needed. |
| Lower-body leg/foot IK | `DEFERRED` | Design grounded targets and animation-aware weights before adapting RPM patterns. |
| Optical finger embodiment | `NOT STARTED` | Add provider-neutral joint mapping only if required for the first release. |
| Facial/viseme contract and lip sync | `NOT STARTED` | Define generic semantic input and UMA mapping; include Web fallback. |
| UMA material/shader audit | `NOT STARTED` | Audit Editor/URP, Quest, WebGL, and WebGPU by content category; fix only failures. |
| Dual-renderer WebXR/WebGPU delivery | `NOT STARTED` | Re-enable WebXR where required and implement/test runtime renderer/XR gating. |
| Selfie WebGPU asynchronous readback | `NOT STARTED` | Replace synchronous `ReadPixels` path with supported async behavior. |
| Quest standalone performance pass | `DEFERRED` | Run in post-migration Phase 8 after the functional Quest path is established. |
| Fusion AppVersion gating | `NOT STARTED` | Required before external deployment so incompatible NetworkBehaviour layouts cannot share sessions. |
| UMA installation/editor tooling | `PROVISIONAL` | PowerShell sync exists; implement cross-platform Unity Editor workflow after compatible UMA tooling is available. |
| Upstream PR preparation | `DEFERRED` | Do not begin until migration and validation gates pass; keep core, GHA, and UMA changes separable. |
| Login streaming video fix | `PENDING/SCOPE REVIEW` | Determine whether it belongs in the avatar migration or a separate upstream fix; do not silently drop it. |
| Unity Build And Run launcher bug | `DEFERRED/EXTERNAL` | Preserve reporting/workaround notes; not owned by GHA implementation. |

## Migration ledger template

Add one row per migrated unit. Use the frozen source commit and concrete validation evidence.

| Unit | Source path/commit | Target owner/path | Status | Verification | Notes |
|---|---|---|---|---|---|
| Humanoid avatar contracts | `Assets/QVAddons/AvatarSuite/HumanoidAvatarContracts.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Core/HumanoidAvatarContracts.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Provider-neutral; no UMA or VertexForm host dependency. |
| Avatar load timing log | `Assets/QVAddons/AvatarSuite/AvatarLoadTimingLog.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Diagnostics/AvatarLoadTimingLog.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace, cross-assembly accessibility, and marker rename only. Instrumentation retained; analysis remains deferred to Phase 8. |
| Humanoid presentation controller | `Assets/QVAddons/AvatarSuite/HumanoidAvatarPresentationController.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Presentation/HumanoidAvatarPresentationController.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Provider-neutral presentation policy remains in the generic package. |
| Humanoid posture animator | `Assets/QVAddons/AvatarSuite/HumanoidPostureAnimator.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Animation/HumanoidPostureAnimator.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Animator parameter detection and posture reapplication behavior are preserved without provider dependencies. |
| Seated pelvis alignment | `Assets/QVAddons/AvatarSuite/HumanoidSeatedPelvisAlignment.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidSeatedPelvisAlignment.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Generic visual-root correction remains independent of UMA and VertexForm seat implementation types. |
| XR rig startup stabilizer | `Assets/QVAddons/AvatarSuite/XrRigStartupStabilizer.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Tracking/XrRigStartupStabilizer.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace and diagnostic marker rename only; readiness, floor-origin, and locomotion-release behavior are preserved. |
| Local Addressables startup | `Assets/VertexForm3D/Scripts/AddressablesSystem/AddressablesDownloader.cs @ 994d9aa4` | `Assets/VertexForm3D/Scripts/AddressablesSystem/AddressablesDownloader.cs` | `DONE` | Exact QuantumVertex parity restored for the local-bundle branch; Windows Addressables built successfully with zero player compile errors | When `onlyLocalBundles` is true, initialize the built catalog directly. The negated condition incorrectly attempted to load the literal remote profile expression and blocked `LoginScene`. |
| VertexForm humanoid rig input | `Assets/QVAddons/AvatarSuite/VertexFormHumanoidRigInput.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/VertexFormHumanoidRigInput.cs` | `DONE` | Exact source comparison except intentional namespaces/import; zero-error GHA compile | Correctly remains in host-coupled staging because it depends on `AvatarInputConverter`, `PlayerNetworkSetup`, and `ProjectManager`. |
| Avatar extension network sync | `Assets/QVAddons/AvatarSuite/AvatarExtensionSync.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/AvatarExtensionSync.cs` | `DONE` | Behavioral source comparison; GUID `989f755490ee91249b0d5fa7b310dd94` preserved through Unity move; zero-error GHA compile | Only code delta is the correct `new` modifier for inherited `HasInputAuthority`. The Fusion-specific contract now lives in the stable host-integration layer; `Fusion.Unity` was removed from `VertexForm.GHA.Runtime.asmdef`. |
| Avatar camera visibility | `Assets/QVAddons/AvatarSuite/AvatarCameraVisibilityController.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Presentation/AvatarCameraVisibilityController.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace and diagnostic marker rename only; camera culling and XR diagnostic snapshot behavior are preserved. |
| Humanoid tracking lifecycle | `Assets/QVAddons/AvatarSuite/HumanoidTrackingLifecycle.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Tracking/HumanoidTrackingLifecycle.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Tracking loss/regain and delayed reinitialization state remain provider- and host-neutral. |
| Humanoid VR alignment | `Assets/QVAddons/AvatarSuite/HumanoidVrAlignment.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidVrAlignment.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Grounded root, eye-point correction, floor sampling, and one-time scale behavior are preserved without provider/host imports. |
| VR calibration session | `Assets/QVAddons/AvatarSuite/VrCalibrationSession.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Calibration/VrCalibrationSession.cs` | `DONE` | Behavioral source comparison plus zero-error compile after compatibility fix | New writes use the `GHA` PlayerPrefs key; a valid legacy `QVAS` standing-eye-height value is migrated once so upgrades do not silently discard accepted calibration. |
| VR height calibration | `Assets/QVAddons/AvatarSuite/VrHeightCalibration.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Calibration/VrHeightCalibration.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Provider-neutral one-time scale calculation is preserved. |
| Humanoid arm rig | `Assets/QVAddons/AvatarSuite/HumanoidArmRig.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidArmRig.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Runtime Animation Rigging setup, target updates, tracking weights, and cleanup remain provider- and host-neutral. |
| Humanoid locomotion driver | `Assets/QVAddons/AvatarSuite/HumanoidLocomotionDriver.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Animation/HumanoidLocomotionDriver.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Root-motion measurement and `Speed`/`Direction` Animator driving remain provider-neutral. |
| Remote avatar proximity hider | `Assets/QVAddons/AvatarSuite/RemoteAvatarProximityHider.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/RemoteAvatarProximityHider.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Namespace/bootstrap-name adaptations only. Correctly remains staged because current discovery imports UMA and VertexForm player/room types; future provider bounds can generalize it. |
| UMA recipe codec | `Assets/QVAddons/AvatarSuite/UmaRecipeCodec.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/Recipes/UmaRecipeCodec.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Wire version, 128-byte capacity, append-only IDs, DNA, and color encoding are preserved in the UMA adapter package. |
| UMA recipe store | `Assets/QVAddons/AvatarSuite/UmaRecipeStore.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/Recipes/UmaRecipeStore.cs` | `DONE` | Behavioral source comparison plus zero-error compile after compatibility/boundary fixes | New GHA recipe/mode keys migrate valid legacy QVAS values once. Stock mode remains explicitly wire-compatible at `0` without importing `AvatarExtensionSync`; the stable VertexForm stock-selection key is preserved as a literal. |
| UMA avatar catalog code | `Assets/QVAddons/AvatarSuite/UmaAvatarCatalog.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace and CreateAssetMenu rename only; provider policy, serialized fields, append-only DNA/color IDs, and accessors are preserved. Catalog asset contents remain a separate project-wiring verification. |
| UMA customizer row | `Assets/QVAddons/AvatarSuite/UmaCustomizerRow.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/UI/UmaCustomizerRow.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Serialized row roles and UI references are preserved in the UMA adapter package. |
| Avatar load profiler capture | `Assets/QVAddons/AvatarSuite/AvatarLoadProfilerCapture.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Diagnostics/AvatarLoadProfilerCapture.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Namespace, marker, and ignored `Library/GHAProfiles` output-folder rename only. Capture is preserved as evidence; performance analysis remains deferred. |
| Avatar profile report generator | `Assets/QVAddons/AvatarSuite/Editor/AvatarLoadProfileReportGenerator.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Editor/AvatarLoadProfileReportGenerator.cs` | `DONE` | Behavioral source comparison plus zero-error compile after menu cleanup | QVAS names were consistently migrated; redundant `Tools/GHA/GHA` menu path corrected to `Tools/GHA`. Report generation remains deferred to Phase 8. |
| UMA Home avatar host | `Assets/QVAddons/AvatarSuite/UmaHomeAvatar.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaHomeAvatar.cs` | `DONE` | Exact source comparison except intentional namespaces/import; zero-error GHA compile | Correctly remains in host-coupled staging because it coordinates Home rig/controller behavior with the UMA provider. |
| UMA avatar customizer | `Assets/QVAddons/AvatarSuite/UmaAvatarCustomizer.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/UI/UmaAvatarCustomizer.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace/runtime-object rename and duplicate-using cleanup only; tabbed UI, preview, save, provider selection, DNA, colors, and wardrobe behavior are preserved. |
| UMA avatar bridge | `Assets/QVAddons/AvatarSuite/UmaAvatarBridge.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaAvatarBridge.cs` | `DONE` | Exact source comparison except intentional namespaces/import; zero-error GHA compile | Multiplayer provider selection, opaque recipe payload, construction override, visibility, and posture/seat synchronization are preserved in host-coupled staging. |
| UMA avatar puppet | `Assets/QVAddons/AvatarSuite/UmaAvatarPuppet.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaAvatarPuppet.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespaces, runtime-object name, and diagnostic marker renames only; UMA construction, renderer split, visibility, animation, calibration, tracking, and seated alignment behavior are preserved. |

All 26 frozen QuantumVertex AvatarSuite C# units now have an explicit, closed ledger row.
`AvatarExtensionSync` was moved through Unity into the stable VertexForm host-integration layer
with its GUID preserved, removing the generic GHA runtime assembly's only Fusion dependency.
AnkleBreaker verified the resulting assembly references and Unity reported zero errors or warnings.

Script GUID audit on 2026-07-27: all 26 source `.cs.meta` GUIDs match their unique target
counterparts (`26/26`, zero missing, ambiguous, or mismatched). Existing serialized references to
the moved scripts are therefore preserved.

Non-script AvatarSuite asset audit on 2026-07-27: all 20 frozen `.fbx`, `.controller`, `.asset`,
and `.prefab` files have unique target counterparts and preserved `.meta` GUIDs (`20/20`). Seventeen
are byte-identical. The only content differences are the internal names of
`QVAS_Locomotion`/`QVAS_Locomotion_v2` renamed to `GHA_Locomotion`/`GHA_Locomotion_v2`, and the catalog's serialized class
identifier namespace. Unity resolves the catalog as `GHA.AvatarSuite.UmaAvatarCatalog` with all
expected serialized fields through the preserved MonoScript GUID.

| Unit | Source path/commit | Target owner/path | Status | Verification | Notes |
|---|---|---|---|---|---|
| AvatarSuite animation/model asset set | `Assets/QVAddons/AvatarSuite/Animations/** @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Animations/**` | `DONE` | `18/18` unique counterparts and GUID matches; 16 byte-identical; controller diffs inspected | The two renamed GHA controllers differ only in internal controller name. Animator parameters, states, transitions, motions, and imported FBX content are preserved. |
| UMA avatar catalog asset | `Assets/QVAddons/AvatarSuite/UmaAvatarCatalog.asset @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset` | `DONE` | GUID preserved; serialized diff inspected; Unity ScriptableObject resolved with expected type/properties | Only namespace class identifier differs. Lists and object-reference GUIDs are unchanged; referenced installed UMA content/Global Library validation remains a Phase 2 runtime/editor gate. |
| UMA DNA slider prefab | `Assets/QVAddons/AvatarSuite/UMADNASlider.prefab @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/UI/UMADNASlider.prefab` | `DONE` | GUID and SHA-256 content match | UI prefab serialized content is byte-identical. |

## New-agent start sequence

1. Read `AGENTS.md`, `GHA-SESSION-HANDOFF-2026-08-25.md`, this runbook, package boundaries,
   architecture, and the original QuantumVertex plan.
2. Inspect both repositories' branches, remotes, HEADs, and dirty files without modifying them.
3. Check whether the frozen QuantumVertex commit has been recorded in Phase 0.
4. If it is still `TBD`, create the authorized functional checkpoint without beginning timing
   analysis or performance optimization. Do not claim target parity.
5. Check target compilation. If the AnkleBreaker UMA compatibility blocker remains, report it and
   resolve it before Unity runtime work.
6. Re-run the file inventory against the frozen source commit.
7. Pick one migration ledger row and complete its ownership classification, implementation,
   compile check, and proportionate runtime verification.
8. Update this runbook before moving to another slice.

## Definition of migration complete

The migration is complete only when:

- QuantumVertex is frozen at a recorded commit and treated as read-only;
- every original-plan item is `DONE`, `SUPERSEDED`, or explicitly user-approved as `DEFERRED`;
- every source file and project modification is accounted for;
- GHA and UMA package boundaries are enforced by assemblies and source dependencies;
- UMA can be reproduced from the pinned external checkout without committing vendor code;
- the target passes the agreed Desktop, VR, multiplayer, scene-transition, seating, persistence,
  Web, and functional Quest validation matrix;
- credentials and unrelated changes are excluded;
- commits are separated so VertexForm3D authors can evaluate core, generic GHA, and UMA work
  independently.

Performance analysis and optimization are deliberately outside this definition. They begin only
after the completed functional migration has produced a stable GHA baseline.
