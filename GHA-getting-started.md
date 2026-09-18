# Getting started: adding GHA avatars to an existing Vertex Form3D project

For Vertex Form3D users who want customizable full-body avatars in their own project.

> **Status — draft, September 18, 2026.** This guide describes the intended installation. The
> GHA UMA package is **not published yet**, the GHA host layer is **not in upstream Vertex
> Form3D yet**, and the flow has not been run end to end on a stock project. Until the
> [work still required](#work-still-required) is done, follow the
> [developer guide](GHA-developer-getting-started.md) instead.

## What you get

GHA (Generic Humanoid Avatars) is a provider-neutral humanoid avatar layer in Vertex Form3D.
Its UMA provider adds customizable full-body avatars next to the existing Classic avatars. Both
appear in **Change Avatar** in Home. Nothing else in your project changes.

## Before you start

- An existing Vertex Form3D project on Unity **6000.3.11f1**, using URP, updated to a Vertex
  Form3D version that includes the GHA host layer (version **to be announced**). Check for the
  **Tools > GHA** menu.
- About **10 GB** free for the UMA import and Unity's cache.
- Your project committed or backed up. The installer modifies the Home scene component prefab
  and both player prefabs. It keeps restore snapshots under `UserSettings/`, but a commit is
  the safer rollback.
- Do not enter Play Mode until step 5.

## 1. Download two packages

| Package | Where | What it contains |
| --- | --- | --- |
| `UMA3_f5.unitypackage` (1.08 GB) | [UMA v3.05 release](https://github.com/umasteeringgroup/UMA/releases/tag/v3.05) | UMA 3 itself. This release is the exact revision GHA supports. |
| `VertexForm3D-GHA-UMA-<version>.unitypackage` | GHA releases page (**not published yet**) | The GHA UMA provider: customization UI, catalog and installer. |

Download only `UMA3_f5.unitypackage` from the UMA release, not the `UMA2_For_UMA3` package or
the source archives.

**Until upstream Vertex Form3D includes the host layer**, there is a third download,
`VertexForm3D-GHA-Host-<version>.unitypackage`, imported before the UMA provider package and
followed by **Tools > GHA > Integration > Install GHA Host Layer**. It patches five core scripts
with `git apply`, so Git must be installed and the scripts must match Vertex Form3D 1.1.9.

## 2. Import the packages, in this order

Open your project and import each package with **Assets > Import Package > Custom Package**,
keeping every item selected. Wait for the import and compilation to finish between packages.

1. `UMA3_f5.unitypackage`. This is large; expect a long import.
2. `VertexForm3D-GHA-UMA-<version>.unitypackage`

## 3. Install the UMA provider

1. Close any open **UMA > Global Library** window.
2. Run **Tools > GHA > Integration > Install UMA Provider Layer**.
3. Wait for compilation and for `GHA UMA provider assets installed` in the Console.

The installer adds the UMA provider to both player prefabs and Avatar Studio, then creates and
fills the UMA Global Library from the default UMA content at
`Assets/UMAProjectData/Resources/AssetIndexerProject.asset`. Keep the Editor in the foreground
until it finishes.

## 4. Check the library

Open **UMA > Global Library** and confirm `Human Male 3.0` and `Human Female 3.0` are listed.
Do not run the deletion or Addressables operations in that window.

## 5. Try it

1. Open `Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes/LoginScene.unity` and press Play.
2. Log in, then open **Change Avatar** in Home.
3. Avatar Studio shows **Classic** and **Custom** tabs. Under Custom, change the body type,
   sliders, colors and outfits, then **Save Avatar**.

Always start Play Mode from LoginScene; starting in Home skips the initialization the avatars
depend on.

## Removing the UMA provider

Run **Tools > GHA > Integration > Uninstall UMA Provider Layer**. It restores the prefabs from
the snapshots taken at install time. The generated UMA library and `Assets/UMA` stay; delete
them yourself if you also want to remove UMA. The GHA host layer is part of Vertex Form3D and
stays; Classic avatars keep working without any provider.

## Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| No **Tools > GHA** menu | Your Vertex Form3D version predates the GHA host layer. Update Vertex Form3D, or use the temporary host package described in step 1. |
| `A compatible UMA 3 installation was not found under Assets/UMA` | UMA was not imported into `Assets/UMA`. Import `UMA3_f5.unitypackage` with all items selected. |
| `Install the GHA host layer before installing the UMA provider layer` | Only with the temporary host package: run **Install GHA Host Layer** first. |
| `Cannot install the GHA host source patch cleanly` | Only with the temporary host package: a core script differs from 1.1.9. Compare it with the stock file, revert your change, install, then reapply it. |
| An installer fails partway | Fix the reported error, then rerun the **same** Install menu. Do not uninstall first. |
| Pink materials on avatars | Shader graphs failed to import. See the known limitations in the [developer guide](GHA-developer-getting-started.md#known-limitations-and-verification-status). |
| Avatars frozen or the Editor pauses when the avatar loads | UMA generation blocks the main thread on first load. This is a known limitation, not an installation error. |

## Work still required

For maintainers. These must be done before the guide above is accurate. Decision (September
18): the GHA host layer is to be contributed upstream to Vertex Form3D; the `.unitypackage`
files are strictly end-user deliverables built from this repository, and developers keep
building from source with both upstream repositories.

1. **Contribute the host layer upstream.** The five core-script seams (as real edits, not a
   patch), `com.vertexform3d.gha`, the `Integration/VertexForm` adapters, the Avatar Studio
   panel prefab and icons, and the prefab/Home wiring the host installer currently performs.
   Upstream ships these pre-wired, so the host installer and the `VERTEXFORM_GHA_HOST` define
   go away for users. Evidence maintainers will want: a stock-only runtime pass showing
   Classic avatars, networking and seating unchanged with no provider installed; zero UMA
   references in the contribution; no new package dependencies (all four the package declares
   are already in the 1.1.9 manifest); and a reviewable diff, which is why the runbook's commit
   separation matters. Until this lands, the host package is a temporary third download.
2. **Move both GHA packages under `Assets`.** `com.vertexform3d.gha` and
   `com.vertexform3d.gha.uma` currently sit under `Packages/` as embedded UPM packages, which a
   `.unitypackage` cannot carry, and Vertex Form3D's own Package Updater delivers
   `.unitypackage` files, so even the upstream host layer must live under `Assets` to reach
   users who update that way. Move both folders, with their asmdefs, under
   `Assets/VertexForm3D/3rdPartyAssets/GHA` through the Unity Editor so `.meta` files travel
   with them; update the 14 hardcoded `Packages/com.vertexform3d.gha*/...` paths in
   `GhaUmaAssetInstaller.cs` and `Tools/*.cs`; let Unity drop the embedded records from
   `packages-lock.json`. Do this before the upstream PR so upstream reviews the final layout.
3. **Add an exporter for the UMA package** (and, temporarily, the host package): an Editor
   script with a fixed path list per deliverable. The export must never include the core
   scripts, the installer-modified prefabs, `Assets/UMA` or the generated index.
4. **Test the UMA release package.** `UMA3_f5.unitypackage` is untested here. Check that it
   imports into `Assets/UMA`, whether it includes `Assets/SourceShaders`, and whether the 12
   shader graphs that need repair in the Git checkout also fail in the release package. If they
   do, the UMA provider package must ship or apply the repairs.
5. **Confirm the installers reproduce the branch on a stock 1.1.9 project.** The branch also
   carries small changes the installers do not make: the UMA tags in `TagManager.asset` (UMA
   adds these itself on import), two OpenXR feature flags, a `FormerlySerializedAs` attribute in
   `SerializedDataBase.cs`, and an inverted `onlyLocalBundles` check in
   `AddressablesDownloader.cs`. Decide which belong in the upstream contribution.
6. **Run this guide on a clean stock project** and record the result in the runbook.
7. **Pin the versions.** Record the Vertex Form3D, UMA and GHA UMA package versions each
   release supports, and make the provider installer refuse a mismatched host version. When
   bumping the UMA pin, choose a released tag so developers and users stay on one revision;
   v3.05 is the current pin.
8. **Test Vertex Form3D updates after install.** The stock Package Updater imports a new
   `.unitypackage` over the project. Once the host layer is upstream this only refreshes it;
   until then it removes the host patch, and rerunning **Install GHA Host Layer** must be
   confirmed to recover cleanly.
9. **Regenerate the host patch after every upstream merge** while the temporary host package
   exists. The development branch carries `VertexFormGhaHost.patch` already applied; after
   merging upstream Vertex Form3D, rebuild it against the new stock versions of the five core
   scripts and bump the supported version.

## Related

- [Developer getting-started guide](GHA-developer-getting-started.md) — source-clone setup,
  full test checklist, headset and multiplayer follow-up, known limitations.
- [Architecture](Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md) and
  [package boundaries](Packages/com.vertexform3d.gha/Documentation~/PACKAGE-BOUNDARIES.md) —
  the three-layer design and what each layer may reference.
- [UMA setup contract](Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md) — body-type
  authoring rules for maintainers.
