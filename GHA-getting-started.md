# Getting started: adding GHA avatars to an existing Vertex Form3D project

For Vertex Form3D users who want customizable full-body avatars in their own project.

> **Status — draft, September 23, 2026.** This guide describes the intended installation. The
> GHA UMA package is **not published yet**, the GHA host layer is **not in upstream Vertex
> Form3D yet**, and the Asset Store/GitHub/local-source installation matrix has not passed. Until the
> [work still required](#work-still-required) is done, follow the
> [developer guide](GHA-developer-getting-started.md) instead.

## What you get

GHA (Generic Humanoid Avatars) is a provider-neutral humanoid avatar layer in Vertex Form3D.
Its UMA provider adds customizable full-body avatars next to the existing Classic avatars. Both
appear in **Change Avatar** in Home. The installer adds provider wiring to the Home/player
prefabs and Studio and creates the required project configuration.

## Before you start

- An existing supported Vertex Form3D project on Unity **6000.3.11f1**, using URP. The host
  layer must come from either a verified GHA Host package or a VertexForm release that includes
  it upstream (version **to be announced**). Neither distribution path is release-accepted yet.
- About **10 GB** free for the UMA import and Unity's cache.
- Your project committed or backed up. The installer modifies the Home scene component prefab
  and both player prefabs. It keeps restore snapshots under `UserSettings/`, but a commit is
  the safer rollback.
- Do not enter Play Mode until step 5.
- Use the Windows build target for installer prefab saves in the current baseline. Release
  compatibility checks are planned; the current source pin does not certify downloaded packages.

## 1. Choose a UMA source and obtain the integration

UMA acquisition preference is:

| Preference | Source | Intended use |
| --- | --- | --- |
| 1 | **Unity Asset Store** | Preferred user route; use the product/release explicitly listed in the integration compatibility record. Exact Store artifact validation is pending. |
| 2 | **Official UMA GitHub release package** | Download the supported Unity package. The recorded candidate is [UMA v3.05](https://github.com/umasteeringgroup/UMA/releases/tag/v3.05), `UMA3_f5.unitypackage`; package acceptance is pending. |
| 3 | **Local UMA repository** | Maintained developer route using `Tools/Sync-Uma.ps1` and related scripts in the [developer guide](GHA-developer-getting-started.md). |

Choose one UMA route per project. This order is a recommendation, not automatic downloading,
replacement or switching. Developers can deliberately use the local route. If a channel does
not offer a supported version, explicitly choose another supported source. Do not overlay
multiple distributions in one installation or assume matching version labels mean identical content.

The current development candidate is v3.05 / source commit `c9204fe4`. The compatibility record
must confirm each downloaded artifact before a user release is advertised. For the GitHub
candidate, use `UMA3_f5.unitypackage`, not its source archives or `UMA2_For_UMA3` package.
Ordinary package users do not need a UMA source checkout or the sync script.

Also obtain `VertexForm3D-GHA-UMA-<version>.unitypackage` from the integration releases location
(**not published yet**). This contains our customization UI, catalog and installer, not UMA itself.

There are two GHA-owned package deliverables: **GHA Host** and **GHA UMA Integration**. UMA
itself is a separate third-party installation, not included in either export. Both GHA packages
must pass clean-install tests; approval of the implementation plan is not release acceptance.

**Until upstream Vertex Form3D includes the host layer**, the planned additional download is
`VertexForm3D-GHA-Host-<version>.unitypackage`, imported before the UMA provider package and
followed by **Tools > GHA > Integration > Install GHA Host Layer**. The current transitional
installer uses `git apply`, requiring Git and matching core scripts for VertexForm 1.1.9. The
final patch scope and exact supported revision still require the Phase 2 audit and Phase 3 tests.
An approved release using this verified host-package route need not wait for upstream acceptance.

## 2. Import the packages, in this order

Import UMA through the selected source's documented Unity import flow. For downloaded
`.unitypackage` files, use **Assets > Import Package > Custom Package**. Import the required
default content specified for the accepted artifact and wait for compilation between packages.

1. If GHA is not included in the supported VertexForm release, import the matching GHA Host
   package and run **Install GHA Host Layer**. Skip this only when the host is already integrated.
2. The supported UMA installation from your selected source. Expect a large import.
3. `VertexForm3D-GHA-UMA-<version>.unitypackage`

## 3. Install the UMA provider

1. Close any open **UMA > Global Library** window.
2. Run **Tools > GHA > Integration > Install UMA Provider Layer**.
3. Wait for compilation and for `GHA UMA provider assets installed` in the Console.

Planned release behavior: preflight verifies supported VertexForm/GHA host and UMA versions plus
required installed content before changing the project. All three sources use this same validation.
These full checks are not implemented yet; see the setup contract and implementation plan.

The installer adds the UMA provider to both player prefabs and Avatar Studio, then creates and
fills the UMA Global Library from the default UMA content at
`Assets/UMAProjectData/Resources/AssetIndexerProject.asset`. Keep the Editor in the foreground
until it finishes. Existing indexes are preserved. A separate editable project catalog is planned;
the current installer still uses the package catalog. No manual library rebuild is needed for a
fresh installation.

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

Run **Tools > GHA > Integration > Uninstall UMA Provider Layer**. Unchanged targets are restored
from install snapshots; edited targets have only the installer's known contributions removed.
UMA content and the generated library remain; removing them is a separate, backed-up operation.
The host remains installed, either from upstream or the temporary host package. Classic must
continue working without UMA; proving that behavior is a required pre-release gate, not an
accepted result for the current combined development project.

## Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| No **Tools > GHA** menu | Your Vertex Form3D version predates the GHA host layer. Update Vertex Form3D, or use the temporary host package described in step 1. |
| `A compatible UMA 3 installation was not found under Assets/UMA` | Check that the supported UMA distribution and its required content were imported successfully. |
| `Install the GHA host layer before installing the UMA provider layer` | Only with the temporary host package: run **Install GHA Host Layer** first. |
| `Cannot install the GHA host source patch cleanly` | Only with the temporary host package: the core scripts differ from its supported baseline. Preserve your changes and report the conflict before continuing. |
| An installer fails partway | Fix the reported error, then rerun the **same** Install menu. Do not uninstall first. |
| Pink materials on avatars | Shader graphs failed to import. See the known limitations in the [developer guide](GHA-developer-getting-started.md#known-limitations-and-verification-status). |
| Avatars frozen or the Editor pauses when the avatar loads | UMA generation blocks the main thread on first load. This is a known limitation, not an installation error. |

## Work still required

The September 23 [implementation plan](GHA-IMPLEMENTATION-PLAN.md) supersedes the ordering below.
Complete host contracts, source compatibility and installation ownership before separating the
projects. Asset Store, GitHub release and local-source validation each need their own evidence.
The list below retains the packaging deliverables; no route is accepted merely by documenting it.

For maintainers: complete the deliverables applicable to the supported installation route before
publishing this guide as tested. Upstream acceptance is not required for a release using a verified
temporary host package.
The generic host is the upstream contribution. The designated VertexForm integration working
copy produces the UMA integration package and maintains the local-source developer scripts.

1. **Contribute the host layer upstream.** The five core-script seams (as real edits, not a
   patch), `com.vertexform3d.gha`, the `Integration/VertexForm` adapters, the Avatar Studio
   shared panel prefab/chrome, and provider-neutral prefab/Home wiring. UMA-specific category
   icons, swatches and wiring stay with the integration.
   Upstream ships these pre-wired, so the host installer and the `VERTEXFORM_GHA_HOST` define
   go away for users. Evidence maintainers will want: a stock-only runtime pass showing
   Classic avatars, networking and seating unchanged with no provider installed; zero UMA
   references in the contribution; a reviewed dependency delta against the actual upstream
   baseline; and a reviewable host-only diff. Until this lands in the supported upstream release,
   the host package is a temporary third download.
2. **Move both GHA packages under `Assets`.** `com.vertexform3d.gha` and
   `com.vertexform3d.gha.uma` currently sit under `Packages/` as embedded UPM packages, which a
   `.unitypackage` cannot carry, and Vertex Form3D's own Package Updater delivers
   `.unitypackage` files, so even the upstream host layer must live under `Assets` to reach
   users who update that way. Move both folders, with their asmdefs, under
   `Assets/VertexForm3D/3rdPartyAssets/GHA` through the Unity Editor so `.meta` files travel
   with them; audit all hardcoded `Packages/com.vertexform3d.gha*/...` paths, including
   `GhaUmaAssetInstaller.cs` and `Tools/*.cs`; reconcile the embedded records in
   `packages-lock.json`. Do this before the upstream PR so upstream reviews the final layout.
3. **Add an exporter for the UMA package** (and, temporarily, the host package): an Editor
   script with a fixed path list per deliverable. The export must never include the core
   scripts, the installer-modified prefabs, `Assets/UMA` or the generated index.
4. **Test the Asset Store and GitHub UMA packages separately.** The exact Store artifact and
   `UMA3_f5.unitypackage` are not accepted yet. For each, check that it
   imports into `Assets/UMA`, whether it includes `Assets/SourceShaders`, and whether the 12
   shader graphs that need repair in the Git checkout also fail in the release package. If they
   do, evaluate an explicit version-scoped repair that users can apply without a source checkout.
   Do not reuse the developer overlay automatically; an artifact remains unsupported until its
   repair and installation are verified. Maintain the existing local-source scripts throughout.
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
   merging upstream Vertex Form3D, rebuild it against the reviewed stock scripts and record
   the supported version only after validation.

## Related

- [Developer getting-started guide](GHA-developer-getting-started.md) — source-clone setup,
  full test checklist, headset and multiplayer follow-up, known limitations.
- [Architecture](Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md) and
  [package boundaries](Packages/com.vertexform3d.gha/Documentation~/PACKAGE-BOUNDARIES.md) —
  the three-layer design and what each layer may reference.
- [UMA setup contract](Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md) — body-type
  authoring rules for maintainers.
