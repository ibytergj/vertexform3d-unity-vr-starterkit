# Getting started: testing the GHA / UMA integration

For Vertex Form3D maintainers reviewing the `Generic-Humanoid-Avatars` branch.

**Review instructions — September 7, 2026. Full clean-install acceptance is pending.**
Follow the steps below from a fresh clone of the review branch. Record the downloaded commit
IDs with your results; this test is intended to verify the complete setup end to end.

## What you are testing

GHA adds a provider-neutral humanoid avatar layer to Vertex Form3D. Its optional UMA provider
adds customizable full-body avatars alongside Classic avatars, through **Change Avatar**.

Start with the Windows Editor and Home avatar studio. Test headset behavior and multiplayer
after that succeeds. This is a functional review, not a claim of production-ready VR performance:
UMA generation can still cause substantial main-thread pauses.

## 1. Prerequisites

- Windows, Git on `PATH`, and PowerShell. The installer uses `robocopy.exe`, which is
  included with Windows; no separate download is needed. macOS/Linux installation has not
  been validated.
- **Free disk space:** plan for at least **25 GB free before starting; 30 GB is recommended**
  for both repositories, the UMA copy installed into GHA, Unity's generated cache, and
  temporary setup files. Unity Editor/module installations and build outputs need additional space.
- Unity **6000.3.11f1**, matching `ProjectSettings/ProjectVersion.txt`. Start with the Windows
  target. Android support is needed only for a separate Quest standalone build test.
- Let Unity resolve the checked-in packages. This project uses **URP**, not HDRP. Do not upgrade
  the pipeline or replace the package manifest during the first review.
- A Photon Fusion test application for networked testing; Photon Voice configuration if testing
  voice. Use your own or privately supplied test configuration, not production credentials.
- For the headset pass: a working PC/headset Link connection and appropriate OpenXR runtime.
  The desktop pass does not require a headset.

Disk-space estimates measured on September 7, 2026 (rounded, including Git history):

| Workspace content | Approximate space |
| --- | --- |
| GHA repository clone, before UMA installation or Unity import | 2.3 GB |
| Full UMA source repository clone, including all-branch history | 6.1 GB |
| Both clones combined | 8.5 GB |
| Both repositories after UMA installation and GHA's Unity import | 15 GB |

These are planning estimates, not fixed limits. Cache size varies by platform and packages;
updates and retained replacement backups can increase usage. The 25–30 GB allowance provides
headroom above the measured workspace. The separate UMA source project does not need to be
opened in Unity, so do not generate a second Unity cache there for this test.

An AI agent, AnkleBreaker connection, or Unity CLI session is not required for the manual Editor
steps. Keep the project's package records intact.

## 2. Obtain the repositories

Use a short, writable parent directory. Run these commands there in PowerShell; both destination
folders must be new. Do not open either project in Unity yet.

```powershell
git clone --branch Generic-Humanoid-Avatars https://github.com/ibytergj/vertexform3d-unity-vr-starterkit.git GHA-Review
git clone --branch master --no-single-branch https://github.com/umasteeringgroup/UMA.git UMA
```

### Verify the downloads before installing anything

These checks identify exactly which code you downloaded, confirm that UMA has no local edits,
and check that the temporary shader repair is available. They do not change either repository.
A commit ID is Git's identifier for an exact saved revision; copy both IDs into your test report
so the maintainer can reproduce your setup.

```powershell
git -C GHA-Review rev-parse HEAD
git -C UMA rev-parse HEAD
git -C UMA status --short
git -C UMA cat-file -e f4edf41ba1017b4a745fd1ba7286824332652b7d
if ($LASTEXITCODE -ne 0) { throw 'Required UMA shader-repair commit is missing. Stop and contact the integration maintainer.' }
Write-Host 'Required UMA shader-repair commit is available.'
```

| Check | Expected output and what to do |
| --- | --- |
| `git -C GHA-Review rev-parse HEAD` | One commit ID. Record it and compare it with the GHA revision supplied for this review. If they differ, confirm the intended revision with the maintainer before continuing. |
| `git -C UMA rev-parse HEAD` | Exactly `c9204fe475b334162617da5ca923afcc6050e01e`. Record it. Any other value needs a reviewed pin update before continuing. |
| `git -C UMA status --short` | No output: the UMA working tree is clean. Any listed files mean local changes or untracked files; stop and report them rather than deleting or resetting them. |
| `git -C UMA cat-file -e ...` | The Git command itself is silent on success. The following PowerShell check prints `Required UMA shader-repair commit is available.` If Git reports an error, the check stops with an explanation. |

The GHA branch includes the integration code, installers and this guide. You do not need to
copy additional GHA patches or make a local commit before testing. UMA itself is installed
separately in step 3.

Continue only when all four checks meet the expectations above. Use a full clone, not a shallow clone.

**Temporary workaround:** `--no-single-branch` downloads history for all branches so the
installer can access the reviewed shader fixes on `develop`, which are not in the supported
`master` baseline yet. Only `master` is checked out; the installer copies the selected fixes
into GHA without switching or modifying the source repository. Once those fixes are included
in a reviewed, supported `master` revision and the installer pin is updated, we expect to need
only a master-branch clone (`--branch master --single-branch`). The all-branch download is
not intended to be a permanent setup requirement.

If master has advanced beyond this baseline, stop and arrange a reviewed pin update with the
integration maintainer. Do not reset an existing repository, change the expected hash to bypass
validation, or check out develop. Source master must remain verbatim upstream content.

## 3. Install UMA into GHA

UMA is installed separately and intentionally excluded from this public fork. The integration
code alone is not a complete UMA installation. Keep the **GHA Unity Editor closed**.

### First, preview the installation (nothing is copied)

From the parent folder containing `GHA-Review` and `UMA` (for example, `E:\Test`), run:

```powershell
Set-Location GHA-Review
./Tools/Sync-Uma.ps1 -UmaRepositoryPath ../UMA -WhatIf
```

If your prompt already ends in `GHA-Review`, skip `Set-Location GHA-Review`.
`../UMA` means the separate UMA folder beside GHA-Review. `-WhatIf` means **show the plan
without installing anything**.

The expected message starts with **`What if: Performing the operation "Install UMA master commit ..."`**.
This is a preview, not an error or a question you need to answer. In plain language it says:

- **From `...\UMA`:** use the default UMA files in the source repository you downloaded.
- **Into `...\GHA-Review\Assets\UMA` and `...\Assets\SourceShaders`:** copy the required
  UMA assets and shader source files into the GHA test project.
- **Apply 12 shader repairs:** include the temporary upstream fixes needed by this supported
  UMA version. The long letters-and-numbers strings identify the exact source revisions.
- **Source unchanged:** leave your separate UMA repository untouched; only the GHA copy
  will receive the installed files and shader fixes.

Check that the source and destination paths refer to **your test folders**, not your development
project. If they are correct and no error was reported, the preview check has passed.
**UMA is not installed yet.** If a path is wrong or an error appears, stop before the next command.

### Then, perform the installation

From `GHA-Review`, run the same command **without `-WhatIf`**:

```powershell
./Tools/Sync-Uma.ps1 -UmaRepositoryPath ../UMA
```

This time the script asks for confirmation and offers **Yes** and **Yes to All**, among other options.
**Type `Y` (Yes), then press Enter. Do not choose `A` (Yes to All); it is not needed.**
Yes approves the **entire UMA installation shown in the prompt**, not just one file. The script
has one installation confirmation, so you will not have to approve each copied file.
Yes to All would suppress further confirmation prompts for this command; ordinary Yes is sufficient.

Wait for the installation to finish. Look for `Installed UMA base commit: c9204fe475b334162617da5ca923afcc6050e01e`
and the installed file counts. Those are the installation results, unlike the earlier preview.
If the script reports an error, stop and include it in your test report. Otherwise continue to
**step 4**, where Unity's provider installer builds the UMA asset library automatically.

For a fresh clone, do not
add `-ReplaceExisting`. For a deliberate replacement, use that switch in both commands; the
previous installation is retained under `Temp/UMA-Backup-*`. Preserve the backup until the
replacement is accepted. Close Unity if a lockfile blocks installation; do not delete a live lock.

The script copies the reviewed UMA and SourceShaders content, excludes selected sample
content, and exports 12 exact shader files from official develop `f4edf41ba` into **GHA only**.
That repair applies only to master baseline `c9204fe4`. Git blob verification preserves upstream
contents without manual graph edits, source `.gitattributes` changes or source commits.
The script does not fetch, pull, switch or modify the source repository.

## 4. Open GHA and install the integration

Open **GHA-Review**, not the separate UMA source project, in Unity 6000.3.11f1. Allow package
resolution, asset import and C# compilation to finish. Do not enter Play Mode during setup.

**UMA library reminder:** copying the files in step 3 does not finish Unity setup. For this
fresh installation, run the two integration installers below; **Install UMA Provider Layer**
automatically creates and builds the Global Library from the default UMA content.
Do not run a manual library rebuild first. Wait for the installer to finish, then check the
library in step 5 before entering Play Mode.

### Run the integration tools

The branch already includes integration source, package assets and prefab wiring. After importing
UMA, use the provided installers in this order to establish local setup state:

1. Close any open **UMA > Global Library** window, then run
   **Tools > GHA > Integration > Install GHA Host Layer**.
2. Wait for compilation/domain reload and the `GHA host assets installed` success message.
3. **Tools > GHA > Integration > Install UMA Provider Layer**.
4. Wait for compilation/domain reload and the `GHA UMA provider assets installed` success message.

Keep the Editor in the foreground while compilation and domain reload finish. If either installer
reports an error or patch conflict, stop before the next step and report the first relevant error.

The installers configure the Home avatar, player prefabs and Avatar Studio, including their catalog,
animation and slider references. The starting body type is Human Male; Human Female is also
available. No manual assignment of race, T-pose or base recipe is needed. An existing saved
avatar choice is preserved.

For this fresh setup, the provider installer creates the library at
`Assets/UMAProjectData/Resources/AssetIndexerProject.asset` using the supplied default UMA assets.
Wait for indexing to finish before continuing. No custom assets or manual library rebuild are
required. For existing-install upgrades, see the
[maintainer setup contract](Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md).

The symbols are `VERTEXFORM_GHA_HOST` and `VERTEXFORM_GHA_UMA`. Do **not** add `UMA_INSTALLED`:
it currently enables an incompatible UMA 2 tooling bridge.

## 5. Check the default UMA content and catalog

Before entering Play Mode, open **UMA > Global Library** and verify `Human Male 3.0`,
`Human Female 3.0`, their base recipes and wardrobe content resolve. These are included
with the supported UMA installation; testers do not need to create or supply assets.

Do not use the deletion or Addressables-generation operations for this test. If the index is
missing, references are unresolved or compilation fails, stop and report the first relevant error.
See known limitations below for the outstanding upstream shader issue.

Inspect `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset`. Enable stock and
UMA avatars for the mixed-provider review. The catalog contains both human body types.
Never reorder race, wardrobe, DNA or palette entries: their indices are saved/network IDs.

## 6. Configure a desktop test

1. Open **Vertex Form > Platform Selection** and select **Desktop**.
2. In **File > Build Profiles**, use Windows. For a flat-screen test, ensure Windows XR is not
   starting a headset session. Record local XR changes so you can restore them for the VR pass.
3. Open **Tools > Fusion > Realtime Settings** and configure the agreed test Fusion application.
   Use matching test App Version, region and session settings across clients. Use a distinct
   App Version to keep review sessions separate from ordinary users. Configure Voice if testing it.
4. Keep credentials local. Never commit `PhotonAppSettings.asset`, exported configuration JSON,
   tokens or private service URLs with a report.
5. Open `Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes/LoginScene.unity`.
   Keep LoginScene followed by HomeScene in the build scene list; login loads the next scene.

Start Play Mode from **LoginScene**, use its connection action, then open **Change Avatar** in
Home. Do not start directly in Home and treat missing initialization as a UMA failure.
Test Home first: remote Addressables/world-download configuration is a separate prerequisite for
other worlds and should not obscure the initial avatar check.

## 7. Desktop smoke-test checklist

Record each result separately, including the first cold load and a repeated load.

| Test | Expected result |
| --- | --- |
| Open Change Avatar | Avatar Studio has Classic and Custom provider tabs. |
| Classic navigation | Previous/Next change the preview; Save Avatar uses shared button placement. |
| Custom categories | Body, Face, Colors and Outfits have persistent selection and distinct hover feedback. |
| Outfit subsections | Chest, Feet, Hair and Legs indicate the selected subsection. |
| Body type | Under Body, switch Male → Female → Male; the model and compatible outfit options change. |
| Remembered choices | Switching back restores that type's in-session outfit choices, including None. |
| Shape and color | Height, another body/face slider, skin, hair and eye controls update the preview. |
| Save/reopen | Save, close/reopen the studio, then restart from LoginScene; the saved type and appearance return. |
| Desktop animation | The player idles/moves through animation; hands are not held forward by VR tracking. |
| Diagnostics | No new unexpected errors; report missing assets, pink materials and exceptions. |

Saving persists the active type's recipe, not a separate permanent wardrobe for every type.
Per-type outfit recall applies within the current customizer session.

## 8. Headset and multiplayer follow-up

Stop Play Mode before changing platform/XR settings. For PC Link, select **VR** in Vertex Form
while keeping the **Windows** target. Enable Windows OpenXR and **Initialize XR on Startup**
under Project Settings > XR Plug-in Management. Establish Link and the appropriate active
OpenXR runtime before restarting from LoginScene. This tests PC VR, not standalone Quest Android.

- Confirm head tracking does not drag the environment with it.
- Confirm arms/hands follow tracking, including after switching body type and saving.
- Check local head hiding and full-body visibility from another player's view.
- Check avatar height/feet, seating, scene transitions and tracking-loss recovery.
- Repeat a desktop pass after leaving VR: tracking must no longer override idle arms.
- For two clients, use identical GHA revisions/catalogs, test Photon configuration and world
  content. Check appearance, clothing, movement and late-join reconstruction.

Quest standalone, WebGL/WebGPU/WebXR and two-client behavior need separate build/device reports.
Do not infer their results from an Editor or PC Link pass.

## Known limitations and verification status

- **Acceptance:** the current fresh-clone review is in progress; the complete setup and
  smoke-test checklist have not yet been accepted end to end.
- **Loading stalls:** UMA generation still blocks the main thread. A loading label does not prove
  smooth headset frames; report timings and visible freezes.
- **Body types:** compilation, isolated selector/state/recipe tests and dependency checks passed.
  Actual male/female generation, save/reopen and VR acceptance remain pending.
- **Existing desktop/VR:** the owner reported desktop arms fixed and VR reasonably working in the
  development copy. This is not full regression or clean-install acceptance.
- **Shaders:** the 12 repaired graphs imported successfully in the development copy.
  `Assets/UMA/SRP/ShaderGraphs/Materials/UMA_SG_Diffuse.shadergraph` remains malformed in the
  reviewed upstream revisions and is outside the repairs. Record that exact error separately;
  do not dismiss other shader errors or pink active materials as the same issue.
- **Appearance:** some outfit thumbnails differ from materials, and lighting may wash out colors.
  These adjustments are deferred; include screenshots in feedback.

## Reporting results

Include GHA and UMA commit IDs, Unity version, OS, desktop/Link/standalone mode, headset/runtime
if applicable, and whether this is a fresh clone or existing copy. Add reproduction steps,
expected/actual behavior, selected type/outfit and screenshots.

For stalls, include Console entries marked `[GHA LOAD TIMING]`, cold/repeated-load results and
a Unity Profiler capture if available. For errors, include the first relevant exception and stack
trace. Redact credentials and unrelated information from logs before sharing.

## Maintainer references

- [UMA setup and body-type authoring contract](Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md)
- [GHA package overview](Packages/com.vertexform3d.gha/README.md)
- [UMA adapter overview](Packages/com.vertexform3d.gha.uma/README.md)
- [Migration status and acceptance matrix](GHA-MIGRATION-RUNBOOK.md)
- [Official UMA race guide](https://github.com/umasteeringgroup/UMA/blob/master/UMAProject/Assets/UMA/Docs/CreatingANewRace.md)
