# VertexForm3D GHA UMA Adapter

For installation and review, read the
[developer getting-started and testing guide](../../GHA-developer-getting-started.md) first.
It identifies fresh-install verification requirements and unverified runtime cases. Users
installing from packages should use the [user getting-started guide](../../GHA-getting-started.md).

`com.vertexform3d.gha.uma` is the optional UMA provider for
`com.vertexform3d.gha`.

This package contains only integration code and GHA-facing UMA configuration. It does not
redistribute UMA. A compatible UMA checkout must be installed separately into the consuming
project.

## Supported UMA baseline

- Exact upstream master baseline: `c9204fe475b334162617da5ca923afcc6050e01e`
- Destination-only shader repairs: 12 exact files from upstream develop `f4edf41ba1017b4a745fd1ba7286824332652b7d`
- Source repository layout: `UMAProject/Assets/UMA`
- Vendor destination: `Assets/UMA`

The current Windows copy workflow is `Tools/Sync-Uma.ps1`; it reads a clean upstream source
checkout without modifying it. Editor menu installers configure the GHA host and UMA provider
layers and automatically create and populate a missing UMA index from the default installed
UMA content. A full fresh-checkout test is pending; a cross-platform setup workflow remains
planned. See the [setup contract](Documentation~/SETUP.md) for details.
Vendor content and credentials remain excluded from the integration change set.

## Dependency direction

```text
VertexForm3D host contracts <- com.vertexform3d.gha <- com.vertexform3d.gha.uma
```

GHA never references this package or UMA types.
