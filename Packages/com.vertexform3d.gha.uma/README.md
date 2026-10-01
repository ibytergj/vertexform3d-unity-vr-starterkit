# VertexForm3D GHA UMA Adapter

For installation and review, read the
[developer getting-started and testing guide](../../GHA-developer-getting-started.md) first.
It identifies fresh-install verification requirements and unverified runtime cases. Users
installing from packages should use the [user getting-started guide](../../GHA-getting-started.md).

`com.vertexform3d.gha.uma` is the optional UMA provider for
`com.vertexform3d.gha`.

This package contains integration code and GHA-facing UMA configuration. UMA is installed
separately into the consuming project. Intended source preference: **Unity Asset Store**, then
**official GitHub release package**, then **local repository** for developer script workflows.
The Asset Store and GitHub package routes still require artifact validation; a source checkout
is not a planned requirement for package users.

## Recorded development baseline

- Exact upstream master baseline: `c9204fe475b334162617da5ca923afcc6050e01e`
- Destination-only shader repairs: 12 exact files from upstream develop `f4edf41ba1017b4a745fd1ba7286824332652b7d`
- Source repository layout: `UMAProject/Assets/UMA`
- Vendor destination: `Assets/UMA`

The current Windows copy workflow is `Tools/Sync-Uma.ps1`; it reads a clean upstream source
checkout without modifying it. Editor menu installers configure the GHA host and UMA provider
layers and automatically create and populate a missing UMA index from the default installed
UMA content. These scripts remain supported developer tooling. Full release compatibility checks,
package artifact validation and clean-install acceptance remain planned; the source pin does not
prove package acceptance. See the [setup contract](Documentation~/SETUP.md) for details and the
[implementation plan](../../GHA-IMPLEMENTATION-PLAN.md) for the separation sequence.
Vendor content and credentials remain excluded from the integration change set.

The planned GHA Host and GHA UMA Integration exports are tested separately and together with
each supported UMA source. UMA itself is never included in those exports. A verified temporary
host package can support an approved release before upstream acceptance; no package is published
by approval of the implementation plan alone.

## Dependency direction

```text
VertexForm3D host contracts <- com.vertexform3d.gha <- com.vertexform3d.gha.uma
```

GHA never references this package or UMA types.
