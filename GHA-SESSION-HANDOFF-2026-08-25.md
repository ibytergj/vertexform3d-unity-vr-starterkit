# VertexForm3D GHA Session Handoff — 2026-08-25

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
