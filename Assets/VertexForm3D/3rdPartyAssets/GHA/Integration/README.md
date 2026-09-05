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

`UMA/` contains the current host-coupled UMA bridge, Home host, customization station, and
diagnostics. UMA construction/data primitives that no longer depend on VertexForm are already in
`com.vertexform3d.gha.uma`.

## Exit criteria

Files leave this staging directory only after their direct dependencies on `PlayerNetworkSetup`,
`ProjectManager`, `RoomManager`, `SitSpot`, `AvatarSelectionManager`, `SceneLoader`, and the
legacy Home customization station have been replaced by stable host contracts.

The intended destination is:

```text
VertexForm host contracts <- com.vertexform3d.gha <- com.vertexform3d.gha.uma
```

Nothing under the generic GHA package may reference UMA. Nothing under VertexForm core may
reference GHA or UMA provider implementation types.
