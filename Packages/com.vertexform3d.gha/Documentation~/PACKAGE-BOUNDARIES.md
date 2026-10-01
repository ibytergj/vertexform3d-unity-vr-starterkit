# Package boundaries

## VertexForm3D Core

VertexForm3D Core owns player authority, semantic tracking targets, locomotion, scenes, UI layout,
and the minimal inert extension seams required by an external avatar system. Core must not
reference UMA, recipes, wardrobe, DNA, or provider packages.

## `com.vertexform3d.gha`

GHA owns provider-neutral Humanoid contracts, embodiment, calibration, animation, presentation,
posture state, the generic avatar-configuration panel shell, and VertexForm host adapters.
Installing GHA without UMA must leave the stock avatar path functional.

## `com.vertexform3d.gha.uma`

The UMA adapter owns Dynamic Character Avatar construction, catalogs, recipes, DNA, wardrobe,
UMA renderer discovery, customization controls, and UMA setup/import tooling. It depends on GHA
and a separately installed, supported UMA version. Preferred acquisition order is Unity Asset
Store, an official GitHub release package, then a local source checkout. These are explicit
alternatives, not automatic fallbacks. The source-sync scripts remain maintained for developers;
package users must not need a UMA checkout or Git. It never vendors UMA source.

## Separation work still required (updated September 27)

These are ownership targets, not a claim that today's project is already separated. Follow the
[implementation plan](../../../GHA-IMPLEMENTATION-PLAN.md) before removing any provider files.

- The first D1 slice moves Home lifecycle and Classic Save to `VertexFormHomeAvatar`;
  `UmaHomeAvatar` supplies `IHomeAvatarProvider` and a legacy-prefab compatibility shim.
  Eight isolated Edit Mode checks pass; runtime and true UMA-absence proof remain pending.
  Extract the remaining player/network lifecycle and provider-side host dependencies.
- Add runtime provider registration and host services; leave construction, UMA payload decoding
  and customization in the provider. The UI registry alone is insufficient.
- Prove the contracts with a minimal non-UMA Humanoid instance and a clean host-only project.
  The D2 static-avatar proof uses fixed appearance with normal supported animation/tracking.
  The linked discussion was reviewed at Phase 1 entry on September 27; local RPM candidate
  assessment remains pending. The full static-avatar provider package remains separate later work.
- Split the currently shared installer implementation and serialized wiring. The host's shipped
  panel and player/Home assets must have no UMA components or references. UMA installation adds
  its contributions explicitly and uninstall removes only those contributions.
- Preserve asset GUIDs when relocating the embedded packages under `Assets`. Keep generated
  catalogs project-owned, preserve valid existing UMA indexes, and test the supported artifacts
  from each acquisition route independently.

The generic host is the upstream contribution. UMA integration development continues in a
separate VertexForm working copy; a dedicated standalone provider repository is not required.
Repository/branch names are chosen at the split, after preserving the combined baseline.

The two planned exports are GHA Host and GHA UMA Integration. UMA vendor content is acquired
separately and excluded from both. A verified temporary host package can support an approved
integration release before upstream acceptance; retire it for a release only after validating the
upstream revision that includes the host.

## UI integration

VertexForm's existing **Change Avatar** station receives one provider-neutral Avatar panel prefab.
GHA supplies the shell and the stock provider; the local Humanoid proof provider is planned.
Optional providers register their
capabilities and panels at runtime. Installing UMA therefore adds UMA choices without adding
another main-menu destination or changing the VertexForm UI layout again.

The panel is deliberately not registered as an enabled `UILayoutConfig` Custom entry: the
documented prefab baker exposes every enabled entry as a bottom-navigation tab. The GHA installer
removes any earlier generated Avatar entry/tab and embeds the shell under the existing
`AvatarSelectionManager` station instead.

## Reversible installation ownership

Install the integration in two explicit layers:

1. **Tools > GHA > Integration > Install GHA Host Layer** applies the minimal provider-neutral
   VertexForm seams, enables `VERTEXFORM_GHA_HOST`, installs the panel shell, and wires
   `AvatarExtensionSync` on both player prefabs.
2. **Tools > GHA > Integration > Install UMA Provider Layer** enables
   `VERTEXFORM_GHA_UMA` and contributes only UMA-specific panel, Home, preview, and player-bridge
   wiring.

Uninstall in reverse order. Exact pre-install snapshots are stored under `UserSettings` rather
than in the repository. When an installed target is unchanged, uninstall restores the exact
snapshot; when it has been edited since installation, uninstall removes only the owning layer's
known contributions.

Neither layer defines `UMA_INSTALLED`. That symbol currently enables AnkleBreaker's UMA 2 bridge
and is intentionally unavailable while this project uses UMA 3.

`ProjectSettings/TagManager.asset` is not owned by either GHA layer. UMA's own
`Assets/UMA/Core/Editor/Scripts/ImportProcessor.cs` ensures the `UMAIgnore` and `UMAKeepChain`
tags whenever UMA content is installed, so those tags persist after removing the GHA UMA provider.
