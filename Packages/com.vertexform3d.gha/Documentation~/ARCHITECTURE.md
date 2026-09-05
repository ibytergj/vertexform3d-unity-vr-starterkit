# Avatar Suite Architecture Boundaries

Status: July 12, 2026

Reference review: see `REFERENCE-REVIEW.md` for the consolidated platform documentation,
player-prefab documentation, retired RPM rig audit, implementation decisions, and revised work
order.

## Accepted Product Direction

The target product boundary is:

```text
VertexForm3D Core
    Minimal, inert, provider-neutral extension hooks

Avatar Model Integration Framework
    Shared Unity Humanoid embodiment and VertexForm integration

Avatar provider packages
    UMA 3 provider (initial production provider)
    Local Humanoid prefab/FBX provider (framework proof provider)
    Remote Vertex-compatible GLB provider (future)
```

This framework integrates humanoid 3D character models into VertexForm3D. It is not an abstraction
over third-party avatar platforms, account systems, cloud services, or vendor SDKs. Character
Creator, MetaHuman, Blender, or another source matters only to the provider that turns its output
into a compatible Unity Humanoid instance.

Non-humanoid avatars are explicitly outside the current framework scope. They may be supported by
a future parallel integration system without weakening the Humanoid contract defined here.

### Core-change policy

Changes proposed to VertexForm3D Core must be:

- Minimal and easy to review independently.
- Generic across Humanoid model providers.
- Inert when the integration framework is absent or no provider is selected.
- Non-breaking for the existing stock avatar path.
- Kept separate from UMA implementation details and provider commits.
- Suitable for upstream submission; the add-on may use the local hook while acceptance is pending.

Current examples are `PlayerNetworkSetup.AvatarConstructionOverride` and the generic
`AvatarExtensionSync` transport. These should be judged as platform extension points, not UMA
features.

### Provider input forms

The framework supports both without complicating embodiment code:

- A provider that constructs or rebuilds a Humanoid at runtime, such as UMA.
- A provider that returns an already imported Humanoid prefab/model, such as a local FBX-based
  character.

Both paths converge on the same ready-instance contract. The framework does not care how the
provider obtained the instance.

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

The framework should supply a default retargetable Animator Controller and baseline locomotion and
posture clips. Providers may override clips or the controller when necessary. Providers should
declare optional capabilities such as fingers, face, head-only visibility, runtime rebuilding,
and material preparation so unsupported features degrade deliberately.

### Physical fit and avatar scaling policy

Player measurement, avatar appearance, runtime pose, and scene grounding are separate concerns.
Conflating them caused scene-dependent scale and placement changes during the initial Home/Ocean
VR work. The framework therefore needs an explicit physical-fit mode saved with the selected
avatar:

| Mode | Intended providers | Behavior |
|---|---|---|
| `ProviderProportions` | UMA or another parametric provider | The provider adjusts stature-related proportions (for example legs and torso) toward the player's physical measurements. A small, bounded uniform residual scale may be used after rebuilding. |
| `UniformScale` | Conventional imported Humanoid prefabs, FBX, GLB, or providers without proportion controls | The framework uniformly scales the complete ready avatar from its authored eye-to-sole measurement. This preserves skeleton, animation, IK, renderer, attachment, and collider relationships, but also changes width, head size, and every other dimension. It must therefore be an explicit player/author choice, not an invisible scene correction. |
| `AuthoredScale` | Arbitrary imported avatars, unusual rigs, or any provider that cannot promise safe fitting | Preserve the provider's authored scale. Use head/root following plus arm and leg IK, comparable to the retired RPM behavior, to embody differently sized players within the rig's reach limits. This is the required conservative fallback. |

The three modes are ordered by capability, not quality. `ProviderProportions` is the preferred
UMA experience because the player can tune height, leg length, torso length, head size, and other
dimensions without indiscriminately widening the entire character. It is not available for an
arbitrary model merely because that model has a Humanoid Animator. `UniformScale` is the generic
opt-in compromise. `AuthoredScale` is the predictable zero-assumption baseline and should be the
default for an avatar imported from an unknown source.

Each provider or imported-avatar profile should expose enough provider-neutral metadata for the
framework to make a safe decision:

- Authored eye-to-sole height and the transforms used to measure it.
- Recommended minimum and maximum uniform scale, if uniform scaling is supported.
- Whether provider-driven proportion fitting is supported.
- Available head, hand, and foot targets and whether arm/leg IK is supported.
- Any provider-specific rebuild notification required before measurements are valid.

Missing or invalid metadata must resolve to `AuthoredScale`; the framework must not guess that an
unknown rig is safe to resize or reshape.

Physical player measurements belong to a local calibration/profile service and are independent
of any scene. Provider-specific stature and proportion choices belong in that provider's saved
avatar payload. The resolved physical-fit mode and any resulting locked uniform scale belong to
the selected avatar's embodiment profile and, when necessary, are synchronized so remote clients
reconstruct the same appearance.

The customizer/calibration experience should explain the tradeoff rather than silently choosing:

- Show the player's stable measured standing eye height or estimated physical height.
- Show the avatar's authored/current height when the provider can measure it.
- For UMA, offer a provider-driven "match my height" starting point plus separate height, leg,
  torso, head, and other appearance controls.
- For a generic compatible Humanoid, offer "keep original proportions" and an explicit
  "match my height using uniform scale" option.
- For an unknown or unsupported rig, keep the authored size and explain that pose/IK adapts the
  avatar without changing its proportions.

Once resolved, avatar size must remain invariant across Home, Ocean Villa, and every other host.
Scene entry may wait for valid tracking, establish the scene's rendered floor, place the avatar,
and solve head/hands/feet. It must not reinterpret tracking-origin offsets as player height,
recalculate avatar size from a CharacterController/world-space head delta, or mutate provider
appearance data. Tracking loss and scene changes may reset placement readiness, never physical
fit. This preserves the accepted separation:

```text
local physical profile -> selected fit policy -> locked avatar appearance/scale
scene rig + rendered floor -> root placement and IK only
```

### Future remote Humanoid GLB provider

Remote loading is a future provider, not current UMA V2 scope. A standard GLB cannot carry Unity
MonoBehaviours, Animator Controllers, `RigBuilder`, or Unity constraint components. A future
Vertex-compatible avatar authoring/export path should instead produce:

- A validated Humanoid skeleton, meshes, skins, materials, and optional animations.
- Vertex-specific metadata in GLTF extras or a companion versioned manifest.
- A mapping profile sufficient for the provider to create the Unity Humanoid integration.

`RemoteHumanoidGlbProvider` would download that package and return the same ready-instance contract
as UMA or the local prefab provider. The framework would then attach its normal embodiment rig.
Initially, remote assets should come from an experience-controlled catalog/allowlist for security,
deterministic multiplayer, caching, and CORS reliability. The design must not block this provider,
but no remote downloader is required for the initial extraction.

## Purpose

The current add-on proves that UMA avatars can coexist with the stock VertexForm3D avatar
system and can be embodied by the existing desktop and XR rigs. During that work, several
generally useful avatar features were implemented inside classes named `Uma*`, especially
`UmaAvatarPuppet`.

This document separates the implementation into three concerns:

1. UMA-specific avatar construction and customization.
2. Reusable humanoid embodiment behavior.
3. VertexForm3D and Fusion integration that is generic across avatar providers, but is not
   independent of this application framework.

The desired dependency direction is:

```text
VertexForm/Fusion integration ---> generic humanoid embodiment <--- UMA adapter
              |                              ^                      (or another provider)
              +------ opaque avatar data ----+
```

Generic embodiment code must not reference UMA types. An avatar-provider adapter may reference
the generic embodiment API, Unity Humanoid APIs, and its own model-construction dependencies.

## Classification Summary

| Area | Classification | Current location | Long-term owner |
|---|---|---|---|
| Race, wardrobe, DNA, shared colors | UMA-specific | `UmaAvatarCatalog`, `UmaAvatarCustomizer`, `UmaAvatarPuppet` | UMA adapter |
| DCA creation and rebuild lifecycle | UMA-specific | `UmaAvatarPuppet` | UMA adapter |
| UMA head-slot renderer split | UMA-specific implementation of a generic visibility capability | `UmaAvatarPuppet` | UMA adapter |
| Recipe encoding and local persistence | Current payload is UMA-specific; transport pattern is generic | `UmaRecipeCodec`, `UmaRecipeStore` | UMA adapter over generic transport |
| Humanoid bone discovery | Generic | `UmaAvatarPuppet` | Generic embodiment |
| Head and hand target following | Generic | `UmaAvatarPuppet` | Generic embodiment |
| Two-bone arm IK | Generic Unity humanoid rigging | `UmaAvatarPuppet` | Generic embodiment |
| Wrist offsets for hands/controllers | Generic policy with avatar-specific profiles | `UmaAvatarPuppet` | Generic embodiment + provider profile |
| Player measurement and physical-fit policy | Generic contract and persistence policy | `VrHeightCalibration`, `HumanoidVrAlignment`, `UmaAvatarPuppet` | Generic embodiment/profile service |
| Proportion-driven stature fitting | Provider capability | UMA DNA/customizer | UMA adapter (or another parametric provider) |
| Uniform scale application | Generic, only when selected and supported | `HumanoidVrAlignment` | Generic embodiment |
| Authored-scale fallback | Generic policy using provider-authored size plus IK | Partial in current embodiment/RPM reference | Generic embodiment |
| HMD sleep/focus tracking recovery | Generic XR lifecycle | `HumanoidTrackingLifecycle`, with provider rebind execution in `UmaAvatarPuppet` | Generic embodiment |
| VR idle-animation suppression | Generic | `UmaAvatarPuppet` | Generic embodiment |
| Standing/sitting Animator contract | Generic Unity Humanoid behavior | `HumanoidPostureAnimator`, applied by `UmaAvatarPuppet` | Generic embodiment |
| First-person head/body visibility policy | Generic policy | `HumanoidAvatarPresentationController`, with host policy inputs | Generic embodiment/integration |
| Renderer-level head hiding | Provider-specific capability | `UmaAvatarPuppet` | Avatar-provider adapter |
| Local camera culling-mask setup | Generic Unity camera behavior | `HumanoidAvatarPresentationController`, `AvatarCameraVisibilityController` | Generic embodiment/integration |
| Local versus remote authority | VertexForm/Fusion-specific | `UmaAvatarBridge`, `UmaAvatarPuppet` | VertexForm integration |
| Opaque mode/revision/data replication | Generic avatar extension transport | `AvatarExtensionSync` | VertexForm core/integration |
| Standing/sitting posture replication | Generic avatar extension transport | `AvatarExtensionSync.Posture` / `PostureRevision`, published by `UmaAvatarBridge` | VertexForm core/integration |
| Legacy avatar suppression/switching | VertexForm-specific | `UmaAvatarBridge`, `UmaHomeAvatar` | VertexForm integration |
| Remote proximity hiding | Generic concept; current implementation knows stock and UMA layouts | `RemoteAvatarProximityHider` | Generic integration with renderer providers |
| Material/shader repair | UMA-content-specific for current assets | UMA material assets, pending audit | UMA adapter/content package |

## UMA-Specific Work

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

These responsibilities belong in an UMA avatar-provider adapter and cannot be reused by a
prefab avatar, Ready Player Me avatar, Meta Avatar, or another runtime generator as written.

### UMA recipe and customization

`UmaAvatarCatalog`, `UmaRecipeCodec`, `UmaRecipeStore`, `UmaAvatarCustomizer`, and
`UmaCustomizerRow` currently define UMA's authoring and player-choice model:

- Append-only race, wardrobe, DNA, and color identifiers.
- UMA wardrobe-slot exclusivity.
- UMA DNA slider names and values.
- UMA shared-color palette selection.
- Preview DCA construction and rebuilds.
- The current versioned wire payload and PlayerPrefs representation.

The idea of an opaque, versioned avatar payload is generic. The payload schema itself is UMA
specific and should remain behind the UMA adapter.

### Head renderer separation

The user-facing requirement is generic: hide the local avatar's head from the HMD camera while
leaving the body visible and leaving mirrors and remote cameras unaffected.

The current implementation is UMA-specific:

- Inspect `SlotData` during `CharacterBegun`.
- Route slots carrying UMA's `Head` tag to a runtime `UMARendererAsset`.
- Name-match auxiliary UMA slots such as eyes, mouth, and hair when tags live on overlays.
- Locate the resulting `SkinnedMeshRenderer` through `UMAData.GetRendererAsset`.

Another provider needs its own implementation, such as disabling known head renderers, applying
a local-camera render feature, hiding head blend shapes, or using provider-supplied first-person
visibility controls. The generic layer should request `HeadOnly`, `WholeBody`, or `Visible`; it
should not know how a provider achieves that result.

### UMA material pass

Material validation is an UMA content responsibility for this package. It should audit the UMA
materials by race and wardrobe category on Editor/URP, Quest, WebGL, and WebGPU/WebXR. Existing
good materials should remain untouched. Fixes should be limited to failing shader assignments,
surface settings, textures, and import settings.

This material pass is not part of generic XR embodiment, although a generic avatar-provider
validation checklist can require each provider to declare its supported render pipelines and
platforms.

## Reusable Humanoid Embodiment Work

The following behavior is not inherently UMA-specific and should be reusable with any Unity
Humanoid avatar that can expose an Animator, a root transform, and renderer visibility controls.

### Rig inputs

The embodiment layer consumes these semantic targets:

- Tracked head/eye pose.
- Body or locomotion anchor.
- Left and right hand poses.
- Floor height.
- Local/remote ownership and presentation style.
- Whether controller tracking or optical hand tracking is active.

Today these values are read directly from `AvatarInputConverter` and `PlayerNetworkSetup`.
Those are VertexForm integration details. The generic component should receive a small rig-input
interface or serialized target set instead.

### Humanoid binding and IK

The following implementation uses standard Unity humanoid and Animation Rigging APIs:

- Resolve `Head`, `LeftUpperArm`, `LeftLowerArm`, `LeftHand`, and right-side equivalents through
  `Animator.GetBoneTransform(HumanBodyBones.*)`.
- Build `RigBuilder`, `Rig`, and `TwoBoneIKConstraint` components at runtime.
- Drive IK target proxies from tracked hand targets.
- Apply configurable hand/controller wrist rotation profiles.
- Retry binding when the provider recreates its Animator.

The generic layer should own the constraints and target updates. The provider adapter should
notify it when a new humanoid Animator is ready.

### Body alignment and height calibration

These are general embodiment rules:

- Follow the player's body anchor in horizontal position and yaw.
- Align the avatar eye point with the tracked HMD eye point.
- Represent the head-bone-to-eye displacement as a configurable avatar profile.
- Start seated players at the provider's authored/default scale.
- Accept a plausible standing eye height only after a stability hold.
- Calculate avatar scale once from standing eye height and lock it.
- Never rescale the avatar when the player sits, crouches, or puts down the headset.
- Reset calibration only when intentionally changing/rebuilding the avatar or explicitly asking
  to recalibrate.

The current `1.35 m` standing threshold, one-second hold, scale clamp, and eye offsets are tuning
defaults, not UMA rules. They should live in a generic calibration profile and may eventually be
persisted per user.

### Tracking lifecycle and animation ownership

The following work applies to any XR avatar:

- Detect HMD tracking loss and regain.
- Gate local XR-rig startup until the runtime has accepted Floor tracking and reports a present,
  position-tracked headset with a stable, physically plausible floor-relative pose. While this
  one-shot gate is pending, suspend XRI body transformation/gravity so a transitional camera pose
  cannot create an invalid CharacterController or move the player root. Re-arm only when the XR
  runtime reports a tracking-origin change; do not continuously rewrite a settled rig.
- Rebind targets after application focus or headset resume.
- Preserve a completed height calibration across sleep/wake.
- Suppress idle/locomotion animation while live tracking owns the upper-body pose.
- Restore normal animation when tracking is genuinely unavailable.
- Emit change-based alignment diagnostics rather than frame-by-frame console spam.

For a local OpenXR host, `isTracked` and `userPresence` are not sufficient on every runtime:
Horizon Link may leave both true after the headset is removed. The host adapter therefore also
requires the active OpenXR session to remain in the Focused state. When tracking ownership is
lost, provider constraints must be disabled and the current Animator pose evaluated immediately;
simply stopping future tracked-bone writes can leave the final tracked head rotation latched.

Seated animation belongs in this layer as a posture system. Sitting must drive pose/state, hips,
spine, and legs while preserving the locked body scale and tracked head/hands.

### Visibility policy

The generic visibility states should be semantic:

```text
Visible     - third person, mirrors, and remote players
HeadOnly    - local first-person VR; body and hands remain visible
WholeBody   - local desktop first person or temporary provider fallback
```

The embodiment/integration layer chooses the state. The avatar-provider adapter performs the
renderer-specific operation. Local camera culling and mirror-camera inclusion are generic Unity
integration concerns.

## VertexForm3D/Fusion Integration Work

This layer is reusable across avatar providers in Generic Humanoid Avatars, but it is coupled to
VertexForm3D and Fusion.

### Generic extension replication

`AvatarExtensionSync` is already provider-neutral:

- `Mode` selects the avatar provider.
- `Revision` signals a rebuild.
- `Length` and `Data` carry an opaque versioned payload.
- `Posture` carries provider-neutral standing/sitting state.
- `PostureRevision` identifies posture transitions for proxies and late joiners.
- `SeatId` identifies the occupied networked `SitSpot`; the player `NetworkTransform` carries the
  live anchor position and facing.
- Fusion authority determines who may publish avatar state.

It should remain free of UMA references. A provider registry can eventually map each mode byte to
an adapter and payload codec.

The opaque payload describes avatar construction/customization, not continuous HMD and hand poses.
Those poses should continue through the existing multiplayer VR synchronization path.

### Provider selection and legacy coexistence

The following changes are generic extension points even though UMA is the first consumer:

- `PlayerNetworkSetup.AvatarConstructionOverride` allows an external provider to replace stock
  construction for one player.
- `AvatarSelectionManager.SuppressLegacyAvatars` prevents the Home avatar selector from fighting
  an external provider.
- Per-player mode switching and stock fallback allow mixed stock/provider sessions.
- `UmaAvatarBridge` currently translates Fusion state into provider build/teardown calls.

The hook and mode transport should become provider-neutral core/integration facilities. Stock
avatar teardown details remain a VertexForm adapter concern.

### Home and world hosts

`HumanoidAvatarPresentationController` now serves both Home and networked world rigs for semantic
first-person visibility and local-camera culling. `UmaHomeAvatar` and `UmaAvatarBridge` supply
only their host policy (first/third person and whether head-only visibility is safe), camera
discovery, provider creation/refresh, authority, and persistence/network events. Construction and
refresh remain provider/VertexForm concerns until a second avatar provider proves the required
factory contract.

### Proximity hiding

Remote-avatar proximity hiding is provider-neutral behavior, but the current implementation knows
how to find both stock and UMA renderer hierarchies. It should query registered avatar instances
for renderer bounds and visibility instead. That would automatically support future providers.

## Current Mixed-Class Debt

`UmaAvatarPuppet` is the primary mixed class. It currently owns:

- UMA build, recipe, DNA, colors, and renderer splitting.
- Unity humanoid bone binding and arm IK.
- Provider-specific skeleton/IK rebind execution after generic tracking recovery requests.
- height calibration and body alignment.
- animation suppression and locomotion.
- visibility execution and diagnostics.
- direct VertexForm authority/input queries.

This was useful while establishing working behavior, but it is the wrong long-term boundary.
Adding another avatar provider today would either duplicate the VR work or force that provider to
depend on a class that imports UMA.

`UmaAvatarBridge` is also mixed. It combines provider selection/network transport, UMA payload
encoding, local camera policy, and stock-system teardown.

`UmaHomeAvatar` combines generic Home host behavior with UMA persistence and construction.

## Recommended Extraction

Do this after local VR embodiment is stable enough to preserve as a baseline. Avoid a large
rewrite while hand, arm, head, and seated behavior are still changing.

### Extraction status (July 14, 2026)

The first compiling extraction slice is implemented after local VR acceptance:

- `IHumanoidAvatarInstance` and `AvatarVisibility` define the provider-neutral constructed-avatar
  and semantic first-person visibility surface.
- `HumanoidArmRig` owns Unity Humanoid bone lookup, runtime Animation Rigging constraints, target
  proxies, and wrist-axis offsets without UMA, Fusion, Photon, or VertexForm dependencies.
- `VrHeightCalibration` owns standing stability, one-time scale calculation, and calibration lock
  state without provider or host dependencies.
- `HumanoidLocomotionDriver` owns root-motion measurement, locomotion animator parameters, and
  tracked-VR animator suppression/restoration without UMA or VertexForm dependencies.
- `IHumanoidRigInput` defines semantic head, body, hand, authority, and tracking inputs.
- `VertexFormHumanoidRigInput` is the host adapter that translates `AvatarInputConverter`,
  `PlayerNetworkSetup`, platform presentation, and XR tracking into that shared contract.
- `HumanoidVrAlignment` owns grounded root placement, yaw, eye-point correction, and standing
  height calibration while the host supplies its resolved floor height.
- `UmaAvatarPuppet` remains the UMA construction/renderer adapter and delegates arm IK and height
  alignment/calibration and locomotion animation to the framework classes.
- `UmaAvatarBridge` and `UmaHomeAvatar` now request semantic visibility states rather than UMA
  renderer booleans.
- `AvatarCameraVisibilityController` owns provider-neutral local-camera culling and shared XR
  camera diagnostics; Home and Fusion hosts only resolve their camera and choose visibility.
- `HumanoidTrackingLifecycle` owns provider-neutral HMD loss/regain transitions and the delayed
  focus/resume reinitialization queue. `UmaAvatarPuppet` retains only the concrete UMA skeleton/IK
  rebind and standing-idle response required when those transitions occur.
- `HumanoidAvatarPresentationController` owns the shared semantic visibility decision and invokes
  `AvatarCameraVisibilityController`; Home and Fusion retain only policy and camera discovery.

The accepted-baseline framework extraction is complete. Future provider work can generalize
construction factories and proximity bounds once a second implementation supplies concrete
requirements.

### 1. Define provider capabilities

Create a provider-facing contract conceptually equivalent to:

```csharp
public interface IHumanoidAvatarInstance
{
    Transform Root { get; }
    Animator Animator { get; }
    bool IsReady { get; }
    event Action HumanoidRebuilt;
    void SetFirstPersonVisibility(AvatarVisibility visibility, int cullLayer);
    Bounds GetRendererBounds();
}
```

Construction and payload handling should use a separate provider contract so the embodiment code
does not know about recipes, race systems, or SDK assets.

### 2. Extract generic components

Suggested responsibilities, not mandatory final names:

- `HumanoidAvatarEmbodiment`: body alignment, eye matching, tracking lifecycle, animation
  ownership, and orchestration.
- `HumanoidArmRig`: humanoid bone binding, two-bone IK, target proxies, and wrist profiles.
- `VrHeightCalibration`: standing detection, one-time scale calculation, lock/reset state.
- `AvatarVisibilityController`: semantic visibility policy and local-camera culling.
- `AvatarRigInput`: semantic head/body/hand/floor targets supplied by a host adapter.

These classes must not import `UMA`, `UMA.CharacterSystem`, or Fusion.

### 3. Leave an UMA adapter

The remaining UMA adapter should own:

- DCA creation and lifecycle.
- Race, wardrobe, DNA, and color application.
- Recipe codec and persistence.
- Animator-ready/rebuilt notifications.
- Head renderer splitting and UMA renderer bounds.
- UMA material/content validation.

`UmaAvatarPuppet` can either shrink into this adapter or be replaced by an explicitly named
`UmaAvatarProvider`/`UmaAvatarInstance` pair.

### 4. Thin the application hosts

- A Fusion host reads `AvatarExtensionSync`, selects a provider, and supplies authority and
  synchronized rig targets.
- A Home host selects the locally persisted provider and supplies the Home XR rig targets.
- Both hosts use the same generic embodiment components.
- Provider payloads stay opaque outside their provider adapter.

## Network Boundary for Future Sitting

Sitting should not be added to the UMA payload. It is transient embodiment state and should be
generic across avatar providers.

The first provider-neutral posture slice now represents standing/sitting plus a transition revision
on the existing `AvatarExtensionSync`. `PlayerNetworkSetup.IsSitting` remains the local VertexForm
source; `UmaAvatarBridge` publishes it and the current `SeatId` for proxies, and
`HumanoidPostureAnimator` consumes the generic state without referencing UMA.
`GHA_Locomotion_v2.controller`, which is assigned to both player prefabs, exposes the preferred
integer `Posture` parameter and transitions
between its locomotion blend tree and the Humanoid `SitMed01` state. Tracked head and arm solving
continues after the Animator so the animation owns the lower body without disabling upper-body
embodiment.

`SitSpot` now acquires Shared Mode state authority before claiming or releasing a seat, replicates
the occupying player's `NetworkId`, rejects active double-booking, and recovers stale claims left
by disconnected players.

Each seat may also author an optional `SeatedPelvisTarget`. `SitPoint` remains the authoritative
player/root anchor and facing contract. After the seated loop has taken ownership of the lower
body, the provider-neutral `HumanoidSeatedPelvisAlignment` applies a visual-root correction so the
Humanoid Hips bone lands on `SeatedPelvisTarget`, and aligns the avatar's forward axis with the
target transform's positive Z axis. The sit-down and stand-up clips are deliberately left
unpinned. Stable-loop recognition uses the configured Animator state contract rather than
unreliable imported-clip loop metadata. The target is resolved locally from the occupied
`SitSpot` and remotely from the existing replicated `SeatId`; it is not provider data and is not
added to the UMA recipe. When the final visual-root correction is first applied for the local
player, the host adapter applies the same horizontal delta to the local XR camera offset so the
viewpoint remains inside the relocated head. This does not move the network/player root away from
`SitPoint`; the saved camera offset is restored by the standing posture event. The generic
transition decision also suppresses provider rendering from the initial sit request until the B
Loop pelvis/facing/view alignment succeeds. The UMA adapter executes that policy with
`Renderer.forceRenderingOff`, preserving renderer enabled states and preventing the intermediate
root-to-pelvis jump from appearing in first person, mirrors, or remote views.

The remaining reusable network contract should represent:

- Additional posture values such as crouching, if required.
- Seat-height/profile selection for the supplied Ground/Low/Medium/High clips.
- An explicit replicated anchor pose only if future seats can move independently after occupation;
  static seats already use the player's synchronized root position and rotation.
- Optional calibrated body scale if remote clients cannot derive it deterministically.

Local XR tracking remains authoritative for head and hands. The seated animation and lower-body
pose reconstruct around the networked seat anchor without changing avatar scale.

## Near-Term Rule

Going forward:

- UMA build/render/customization fixes may remain in `UmaAvatarPuppet`.
- New XR calibration, tracking, IK, posture, or visibility behavior should be written so it uses
  only Unity humanoid concepts internally, even if its current entry point remains in an UMA-named
  class.
- Do not put seated state, hand poses, or provider-independent rig data into `UmaRecipeCodec`.
- Keep `AvatarExtensionSync` provider-neutral.
- Treat material fixes as UMA content work, not embodiment work.
