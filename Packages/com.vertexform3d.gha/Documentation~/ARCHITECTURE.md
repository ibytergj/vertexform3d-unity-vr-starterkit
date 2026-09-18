# GHA (Generic Humanoid Avatars) Architecture

Status: September 18, 2026. First written July 12, 2026 as the QuantumVertex "Avatar Suite"
architecture; renamed and brought up to date for the VertexForm3D `Generic-Humanoid-Avatars`
branch. The design sections describe the accepted direction. [Current implementation](#current-implementation-september-18-2026)
describes what exists. [Roadmap](#roadmap) lists every design commitment that is not delivered.
[Known defects](#known-defects) lists open bugs. The [Status history](#status-history) records
what changed and when.

Companion documents: `REFERENCE-REVIEW.md` (why the design is what it is),
`PACKAGE-BOUNDARIES.md` (ownership rules and the reversible installer), the UMA package's
`Documentation~/SETUP.md` (UMA setup and body-type authoring), and the repository's
`GHA-MIGRATION-RUNBOOK.md` (controlling status and verification ledger).

## Accepted product direction

The target product boundary is:

```text
VertexForm3D Core
    Minimal, inert, provider-neutral extension hooks

Avatar Model Integration Framework  (com.vertexform3d.gha)
    Shared Unity Humanoid embodiment, presentation, calibration, posture, Avatar Studio shell,
    and VertexForm host adapters

Avatar provider packages
    UMA 3 provider (com.vertexform3d.gha.uma; initial production provider)
    Stock/Classic VertexForm avatars (host-supplied provider inside the Avatar Studio shell)
    Local Humanoid prefab/FBX provider (framework proof provider; roadmap)
    Remote Vertex-compatible GLB provider (roadmap)
```

The framework integrates humanoid 3D character models into VertexForm3D. It is not an abstraction
over third-party avatar platforms, account systems, cloud services, or vendor SDKs. Character
Creator, MetaHuman, Blender, or another source matters only to the provider that turns its output
into a compatible Unity Humanoid instance.

Non-humanoid avatars are outside the framework scope. They may be supported by a future parallel
integration system without weakening the Humanoid contract defined here.

### Core-change policy

Changes proposed to VertexForm3D Core must be:

- Minimal and easy to review independently.
- Generic across Humanoid model providers.
- Inert when the integration framework is absent or no provider is selected.
- Non-breaking for the existing stock avatar path.
- Kept separate from UMA implementation details and provider commits.
- Suitable for upstream submission; the integration may use the local hook while acceptance is
  pending.

The current core seams are `PlayerNetworkSetup.AvatarConstructionOverride` and `IsSitting`,
`AvatarSelectionManager.SuppressLegacyAvatars`, `SitSpot` occupancy authority plus the optional
`SeatedPelvisTarget`, and the generic `SceneLoader` EventSystem sweep. They are applied by the host
installer as one reviewable patch and are the intended upstream contribution (see
[Packaging and distribution](#packaging-and-distribution)). They must be judged as platform
extension points, not UMA features.

### Provider input forms

The framework supports both without complicating embodiment code:

- A provider that constructs or rebuilds a Humanoid at runtime, such as UMA.
- A provider that returns an already imported Humanoid prefab/model, such as a local FBX-based
  character.

Both paths converge on the same ready-instance contract (`IHumanoidAvatarInstance`). The framework
does not care how the provider obtained the instance.

### Shared versus provider responsibilities

| Capability | Framework responsibility | Provider responsibility |
|---|---|---|
| Humanoid embodiment | Alignment, selected scale policy, tracking lifecycle, animation/IK orchestration | Expose a valid Humanoid Animator, rebuild lifecycle, body measurements, and supported fitting capabilities |
| First-person visibility | Choose semantic visibility state and configure local camera | Identify/hide provider head geometry while preserving mirrors/remotes |
| Finger tracking | Shared tracking/joint targets and semantic finger contract | Skeleton mapping, axes, and unsupported-joint fallback |
| Sitting/posture | State, seat alignment, transitions, scale preservation, networking | Retargetable clips/overrides and provider-specific exceptions |
| Facial animation/lip sync | Shared semantic face/viseme input contract | Blendshape/bone/channel mapping and rebinding |
| Hair/clothing | No construction ownership beyond generic bounds/visibility | Construction, attachment, simulation, renderer discovery |
| Materials | Cross-platform validation requirements | Shader/material creation, conversion, and repair |
| Appearance data | Opaque versioned transport | Encode, validate, decode, and build provider payload |
| Avatar configuration UI | Provider-neutral Avatar Studio shell, tabs, preview area, theme, save action | Category rail entries and controls for its own appearance model |

The framework supplies a default retargetable Animator Controller (`GHA_Locomotion_v2`) and
baseline locomotion and posture clips. Providers may override clips or the controller when
necessary. Providers should declare optional capabilities such as fingers, face, head-only
visibility, runtime rebuilding, and material preparation so unsupported features degrade
deliberately.

### Physical fit and avatar scaling policy

Player measurement, avatar appearance, runtime pose, and scene grounding are separate concerns.
Conflating them caused scene-dependent scale and placement changes during the initial Home/Ocean
VR work. The accepted policy is an explicit physical-fit mode saved with the selected avatar
(`ProviderProportions`, `UniformScale`, `AuthoredScale`), provider metadata sufficient to choose a
mode safely, and a customizer that explains the trade-off instead of choosing silently.

**That policy is not implemented.** See [Roadmap R1](#r1-physical-fit-policy) for the full design
and the current behavior, which is a single uniform auto-scale applied to every provider.

What is implemented and must be preserved regardless of R1:

- Physical player measurements belong to a local calibration service independent of any scene
  (`VrCalibrationSession`).
- Avatar size, once resolved, is invariant across Home, Ocean Villa, and every other host. Scene
  entry may wait for valid tracking, establish the rendered floor, place the avatar, and solve
  head/hands/feet. It must not reinterpret tracking-origin offsets as player height, recalculate
  avatar size from a CharacterController/world-space head delta, or mutate provider appearance
  data. Tracking loss and scene changes may reset placement readiness, never physical fit.

```text
local physical profile -> selected fit policy -> locked avatar appearance/scale
scene rig + rendered floor -> root placement and IK only
```

## Current implementation (September 18, 2026)

### Where the code lives

| Layer | Location | Assembly | Notes |
|---|---|---|---|
| VertexForm3D Core seams | Five core scripts under `Assets/VertexForm3D/Scripts` | `Assembly-CSharp` | Applied and reverted by the host installer through `Installer/Editor/Patches/VertexFormGhaHost.patch`. |
| Framework | `Packages/com.vertexform3d.gha/Runtime` | `VertexForm.GHA.Runtime` | No UMA, Fusion, or VertexForm references; the asmdef enforces this. |
| UMA adapter, provider-neutral part | `Packages/com.vertexform3d.gha.uma/Runtime` | `VertexForm.GHA.UMA.Runtime` | Catalog, recipe codec/store, customizer row. References `UMA_Core`; compiled only under `VERTEXFORM_GHA_UMA`. |
| Host-coupled integration staging | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm` and `.../Integration/UMA` | `Assembly-CSharp` | Classes that still reference VertexForm implementation types directly. See `Integration/README.md` for the exit criteria. |
| Installer | `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor` | `Assembly-CSharp-Editor` | Two reversible layers; see `PACKAGE-BOUNDARIES.md`. |
| Generated/authored project assets | `Assets/VertexForm3D/3rdPartyAssets/GHA/Generated/GHAAvatarPanel.prefab`, `Assets/VertexForm3D/Resources/GHA` | — | Avatar Studio panel prefab and category/swatch icons. |
| UMA vendor content | `Assets/UMA`, `Assets/SourceShaders` | UMA's own asmdefs | Installed separately; never committed. Pinned to upstream master `c9204fe4` (= release v3.05). |

The two package folders are scheduled to move under `Assets/VertexForm3D/3rdPartyAssets/GHA`;
see [Packaging and distribution](#packaging-and-distribution). Their asmdefs and boundaries do not
change.

### Framework classes (`com.vertexform3d.gha`)

| Responsibility | Class | Notes |
|---|---|---|
| Provider contracts | `IHumanoidAvatarInstance`, `AvatarVisibility`, `HumanoidPosture`, `IHumanoidRigInput`, `HumanoidTrackingLifecycleUpdate` | `HumanoidAvatarContracts.cs`. The instance contract exposes `Root`, `Animator`, `IsReady`, `HumanoidRebuilt`, `SetFirstPersonVisibility(AvatarVisibility, int cullLayer)` and `TryGetRendererBounds(out Bounds)`. |
| Provider selection persistence | `AvatarProviderSelection` | Persists the selected mode byte locally. No registry maps the byte to an adapter yet (Roadmap R3). |
| Humanoid bone binding and arm IK | `HumanoidArmRig` | Unity Humanoid bone lookup, runtime Animation Rigging `RigBuilder`/`TwoBoneIKConstraint`, target proxies, wrist-axis offsets. VR-only since September 5; leaving VR releases the constraints to the Animator. |
| Body alignment and height calibration | `HumanoidVrAlignment`, `VrHeightCalibration`, `VrCalibrationSession` | Grounded root placement, yaw, eye-point correction; one-time uniform scale from the session's standing eye height; the session keeps the median accepted standing eye height and persists it. Thresholds, hold time and scale clamps are parameters supplied by the host adapter, not framework constants. |
| Seated pelvis alignment | `HumanoidSeatedPelvisAlignment` | Visual-root correction so the Hips bone lands on `SeatedPelvisTarget`. |
| Locomotion and posture animation | `HumanoidLocomotionDriver`, `HumanoidPostureAnimator` | Root-motion measurement, locomotion parameters, tracked-VR animator suppression; int `Posture` parameter contract. |
| Tracking lifecycle | `HumanoidTrackingLifecycle`, `XrRigStartupStabilizer` | HMD loss/regain transitions and the delayed focus/resume rebind queue; one-shot OpenXR Floor-tracking startup gate that suspends XRI body transformation until a stable pose is accepted and re-arms only on a tracking-origin change. |
| Presentation | `HumanoidAvatarPresentationController`, `AvatarCameraVisibilityController` | Semantic first-person visibility decision and local-camera culling for Home and networked hosts. |
| Avatar Studio shell | `AvatarConfigurationPanel`, `IAvatarConfigurationPanelProvider`, `AvatarConfigurationPanelProviderBehaviour`, `AvatarConfigurationPanelContext`, `AvatarConfigurationProviderRegistry`, `AvatarConfigurationTheme`, `AvatarPreviewFraming` | Provider tabs, category rail, preview area, save action, shared theme, and preview framing rules. Providers register at runtime; the shell is embedded under the existing Change Avatar station (see `PACKAGE-BOUNDARIES.md`). |
| Diagnostics | `AvatarLoadTimingLog` | `[GHA LOAD TIMING]` console entries used by the performance baseline. |

### UMA adapter classes

| Responsibility | Class | Location |
|---|---|---|
| Catalog: append-only races, wardrobe, body types, color channels, defaults | `UmaAvatarCatalog` | package |
| Recipe wire payload and PlayerPrefs persistence | `UmaRecipeCodec`, `UmaRecipeStore` | package |
| Customizer row control | `UmaCustomizerRow` | package |
| DCA construction, rebuild lifecycle, head renderer split, seated render suppression | `UmaAvatarPuppet` (implements `IHumanoidAvatarInstance`) | Integration staging |
| Fusion host: authority, provider build/teardown, payload publish, posture/seat publish | `UmaAvatarBridge` | Integration staging |
| Home host: persisted provider, Home rig targets, preview | `UmaHomeAvatar` | Integration staging |
| Avatar Studio provider: Body, Face, Colors, Outfits | `UmaAvatarConfigurationProvider`, `UmaAvatarCustomizer` | Integration staging |
| Remote proximity hiding | `RemoteAvatarProximityHider` | Integration staging |
| Load profiling | `AvatarLoadProfilerCapture`, `AvatarLoadProfileReportGenerator` | Integration staging |

`UmaAvatarPuppet`, `UmaAvatarBridge`, `UmaHomeAvatar`, the two Avatar Studio classes and the
proximity hider still reference `PlayerNetworkSetup`, `AvatarInputConverter` or other VertexForm
implementation types directly. That is why they compile in `Assembly-CSharp` and cannot yet live in
the package (Roadmap R5).

### VertexForm host integration classes

| Class | Role |
|---|---|
| `AvatarExtensionSync` | Fusion `NetworkBehaviour` with `Mode`, `Revision`, `Length`, `Data`, `Posture`, `PostureRevision`, `SeatId`. Provider-neutral. |
| `VertexFormHumanoidRigInput` | `IHumanoidRigInput` adapter over `AvatarInputConverter`, `PlayerNetworkSetup`, platform presentation and XR tracking. Requires the active OpenXR session to be Focused before treating the headset as worn. |
| `VertexFormStockAvatarConfigurationProvider` | Classic avatars as an Avatar Studio provider: Previous/Next selection, preview rebuild, save. Repairs stale `ProjectManager`/`AvatarSelectionManager` references after scene load. |

## UMA-specific work

### Avatar construction

The following behavior exists specifically because UMA builds avatars dynamically:

- Create and configure `DynamicCharacterAvatar` at runtime.
- Resolve a `RaceData` from the UMA global library.
- Apply `UMAWardrobeRecipe` slots.
- Populate `UMAPredefinedDNA` and preserve it across UMA rebuilds.
- Apply UMA shared-color channels and `OverlayColorData` swatches.
- Respect the DCA lifecycle rule that the first `BuildCharacter` must be left to `Start()`.
- Create missing `UMADataEvent` instances before adding `CharacterBegun` and
  `CharacterCreated` listeners.
- Reacquire the Animator and skeleton after each UMA rebuild.
- Stage wardrobe/DNA/colors with UMA building disabled and enable one build when the race
  changes; UMA's internal race-state cache is disabled on GHA-owned avatars because the GHA recipe
  owns their state (September 7).

These responsibilities belong in the UMA adapter and cannot be reused by a prefab avatar or another
runtime generator as written.

### Recipe, customization and body types

`UmaAvatarCatalog`, `UmaRecipeCodec`, `UmaRecipeStore`, `UmaAvatarCustomizer`, and
`UmaCustomizerRow` define UMA's authoring and player-choice model:

- Append-only race, wardrobe, DNA, and color identifiers. The index is the saved/network ID; all
  clients must ship the identical catalog.
- Body types (`bodyTypes`): a label, an explicit starting outfit and the race behind it. Human
  Male (race 0) and Human Female (race 1) are the initial types. Outfit choices are filtered by
  authored UMA race compatibility and wardrobe slots; missing compatibility metadata is never
  treated as permission to equip. The customizer remembers each type's outfit choices, including
  None, within one session; Save Avatar persists only the active type's recipe.
- UMA wardrobe-slot exclusivity, DNA slider names and values, shared-color palette selection.
- Preview DCA construction and rebuilds.
- The versioned wire payload and PlayerPrefs representation.

The idea of an opaque, versioned avatar payload is generic. The payload schema is UMA-specific and
stays behind the adapter.

### Head renderer separation

The user-facing requirement is generic: hide the local avatar's head from the HMD camera while
leaving the body visible and leaving mirrors and remote cameras unaffected.

The UMA implementation inspects `SlotData` during `CharacterBegun`, routes slots carrying UMA's
`Head` tag to a runtime `UMARendererAsset`, name-matches auxiliary slots such as eyes, mouth and
hair when tags live on overlays, and locates the resulting `SkinnedMeshRenderer` through
`UMAData.GetRendererAsset`. Another provider needs its own implementation. The generic layer
requests `HeadOnly`, `WholeBody`, or `Visible`; it does not know how a provider achieves it.

### UMA content and materials

Material validation is UMA content work. The current baseline is upstream master `c9204fe4`
(release v3.05) with 12 shader graphs replaced byte-for-byte from upstream develop `f4edf41ba`
because their master versions fail Unity's JSON import. `UMA_SG_Diffuse.shadergraph` is malformed
on both branches and remains unrepaired. A cross-platform material audit (Editor/URP, Quest,
WebGL, WebGPU/WebXR) has not been done (Roadmap R8).

## Reusable Humanoid embodiment work

### Rig inputs

The embodiment layer consumes semantic targets through `IHumanoidRigInput`: tracked head/eye
pose, body or locomotion anchor, left and right hand poses, floor height, local/remote authority
and presentation style, and whether controller tracking or optical hand tracking is active.
`VertexFormHumanoidRigInput` is the host adapter. `UmaAvatarPuppet` still reads some values from
`AvatarInputConverter` and `PlayerNetworkSetup` directly (Roadmap R5).

### Humanoid binding and IK

`HumanoidArmRig` resolves `Head`, `LeftUpperArm`, `LeftLowerArm`, `LeftHand` and right-side
equivalents through `Animator.GetBoneTransform`, builds `RigBuilder`, `Rig` and
`TwoBoneIKConstraint` at runtime, drives IK target proxies from tracked hand targets, applies
configurable wrist rotation profiles, and rebinds when the provider recreates its Animator. Arm IK
is created only in VR; desktop players use Animator-driven arms.

### Body alignment and height calibration

General embodiment rules, implemented in `HumanoidVrAlignment` and the calibration classes:

- Follow the player's body anchor in horizontal position and yaw.
- Align the avatar eye point with the tracked HMD eye point.
- Represent the head-bone-to-eye displacement as a configurable avatar profile.
- Start seated players at the provider's authored/default scale.
- Accept a plausible standing eye height only after a stability hold; the session keeps the
  median of accepted samples and persists it.
- Calculate avatar scale once and lock it.
- Never rescale the avatar when the player sits, crouches, or puts down the headset.
- Reset calibration only when intentionally changing/rebuilding the avatar or explicitly asking
  to recalibrate.

Minimum standing eye height, hold duration, drift tolerance and scale clamps are host-supplied
parameters. The July values quoted in earlier revisions (1.35 m, one second) are not framework
constants; the alignment code enforces a minimum 2.5 s hold.

### Tracking lifecycle and animation ownership

- Detect HMD tracking loss and regain (`HumanoidTrackingLifecycle`).
- Gate local XR-rig startup until the runtime has accepted Floor tracking and reports a present,
  position-tracked headset with a stable, plausible floor-relative pose; suspend XRI body
  transformation/gravity while pending; re-arm only on a tracking-origin change
  (`XrRigStartupStabilizer`).
- Rebind targets after application focus or headset resume.
- Preserve a completed height calibration across sleep/wake.
- Suppress idle/locomotion animation while live tracking owns the upper-body pose; restore it when
  tracking is genuinely unavailable (`HumanoidLocomotionDriver`).
- Emit change-based alignment diagnostics rather than frame-by-frame console spam.

For a local OpenXR host, `isTracked` and `userPresence` are not sufficient on every runtime;
Horizon Link may leave both true after the headset is removed. The host adapter therefore also
requires the active OpenXR session to remain Focused. When tracking ownership is lost, provider
constraints must be disabled and the current Animator pose evaluated immediately.

### Visibility policy

```text
Visible     - third person, mirrors, and remote players
HeadOnly    - local first-person VR; body and hands remain visible
WholeBody   - local desktop first person or temporary provider fallback
```

`HumanoidAvatarPresentationController` chooses the state; the provider performs the
renderer-specific operation; `AvatarCameraVisibilityController` owns local camera culling and
shared XR camera diagnostics.

## VertexForm3D/Fusion integration work

### Generic extension replication

`AvatarExtensionSync` is provider-neutral: `Mode` selects the provider, `Revision` signals a
rebuild, `Length`/`Data` carry an opaque versioned payload, `Posture` and `PostureRevision` carry
standing/sitting transitions, `SeatId` identifies the occupied networked `SitSpot`, and Fusion
authority determines who may publish. It must remain free of UMA references. The opaque payload
describes construction/customization, not continuous HMD and hand poses; those continue through
the existing multiplayer VR synchronization path.

### Provider selection and legacy coexistence

- `PlayerNetworkSetup.AvatarConstructionOverride` lets an external provider replace stock
  construction for one player.
- `AvatarSelectionManager.SuppressLegacyAvatars` keeps the Home avatar selector from fighting an
  external provider.
- Per-player mode switching and stock fallback allow mixed stock/provider sessions.
- `UmaAvatarBridge` translates Fusion state into provider build/teardown calls.

### Home and world hosts

`HumanoidAvatarPresentationController` serves both Home and networked world rigs.
`UmaHomeAvatar` and `UmaAvatarBridge` supply only host policy (first/third person, whether
head-only visibility is safe), camera discovery, provider creation/refresh, authority, and
persistence/network events. Construction and refresh remain provider/VertexForm concerns until a
second provider proves the factory contract (Roadmap R2).

### Avatar Studio

The provider-neutral panel is embedded under VertexForm's existing Change Avatar station in Home.
It has provider tabs (Classic, Custom), a three-column lower layout (provider-owned category rail,
opaque-black live preview, active controls), and identical bottom-right Save Avatar actions.
Classic contributes one Avatar category with Previous/Next; Custom contributes Body, Face, Colors
and Outfits. Preview framing: Classic centers and scales its renderer bounds to 94 % of the usable
preview; UMA keeps the authored preview transform, centers horizontally and pins the feet to the
preview floor so height grows upward. Only the selected provider's preview may be visible.

### Proximity hiding

`RemoteAvatarProximityHider` hides nearby remote avatars. It currently locates avatars through
`PlayerNetworkSetup` rather than through `IHumanoidAvatarInstance.TryGetRendererBounds`, so it is
host-coupled and would not automatically support another provider (Roadmap R4).

## Network boundary for sitting

Sitting is transient embodiment state, generic across providers, and is not part of the UMA
payload. The posture slice is implemented:

- `PlayerNetworkSetup.IsSitting` is the local VertexForm source; `UmaAvatarBridge` publishes it
  and the current `SeatId`; `HumanoidPostureAnimator` consumes the generic state.
- `GHA_Locomotion_v2.controller`, assigned to both player prefabs, exposes the int `Posture`
  parameter and transitions between its locomotion blend tree and the seated state. Tracked head
  and arm solving continues after the Animator.
- `SitSpot` acquires Shared Mode state authority before claiming or releasing a seat, replicates
  the occupying player's `NetworkId`, rejects double-booking, and recovers stale claims.
- Each seat may author an optional `SeatedPelvisTarget`. `SitPoint` remains the player/root anchor.
  After the seated loop owns the lower body, `HumanoidSeatedPelvisAlignment` moves the visual root
  so the Hips bone lands on the target and aligns the forward axis with its +Z. The target is
  resolved locally from the occupied `SitSpot` and remotely from the replicated `SeatId`.
- For the local player the host adapter applies the same horizontal delta to the XR camera
  offset; the standing posture event restores it. Provider rendering is suppressed from the sit
  request until pelvis/facing/view alignment succeeds; the UMA adapter does this with
  `Renderer.forceRenderingOff`.

Remaining contract items (roadmap, not scheduled): additional posture values such as crouching,
seat-height profile selection for the Ground/Low/Medium/High clips, an explicit replicated anchor
only if seats can move after occupation, and optional calibrated body scale if remote clients
cannot derive it deterministically.

## Status history

### July 14, 2026: framework extraction

First compiling extraction after local VR acceptance: `IHumanoidAvatarInstance`,
`AvatarVisibility`, `HumanoidArmRig`, `VrHeightCalibration`, `HumanoidLocomotionDriver`,
`IHumanoidRigInput`, `VertexFormHumanoidRigInput`, `HumanoidVrAlignment`,
`AvatarCameraVisibilityController`, `HumanoidTrackingLifecycle` and
`HumanoidAvatarPresentationController`. `UmaAvatarPuppet` became the UMA construction/renderer
adapter delegating IK, alignment, calibration and locomotion to the framework. The two packages
and `PACKAGE-BOUNDARIES.md` (July 28) were established in the working tree.

### August 2026: migration into the public branch

- August 18: ignored UMA vendor content rebuilt from official UMA v3.04 (`722b308a`) with
  `Tools/Sync-Uma.ps1`; Global Library rebuilt in the target, not copied.
- August 25: GHA fast-forwarded to official VertexForm3D PR #55 / Starter Kit 1.1.7; package
  manifest and lock reconciled; Fusion bake of both player prefabs verified; migration runbook and
  session handoff written.
- August 26: Avatar Studio built and validated live from LoginScene: Classic/Custom tabs, 3D
  preview, UMA Body/Face/Colors/Outfits workflow. Host integration fault fixed where stale
  `ProjectManager`/`AvatarSelectionManager` references hid Classic.
- August 28: Classic Previous/Next rebuild the visible preview; Classic and Custom share the Save
  Avatar layout.

### September 2026: content baseline, body types, installers, packaging

- September 1: preview framing policy (Classic bounds fit; UMA planted feet, upward growth).
- September 3: three-column reference layout with provider-owned category rail.
- September 4: UMA load performance baseline recorded (`GHA-UMA-PERFORMANCE-BASELINE-2026-09-04.md`).
- September 5: UMA vendor content upgraded to upstream master `c9204fe4`; 12 shader graphs
  repaired from develop `f4edf41ba` (destination only). VertexForm3D 1.1.9 merged. Arm IK made
  VR-only; desktop arm animation accepted by the owner. First GHA commits on the public branch:
  core seams, framework, UMA adapter, Avatar Studio checkpoint.
- September 7: Human Male/Female body types with per-type outfit recall. Host installer
  preflights targets, verifies saves and preserves snapshots; obsolete Cesium components removed
  from both player prefabs. UMA provider installer roots its references across UMA's rebuild,
  validates catalog Humanoid definitions, and creates the project-owned Global Library
  automatically on a fresh install. Source UMA checkout policy corrected to verbatim upstream.
- September 8–17: reviewer setup guide written and simplified; quick-setup appendix added.
- September 18: guide split into developer (source clone) and user (packages) editions;
  packaging and distribution decision recorded below; this document brought up to date.

## Packaging and distribution

Decision, September 18, 2026:

1. **Developers build from source.** The development workflow is this repository plus a clean
   UMA source checkout, pulling from both upstream repositories (VertexForm3D Master and UMA
   master). `Tools/Sync-Uma.ps1` installs the pinned UMA revision; the two Editor menus install
   the layers. When bumping the UMA pin, choose a released tag so developers and users share one
   revision (v3.05 = `c9204fe4` today). After merging upstream VertexForm3D, regenerate the host
   patch against the new stock versions of the five core scripts.
2. **The GHA host layer is to be contributed upstream to VertexForm3D**: the five core-script
   seams as real edits, `com.vertexform3d.gha`, the `Integration/VertexForm` adapters, the Avatar
   Studio panel prefab and icons, and the prefab/Home wiring the host installer performs today.
   Upstream ships these pre-wired, so the host installer and `VERTEXFORM_GHA_HOST` disappear for
   users. Required evidence: a stock-only runtime pass showing Classic avatars, networking and
   seating unchanged with no provider installed; zero UMA references; no new package dependencies
   (all four the package declares are already in the 1.1.9 manifest); a reviewable diff.
3. **`.unitypackage` files are strictly end-user deliverables** built from this repository by an
   Editor export script with a fixed path list per deliverable. Users install UMA from the
   official UMA release (`UMA3_f5.unitypackage`), then the GHA UMA provider package, then run
   Install UMA Provider Layer. Until the host layer is upstream, a temporary host package is a
   third download.
4. **Both package folders move under `Assets/VertexForm3D/3rdPartyAssets/GHA`.** A
   `.unitypackage` cannot carry `Packages/` content, and VertexForm3D's own Package Updater
   delivers `.unitypackage` files, so even the upstream host layer must live under `Assets` to
   reach users who update that way. The asmdefs move with the folders; the compile-time boundaries
   are unchanged. The 14 hardcoded `Packages/com.vertexform3d.gha*/...` paths in the installer
   and validation tools are updated; Unity drops the embedded records from `packages-lock.json`.
5. **UPM is a later option, not the current plan.** `com.vertexform3d.gha.uma` could become a
   UPM package once (a) the host contracts are upstream, (b) Roadmap R5 removes its direct
   VertexForm references, and (c) Roadmap R7 makes the catalog a project asset so an immutable
   package is never written to. Nothing in the current plan closes that route.

The user-facing steps are in `GHA-getting-started.md`; the source workflow is in
`GHA-developer-getting-started.md`.

## Roadmap

Every design commitment in this document that is not delivered, plus new work agreed since July.
Items are not ordered here; `GHA-IMPLEMENTATION-PLAN.md` at the repository root sequences them
together with the known defects.

### R1: Physical-fit policy

Not started. The accepted design:

| Mode | Intended providers | Behavior |
|---|---|---|
| `ProviderProportions` | UMA or another parametric provider | The provider adjusts stature-related proportions (legs, torso) toward the player's measurements. A small, bounded uniform residual scale may follow a rebuild. |
| `UniformScale` | Conventional imported Humanoid prefabs, FBX, GLB, or providers without proportion controls | Uniformly scale the ready avatar from its authored eye-to-sole measurement. Preserves skeleton, animation, IK and attachment relationships but changes every dimension, so it must be an explicit player/author choice. |
| `AuthoredScale` | Arbitrary imported avatars, unusual rigs, or any provider that cannot promise safe fitting | Preserve the authored scale; embody differently sized players with head/root following plus arm and leg IK within the rig's reach. The required conservative fallback and the default for an unknown source. |

Each provider or imported-avatar profile exposes: authored eye-to-sole height and the transforms
used to measure it; recommended uniform-scale range if supported; whether proportion fitting is
supported; available head/hand/foot targets and IK support; any rebuild notification required
before measurements are valid. Missing or invalid metadata resolves to `AuthoredScale`. The
resolved mode and any locked uniform scale belong to the selected avatar's embodiment profile and
are synchronized when needed. The customizer shows the measured standing eye height and the
avatar's authored height, offers "match my height" (provider-driven for UMA; uniform scale as an
explicit opt-in for generic Humanoids) and explains the trade-off.

Current behavior: `HumanoidVrAlignment` applies one uniform scale from the session's standing eye
height whenever `autoScale` is enabled, for every provider, with no mode, metadata or UI. This is
an implicit `UniformScale` and contradicts the policy's requirement that it be explicit.

### R2: Local Humanoid prefab/FBX proof provider

Not started. A provider that returns an already imported Humanoid prefab through the same
`IHumanoidAvatarInstance` contract, proving that shared embodiment does not depend on UMA's DCA
lifecycle. `UmaAvatarPuppet` is currently the only implementation of the contract, so the
construction factory generalization and the contract itself are unproven against a second body.

### R3: Provider registry for the mode byte

Not started. `AvatarProviderSelection` persists a mode byte; nothing maps it to an adapter and
payload codec. The Fusion host should select a provider through a registry rather than through
`UmaAvatarBridge` knowing it is UMA. Depends on R2 for a second registrant.

### R4: Proximity hiding through the instance contract

Not started. `RemoteAvatarProximityHider` should query registered avatar instances for renderer
bounds and visibility (`TryGetRendererBounds`) instead of locating avatars through
`PlayerNetworkSetup`.

### R5: Remove direct VertexForm references from the UMA adapter

In progress since July; six classes remain in `Integration/UMA` plus the UMA installer. Replace
their `PlayerNetworkSetup`, `AvatarInputConverter`, `ProjectManager`, `RoomManager`,
`AvatarSelectionManager` and Home station dependencies with host contracts in
`com.vertexform3d.gha` (implemented in `Integration/VertexForm`, which goes upstream). Exit
criterion from `Integration/README.md`. Prerequisite for UPM delivery of the UMA package.

### R6: Remote Vertex-compatible GLB provider

Future. A standard GLB cannot carry MonoBehaviours, Animator Controllers, `RigBuilder`, or
constraint components. A Vertex avatar export path should produce a validated Humanoid skeleton,
meshes, skins, materials, optional animations, Vertex metadata in GLTF extras or a companion
manifest, and a mapping profile. `RemoteHumanoidGlbProvider` downloads that package and returns the
ready-instance contract. Remote assets come from an experience-controlled catalog/allowlist. The
design must not block this provider; no downloader is required now.

### R7: Catalog as a project asset

Not started. `GhaUmaAssetInstaller` writes to `UmaAvatarCatalog.asset` inside the package when
color channels are missing, and body-type authoring edits the same asset. The installer should copy
the catalog into the project on first install and point the Home avatar, player prefabs and panel at
the copy, so users can add body types without editing package content and an immutable UPM package
is never written to.

### R8: UMA material and content audit

Not started. Audit UMA materials by race and wardrobe category on Editor/URP, Quest, WebGL and
WebGPU/WebXR. Fix only failing shader assignments, surface settings, textures and import settings.
Includes the deferred outfit thumbnail/material mismatches and lighting wash-out (September 5), and
a decision on `UMA_SG_Diffuse.shadergraph`.

### R9: UMA generation performance

Deferred until the functional gates are complete (runbook rule 1). UMA generation blocks the main
thread; a loading label does not make headset frames smooth. Baseline in
`GHA-UMA-PERFORMANCE-BASELINE-2026-09-04.md`; candidate approaches include UMA's incremental mesh
combiner (v3.02+) and pre-warmed default builds.

### R10: Host version gate in the installers

Not started. The host installer should refuse a mismatched VertexForm3D version explicitly instead
of failing on the source patch; the provider installer should refuse a mismatched host version.
Record supported VertexForm3D, UMA and GHA versions per release.

### R11: Jump animation

Not started (reported September 18). Determine whether VertexForm's locomotion exposes a jump
input and whether `GHA_Locomotion_v2` or the supplied clips include a jump state; if a clip is
available, wire the state and trigger; if not, source or author one. Provider-neutral: belongs in
`HumanoidLocomotionDriver` and the shared controller, not in the UMA adapter.

### R12: Upstream contribution of the host layer

Not started. See [Packaging and distribution](#packaging-and-distribution) item 2. Depends on the
package folder move (item 4) and on the stock-only runtime evidence from R13.

### R13: Remaining runtime validation matrix

In progress. From the runbook: stock-only session, UMA-only session, persistence, spawn/remote
reconstruction, scene transitions, seating, two-client, PC Link VR regression after the September 5
arm change, Quest standalone, WebGL/WebGPU/WebXR. Mixed-provider Change Avatar opening, provider
switching and preview construction have accepted Desktop evidence; the rest is pending.

### R14: Sitting contract extensions

Not scheduled. Crouching, seat-height profiles, movable-seat anchors, replicated body scale (see
[Network boundary for sitting](#network-boundary-for-sitting)).

### R15: Lip sync and facial input

Not started (added September 18). The responsibility split above assigns the framework a shared
semantic face/viseme input contract and the provider the blendshape/bone/channel mapping and
rebinding; `REFERENCE-REVIEW.md` items 7 and 16 record the same intent. Nothing implements it.

What exists today:

- VertexForm core has a stock, amplitude-only lip sync in `HighlightVoice` on the player prefab:
  it samples the Photon Voice `Speaker` AudioSource and drives one `viseme_O` blendshape on a
  serialized Classic head mesh. It runs for remote players only (the local Recorder link is
  commented out) and it is Classic-only, because the head mesh is a serialized reference.
- UMA 3 supplies two expression systems on the human races: the legacy `UMAExpressionPlayer`
  (`jawOpen_Close`, `mouthNarrow_Pucker`, lip channels, optional Mecanim jaw override) and the
  v3.04 `DynamicExpressionPlayer` driven by `UMAExpressionGroup` DNA definitions with a
  `SetExpression(id, value)` API. Neither is attached to GHA avatars, and no GHA code references
  them.
- The GHA UMA puppet already knows the generated head renderer (it routes the head slots to its
  own `UMARendererAsset`) and, after BUG-11, re-runs its completion handling on every rebuild, which
  is the rebind point lip sync needs.
- The retired Ready Player Me project (`vertexform3d-unity-vr-starterkit-Dev - RPM`, reviewed
  September 18) is the reference to reinvestigate, with two corrections to the recollection. It
  does **not** contain Oculus LipSync: no `OVRLipSync` package or scripts exist there. Its lip sync
  is the RPM SDK's `LipSync.cs`, an amplitude-only driver that samples the **local microphone**
  directly (`Microphone.Start`, 4096-sample window, ×10 gain, clamped) and writes one `mouthOpen`
  blendshape on the head, beard and teeth meshes; it never used the voice network stream. The
  viseme-style name `viseme_O` in today's `HighlightVoice` is the Oculus/RPM blendshape naming
  left over from that era; the Classic heads carry no such shape, so the stock lip sync is
  effectively inert on Classic avatars. What is worth reusing from RPM: sampling the local
  microphone for the local player's own mouth (mirrors, third person), the multi-mesh blendshape
  map, and the platform microphone-permission handling for Android/iOS. What is not: the SDK's
  RPM-specific mesh lookup, and a second microphone capture next to Photon Voice's `Recorder`,
  which should be the single local audio source.

Design:

1. Framework: `IHumanoidFaceInput` (or an extension of `IHumanoidRigInput`) carrying a normalized
   mouth-open level now and a viseme weight set later; a `HumanoidFaceDriver` that smooths and
   gates it, mirroring `HumanoidLocomotionDriver`. Provider capability flag `SupportsLipSync` so
   unsupported providers degrade silently.
2. Host adapter (`Integration/VertexForm`): feed the level from Photon Voice, remote players from
   the `Speaker` AudioSource as `HighlightVoice` does, local player from the `Recorder` level
   meter so mirrors and third person move the local mouth. No new network data: each client derives
   the level from the audio it already receives, as stock does.
3. UMA provider: map the level to the race's expression system (prefer `DynamicExpressionPlayer`
   with the human `DynamicExpressionSet`; fall back to `UMAExpressionPlayer.jawOpen_Close`),
   attach the player at build, rebind after every rebuild through the puppet's completion handler.
4. Classic provider: keep `HighlightVoice` behavior but source it from the same contract, so the
   stock path and GHA path cannot drift.
5. Later: a real viseme analyzer (uLipSync, Oculus LipSync, or Photon's) behind the same contract;
   the analyzer choice is a host decision, not a provider one.

Acceptance: remote UMA and Classic mouths move with speech on desktop and PC Link; the local UMA
mouth moves in the Home mirror; no movement when muted; no regression after Save Avatar rebuilds;
performance measured per frame on Quest.

## Known defects

Status as of September 18, 2026. "Confirmed" means reproduced by the owner; the September 18 entries
were confirmed in the owner's own sessions, the earlier ones are recorded in the runbook or handoff.
"Fixed, owner-verified on desktop" means the owner re-ran the repro in the Editor on the desktop
platform after the fix; PC Link VR and second-client checks for those fixes are still part of R13.
Fixed entries stay listed until the fix is committed.

| ID | Status | Area | Description |
|---|---|---|---|
| BUG-1 | Fixed Sep 18, owner-verified on desktop | Home / Avatar Studio | Two avatars visible at once, mostly right after startup. Repro: the persisted choice from the previous run is UMA; enter Home; open Change Avatar and pick Classic. The Classic avatar spawns as the player but the persisted UMA avatar is not hidden. Switching back to UMA, saving, then choosing Classic again works. Cause (confirmed live, Sep 18, from the Console and a read-only scene inspection): two faults. (1) With no saved mode key, the hosts default to the catalog's default system (UMA) while the Studio defaulted to its first tab (Classic), so the Studio opened on Classic although the rig had built UMA. (2) Selecting the Classic tab, or pressing Previous/Next, had the stock provider switch `SuppressLegacyAvatars` off and run `AvatarSelectionManager.ActivateAvatarModelAt`, which instantiates the head/body both into the station preview and onto the rig's `CustomAvatar` (`headTransform`/`bodyTransform`) and reactivates that root. That put a Classic body on the rig next to the live UMA puppet without any save. After a UMA save, `UmaHomeAvatar.Refresh` hid the root again, which is why the sequence "UMA, save, Classic" worked. Fix applied Sep 18 (compiles with zero errors; runtime check pending): `ActivateAvatarModelAt` now refreshes only the platform preview while suppression is on (core seam, host patch regenerated); the stock provider no longer toggles suppression; `AvatarProviderSelection.DefaultMode` carries the host default so the Studio opens on the provider the host built. The rig's body still changes only on Save through `UmaHomeAvatar`. |
| BUG-2 | Fixed Sep 18, owner-verified on desktop | Avatar Studio / UMA | DNA slider changes reach the preview but are not applied to the player avatar, and are not applied to either avatar after a restart, although the values persist. Colors and outfits save and restore correctly. Owner log from a save: `Saved — system 'UMA Avatar', race 0, 4 wardrobe, 2 dna, 3 color(s)`, followed by `[UmaHomeAvatar] Refresh — mode UMA`, a rebuild trace (`build=rebuild ... dna=2`) and `[UmaAvatarPuppet] Rebuilt avatar ... 2 dna`. So the recipe carries the two DNA values through save, persistence and rebuild; the fault is in applying them. Cause (confirmed in source and in the Editor, Sep 18): both `UmaAvatarPuppet.ApplyDna` and the customizer's `ApplyPreviewDna` stage the values in the DCA's `predefinedDNA`. UMA's `ApplyPredefinedDNA()` returns immediately for races with `useNewDNA`, and both `Human Male 3.0` and `Human Female 3.0` have `useNewDNA=True` (read from the RaceData assets through the Editor). So predefined DNA is never applied to these races on any build. Live slider moves work only because they go through `GetDNA()` setters plus a DNA-only `ForceUpdate`. Fix applied Sep 18 (compiles, runtime check pending): both paths now also write the values through the DNA setters, before a plain rebuild (UMA keeps the live new-DNA collection across it) or in the `CharacterUpdated` callback after a first or race-change build, followed by `ForceUpdate(true, false, false)`. `predefinedDNA` staging is kept for legacy-DNA races. |
| BUG-3 | Confirmed Sep 18 | Locomotion | The walk animation sometimes does not play on the first load of Home; the avatar slides with idle playing. Not yet seen in the Editor with the Console open; trigger and recovery unknown. Suspects: `HumanoidLocomotionDriver` root-motion measurement not started for the first build, tracked-VR suppression left on for a desktop session, or the Animator reference not reacquired after the first UMA rebuild. Owner will capture Console output on the next occurrence. |
| BUG-4 | Confirmed Aug 26 | Runtime | Repeated XR Affordance receiver and `AvatarInputConverter.Update` null-reference errors, pre-existing before Avatar Studio work; needs a separate gameplay-debug pass. |
| BUG-5 | Confirmed Sep 5 | UMA content (upstream) | `Assets/UMA/SRP/ShaderGraphs/Materials/UMA_SG_Diffuse.shadergraph` fails JSON import on upstream master and develop. Not repaired; tracked under R8. |
| BUG-6 | Confirmed Sep 7 | VertexForm content (upstream) | SketchUp importer assertions for `Assets/VertexForm3D/Example_Assets/Ocean Villa/Tree.skp`. Not a GHA defect; report upstream. |
| BUG-7 | Confirmed Sep 7 | VertexForm content (upstream) | Missing nested VRKeys prefab references in the `[ENVIRONMENT].prefab` assets under `LoginSceneAssets/` and `HomeSceneAssets/`. Not a GHA defect; report upstream. |
| BUG-8 | Confirmed Sep 5, deferred | UMA content | Some outfit thumbnails differ from their materials; lighting washes out colors. Tracked under R8. |
| BUG-9 | Confirmed Sep 4 | Performance | UMA generation stalls the main thread on first and repeated loads. Tracked under R9. |
| BUG-10 | Pending recheck | VR | PC Link VR regression check after the September 5 VR-only arm IK change has not been performed. Tracked under R13. |
| BUG-11 | Fixed Sep 18, owner-verified on desktop | UMA adapter | After any rebuild of the player avatar (Save with slider, outfit or color changes) in first person, the camera appears to sit inside the head: the whole avatar renders around the camera. Live inspection showed the puppet still requesting head/body hidden on cull layer 7 while both freshly generated renderers sat on layer 0. Cause: the puppet handled build completion only in UMA's `CharacterCreated`, which fires once per character; rebuilds raise only `CharacterUpdated`, so visibility re-application, bone caching, IK rebind and `HumanoidRebuilt` were skipped after every rebuild. Fix applied Sep 18 (compiles with zero errors; runtime check pending): completion handling runs on every non-cancelled `CharacterUpdated`, once per build. Likely also a contributor to BUG-3, since Animator/bone state was not refreshed after rebuilds. Separate observation from the same session: a single mouse-wheel notch over the Studio UI is consumed by `XRRigController.HandleZoom` and can flip the rig into first person; upstream VertexForm behavior, not GHA. |

| BUG-12 | Confirmed Sep 18 | XR Interaction Toolkit (upstream) | `No available indices for pointer registration` logged from XRI's `XRUIToolkitHandler` while Fusion instantiates a remote player prefab (`NetworkObjectProviderDefault.InstantiatePrefab`). The spawned prefab's XR interactors register as UI pointers and XRI's pointer table is full. Not GHA: the player prefabs carry the interactors in stock VertexForm. Effect is that the extra pointers cannot drive UI Toolkit panels, which remote players should not do anyway. Fix belongs upstream: disable UI interactors on non-local players at spawn, or raise nothing and accept the log. Track with BUG-6/BUG-7 as upstream reports. |

Jump animation (reported September 18) is a missing feature, not a defect, and is Roadmap R11.

## Near-term rules

- UMA build/render/customization fixes may remain in `UmaAvatarPuppet` and the staging classes.
- New XR calibration, tracking, IK, posture, or visibility behavior is written against Unity
  Humanoid concepts only, in `com.vertexform3d.gha`, even if its entry point is a UMA-named class.
- Do not put seated state, hand poses, or provider-independent rig data into `UmaRecipeCodec`.
- Keep `AvatarExtensionSync` provider-neutral.
- Treat material fixes as UMA content work, not embodiment work.
- Do not modify pristine UMA vendor source to solve GHA integration issues; repairs are
  destination-only and recorded.
- Do not start R9 performance work before the functional gates in R13 are complete.
