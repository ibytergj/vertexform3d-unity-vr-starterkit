# VertexForm3D GHA Session Handoff — updated October 1, 2026

## Current continuation instructions

Read `AGENTS.md`, this current section, `GHA-MIGRATION-RUNBOOK.md` (controlling status/evidence),
then `GHA-IMPLEMENTATION-PLAN.md` (sequence and stable item IDs). The filename is retained for
existing links. The August 25 snapshot below is historical and is superseded wherever it conflicts
with this section or the current runbook.

**Owner decision: further E3 / R9 / BUG-9 performance investigation and optimization is deferred
until after P5-4, the UMA 3.1 upgrade.** Preserve the smaller-index improvement and evidence;
resume the separation/lifecycle work on the current pinned UMA version. Do not start another
profiling, async-loading or texture-optimization pass now. UMA 3.1 may change the result but is
not assumed to fix the remaining stalls. The upgrade remains after the current-version separation
and before additional testers or public release, followed by performance reassessment.

| Item | Current state / remaining work |
| --- | --- |
| Plan | Six phases (0–5), 46 parent items; original IDs retained. Phases 0–3 and 5 plan content approved; Phase 4 conditionally accepted. Re-review each upcoming phase when its predecessor is complete. Plan approval is not implementation acceptance. |
| Phase 0 / C2 | Bounded Desktop baseline recorded. Local implementation and documentation checkpoints authorized October 1; applicable acceptance/inventory gaps remain. Do not mark the entire phase complete or treat a checkpoint as Phase 4 commit separation. |
| D1 | Home/Classic host lifecycle extracted into `VertexFormHomeAvatar`, with `IHomeAvatarProvider` and optional UMA build adapter. Compilation, eight isolated Edit Mode checks and bounded Desktop Home construction passed. Full Classic/UMA absence, Home/world/return, network and VR acceptance remain incomplete. |
| D2 | One imported Humanoid avatar with fixed appearance is still the contract proof; normal animation/tracking required. Revisit the linked static-avatar discussion: `codex://threads/01a0c97b-c915-7b41-8020-2c62d65371d2`. The full static-avatar plugin remains separate scope. |
| Phase 3 | Export and clean-install test both GHA and UMA packages; current experimental source presence is not package acceptance. |
| A4 / A5 | Pointer-registration errors and remaining VR readiness/body-visibility acceptance remain open. Arms intersecting torso (A5.1) and eye/body alignment too far back (A5.2) remain later work. BUG-3 walking investigation remains owner-deferred. |
| E3 / P5-4 | Smaller index remains a local experiment. Further performance work waits until after the reviewed UMA 3.1 upgrade; required functional and release/platform checks are not waived. |
| Git | Owner explicitly authorized local commits on the current branch at session close. Implementation checkpoint: `1e0cec7d`; this handoff accompanies the documentation checkpoint. No push authorized or performed. Credentials, generated/vendor state and unrelated work remain outside these commits. |

## Current environment and last verified state

| Field | Recorded value |
| --- | --- |
| Continuation checkout | `E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit-GHA` |
| Branch / checkpoints | `Generic-Humanoid-Avatars`; pre-session base `8c19b5f773985b71045df92337fc107595512d56`, implementation checkpoint `1e0cec7d`, followed by the documentation checkpoint containing this handoff. Use `git log` for the final HEAD. Other local changes remain outside the checkpoint. |
| Unity | `6000.3.11f1` (`3000ef702840`) |
| UMA source | `E:\src\Unity\6000.3\UMA`, pinned `c9204fe475b334162617da5ca923afcc6050e01e`; sync also needs reviewed shader-repair objects at `f4edf41ba1017b4a745fd1ba7286824332652b7d` |
| Tooling | Unity standalone CLI `1.0.0-beta.6`; project Pipeline `0.6.0-exp.1`. Preserve manifest/lock and embedded GHA/UMA package records; do not force-install tooling. |
| Last verified Editor state | Original project, stopped in LoginScene, Desktop / StandaloneWindows64; owner disabled XR startup for Windows and MetaQuest. Profiler off, zero compilation errors after temporary helpers were removed. This is a recorded state, not a guarantee after transfer/reopening. |
| Historical comparison | Closed worktree at `C:\Users\Blender\.codex\worktrees\september-24-avatar-baseline\vertexform3d-unity-vr-starterkit-GHA`; used for tests/reconstruction, not ongoing implementation. Owner directed recreating it on the destination if needed. Preserve its test evidence, not the checkout; do not copy its linked `.git` file as a standalone repository. |

CLI inspection timed out during this investigation, so exact-project AnkleBreaker discovery was
used for the missing inspection. Rediscover connections on the destination; never copy or expose
Pipeline connection descriptors/tokens. Project paths in instructions must be reviewed for the
destination location before using tools. Follow the owner's Play Mode approval requirements.

## Preserved improvement and limits

`Tools/CreateCatalogScopedUmaIndex.cs` creates a new project index from both races and all 24
current UI wardrobe choices and their dependencies. The original full vendor index and catalog
were preserved. The experiment created 100 persistent entries, with 468 dependency paths including
122 textures, versus 513 records (508 persistent) and 464 texture paths in the original full index.
The generated local asset is `Assets/UMAProjectData/Resources/AssetIndexerProject.asset`.

| Same-project fresh-Editor measurement | Full index | Scoped index |
| --- | --- | --- |
| DCA initialized to BuildBegun | 5.566 s | 0.558 s |
| Home START to FINISH | 7.190 s | 6.474 s |
| Largest observed Home frame (`unscaledDeltaTime`) | 6.927 s | 1.921 s |

This was one pair with no OS/GPU cache flush, not a statistical benchmark or headset-build test.
The 24 choices are recipes referencing meshes, materials and textures for both races; reducing
recipe count does not remove all avatar construction/rendering cost. The remaining scoped-run
delay has not been fully attributed. Earlier full-index profiles showed substantial synchronous
asset reads and GPU texture uploads; do not assume those fully explain the scoped run.

Thirty-one fresh construction cases passed (male base plus 17 compatible choices; female base
plus 12 compatible choices). Checks covered readiness, a Humanoid Animator, nonempty meshes,
assigned wardrobe and no errors. They did not cover every visual/material combination, network,
VR or device acceptance. The saved recipe was unchanged. Temporary test components were removed;
their source and results are retained privately under Logs.

No background streaming is enabled. The full index still exists under Resources, so the experiment
does not establish a smaller build or final installer/index ownership. Do not overwrite the
scoped index with a generic full-library rebuild. The historical ~1.2 s measurement covered avatar
build, with a separate visibility delay; later repeat/cache/storage conditions differed. A complete
explanation or exact reproduction of that historical timing has not been established.

## Machine-transfer checklist

Paths below are relative to the continuation checkout unless absolute. Keep private material out
of the public repository. The inventory is a preservation aid, not an approved staging list.

| Transfer item | What to preserve / how to use it |
| --- | --- |
| Reviewed project source and Unity metadata | Carry the local implementation and documentation commits, plus the separately listed uncommitted files needed for the local environment, preserving `.meta` files. The runtime files below are in `1e0cec7d`; a clone from the unchanged remote will omit them. Preserve unrelated work separately for owner review. |
| Git continuity | Transfer a complete standalone repository/history backup or a Git bundle containing the local commits; a remote clone alone cannot recover unpushed commits. Transfer uncommitted/new files separately as well. A bundle does not contain them. No bundle or push was created by this handoff. |
| Pinned UMA source / repair objects | Retain access to the sibling UMA repository and both exact commits above, or transfer it privately. Follow the developer guide and `Tools/Sync-Uma.ps1` with the destination paths to reconstruct the ignored vendor installation. Do not upgrade during transfer. Preserve any other required licensed/ignored dependencies privately. |
| Generated UMA indexes | Rebuild locally, including the scoped experiment if retaining its behavior. Do not copy or merge either generated index from this machine. See the prerequisites below; normal installer regeneration alone creates a full index and does not reproduce the experiment. |
| Private application configuration | Preserve or recreate `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` privately. It currently appears as a modified tracked file: ignoring a path does not remove an already tracked file from Git. Exclude its credential-bearing contents from public commits. Review other local configuration such as `.claude/settings.local.json` privately rather than copying it into a public commit. |
| Saved avatar/preferences, optional | Private snapshots exist at `Logs/September24Worktree-20261001/editor-prefs-before.reg` and `editor-prefs-after.reg`. Review before using on Windows; they contain project preferences, not just an avatar recipe. Do not blindly restore device calibration. Recalibrate on the new headset/machine as needed. |
| Latest experiment evidence | Copy `Logs/ScopedUmaIndex-20261001/` and `Logs/UMA-Streaming/` privately, including `content-plan-20261001-153836.json`. These preserve the index reports, timing logs and construction results. |
| Historical comparison evidence | Copy `Logs/September24Worktree-20261001/`, `Logs/HomeAB-20261001/`, and `Logs/VRFirstLoad-20261001/` privately. The worktree evidence folder includes private preference exports; do not publish the folder wholesale. |
| Raw profiles | Copy only `Library/GHAProfiles/` from Library if retaining the performance recordings (about 714 MB). These are evidence for the post-3.1 revisit; the rest of Library can be regenerated. |
| Baseline/earlier evidence | Preserve `Logs/SeparationBaseline-20260923/`, `Logs/Phase1Home-20260927/`, and `Logs/MetaOpenXR-Fix-2026-09-04/` if retaining the earlier acceptance and slow-load history. |
| Working-tree inventory | `Logs/SessionHandoff-20261001/working-tree-transfer-inventory.csv` preserves the pre-commit inventory; `remaining-local-files.csv` lists files still outside the checkpoints; `committed-files.txt` lists checkpoint paths and `checkpoint-commits.txt` records revisions. `staged-files-before-handoff.txt` records the original staged scope. These lists do not back up file contents. |
| Local agent instructions, optional | Retain custom workflow instructions at `C:\Users\Blender\.agents\skills\unity-cli\` and `C:\Users\Blender\.codex\skills\unity-mcp-skill\` if needed on the destination; review their machine-specific paths. Repository handoff documents are the continuation record. Reinstall required tooling rather than transferring authentication or connection-token files. |
| Reference repositories, optional | Retain access to frozen `QuantumVertex`, the clean VertexForm3D reference and the RPM fixture repository recorded under D2. These support traceability and later proof-provider work; the historical worktree is not required to resume current development. |

Do not transfer ordinary `Library`, `Temp`, `obj`, generated solution/project files or connection
descriptors as required source. `Library/GHAProfiles` is the explicit evidence exception. Private
preference/configuration backups must remain private. No backup archive was created in this pass.

### Implementation checkpoint and remaining local configuration

The following implementation/tooling source is preserved in `1e0cec7d`. The last row distinguishes
configuration retained outside that commit:

| Files | Role |
| --- | --- |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/VertexFormHomeAvatar.cs` and `.meta` (new) | Extracted host lifecycle |
| `Packages/com.vertexform3d.gha/Runtime/Core/IHomeAvatarProvider.cs` and `.meta` (new) | Provider contract |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaHomeAvatar.cs` | UMA Home adapter |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/UI/UmaAvatarCustomizer.cs` | Customizer integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/GhaUmaAssetInstaller.cs` and `GhaVertexFormAssetInstaller.cs` in the same folder | Installer wiring |
| `Assets/VertexForm3D/Scripts/Utility/SceneLoader.cs` | Desktop/XR startup handling |
| `Tools/ValidateGhaHomeLifecycle.cs` (new) | Lifecycle checks |
| `Tools/CreateCatalogScopedUmaIndex.cs` (new) | Rebuild recipe for the scoped-index experiment |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Editor/GhaOneShotAvatarProfile.cs` and `.meta` (new) | Diagnostic capture source; retain for later performance work |
| `.gitignore` | Checkpoint includes the `.utmp/` generated-state exclusion |
| `Packages/manifest.json`, `Packages/packages-lock.json`, relevant `ProjectSettings` and platform/XR assets | Still local and listed in `remaining-local-files.csv`; transfer privately if preserving this exact development environment, then review separately. These include tooling and owner Desktop/XR choices. Do not blanket-stage all modified assets. |

The private inventory includes additional user/editor changes; this table is not an exhaustive
commit list. In particular, lighting/reflection metadata, Ocean Villa content, local settings and
generated files must not silently enter a GHA documentation or runtime commit.

The companion documentation checkpoint covers these 12 paths: `GHA-IMPLEMENTATION-PLAN.md`,
`GHA-MIGRATION-RUNBOOK.md`, `GHA-SEPARATION-BASELINE-2026-09-23.md`, this handoff,
`GHA-developer-getting-started.md`, `GHA-getting-started.md`, `README.md`,
`Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/README.md`,
`Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md`,
`Packages/com.vertexform3d.gha.uma/README.md`,
`Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md`, and
`Packages/com.vertexform3d.gha/Documentation~/PACKAGE-BOUNDARIES.md`.

### Scoped-index reconstruction caveat

The tool is an Editor-eval C# method body, not a standalone program. It requires a full index
rebuilt from destination assets at `Assets/UMA/InternalDataStore/InGame/Resources/AssetIndexer.asset`
and no existing `Assets/UMAProjectData/Resources/AssetIndexerProject.asset`; it refuses to replace
an existing project index. The normal provider installer can instead create the full index at
the project path. Therefore **do not blindly run the tool after the normal installer**.

On the destination, inspect the generated layout first and use Unity asset operations to make
the locally rebuilt full source index available at the tool's expected path, preserving any
existing index before attempting creation. Reconcile any path differences explicitly; never copy
the old generated index, overwrite the current curated index, or manipulate serialized asset
contents by hand. Validate the catalog/dependencies and both races after reconstruction. The
fresh-machine reconstruction sequence has not been tested; retain this as a setup prerequisite,
not a claim that the local experiment is already portable. This is preservation/setup work,
not authorization to restart performance optimization before UMA 3.1.

## Suggested next-session instruction

Read AGENTS.md, the current section of GHA-SESSION-HANDOFF-2026-08-25.md, the controlling
GHA-MIGRATION-RUNBOOK.md and GHA-IMPLEMENTATION-PLAN.md. Verify the transferred source and pinned
dependencies before changing anything. Resume D1 separation/lifecycle acceptance within the
agreed phase review process. Preserve the scoped-index experiment; E3 / R9 / BUG-9 performance
work is deferred until after P5-4 UMA 3.1. Keep the existing phase/item identifiers. Do not
stage, commit or push without the owner's exact-scope approval; follow Play Mode approval rules.

---

## Historical August 25 snapshot — superseded for current status

## Start here

This handoff is the short operational entry point for the next agent. The controlling migration
record remains `GHA-MIGRATION-RUNBOOK.md`; read it before changing migration code or claiming a
phase complete.

Project paths:

- Active migration target: `E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit-GHA`
- Updated clean VertexForm3D reference: `E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit`
- Frozen QuantumVertex source: `E:\src\Unity\6000.3\QuantumVertex`
- Clean UMA source: `E:\src\Unity\6000.3\UMA`

## Repository state

- GHA branch: `Generic-Humanoid-Avatars`
- GHA HEAD: `5257c76b7ebce05b43b8d43f4186ce7498a50bea`
- HEAD description: official VertexForm3D PR #55 / Starter Kit `1.1.7`
- Fork tracking branch: `origin/Generic-Humanoid-Avatars` remains at `346d84c3`; local GHA is four
  commits ahead because the official upstream history was fetched and fast-forwarded locally.
- The GHA working tree is intentionally dirty with the uncommitted migration, project integration,
  generated/editor state, credentials, and unrelated user content classified by the runbook.
- No stash was used for the upstream integration. No files were staged, committed, pushed, or
  submitted in a pull request.
- This repository is public. Never stage, commit, push, or open/update a pull request without the
  owner's express approval for the exact Git action and reviewed file scope.

Credential-bearing `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`, ignored UMA vendor
content, generated state, screenshots, and unrelated Ocean Villa content must not drift into a GHA
commit.

## Completed immediately before this handoff

### UMA v3.04 synchronization

The ignored GHA UMA installation was rebuilt from the clean official UMA `v3.04` source at:

`722b308aebfe5dee7d0048e24c1c464c00b5fc06`

`Tools/Sync-Uma.ps1` completed successfully with documented pruning, current documentation,
`Assets/SourceShaders`, and the retained Photobooth authoring set. Static comparisons passed:

- pruned UMA core comparison: no differences under the documented policy;
- `Assets/SourceShaders`: no differences;
- retained Photobooth checksum pass: 11 files, no missing files or mismatches.

The replacement backup is:

`Temp/UMA-Backup-20260818-113414`

`UMA_INSTALLED` remains intentionally undefined because AnkleBreaker's optional UMA bridge targets
UMA 2 and does not compile against UMA 3. Do not patch `Library/PackageCache`.

### VertexForm3D PR #55 synchronization

The clean original Starter Kit was on `Master` at `5257c76b`. GHA was fast-forwarded from
`346d84c3` to that commit without a stash or temporary commit. The six-file official delta:

- removed the unused `com.cesium.unity` dependency and lock entry;
- removed Moon Terrain from `LocalScenes.asset`;
- advanced `Project Data SO.asset` from `1.1.6` to `1.1.7`;
- added the Tutorials entry to `VertexForm3DHelp.cs`;
- corrected the `SettingsUISO` lookup/reuse logic in `VertexFormSDKMenu.cs`;
- updated `Packages/packages-lock.json`.

The two overlapping package files were reconciled deliberately. The upstream Cesium removal is
present, while the GHA-owned AnkleBreaker dependency and embedded `com.vertexform3d.gha` and
`com.vertexform3d.gha.uma` lock records remain. Both JSON files parse, there are no unmerged index
entries, and the four non-package files match the clean reference after line-ending normalization.

## Immediate gate completed on 2026-08-25

The exact GHA project was opened with Unity's supported standalone CLI in Unity `6000.3.11f1`.
Package Manager completed its first import and resolved 80 packages. The resolved lock now includes
Pipeline `0.5.0-exp.1`, while the reconciled AnkleBreaker and embedded GHA/UMA records remain and
Cesium remains absent. The editor reached `ready` with `compiling=false`,
`domainReloadInProgress=false`, Play Mode stopped, and recompile status idle.

Structured console inspection contained zero errors before validation. Two tooling-only Pipeline
errors are preserved across the 2026-08-25 and 2026-08-26 editor sessions: in each case, a first
cold synchronous `eval`/`eval_file` exceeded the bridge's five-second main-thread timeout. The same
work then completed through the supported detached-job route. These were not compiler errors.

The UMA Global Library was rebuilt in the target rather than copied. The generated, ignored
`Assets/UMA/InternalDataStore/InGame/Resources/AssetIndexer.asset` was rewritten and stale entries
were pruned. The rebuilt type counts were:

- 146 slots, 114 overlays, 11 races, 10 text recipes, and 162 wardrobe recipes;
- 10 runtime animator controllers, 10 animator override controllers, and 10 animator controllers;
- 34 UMA materials and 1 mesh-hide asset.

The GHA UMA catalog validator passed with one race, 17 wardrobe entries, four default wardrobe
entries, three color channels, 26 slot references, 28 overlay references, and seven unique
materials. It checked catalog races, base recipes, packed slots/overlays, wardrobe compatibility,
materials, defaults, and palette indices against the rebuilt index. The generated UMA index remains
ignored and no UMA vendor content was staged.

The UMA Core suite was requested in EditMode and completed with 216 total, 212 passed, three
failed, and one skipped. During its post-test assembly refresh, Unity nevertheless logged an
internal test-runner transition (`Entering playmode with assembly reload locked`). No application
runtime validation was performed. The skip is the intentionally absent optional UMA2 package. The
three failures are retained baseline/pruning-policy checks, not compilation or catalog failures:

- two readiness tests expect sample content deliberately excluded by `Tools/Sync-Uma.ps1`
  (`UMA3/Scenes` and the `RandomCharacterWalker` sample under `UMA3/RandomCharacters`);
- the release asset validator reports 11 optional HDRP/ShaderGraph script GUIDs that are not
  installed in this URP target.

After the owner reopened the project on 2026-08-26, the supported CLI confirmed the exact GHA
editor was `ready`, `compiling=false`, `domainReloadInProgress=false`, Play Mode stopped, and the
recompile result completed with no errors. Its structured error console was empty before the prefab
audit; afterward it contained only the preserved synchronous Pipeline timeout described above.

Fusion bake verification also completed on discarded prefab-editing clones, without saving either
asset. Both `NewGenericMRDesktopPrefab.prefab` and `NewGenericMRVRPrefab.prefab` passed the official
Fusion edit-time baker with `hadChanges=false`, 12 network objects, 20 network behaviours, and zero
null baked entries. `AvatarExtensionSync` is present in each baked behaviour table;
`UmaAvatarBridge` is correctly a non-network behaviour.

The local, credential-bearing Photon settings asset now uses the distinct non-secret AppVersion
`gha-migration-20260826` for isolated migration test sessions. Its App IDs were not read or
displayed, and the asset remains unstaged and excluded from any publishable migration scope.

## Avatar Configuration Studio UI refresh completed on 2026-08-26

The provider-neutral Change Avatar surface was rebuilt and validated live from `LoginScene` through
anonymous login into `HomeScene`. The final UI uses a shared navy/violet theme, a clear
`AVATAR STUDIO` hierarchy, Classic/Custom provider tabs, a dedicated 3D preview area, readable
Classic selection controls, and the UMA Body/Face/Colors/Outfits workflow with a prominent save
action.

The live gate found and fixed a host integration fault that initially hid Classic: the legacy
`ProjectManager.instance` and `AvatarSelectionManager.Instance` fields could be null while their
live components and the 10-avatar `Main UI Database.asset` were present. The stock provider now
repairs those references after scene load and safely rebuilds an absent Classic preview when the
tab is selected. The UMA provider uses the same live-manager fallback for its preview anchor.

Retained evidence:

- `Assets/Screenshots/GHA_Avatar_Studio_Classic_Final.png`
- `Assets/Screenshots/GHA_Avatar_Studio_Custom_Final.png`
- `Assets/Screenshots/GHA_Avatar_Studio_Classic_Player_Final.png`
- `Assets/Screenshots/GHA_Avatar_Studio_Custom_Player_Final.png`

Verified results: both provider buttons were present; switching Custom -> Classic -> Custom
completed; Classic recovered three active preview renderers; Custom retained one active preview
renderer; compilation completed with no errors; and the editor returned to ready with Play Mode
stopped. The broader runtime gate is not complete. The session also retained pre-existing repeated
XR Affordance receiver and `AvatarInputConverter.Update` null-reference errors, which were not
introduced by the Avatar Studio UI work and still require a separate gameplay-debug pass.

Classic UI follow-up completed on 2026-08-28. Previous/Next now temporarily release the stock
renderer suppression while rebuilding the preview, so the visible head/body changes with the
selection. A live `LoginScene` -> `HomeScene` test moved selection 4 -> 5 and instantiated
`Head6(Clone)`/`Body6(Clone)`, then Previous returned 5 -> 4 and instantiated
`Head5(Clone)`/`Body5(Clone)`. Classic's action is now labeled `SAVE AVATAR` and shares the Custom
button's exact embedded layout: 176 x 56, bottom-right anchor, position (-12, 12). Unity compilation
completed with no errors, the final Classic screenshot was refreshed, and Play Mode was stopped.

Preview framing follow-up completed on 2026-09-01. Classic measures its active renderer bounds,
scales them to 94% of the usable `Provider Preview` rectangle, and centers the projected bounds on
that backdrop after provider selection and every Previous/Next rebuild. UMA follows the final
owner-approved policy instead: it retains the authored preview transform (`GHA UMA Preview` at
local position `(0.9, -0.50, 0)`, scale `0.40`, with the DCA child at 1.0), centers the rendered
body horizontally, and pins its rendered feet to
the bottom of the usable preview after every character update. Height changes therefore grow
upward from planted feet and may extend above the panel. Fresh `LoginScene` -> `HomeScene` evidence
placed Classic at screen center (1026.962, 306.185). At UMA's center height, horizontal error was
0.081 px; at maximum height, horizontal error was 0.029 px while projected height grew from about
247 px to 389 px. The preview floor now sits 3.5% above the actual card edge, leaving only 8.6 px
of visible shoe clearance instead of reserving the former bottom caption band. The provider caption
was moved to the top header as `CUSTOM · LIVE PREVIEW`/`CLASSIC · LIVE PREVIEW`. The shared
Classic/Custom preview-card backdrop is opaque black. Retained
captures are `GHA_Avatar_Studio_Classic_Final.png`, `GHA_Avatar_Studio_UMA_Position_Check.png`
(before), `GHA_Avatar_Studio_UMA_Final.png` (center height), and
`GHA_Avatar_Studio_UMA_Max_Height_Check.png`. Compilation passed and Play Mode was stopped.

Reference-layout follow-up completed on 2026-09-03. The shared lower studio is now a three-column
composition inspired by the supplied earlier Ready Player Me mockup: a narrow provider-owned
category rail on the left, the opaque-black live preview in the centre, and the active controls on
the right. Classic contributes one selected Avatar category; Custom contributes Body, Face,
Colors, and Outfits as vertical rounded tiles with bordered icon/label treatment. Provider tabs
remain across the top, and existing preview framing plus the identical bottom-right `SAVE AVATAR`
actions are unchanged. A fresh `LoginScene` -> anonymous `HomeScene` Play Mode pass confirmed the
preview image at RGBA `(0, 0, 0, 1)`, exercised Custom Body and Outfits, moved Classic selection
4 -> 5 with Next and restored 5 -> 4 with Previous, and ended with zero Console errors. Retained
captures are `GHA-avatar-studio-reference-layout.png` (Custom) and
`GHA-avatar-studio-classic-three-column.png` (Classic). Compilation passed and Play Mode was
stopped.

## Immediate next gate

Continue the remaining runtime matrix from `LoginScene`. The owner granted permission on
2026-08-26 to enter and exit Play Mode for this work. Preserve console evidence and validate the
ordered stock-only, UMA-only, persistence, spawn/remote, scene-transition, seating, and two-client
cases in the controlling runbook. Mixed-provider Change Avatar opening, provider switching, and
preview construction now have accepted Desktop evidence. The revised `0.02m` desktop footwear
clearance remains part of the broader runtime gate.

Use Unity's supported standalone CLI first:

`C:\Users\Blender\AppData\Local\Unity\bin\unity.exe`

Verified CLI version when handed off: `1.0.0-beta.6`.

Always pass:

`--project-path "E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit-GHA"`

The Pipeline package is per project. Its resolved `0.5.0-exp.1` installation has now been audited;
do not force-install or upgrade it without reviewing the exact manifest/lock delta and preserving
the reconciled AnkleBreaker and embedded GHA/UMA records.

`Library/Pipeline/.unity-pipeline-port` contains a bearer token. Never print or expose it. A
restricted shell may falsely report no reachable editors if it cannot read the descriptor.

## Unity operating rules

- Unity CLI is primary. Use AnkleBreaker only when the CLI lacks the operation or cannot adequately
  inspect/validate the result, and record why the fallback was necessary.
- Never call either tool's private HTTP bridge directly.
- Ask the owner and wait for explicit approval before entering or exiting Play Mode.
- VertexForm runtime validation starts from `LoginScene`. The UMA Photobooth authoring workflow is
  the documented exception.
- During a synchronous build, do not call main-thread-required inspection commands. Poll only the
  supported off-thread build status and console commands.
- Do not claim success from a command return alone. Verify compilation/build status, structured
  console output, and produced artifact properties.

## After the immediate gate

Continue from the ordered phases and reconciliation ledger in `GHA-MIGRATION-RUNBOOK.md`. The
functional migration and validation matrix remain incomplete. The planned Avatar Configuration
Studio UI refresh, provider/runtime validation, Desktop/VR/Web/Quest coverage, two-client tests,
and post-migration performance work must remain within the documented package and host-layer
ownership boundaries.

Do not begin performance optimization before the functional migration gates are complete. Do not
modify pristine UMA vendor source to solve GHA integration issues unless a separately reviewed UMA
upstream contribution is explicitly authorized.
