# UMA setup contract

The package setup workflow must:

1. Detect an existing compatible `Assets/UMA` installation.
2. Otherwise accept a clean UMA repository checkout and verify the supported revision.
   The current migration pin also requires the fetched upstream Icon Creator PR #564 merge object;
   `Tools/Sync-Uma.ps1` applies that narrow overlay until UMA 3.0.3 or later is released and pinned.
3. Copy `UMAProject/Assets/UMA` while excluding demos and documentation not required at runtime.
4. Preserve vendor metadata and keep `Assets/UMA` outside the GHA/UMA adapter commit.
5. Refresh Unity and rebuild the UMA Global Library.
6. Validate required races, wardrobe recipes, materials, and the GHA UMA catalog.
7. Never modify or vendor the source UMA repository.

The current `Tools/Sync-Uma.ps1` remains the migration reference until this workflow is implemented
as cross-platform Unity Editor tooling.

## Project integration sequence

After installing and validating the compatible UMA checkout:

1. Run **Tools > GHA > Integration > Install GHA Host Layer**.
2. Wait for the define change and Unity domain reload to finish.
3. Run **Tools > GHA > Integration > Install UMA Provider Layer**.
4. Wait for compilation, then confirm the installer success log and zero compilation errors.

On the current Windows workstation, start `Tools/Keep-UnityCompileForeground.ps1` for the target
Unity process before each menu operation that changes a layer symbol. Stop that exact helper
process after AnkleBreaker reports `isCompiling: false`.

Remove the integration in reverse order: UMA provider first, then GHA host. The installers use
exact snapshots when safe and surgical removal when an installed target was changed later.

The provider installer uses `VERTEXFORM_GHA_UMA`; it must not add `UMA_INSTALLED`. The latter
currently enables an AnkleBreaker UMA 2 bridge that is incompatible with UMA 3.

## UMA-owned tags

UMA 3.0.2's `Assets/UMA/Core/Editor/Scripts/ImportProcessor.cs` unconditionally ensures the
`UMAIgnore` and `UMAKeepChain` tags. They are part of the separately installed UMA plugin state,
not the GHA UMA provider layer, and therefore remain after the provider layer is uninstalled.
