# VertexForm3D Generic Humanoid Avatars

`com.vertexform3d.gha` is the provider-neutral Humanoid avatar framework for VertexForm3D.

The package owns reusable Humanoid contracts, embodiment, calibration, tracking lifecycle,
presentation, animation, and provider-neutral appearance/posture transport. It does not contain
UMA source or UMA-specific construction and customization code.

## Integration boundary

VertexForm3D Core supplies a small set of inert host contracts and extension seams. This package
depends on those contracts, while VertexForm3D Core never depends on an avatar provider.

Optional providers depend on this package:

- `com.vertexform3d.gha.uma` — UMA adapter and setup tooling; UMA itself is installed separately.
- Local Unity Humanoid prefab/FBX provider — framework validation provider.

The dependency direction is always:

```text
VertexForm3D host contracts <- GHA <- avatar provider
```

Photon Fusion is currently supplied by the VertexForm3D project under `Assets/Photon`. A later
host-contract extraction will remove direct package assumptions about VertexForm implementation
assemblies while preserving the Fusion network layout.
