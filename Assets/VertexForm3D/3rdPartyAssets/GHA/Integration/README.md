# GHA host-integration staging

This directory contains the remaining adapters that still compile in `Assembly-CSharp` because
they directly reference current VertexForm3D implementation classes.

It is intentionally separate from both installable packages:

- `Packages/com.vertexform3d.gha`
- `Packages/com.vertexform3d.gha.uma`

## VertexForm

`VertexForm/` adapts the current `AvatarInputConverter`, `PlayerNetworkSetup`, platform, and
semantic target hierarchy to the provider-neutral GHA runtime.

## UMA

`UMA/` contains the current host-coupled UMA bridge, Home build adapter, customization station, and
diagnostics. UMA construction/data primitives that no longer depend on VertexForm are already in
`com.vertexform3d.gha.uma`.

## Exit criteria

September 23 sequencing: satisfy these boundaries before the physical host/UMA split, following
the repository-root `GHA-IMPLEMENTATION-PLAN.md`. This is required for independent delivery,
not merely an optional future UPM cleanup.

Files leave this staging directory only after their direct dependencies on `PlayerNetworkSetup`,
`ProjectManager`, `RoomManager`, `SitSpot`, `AvatarSelectionManager`, `SceneLoader`, and the
legacy Home customization station have been replaced by stable host contracts.

The intended destination is:

```text
VertexForm host contracts <- com.vertexform3d.gha <- com.vertexform3d.gha.uma
```

Nothing under the generic GHA package may reference UMA. Nothing under VertexForm core may
reference GHA or UMA provider implementation types.

Shared Home/player lifecycle, Classic Save handling, provider selection and network transport
belong in the host. UMA construction, recipe decoding and customization belong in the provider.
September 27: the first D1 source slice moves Home orchestration and Classic Save to
`VertexForm/Runtime/VertexFormHomeAvatar.cs`. `UmaHomeAvatar` implements the optional
`IHomeAvatarProvider` seam and retains compatibility entry points for existing prefabs.
Eight isolated Edit Mode checks pass; runtime and true UMA-absence proof remain pending.
The full Home/network registry and player lifecycle extraction are still required.
Replace direct host calls with contracts and explicit registration before relocating the remaining
UMA files into their provider assembly; an asmdef cannot solve dependencies on `Assembly-CSharp`.
Preserve `.meta` GUIDs and verify every serialized reference during the move.

D2 verifies these contracts with a minimal fixed-appearance Humanoid provider before the split.
The static-avatar discussion was reviewed on September 27; assessment of the local RPM candidates
recorded in the implementation plan remains pending. Reuse the avatar model with shared GHA embodiment; do not import the old RPM
project's complete XR/network rig. The complete static-avatar package remains later work.

The host contribution remains in this VertexForm fork. Maintain the integration in a separate
VertexForm working copy and export its user package there. UMA itself remains external, acquired
from the Asset Store, official release package, or the maintained local-source developer workflow,
in that preference order. A custom UMA source fork is only needed to modify UMA itself.
