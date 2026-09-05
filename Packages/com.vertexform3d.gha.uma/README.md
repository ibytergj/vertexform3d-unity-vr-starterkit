# VertexForm3D GHA UMA Adapter

`com.vertexform3d.gha.uma` is the optional UMA provider for
`com.vertexform3d.gha`.

This package contains only integration code and GHA-facing UMA configuration. It does not
redistribute UMA. A compatible UMA checkout must be installed separately into the consuming
project.

## Supported UMA baseline

- UMA release: 3.0.2
- Source repository layout: `UMAProject/Assets/UMA`
- Vendor destination: `Assets/UMA`

The provider package will supply an Editor setup workflow that verifies the selected UMA checkout,
copies only the required vendor content, rebuilds the UMA Global Library, and creates project-owned
configuration assets. Vendor content and credentials remain excluded from the integration
change set.

## Dependency direction

```text
VertexForm3D host contracts <- com.vertexform3d.gha <- com.vertexform3d.gha.uma
```

GHA never references this package or UMA types.
