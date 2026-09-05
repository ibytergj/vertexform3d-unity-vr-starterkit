# Avatar Integration Reference Review

Status: July 11, 2026

## Scope

This review consolidates three references for the UMA 3 integration:

1. [VertexForm3D overall developer documentation](https://vertexform-dev-docs.notion.site/DEVELOPER_DOCUMENTATION-3734bf8b713980c0a1a8d0e5791add43)
2. [NewGenericMRDesktopPrefab developer documentation](https://vertexform-dev-docs.notion.site/NewGenericMRDesktopPrefab_DEVELOPER_DOCUMENTATION-3734bf8b7139801cb9bee584cff9a622)
3. The retired Ready Player Me project at
   `E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit-Dev - RPM`

The RPM audit intentionally covers XR-rig-to-humanoid interaction rather than RPM loading,
customization, or avatar-recipe networking.

This document builds on `ARCHITECTURE.md`. That document defines the UMA-specific, generic
humanoid, and VertexForm/Fusion boundaries. This review decides how the external references alter
the implementation plan.

## Executive Decision

Do not build an UMA-specific XR rig and do not transplant the retired RPM rig wholesale.

Keep the VertexForm player prefab, XR Origin, camera, locomotion, authority model,
`AvatarInputConverter`, and `MultiplayerVRSynchronization` as the platform-facing pose pipeline.
Build a provider-neutral humanoid embodiment layer that consumes the existing semantic
`MainAvatar`, head, body, and hand targets. UMA should implement construction, rebuild
notification, renderer visibility, and content/material concerns behind that layer.

The target flow is:

```text
XRI devices / desktop controls
        |
        v
VertexForm XR Origin + locomotion + CharacterController
        |
        v
AvatarInputConverter semantic targets
        |
        +------------------------------+
        | local authority              | Fusion pose synchronization
        v                              v
generic humanoid embodiment       remote semantic targets
        |                              |
        +---------------+--------------+
                        v
            provider avatar instance
             (UMA now, others later)
```

The embodiment layer must never move or parent the XR camera to the avatar. The camera and XR
Origin are authoritative; the visible humanoid follows their semantic targets.

### Confirmed packaging and scope

The accepted architecture is three layers:

1. VertexForm3D Core with minimal, inert, provider-neutral hooks.
2. A separately installable Avatar Model Integration Framework for Unity Humanoid models.
3. Separately installable provider packages, beginning with UMA 3.

The initial framework will also include or be validated by a simple local Humanoid prefab/FBX
provider. This is the proof that shared embodiment does not depend on UMA's DCA lifecycle.

The future remote provider is a Vertex-compatible Humanoid GLB loader. Its source model may have
originated in Character Creator, MetaHuman, Blender, or another tool, but it must be exported into
the framework's validated skeleton/metadata contract. Remote loading is not an active V2 feature.

## 1. Overall Platform Documentation

### Confirmed platform contract

The overall documentation describes this player lifecycle:

1. The user selects an avatar in Home.
2. `RoomManager` spawns the Fusion player prefab.
3. `PlayerNetworkSetup.Spawned()` separates local and remote behavior.
4. The local player enables its control rig, camera, voice, input, and avatar conversion.
5. Remote players disable local tracking/input and retain avatar presentation.
6. `AvatarInputConverter` produces avatar-facing head/body/hand transforms.
7. `MultiplayerVRSynchronization` publishes those transforms and interpolates them remotely.

The documentation treats `PlayerNetworkSetup` as the master network-player component. UMA should
therefore remain a construction/presentation provider below that lifecycle rather than replacing
the player root, Fusion `NetworkObject`, camera, locomotion, voice, UI, or authority setup.

### Scene and lifetime implications

VertexForm uses a persistent bootstrap and an additive Fusion-controlled world flow. Generated
avatars must:

- Be parented beneath the network player.
- Avoid assumptions about which world scene is active.
- Tolerate recipe state arriving after `Spawned()`.
- Handle late joins and repeated rebuild requests idempotently.
- Ignore or cancel delayed build callbacks after player despawn.
- Keep UMA catalogs/runtime services available across additive world transitions.

The public documentation describes direct Home play as possible. This repository's actual
bootstrap requires `LoginScene`, so the local `AGENTS.md` rule is authoritative for testing.

### Platform implications

Platform and browser presentation are runtime/networked state, not compile-time assumptions.
The avatar layer must distinguish:

- Local immersive VR/WebXR.
- Local desktop first/third person.
- Remote presentation of either platform.

UMA construction should not branch on build target alone. The VertexForm platform presentation
helpers and authority state remain the host's source of truth.

## 2. Prefab Documentation

### What the page actually documents

The page explicitly documents:

`Assets/VertexForm3D/Resources/NewGenericMRDesktopPrefab.prefab`

It describes the asset as a `FusionPrefab` and a variant of XRI Starter Assets 3.3.1
`XR Origin (XR Rig)`. Despite the `Desktop` name, the page calls it the local player XR rig used
in multiplayer worlds and lists all of these responsibilities:

- VR/MR head, controller, and optional optical-hand tracking.
- XRI move, turn, teleport, climb, fly, grab-move, gravity, and jump.
- Fusion player, avatar-pose, voice, and hand-joint synchronization.
- Desktop/non-VR movement and orbit-camera fallback.
- In-world VR and desktop user interfaces.

The live Unity prefab inspection also confirms that both current player assets are variants of
the same XRI base:

- `NewGenericMRVRPrefab.prefab`
- `NewGenericMRDesktopPrefab.prefab`

The two current prefabs may still carry different overrides and platform selection behavior, but
they share one architectural foundation. We should not design separate humanoid embodiment
systems for their names.

### Important hierarchy boundary

The documented prefab separates:

- `Camera Offset`, Main Camera, controllers, optical hands, and locomotion.
- `MainAvatar` with `Head`, `Body`, `LHand`, and `RHand` semantic targets.
- Root-level `PlayerNetworkSetup`, `AvatarInputConverter`,
  `MultiplayerVRSynchronization`, `XRHandJointSync`, and `XRRigController`.

That `MainAvatar` target hierarchy is the reusable boundary. Avatar providers should consume it;
they should not read device transforms independently or take ownership of the camera hierarchy.

### Input and execution responsibility

The prefab documentation assigns:

- Platform/device conversion to `AvatarInputConverter`.
- Root/capsule locomotion to XRI or `XRRigController`.
- Local/remote lifecycle to `PlayerNetworkSetup`.
- Head/body/hand pose replication to `MultiplayerVRSynchronization`.
- Optical finger-joint replication to `XRHandJointSync`.

The generic embodiment layer should run after semantic targets are updated, ideally in
`LateUpdate`, and drive Animation Rigging targets before the rig evaluation point. It must not
introduce another independent device-to-avatar conversion path.

## 3. Retired RPM Rig

### Correct primary prefab and ownership

The primary reference is:

`Assets/RPM/Prefabs/VR/RPM XR Origin (XR Rig) Variant.prefab`

It is a prefab variant of the XRI 3.2.1 Starter Assets `XR Origin (XR Rig).prefab`. It is
instantiated directly by `AvatarCreatorWizardXR.unity` and nested by
`HomeSceneComponent.prefab`. It is not nested by `NewGenericMRVRPrefab`; that network-player
prefab contains a separate, duplicated RPM rig implementation.

This distinction corrects the first RPM audit. Behavior found only on
`NewGenericMRVRPrefab` cannot be attributed to the reusable RPM XR Origin prefab.

### Full embodiment hierarchy

The RPM XR Origin variant contains these functional branches:

- Inherited XRI root: `XROrigin`, CharacterController/locomotion support, interaction management,
  and controller/hand modality management.
- `Camera Offset`: Main Camera, tracked controllers, optical-hand branches, and stabilized
  teleport origins.
- Controller and hand mapping sources: `VRHead`, `VRLeftController`, `VRRightController`, plus
  complete XRI optical-hand skeletons and interaction geometry.
- `AvatarIK`: the stable humanoid template containing Animator, `RigBuilder`, `LoadRPMAvatar`,
  `IKTargetFollowVRRig`, `AnimateOnInput`, the full humanoid armature, and renderer children.
- `IKRig`: a Rig with head, left/right arm, and left/right leg constraints, including targets and
  hints.
- Renderer branches: body, head, eyes, teeth, hair, beard, glasses, facewear, top, bottom, and
  footwear.

The exact serialized source is
`Assets/RPM/Prefabs/VR/RPM XR Origin (XR Rig) Variant.prefab`; the `AvatarIK` component list begins
near line 5581, mapping calibration near line 5770, and representative arm constraint data near
line 1406 in the retired project.

### What it actually used

The retired RPM XR Origin does not use FinalIK/VRIK. It uses Unity Animation Rigging:

- One active `RigBuilder` layer at full weight.
- A head constraint.
- Two-bone IK constraints for both arms and both legs.
- Explicit wrist/foot targets and elbow/knee hints.
- Full target position, rotation, and hint weights.
- Head and hand mapping components updated from the local XR rig.

This is strong evidence that Unity Animation Rigging is sufficient for the first embodiment
scope and avoids requiring users to license a third-party IK package. It also provides a future
reference for lower-body targets and hints after the local head/arm baseline is stable.

### Intrinsic pose flow

Inside this prefab:

1. XRI tracked-pose components update the Main Camera, controller branches, and optical-hand
   skeletons.
2. `IKTargetFollowVRRig.LateUpdate` maps those sources into the head and wrist IK targets.
3. The same component places and yaws `AvatarIK` from the mapped head/body offset.
4. `RigBuilder` solves the head and four limb constraints.
5. The Animator supplies locomotion and hand input parameters.
6. `LoadRPMAvatar` transfers loaded RPM meshes and eye placement onto the retained template
   skeleton, Animator, and constraints.

The reusable idea is not RPM loading itself; it is retaining a known humanoid/constraint template
and adapting a provider's visual avatar to that stable embodiment skeleton.

### Serialized calibration

The prefab's `AvatarIK` root is authored at local zero with identity rotation and unit scale.
Its mapper uses:

| Target | Position offset | Rotation offset |
|---|---:|---:|
| Head | `(0, -0.10, +0.07)` | `(0, 0, 0)` |
| Left controller | `(-0.01, -0.04, -0.15)` | `(7, 65.8, 72.9)` |
| Left tracked hand | `(0, -0.01, -0.07)` | `(86.88, 65.8, 72.9)` |
| Right controller | `(+0.01, -0.04, -0.15)` | `(170, 107.1, 99.9)` |
| Right tracked hand | `(0, -0.01, -0.09)` | `(83.8, 107.1, 99.9)` |

It also uses body offset `(0, -0.47, 0)`, turn smoothing `0.1`, movement threshold `0.1`, and
blend speed `40`.

These values prove that controller and optical-hand calibration must be separate and that an eye
anchor is offset from the head bone/root. They are RPM/template-specific values, not defaults to
copy into UMA.

### Reusable RPM patterns

Keep these ideas:

- Separate XR sources from calibrated humanoid targets.
- Use provider/avatar-specific calibration profiles for eye and wrist offsets.
- Maintain separate controller and optical-hand offset profiles.
- Bind Unity Humanoid bones to Animation Rigging constraints.
- Run target following after tracking inputs update.
- Gate pose production by Fusion input authority.
- Let remotes consume synchronized semantic targets.
- Rebind the rig when a generated avatar recreates its Animator/skeleton.

The RPM prefab's separate controller and optical-hand corrections validate the provider-profile
approach already used by the UMA work. The profile must belong to the avatar/template adapter,
while modality selection belongs to the VertexForm rig input.

### RPM patterns to reject

Do not copy these implementation choices:

- Moving the avatar root before updating the mapped head target, which introduces a one-frame
  dependency on the previous pose.
- Combining Animator root motion with a script-driven avatar root.
- Choosing hand/controller mode from GameObject active state instead of tracking validity or the
  platform's authoritative modality state.
- Direct world-space finger-bone copying without a provider-neutral joint map and axis profile.
- Assuming local-space poses remain correct under different avatar scales and parent hierarchies.
- Disabling the complete tracked rig during sitting.
- Treating remote sitting as only a `RigBuilder` enable/disable change.
- Omitting headset sleep/focus recovery.

Our current tracking-resume handling and one-time standing scale calibration improve on the RPM
baseline and should be preserved through extraction.

### Sitting and networking are external to this prefab

The RPM XR Origin prefab itself has no sitting component and no Fusion pose-replication component.
`AvatarCreatorWizardXR.unity` adds `SittingController` and `LegHintController`, with incomplete/null
camera-offset and network references. `NewGenericMRVRPrefab` contains a separate networked RPM rig
and its own pose flow.

The scene-added sitting implementation is useful as a list of failure cases, not as transplantable
code. It:

- Disables head following, locomotion, `RigBuilder`, and hand copying.
- Applies a seat position plus a hard-coded height offset.
- Changes the camera height.
- Drives gender-specific Animator booleans.
- Has incomplete stand-up placement and incomplete remote transitions.

The new sitting system should preserve tracked head and hands, lock avatar scale, own lower-body
posture through animation/constraints, and replicate explicit posture/seat state.

Network architecture must therefore be learned from VertexForm's current
`MultiplayerVRSynchronization`, not assumed to be an intrinsic feature of the RPM XR Origin
template.

## 4. Comparison With Current UMA Work

### Decisions already validated

The current source is directionally correct in these areas:

- It keeps `PlayerNetworkSetup` and Fusion player objects intact.
- It uses `AvatarInputConverter` semantic targets.
- It uses Unity Animation Rigging rather than third-party IK.
- It recreates humanoid bindings after UMA rebuilds.
- It separates hand and controller wrist profiles.
- It hides only the local head while keeping the body visible in VR.
- It recovers tracking bindings after HMD sleep/focus changes.
- It calibrates standing scale once rather than rescaling during sitting/crouching.
- It keeps UMA recipe replication separate from continuous pose synchronization.

### Current gaps revealed by the references

1. **The generic embodiment code is still inside `UmaAvatarPuppet`.** Another humanoid provider
   cannot reuse it without importing UMA.
2. **Scale is local-only state.** A remote client or late joiner needs the locked avatar scale to
   reproduce identical proportions, especially when the player is currently seated.
3. **Posture is not a complete network contract.** Current `PlayerNetworkSetup.IsSitting` is a
   local, non-networked property. `SitSpot.isOccupied` does not tell every client which avatar is
   seated or which animation transition to reconstruct.
4. **Pose spaces need an explicit contract.** `MultiplayerVRSynchronization` mixes a world-space
   player root with local-space avatar/head/hand values. The embodiment layer must define the
   parent and scale assumptions for every synchronized value.
5. **Update order is implicit.** The target producer, embodiment root alignment, Animation Rigging
   solve, and network capture need a documented execution order to avoid one-frame lag and
   camera/avatar feedback.
6. **Finger tracking is not integrated with the humanoid provider.** `XRHandJointSync` exists,
   but mapping those joints to UMA fingers needs a provider profile and retargeting step.
7. **Lip-sync rebinding remains provider-specific work.** UMA rebuilds can invalidate facial
   renderer/blendshape references.

## 5. Implementation Decisions

### A. Preserve the platform rig

Do not modify the XR Origin hierarchy for UMA embodiment and do not parent the camera under an
avatar bone. Keep locomotion and world movement on the player root/CharacterController.

The avatar follows:

- `MainAvatarTransform` for the avatar presentation root.
- `AvatarBody` for body yaw/anchor semantics.
- `AvatarHead` for head orientation.
- `AvatarHand_Left` and `AvatarHand_Right` for hand targets.

Raw `XRHead` may be used by the VertexForm rig-input adapter for measured local eye height, but
generic embodiment code should receive that through an input contract rather than importing or
searching the XR hierarchy.

### B. Use Unity Animation Rigging

Continue with runtime two-bone arm constraints for the initial solution. Add elbow-hint placement
and reach clamping before considering a more complex full-body solver. Do not add a FinalIK/VRIK
dependency.

Future lower-body work can reuse the RPM idea of foot targets and leg hints, but it should be
implemented from measured hips/capsule/ground data rather than copied offsets.

### C. Separate construction from embodiment

Extract in this order after the current local VR baseline is accepted:

1. `AvatarRigInput`: semantic targets, floor, tracking validity, modality, and authority.
2. `VrHeightCalibration`: authored scale, standing candidate, locked scale, and reset policy.
3. `HumanoidArmRig`: bone binding, IK targets/hints, reach, and wrist profiles.
4. `HumanoidAvatarEmbodiment`: update order, root/eye alignment, posture, and animation ownership.
5. `AvatarVisibilityController`: semantic local visibility and camera culling.
6. `UmaAvatarInstance`: DCA lifecycle, recipe application, rebuild notification, renderer split,
   bounds, and provider calibration profile.

Keep Home and networked-world hosts thin. Both should provide the same rig-input contract and use
the same embodiment implementation.

### D. Add explicit generic embodiment state

Do not put transient posture into `UmaRecipeCodec`. Add explicit provider-neutral network state
for:

- Locked avatar scale and calibration revision.
- Standing/crouching/sitting posture.
- Seat identity or deterministic network reference.
- Seat anchor pose/facing when required.
- Sit/stand transition revision.

This can be added to an existing generic player synchronization component or a generic extension
component, but it must be included in Fusion prefab-layout/AppVersion planning. Avoid adding a
second provider-specific `NetworkBehaviour` solely for UMA.

### E. Define pose spaces and update order

Use one documented pipeline:

```text
Update:       platform rig updates semantic targets
LateUpdate:   embodiment aligns root and updates IK proxies
Rig solve:    Animation Rigging evaluates humanoid constraints
Fusion tick:  authoritative semantic pose/state is captured
Remote frame: synchronized targets interpolate, then the same embodiment solve runs
```

Every replicated position must declare world or local space and its parent transform. Locked
scale must be applied before interpreting local-space hand offsets.

### F. Keep visibility provider-based

The RPM layer approach confirms that first-person visibility is a rendering concern rather than a
reason to remove the head skeleton. Keep the semantic visibility states from `ARCHITECTURE.md`.
UMA continues to implement `HeadOnly` by splitting head slots into a separately culled renderer.
Mirrors, shadows, and remote cameras must retain the full head.

## 6. RPM Reuse Plan

The RPM project is not merely research. We should deliberately harvest the useful rig work, but
not import `RPM XR Origin (XR Rig) Variant.prefab` wholesale. That prefab is based on XRI 3.2.1,
contains RPM loading/editor dependencies, and would create a second XR Origin instead of extending
the current VertexForm 3.3.1 player rig.

### Directly adapt

| RPM source | Reuse action | Destination |
|---|---|---|
| `IKTargetFollowVRRig.VRMap` | Adapt the intermediate target mapping and separate controller/optical-hand profiles; replace active-GameObject modality detection with VertexForm tracking/modality state; update targets before root alignment | `AvatarRigInput` plus provider calibration profile |
| `AvatarIK/IKRig` arm constraints | Reproduce its target/hint structure and full position/rotation/hint weighting against the current UMA Humanoid Animator | `HumanoidArmRig` |
| RPM head/body calibration model | Keep a configurable head-to-eye target and body offset; calculate UMA values from its generated skeleton instead of copying RPM numbers | `HumanoidAvatarEmbodiment` plus `UmaAvatarInstance` profile |
| RPM controller/hand serialized offsets | Use as evidence and test vectors for separate profiles; do not copy values as UMA defaults | Provider calibration tests/tuning |
| `LegHintController` structure | Adapt later for knee hints; fix the unused foot-target input and derive hints from hips, foot target, gait, and ground normal | Future `HumanoidLegRig` |
| RPM leg two-bone constraint hierarchy | Reproduce later when grounded foot targets exist | Future `HumanoidLegRig` |
| `AnimateOnInput` / `PlayLegsAnimation` concept | Reuse the animation-first gait pattern and smooth blend transitions; feed it from VertexForm movement velocity rather than RPM's raw stick action | Generic locomotion animation driver |

### Reuse conceptually, not as copied code

| RPM pattern | How we use it |
|---|---|
| Stable `AvatarIK` template with provider meshes | Define a stable generic embodiment contract. UMA keeps its generated skeleton but must expose a ready Animator and rebuild event; providers that support mesh transfer may use a fixed template later |
| Dedicated `IKRig` target/hint branch | Keep all runtime targets and hints in one owned hierarchy so provider rebuilds can rebind cleanly |
| Renderer branches separated from the armature | Require providers to expose semantic visibility and bounds without changing the shared skeleton |
| Animation followed by constraint solving | Let animation own the base full-body pose, then blend tracked arms/head and future grounded feet through constraint weights |

### Explicitly do not reuse

| RPM source/behavior | Reason |
|---|---|
| Entire RPM XR Origin prefab | Wrong XRI version, duplicate XR Origin/camera/locomotion, RPM-specific loading and scene wiring |
| `LoadRPMAvatar` | RPM mesh-transfer/loading implementation; UMA has its own DCA lifecycle |
| RPM root-motion plus scripted root following | Competing root owners and one-frame stale head mapping |
| `SittingController` | Disables tracking/rigging, hard-codes camera/seat offsets, and has incomplete host/network wiring |
| Direct `XRHandToAvatar` world-space joint copying | No provider-neutral joint/axis map or bounds validation |
| Controller-active modality test | Does not prove valid tracking and duplicates VertexForm modality ownership |

## 7. Recommended Work Order

1. Preserve the current working UMA baseline and extract the RPM `VRMap` idea into a generic
   semantic target/profile design. Compare our current hand offsets and update order against the
   RPM serialized mappings without replacing the VertexForm targets.
2. Reproduce the RPM `AvatarIK/IKRig` arm target-and-hint structure in the current runtime UMA
   arm rig. Add elbow hints, reach clamping, and blendable constraint weights.
3. Finish and accept the local VR body/eye/hand baseline using those adapted RPM patterns.
4. Verify local tracking recovery and locked calibration through headset sleep/wake. Preserve our
   recovery logic because RPM has none.
5. Add synchronized locked scale so remotes and late joiners match local proportions.
6. Extract the generic rig-input, calibration, arm-rig, embodiment, and visibility layers, using
   the RPM `AvatarIK` branch as the structural reference and the current VertexForm
   `MainAvatar` targets as inputs.
7. Define the minimal ready-instance, provider capability, provider registry, default Animator,
   and opaque appearance-payload contracts in the separate Avatar Model Integration Framework.
8. Make UMA a provider adapter over those generic layers; keep DCA construction/rebuild and UMA
   renderer splitting provider-specific.
9. Add a local Humanoid prefab/FBX provider and run the same local VR validation against it. Do not
   proceed until this proves the framework has no UMA dependency.
10. Mirror the accepted local pose remotely through the existing semantic target pipeline and run
   two-client/late-join tests. Do not copy the separate RPM network-player implementation.
11. Design the generic posture/seat network contract before implementing seated animation. Use the
   RPM `SittingController` only as a failure-case checklist.
12. Add seated lower-body animation while preserving live head/hands and locked scale.
13. Adapt the RPM leg-constraint and knee-hint structure only after grounded foot targets and
   animation-aware foot weights are designed; do not enable full-weight static foot targets.
14. Adapt the RPM animation-first lower-body blend to VertexForm velocity and the real walk clip.
15. Add provider-neutral finger-joint mapping if full optical finger embodiment is required; use
   RPM's finger hierarchy as mapping evidence, not direct world-space copying.
16. Add the shared facial/viseme contract and lip-sync rebinding as an UMA provider capability.
17. Run the dedicated UMA material/shader audit across Editor, Quest, WebGL, and WebGPU/WebXR.
18. Record the future `RemoteHumanoidGlbProvider` manifest/mapping requirements without building
   the downloader during initial V2.

## 8. Validation Matrix

Each embodiment change should be tested from `LoginScene` in these cases:

| Case | Required result |
|---|---|
| Local VR, standing start | Eye alignment calibrates once; body scale locks |
| Local VR, seated start | Authored scale remains until a stable standing sample |
| Stand, sit, crouch, stand | Scale never changes after calibration |
| Controller to hand tracking | Correct profile changes without axis coupling |
| HMD sleep/wake | Camera/world remain stable; bindings recover; scale remains locked |
| Local mirror | Full head and body remain visible |
| Local HMD view | Head hidden; body and hands visible |
| Remote same-platform client | Pose, scale, visibility, and modality match |
| Remote cross-platform client | Runtime platform presentation remains correct |
| Late join while standing | Recipe, locked scale, and pose reconstruct |
| Late join while seated | Recipe, scale, posture, seat, and facing reconstruct |
| UMA rebuild | Animator, IK, visibility, lip-sync, and bounds rebind |
