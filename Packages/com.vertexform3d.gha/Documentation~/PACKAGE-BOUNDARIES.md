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
and a separately installed compatible UMA checkout. It never vendors UMA source.

## UI integration

VertexForm's existing **Change Avatar** station receives one provider-neutral Avatar panel prefab.
GHA supplies the shell and the stock/local Humanoid provider. Optional providers register their
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
